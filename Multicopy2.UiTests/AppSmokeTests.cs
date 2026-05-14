using System.IO;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace Multicopy2.UiTests;

public class AppSmokeTests
{
    private static string LocateExe()
    {
        // Walk up from the test bin folder to find the Multicopy2 published exe.
        // Tests run from: Multicopy2.UiTests\bin\Debug\net9.0-windows\
        // Target:          Multicopy2\bin\Debug\net9.0-windows\Multicopy2.exe
        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 6 && dir != null; i++)
        {
            var candidate = Path.Combine(dir, "Multicopy2", "bin", "Debug", "net9.0-windows", "Multicopy2.exe");
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar));
        }
        throw new FileNotFoundException(
            "Could not locate Multicopy2.exe. Build the main project first " +
            "(dotnet build Multicopy2/Multicopy2.csproj).");
    }

    [Fact]
    public void AppLaunches_AndMainWindowAppears()
    {
        string exe = LocateExe();

        using var app = Application.Launch(exe);
        try
        {
            using var automation = new UIA3Automation();
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));

            Assert.NotNull(window);
            Assert.Contains("Multicopy", window.Title);
            Assert.False(app.HasExited, "App should still be running after main window appears");
        }
        finally
        {
            try { app.Close(); } catch { }
        }
    }

    [Fact]
    public void SelfTestMode_ExitsCleanly()
    {
        string exe = LocateExe();

        var psi = new System.Diagnostics.ProcessStartInfo(exe, "--self-test")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        var proc = System.Diagnostics.Process.Start(psi)!;
        bool exited = proc.WaitForExit(15_000);

        if (!exited)
        {
            proc.Kill();
            Assert.Fail("Self-test did not exit within 15 seconds (hang)");
        }

        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();

        Assert.True(
            proc.ExitCode == 0,
            $"Self-test exit code {proc.ExitCode}\n--- stdout ---\n{stdout}\n--- stderr ---\n{stderr}");
        Assert.Contains("PASSED", stdout);
    }
}
