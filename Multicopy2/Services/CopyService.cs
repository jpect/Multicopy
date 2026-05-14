using System.Diagnostics;
using System.IO;
using Multicopy2.Models;

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
        IList<CopySource> sources,
        DriveInfo drive,
        bool eraseBefore,
        bool overwrite,
        IProgress<CopyProgressUpdate> progress,
        CancellationToken ct)
        => CopyToDirectoryAsync(sources, drive.RootDirectory.FullName, eraseBefore, overwrite, progress, ct);

    /// <summary>Backwards-compat overload used by existing tests — single folder, contents-only.</summary>
    public static Task CopyToDirectoryAsync(
        string sourcePath,
        string destPath,
        bool eraseBefore,
        bool overwrite,
        IProgress<CopyProgressUpdate> progress,
        CancellationToken ct)
        => CopyToDirectoryAsync(
            new[] { new CopySource(sourcePath, IsFolder: true, IncludeFolderName: false) },
            destPath, eraseBefore, overwrite, progress, ct);

    public static async Task CopyToDirectoryAsync(
        IList<CopySource> sources,
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

        var plan = await Task.Run(() => BuildCopyPlan(sources), ct);
        long totalBytes = plan.Sum(item => item.Source.Length);
        long bytesCopied = 0;
        var sw = Stopwatch.StartNew();

        foreach (var item in plan)
        {
            ct.ThrowIfCancellationRequested();

            string fileDestPath = Path.Combine(destPath, item.RelativeDest);
            string? destDir = Path.GetDirectoryName(fileDestPath);

            if (destDir is not null && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            await Task.Run(() => item.Source.CopyTo(fileDestPath, overwrite), ct);

            bytesCopied += item.Source.Length;

            double elapsed = sw.Elapsed.TotalSeconds;
            double speed = elapsed > 0.01 ? bytesCopied / elapsed : 0;
            long remaining = totalBytes - bytesCopied;
            TimeSpan eta = speed > 0 ? TimeSpan.FromSeconds(remaining / speed) : TimeSpan.Zero;

            progress.Report(new CopyProgressUpdate(bytesCopied, totalBytes, speed, eta, item.RelativeDest));
        }
    }

    private record CopyPlanItem(FileInfo Source, string RelativeDest);

    private static List<CopyPlanItem> BuildCopyPlan(IList<CopySource> sources)
    {
        var plan = new List<CopyPlanItem>();

        foreach (var source in sources)
        {
            if (!source.IsFolder)
            {
                var file = new FileInfo(source.Path);
                if (!file.Exists)
                    throw new FileNotFoundException($"Source file not found: {source.Path}");
                plan.Add(new CopyPlanItem(file, file.Name));
                continue;
            }

            var dir = new DirectoryInfo(source.Path);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source folder not found: {source.Path}");

            string prefix = source.IncludeFolderName ? dir.Name : "";

            foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories).OrderBy(f => f.FullName))
            {
                string rel = Path.GetRelativePath(dir.FullName, file.FullName);
                string dest = string.IsNullOrEmpty(prefix) ? rel : Path.Combine(prefix, rel);
                plan.Add(new CopyPlanItem(file, dest));
            }
        }

        return plan;
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
