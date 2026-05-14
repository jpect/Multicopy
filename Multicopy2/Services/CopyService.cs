using System.Diagnostics;
using System.IO;

namespace Multicopy2.Services;

public record CopyProgressUpdate(
    long BytesCopied,
    long TotalBytes,
    double SpeedBytesPerSecond,
    TimeSpan Eta,
    string CurrentFile);

public static class CopyService
{
    public static Task CopyToDriveAsync(
        string sourcePath,
        DriveInfo drive,
        bool eraseBefore,
        bool overwrite,
        IProgress<CopyProgressUpdate> progress,
        CancellationToken ct)
        => CopyToDirectoryAsync(sourcePath, drive.RootDirectory.FullName, eraseBefore, overwrite, progress, ct);

    public static async Task CopyToDirectoryAsync(
        string sourcePath,
        string destPath,
        bool eraseBefore,
        bool overwrite,
        IProgress<CopyProgressUpdate> progress,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (eraseBefore)
            await Task.Run(() => EraseRootContents(new DirectoryInfo(destPath)), ct);

        ct.ThrowIfCancellationRequested();

        var allFiles = await Task.Run(() => EnumerateFiles(sourcePath), ct);
        long totalBytes = allFiles.Sum(f => f.Length);
        long bytesCopied = 0;
        var sw = Stopwatch.StartNew();

        foreach (var file in allFiles)
        {
            ct.ThrowIfCancellationRequested();

            string relativePath = Path.GetRelativePath(sourcePath, file.FullName);
            string fileDestPath = Path.Combine(destPath, relativePath);
            string? destDir = Path.GetDirectoryName(fileDestPath);

            if (destDir is not null && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            await Task.Run(() => file.CopyTo(fileDestPath, overwrite), ct);

            bytesCopied += file.Length;

            double elapsed = sw.Elapsed.TotalSeconds;
            double speed = elapsed > 0.01 ? bytesCopied / elapsed : 0;
            long remaining = totalBytes - bytesCopied;
            TimeSpan eta = speed > 0 ? TimeSpan.FromSeconds(remaining / speed) : TimeSpan.Zero;

            progress.Report(new CopyProgressUpdate(bytesCopied, totalBytes, speed, eta, relativePath));
        }
    }

    private static List<FileInfo> EnumerateFiles(string path)
    {
        var dir = new DirectoryInfo(path);
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"Source directory not found: {path}");
        return [.. dir.EnumerateFiles("*", SearchOption.AllDirectories).OrderBy(f => f.FullName)];
    }

    private static void EraseRootContents(DirectoryInfo root)
    {
        if (!root.Exists) return;
        foreach (var file in root.GetFiles())
            file.Delete();
        foreach (var dir in root.GetDirectories())
            dir.Delete(true);
    }
}
