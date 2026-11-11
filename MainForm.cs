using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace AcousticVoid.Downloader;

public sealed class MainForm : Form
{
    private const int Parallel = 6;

    private readonly DownloadEngine _engine = new();
    private readonly HashSet<string> _finished = new();
    private readonly HashSet<string> _failed = new();
    private readonly List<string> _failureNames = new();
    private readonly Label _folderLabel = new();
    private readonly MonoCheck _exclusion = new();
    private readonly LinkLabel _changeFolder = new();
    private readonly Button _download = Theme.MakePrimaryButton("DOWNLOAD ALL TOOLS", 360, 56);

    private CancellationTokenSource? _cts;
    private string? _customFolder;
    private string _runFolder = string.Empty;
    private int _total;
    private int _done;
    private int _completed;
    private bool _running;
    private bool _ignoreProgress;
    private bool _closeAfterRun;

    public MainForm()
    {
        Text = "acousticvoid downloader";
        BackColor = Theme.Ink;
        ForeColor = Theme.White;
        Font = Theme.Ui9;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Size = new Size(700, 500);
        AutoScaleMode = AutoScaleMode.Dpi;
        DoubleBuffered = true;

        try
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch
        {
            // Falls back to the stock WinForms icon.
        }

        BuildUi();
        LoadSettings();
        WireEvents();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int dark = 1;
            if (DwmSetWindowAttribute(Handle, 20, ref dark, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(Handle, 19, ref dark, sizeof(int));
            }
        }
        catch
        {
            // Older Windows builds ignore the immersive dark-mode attribute.
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private void BuildUi()
    {
        var hero = new HeroPanel();

        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 9,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0),
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 8));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        var brand = new BrandMark
        {
            Size = new Size(160, 160),
            Anchor = AnchorStyles.None,
        };
        stack.Controls.Add(brand, 0, 1);

        var title = Theme.MakeLabel("ACOUSTICVOID DOWNLOADER", Theme.Title, Theme.White);
        title.AutoSize = false;
        title.Size = new Size(640, 46);
        title.TextAlign = ContentAlignment.MiddleCenter;
        title.Anchor = AnchorStyles.None;
        stack.Controls.Add(title, 0, 3);

        _download.Anchor = AnchorStyles.None;
        _download.Click += (_, _) => OnDownloadClicked();
        stack.Controls.Add(_download, 0, 5);

