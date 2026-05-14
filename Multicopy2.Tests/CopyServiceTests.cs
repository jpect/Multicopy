using System.IO;
using Multicopy2.Services;
using Xunit;

namespace Multicopy2.Tests;

public class CopyServiceTests : IDisposable
{
    private readonly string _src;
    private readonly string _dst;

    public CopyServiceTests()
    {
        _src = CreateTempDir();
        _dst = CreateTempDir();
    }

    public void Dispose()
    {
        TryDelete(_src);
        TryDelete(_dst);
        GC.SuppressFinalize(this);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "mc_test_" + Path.GetRandomFileName());
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDelete(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); }
        catch { }
    }

    private static void WriteFile(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    private static IProgress<CopyProgressUpdate> NullProgress() => new Progress<CopyProgressUpdate>();

    [Fact]
    public async Task CopiesAllFilesInRoot()
    {
        WriteFile(Path.Combine(_src, "a.txt"), "hello");
        WriteFile(Path.Combine(_src, "b.txt"), "world");

        await CopyService.CopyToDirectoryAsync(_src, _dst, false, false, NullProgress(), CancellationToken.None);

        Assert.Equal("hello", File.ReadAllText(Path.Combine(_dst, "a.txt")));
        Assert.Equal("world", File.ReadAllText(Path.Combine(_dst, "b.txt")));
    }

    [Fact]
    public async Task RecursesIntoSubdirectories()
    {
        WriteFile(Path.Combine(_src, "sub1", "x.txt"), "a");
        WriteFile(Path.Combine(_src, "sub1", "sub2", "y.txt"), "b");

        await CopyService.CopyToDirectoryAsync(_src, _dst, false, false, NullProgress(), CancellationToken.None);

        Assert.Equal("a", File.ReadAllText(Path.Combine(_dst, "sub1", "x.txt")));
        Assert.Equal("b", File.ReadAllText(Path.Combine(_dst, "sub1", "sub2", "y.txt")));
    }

    [Fact]
    public async Task OverwriteTrue_ReplacesExistingFile()
    {
        WriteFile(Path.Combine(_src, "f.txt"), "new");
        WriteFile(Path.Combine(_dst, "f.txt"), "old");

        await CopyService.CopyToDirectoryAsync(_src, _dst, false, overwrite: true, NullProgress(), CancellationToken.None);

        Assert.Equal("new", File.ReadAllText(Path.Combine(_dst, "f.txt")));
    }

    [Fact]
    public async Task OverwriteFalse_ThrowsOnExistingFile()
    {
        WriteFile(Path.Combine(_src, "f.txt"), "new");
        WriteFile(Path.Combine(_dst, "f.txt"), "old");

        await Assert.ThrowsAsync<IOException>(() =>
            CopyService.CopyToDirectoryAsync(_src, _dst, false, overwrite: false, NullProgress(), CancellationToken.None));
    }

    [Fact]
    public async Task EraseBeforeTrue_ClearsDestination()
    {
        WriteFile(Path.Combine(_src, "new.txt"), "new content");
        WriteFile(Path.Combine(_dst, "old.txt"), "should be gone");
        Directory.CreateDirectory(Path.Combine(_dst, "old_subdir"));
        WriteFile(Path.Combine(_dst, "old_subdir", "nested.txt"), "also gone");

        await CopyService.CopyToDirectoryAsync(_src, _dst, eraseBefore: true, overwrite: false, NullProgress(), CancellationToken.None);

        Assert.False(File.Exists(Path.Combine(_dst, "old.txt")));
        Assert.False(Directory.Exists(Path.Combine(_dst, "old_subdir")));
        Assert.True(File.Exists(Path.Combine(_dst, "new.txt")));
    }

    [Fact]
    public async Task ReportsProgressUpdates()
    {
        WriteFile(Path.Combine(_src, "a.txt"), "abc");
        WriteFile(Path.Combine(_src, "b.txt"), "defgh");

        var updates = new List<CopyProgressUpdate>();
        var progress = new Progress<CopyProgressUpdate>(u => updates.Add(u));

        await CopyService.CopyToDirectoryAsync(_src, _dst, false, false, progress, CancellationToken.None);
        await Task.Delay(200);

        Assert.NotEmpty(updates);
        Assert.Equal(8L, updates.Last().BytesCopied);
        Assert.Equal(8L, updates.Last().TotalBytes);
    }

    [Fact]
    public async Task Cancellation_StopsCopy()
    {
        for (int i = 0; i < 200; i++)
            WriteFile(Path.Combine(_src, $"file_{i:D3}.dat"), new string('x', 4096));

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(20);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CopyService.CopyToDirectoryAsync(_src, _dst, false, false, NullProgress(), cts.Token));

        int copied = Directory.EnumerateFiles(_dst).Count();
        Assert.True(copied < 200, $"Expected partial copy but got {copied}/200");
    }

    [Fact]
    public async Task NonExistentSource_Throws()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() =>
            CopyService.CopyToDirectoryAsync(
                Path.Combine(Path.GetTempPath(), "_mc_definitely_missing_" + Guid.NewGuid()),
                _dst, false, false, NullProgress(), CancellationToken.None));
    }

    [Fact]
    public async Task EmptySource_CompletesWithoutCopyingAnything()
    {
        await CopyService.CopyToDirectoryAsync(_src, _dst, false, false, NullProgress(), CancellationToken.None);

        Assert.Empty(Directory.EnumerateFileSystemEntries(_dst));
    }
}
