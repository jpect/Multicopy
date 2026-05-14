using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Multicopy2;

public partial class App : Application
{
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int processId);
    private const int ATTACH_PARENT_PROCESS = -1;

    private bool _isSelfTest;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnUnhandledException;

        _isSelfTest = e.Args.Any(a => a.Equals("--self-test", StringComparison.OrdinalIgnoreCase));

        if (_isSelfTest)
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
            _ = RunSelfTestAsync();
        }
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_isSelfTest)
        {
            Console.Error.WriteLine($"[self-test] FAILED: {e.Exception.GetType().Name}: {e.Exception.Message}");
            Environment.Exit(1);
        }

        MessageBox.Show(
            $"Unexpected error:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
            "Multicopy — Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private async Task RunSelfTestAsync()
    {
        try
        {
            Console.WriteLine("[self-test] Constructing MainWindow...");
            var main = new MainWindow { Left = -10000, Top = -10000, ShowInTaskbar = false };
            main.Show();
            await Task.Delay(500);

            Console.WriteLine("[self-test] Constructing CopyWindow with empty source...");
            string tempSource = Path.Combine(Path.GetTempPath(), $"_mc_selftest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempSource);
            try
            {
                var drives = DriveInfo.GetDrives().Where(d => d.IsReady).Take(1).ToList();
                if (drives.Count == 0)
                {
                    Console.WriteLine("[self-test] No ready drives - skipping CopyWindow check.");
                }
                else
                {
                    var copy = new CopyWindow(tempSource, drives, false, false, false, "", main)
                    {
                        Left = -10000,
                        Top = -10000,
                        ShowInTaskbar = false
                    };
                    copy.Show();
                    await Task.Delay(2000);
                }
            }
            finally
            {
                try { Directory.Delete(tempSource, true); } catch { }
            }

            Console.WriteLine("[self-test] PASSED");
            Shutdown(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[self-test] FAILED: {ex.GetType().Name}: {ex.Message}");
            Shutdown(1);
        }
    }
}
