using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace AcousticVoid.Downloader;

public enum FileState
{
    Downloading,
    Extracting,
    Completed,
    Failed,
}

public sealed record FileProgress(
    string Key,
    string Name,
    FileState State,
    long Received = 0,
    long Total = -1,
    string? Error = null);

public sealed record DownloadResult(bool Ok, string Name, long Bytes, string? Error = null);

/// <summary>
/// Port of the PowerShell download engine: unique file reservation, 4-way ranged
/// transfer for files >= 8 MB, three attempts, zip extraction with traversal guard.
/// </summary>
public sealed class DownloadEngine : IDisposable
{
    private const long SegmentThreshold = 8L * 1024 * 1024;
    private const int SegmentCount = 4;
    private const int CopyBuffer = 1024 * 1024;

    private readonly HttpClient _http;

    public DownloadEngine()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            EnableMultipleHttp2Connections = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            ConnectTimeout = TimeSpan.FromSeconds(15),
            MaxConnectionsPerServer = 64,
            AllowAutoRedirect = true,
        };

        _http = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(10) };
        _http.DefaultRequestVersion = HttpVersion.Version20;
        _http.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("acousticvoid-downloader/1.0");
    }

    public static string MakeKey(string group, string url) => group + "\n" + url;

    public async Task WarmUpAsync(IEnumerable<string> urls)
    {
        var hosts = urls
            .Select(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.GetLeftPart(UriPartial.Authority) : null)
            .Where(host => host != null)
            .Select(host => host!)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        var warmups = hosts.Select(async host =>
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, host);
                using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(false);
            }
            catch
            {
                // Warm-up is opportunistic; real failures surface during the download.
            }
        });

        await Task.WhenAll(warmups).ConfigureAwait(false);
    }

    public async Task<DownloadResult> DownloadAsync(
        string group,
        string url,
        string directory,
        IProgress<FileProgress>? progress,
        CancellationToken cancellationToken)
    {
        string key = MakeKey(group, url);
        string name = GuessName(url);

        for (int attempt = 1; attempt <= 3; attempt++)
        {
            string? destination = null;
            string? extractDirectory = null;
            bool isZip = false;

            try
            {
                progress?.Report(new FileProgress(key, name, FileState.Downloading));

                using var response = await _http
                    .GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                name = ResolveName(url, response);
                if (name.Equals("ToolsDownloader++", StringComparison.OrdinalIgnoreCase))
                {
                    name += ".exe";
                }
                isZip = name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);

                string baseName = Path.GetFileNameWithoutExtension(name);
                string extension = Path.GetExtension(name);
                long declaredLength = response.Content.Headers.ContentLength ?? -1;
                (destination, extractDirectory) = Reserve(directory, baseName, extension, isZip);

                progress?.Report(new FileProgress(key, name, FileState.Downloading, 0, declaredLength));

                bool useRanges = attempt == 1
                                 && declaredLength >= SegmentThreshold
                                 && response.Content.Headers.ContentEncoding.Count == 0
                                 && SupportsRanges(response);

                if (useRanges)
                {
                    // Release the header-only response before firing the segment requests.
                    response.Dispose();
                    await SegmentedAsync(url, destination, declaredLength, progress, key, name, cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await using var file = new FileStream(destination, FileMode.Open, FileAccess.Write,
                        FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
                    file.SetLength(0);
                    await using var source = await response.Content.ReadAsStreamAsync(cancellationToken)
                        .ConfigureAwait(false);
                    await CopyWithProgressAsync(source, file, declaredLength, progress, key, name, cancellationToken)
                        .ConfigureAwait(false);
                    file.SetLength(file.Position);
                    if (declaredLength >= 0 && file.Length != declaredLength)
                    {
                        throw new IOException($"size mismatch ({file.Length} / {declaredLength})");
                    }
                }

                long finalLength = declaredLength >= 0
                    ? declaredLength
                    : new FileInfo(destination).Length;

                if (isZip)
                {
                    progress?.Report(new FileProgress(key, name, FileState.Extracting, finalLength, finalLength));
                    await Task.Run(() => ExtractZip(destination!, extractDirectory!), cancellationToken)
                        .ConfigureAwait(false);
                    File.Delete(destination!);
                    destination = null;
                }

                progress?.Report(new FileProgress(key, name, FileState.Completed, finalLength, finalLength));
                return new DownloadResult(true, name, finalLength);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Cleanup(destination, isZip, extractDirectory);
                progress?.Report(new FileProgress(key, name, FileState.Failed, 0, -1, "cancelled"));
                throw;
            }
            catch (Exception ex)
            {
                Cleanup(destination, isZip, extractDirectory);
                if (attempt == 3)
                {
                    progress?.Report(new FileProgress(key, name, FileState.Failed, 0, -1, ex.Message));
                    return new DownloadResult(false, name, -1, ex.Message);
                }

                await Task.Delay(400 * attempt, cancellationToken).ConfigureAwait(false);
            }
        }

        progress?.Report(new FileProgress(key, name, FileState.Failed, 0, -1, "download failed"));
        return new DownloadResult(false, name, -1, "download failed");
    }

    private async Task CopyWithProgressAsync(
        Stream source,
        FileStream destination,
        long total,
        IProgress<FileProgress>? progress,
        string key,
        string name,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[CopyBuffer];
        long received = 0;
        int read;

        while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                   .ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            received += read;
            progress?.Report(new FileProgress(key, name, FileState.Downloading, received, total));
        }
    }

    private async Task SegmentedAsync(
        string url,
        string path,
        long length,
        IProgress<FileProgress>? progress,
        string key,
        string name,
        CancellationToken cancellationToken)
    {
        using (var preallocate = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite))
        {
            preallocate.SetLength(length);
        }

        var received = new long[1];
        long chunk = (length + SegmentCount - 1) / SegmentCount;
        var segments = new List<Task>(SegmentCount);

        for (int i = 0; i < SegmentCount; i++)
        {
            long from = i * chunk;
            long to = Math.Min(length, from + chunk) - 1;
            if (from > to) break;

            segments.Add(SegmentAsync(url, path, from, to, written =>
            {
                long total = Interlocked.Add(ref received[0], written);
                progress?.Report(new FileProgress(key, name, FileState.Downloading, total, length));
            }, cancellationToken));
        }

        await Task.WhenAll(segments).ConfigureAwait(false);

        var info = new FileInfo(path);
        if (info.Length != length)
        {
            throw new IOException($"size mismatch ({info.Length} / {length})");
        }
    }

    private async Task SegmentAsync(
        string url,
        string path,
        long from,
        long to,
        Action<long> onWritten,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new RangeHeaderValue(from, to);

        using var response = await _http
            .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode != HttpStatusCode.PartialContent)
        {
            throw new IOException("server did not honour the range request");
        }

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.ReadWrite,
            4096, FileOptions.Asynchronous);

        destination.Seek(from, SeekOrigin.Begin);

        var buffer = new byte[CopyBuffer];
        long written = 0;
        int read;

        while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                   .ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            written += read;
            onWritten(read);
        }

        if (written != to - from + 1)
        {
            throw new IOException("range response was truncated");
        }
    }

    private static (string File, string? ExtractDirectory) Reserve(
        string directory,
        string baseName,
        string extension,
        bool isZip)
    {
        for (int suffixIndex = 1; suffixIndex <= 1000; suffixIndex++)
        {
            string suffix = suffixIndex == 1 ? string.Empty : "_" + suffixIndex;
            string file = Path.Combine(directory, baseName + suffix + extension);
            string extract = Path.Combine(directory, baseName + suffix);

            if (isZip && Directory.Exists(extract)) continue;

            try
            {
                using var reserved = new FileStream(file, FileMode.CreateNew, FileAccess.Write,
                    FileShare.ReadWrite, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
                return (file, isZip ? extract : null);
            }
            catch (IOException) when (File.Exists(file))
            {
                // Name is taken; try the next suffix.
            }
        }

        throw new IOException("cannot allocate a unique destination name");
    }

    public static string GuessName(string url)
    {
        if (Regex.IsMatch(url, @"/ToolsDownloader\+\+$")) return "ToolsDownloader++.exe";

        try
        {
            var uri = new Uri(url);
            string candidate = Uri.UnescapeDataString(Path.GetFileName(uri.AbsolutePath));
            return string.IsNullOrWhiteSpace(candidate) ? "download.bin" : Sanitize(candidate);
        }
        catch
        {
            return "download.bin";
        }
    }

    private static string ResolveName(string url, HttpResponseMessage response)
    {
        var disposition = response.Content.Headers.ContentDisposition;
        if (!string.IsNullOrWhiteSpace(disposition?.FileNameStar))
        {
            return CleanName(disposition.FileNameStar);
        }
        if (!string.IsNullOrWhiteSpace(disposition?.FileName))
        {
            return CleanName(disposition.FileName);
        }

        if (Regex.IsMatch(url, @"/ToolsDownloader\+\+$")) return "ToolsDownloader++.exe";

        var finalUri = response.RequestMessage?.RequestUri;
        if (finalUri != null)
        {
            string candidate = Uri.UnescapeDataString(Path.GetFileName(finalUri.AbsolutePath));
            if (!string.IsNullOrWhiteSpace(candidate) && Path.HasExtension(candidate))
            {
                return Sanitize(candidate);
            }
        }

        return GuessName(url);
    }

    private static string CleanName(string name)
    {
        name = name.Trim().Trim('"');
        try
        {
            name = Uri.UnescapeDataString(name);
        }
        catch
        {
            // Leave the original name if it contains invalid escape sequences.
        }

        return Sanitize(name);
    }

    private static string Sanitize(string name)
    {
        name = Path.GetFileName(name.Trim().Trim('"'));
        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }
        return string.IsNullOrWhiteSpace(name) ? "download.bin" : name;
    }

    private static bool SupportsRanges(HttpResponseMessage response)
    {
        foreach (string value in response.Headers.AcceptRanges)
        {
            if (string.Equals(value, "bytes", StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static void ExtractZip(string archivePath, string directory)
    {
        try
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            ZipFile.ExtractToDirectory(archivePath, directory, overwriteFiles: false);
            return;
        }
        catch
        {
            // Fall through to entry-by-entry extraction for archives the bulk API rejects.
        }

        if (Directory.Exists(directory)) Directory.Delete(directory, true);
        Directory.CreateDirectory(directory);

        string root = Path.GetFullPath(directory + Path.DirectorySeparatorChar);
        using var archive = ZipFile.OpenRead(archivePath);

        foreach (var entry in archive.Entries)
        {
            string target = Path.GetFullPath(Path.Combine(directory, entry.FullName));
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) continue;

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(target);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    private static void Cleanup(string? file, bool isZip, string? extractDirectory)
    {
        try
        {
            if (file != null && File.Exists(file)) File.Delete(file);
        }
        catch
        {
            // Best effort; a leftover file is harmless.
        }

        try
        {
            if (isZip && extractDirectory != null && Directory.Exists(extractDirectory))
            {
                Directory.Delete(extractDirectory, true);
            }
        }
        catch
        {
            // Best effort.
        }
    }

    public void Dispose() => _http.Dispose();
}
