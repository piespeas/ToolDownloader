namespace AcousticVoid.Downloader;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetColorMode(SystemColorMode.Dark);
        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, args) => ShowError(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception) ShowError(exception);
        };

        Application.Run(new MainForm());
    }

    private static void ShowError(Exception exception)
    {
        MessageBox.Show(exception.ToString(), "acousticvoid downloader — unexpected error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