        var meta = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0),
            Padding = new Padding(0),
            Anchor = AnchorStyles.None,
        };

        var outputTag = Theme.MakeLabel("OUTPUT", Theme.Ui8Bold, Theme.Dim);
        outputTag.Margin = new Padding(0, 4, 10, 0);
        meta.Controls.Add(outputTag);

        _folderLabel.AutoSize = true;
        _folderLabel.AutoEllipsis = true;
        _folderLabel.MaximumSize = new Size(380, 0);
        _folderLabel.Font = Theme.Ui9;
        _folderLabel.ForeColor = Theme.Muted;
        _folderLabel.BackColor = Color.Transparent;
        _folderLabel.Margin = new Padding(0, 2, 8, 0);
        meta.Controls.Add(_folderLabel);

        _changeFolder.Text = "change";
        _changeFolder.AutoSize = true;
        _changeFolder.Font = Theme.Ui9;
        _changeFolder.LinkColor = Theme.White;
        _changeFolder.ActiveLinkColor = Theme.White;
        _changeFolder.VisitedLinkColor = Theme.White;
        _changeFolder.LinkBehavior = LinkBehavior.HoverUnderline;
        _changeFolder.BackColor = Color.Transparent;
        _changeFolder.Margin = new Padding(0, 2, 0, 0);
        _changeFolder.Cursor = Cursors.Hand;
        meta.Controls.Add(_changeFolder);

        stack.Controls.Add(meta, 0, 6);

        _exclusion.Text = "Add a Windows Defender exclusion for this folder";
        _exclusion.AutoSize = true;
        _exclusion.Font = Theme.Ui9;
        _exclusion.ForeColor = Theme.Muted;
        _exclusion.BackColor = Color.Transparent;
        _exclusion.Checked = true;
        _exclusion.Anchor = AnchorStyles.None;
        stack.Controls.Add(_exclusion, 0, 7);

        hero.Controls.Add(stack);
        Controls.Add(hero);
    }

    private void WireEvents()
    {
        _changeFolder.LinkClicked += (_, _) => ChooseFolder();
        _exclusion.CheckedChanged += (_, _) => SaveSettings();
        FormClosing += MainForm_FormClosing;
        FormClosed += (_, _) =>
        {
            _cts?.Cancel();
            _engine.Dispose();
        };
    }

    private async void OnDownloadClicked()
    {
        if (_running) return;
        await RunAllAsync();
    }

    private async Task RunAllAsync()
    {
        if (_running) return;

        string folder = _customFolder ?? NextSsFolder();
        try
        {
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Cannot create the output folder:\n\n{ex.Message}",
                "acousticvoid downloader", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _runFolder = folder;
        _folderLabel.Text = folder;
        _finished.Clear();
        _failed.Clear();
        _failureNames.Clear();
        _total = Catalog.Groups.Sum(group => group.Urls.Length);
        _done = 0;
        _completed = 0;
        _ignoreProgress = false;

        _cts = new CancellationTokenSource();
        var cancellationToken = _cts.Token;
        var progress = new Progress<FileProgress>(OnProgress);

        SetRunning(true);

        try
        {
            if (_exclusion.Checked)
            {
                await AddExclusionAsync(folder).ConfigureAwait(true);
            }

            for (int i = 0; i < Catalog.Groups.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var group = Catalog.Groups[i];
                string groupDirectory = Path.Combine(folder, group.Name);
                Directory.CreateDirectory(groupDirectory);

                using var gate = new SemaphoreSlim(Parallel);
                var tasks = group.Urls
                    .Select(url => DownloadOneAsync(gate, group.Name, url, groupDirectory, progress, cancellationToken))
                    .ToList();

                await Task.WhenAll(tasks).ConfigureAwait(true);
            }

            _ignoreProgress = true;

            string message;
            MessageBoxIcon icon;
            if (_failed.Count == 0)
            {
                message = $"{_completed} tools saved to:\n\n{folder}";
                icon = MessageBoxIcon.Information;
            }
            else
            {
                string names = string.Join("\n", _failureNames.Take(8));
                string more = _failureNames.Count > 8 ? $"\n…and {_failureNames.Count - 8} more" : string.Empty;
                message = $"{_completed} tools saved to:\n\n{folder}\n\n{_failed.Count} failed:\n{names}{more}";
                icon = MessageBoxIcon.Warning;
            }

            if (!_closeAfterRun)
            {
                MessageBox.Show(this, message, "Download complete", MessageBoxButtons.OK, icon);
            }
        }
        catch (OperationCanceledException)
        {
            _ignoreProgress = true;
        }
        catch (Exception ex)
        {
            _ignoreProgress = true;
            if (!_closeAfterRun)
            {
                MessageBox.Show(this, $"The download stopped:\n\n{ex.Message}",
                    "acousticvoid downloader", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            SetRunning(false);

            SaveSettings();

            if (_closeAfterRun)
            {
                _closeAfterRun = false;
                Close();
            }
        }
    }

    private async Task DownloadOneAsync(
        SemaphoreSlim gate,
        string group,
        string url,
        string directory,
        IProgress<FileProgress> progress,
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _engine.DownloadAsync(group, url, directory, progress, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private void OnProgress(FileProgress progress)
    {
        if (IsDisposed || _ignoreProgress) return;

        switch (progress.State)
        {
            case FileState.Completed:
                Complete(progress.Key, true);
                break;
            case FileState.Failed:
                Complete(progress.Key, false, progress.Name);
                break;
        }
    }

    private void Complete(string key, bool ok, string? name = null)
    {
        if (!_finished.Add(key)) return;
        _done++;
        if (ok)
        {
            _completed++;
        }
        else
        {
            _failed.Add(key);
            if (!string.IsNullOrWhiteSpace(name)) _failureNames.Add(name);
        }

        if (_running)
        {
            _download.Text = $"DOWNLOADING  {_done} / {_total}";
            Text = $"acousticvoid downloader  —  {_done} / {_total}";
        }
    }

    private void SetRunning(bool running)
    {
        _running = running;
        _download.Enabled = true;
        _download.TabStop = !running;
        _download.Text = running ? "DOWNLOADING…" : "DOWNLOAD ALL TOOLS";
        _download.BackColor = running ? Theme.Card : Theme.White;
        _download.ForeColor = running ? Theme.White : Theme.Ink;
        _download.FlatAppearance.BorderSize = running ? 1 : 0;
        _download.FlatAppearance.BorderColor = Theme.Line;
        _download.FlatAppearance.MouseOverBackColor = running ? Theme.Card : Color.FromArgb(226, 226, 226);
        _download.FlatAppearance.MouseDownBackColor = running ? Theme.Card : Color.FromArgb(202, 202, 202);
        _download.Cursor = running ? Cursors.Default : Cursors.Hand;
        _changeFolder.Enabled = !running;
        _exclusion.Enabled = !running;
        Text = "acousticvoid downloader";
    }

    private string DisplayFolder() => _customFolder ?? NextSsFolder();

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose where the tools are saved",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
            SelectedPath = Directory.Exists(DisplayFolder()) ? DisplayFolder() : "C:\\",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        _customFolder = dialog.SelectedPath;
        _folderLabel.Text = _customFolder;
        SaveSettings();
    }

    private static string NextSsFolder()
    {
        for (int i = 1; i <= 999; i++)
        {
            string candidate = $@"C:\ss{i}";
            if (!Directory.Exists(candidate)) return candidate;
        }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "acousticvoid");
    }

    private static async Task<string?> AddExclusionAsync(string path)
    {
        try
        {
            var startInfo = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add($"Add-MpPreference -ExclusionPath '{path.Replace("'", "''")}' -ErrorAction Stop");

            using var process = Process.Start(startInfo);
            if (process == null) return "could not launch PowerShell";

            var errorTask = process.StandardError.ReadToEndAsync();
            var outputTask = process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync().ConfigureAwait(false);
            string error = await errorTask.ConfigureAwait(false);
            await outputTask.ConfigureAwait(false);

            return process.ExitCode == 0
                ? null
                : string.IsNullOrWhiteSpace(error) ? $"PowerShell exited with code {process.ExitCode}" : error.Trim();
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveSettings();
        if (!_running) return;

        if (_closeAfterRun)
        {
            e.Cancel = true;
            return;
        }

        var answer = MessageBox.Show(this,
            "A download is still running. Stop it and exit?",
            "acousticvoid downloader",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer == DialogResult.Yes)
        {
            _closeAfterRun = true;
            e.Cancel = true;
            _cts?.Cancel();
        }
        else
        {
            e.Cancel = true;
        }
    }

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "acousticvoid", "downloader", "settings.json");

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                if (settings != null)
                {
                    _customFolder = string.IsNullOrWhiteSpace(settings.CustomFolder) ? null : settings.CustomFolder;
                    _exclusion.Checked = settings.DefenderExclusion;
                }
            }
        }
        catch
        {
            // Corrupt or unreadable settings fall back to defaults.
        }

        _folderLabel.Text = DisplayFolder();
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            var settings = new AppSettings
            {
                CustomFolder = _customFolder,
                DefenderExclusion = _exclusion.Checked,
            };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true,
            }));
        }
        catch
        {
            // Settings persistence is best effort.
        }
    }
}

internal sealed class AppSettings
{
    public string? CustomFolder { get; set; }
    public bool DefenderExclusion { get; set; } = true;
}
