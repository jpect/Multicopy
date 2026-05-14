using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using Multicopy2.Models;
using Multicopy2.Services;

namespace Multicopy2;

public partial class CopyWindow : Window
{
    private readonly IList<CopySource> _sources;
    private readonly string _sourceDescription;
    private readonly List<DriveInfo> _drives;
    private readonly bool _eraseBefore;
    private readonly bool _overwrite;
    private readonly bool _setName;
    private readonly string _volumeName;
    private readonly CancellationTokenSource _cts = new();

    private bool _finished = false;

    public ObservableCollection<DriveProgress> DriveProgressItems { get; } = [];

    public CopyWindow(
        IList<CopySource> sources,
        string sourceDescription,
        List<DriveInfo> drives,
        bool eraseBefore,
        bool overwrite,
        bool setName,
        string volumeName,
        Window owner)
    {
        InitializeComponent();
        DataContext = this;
        Owner = owner;

        _sources = sources;
        _sourceDescription = sourceDescription;
        _drives = drives;
        _eraseBefore = eraseBefore;
        _overwrite = overwrite;
        _setName = setName;
        _volumeName = volumeName;

        foreach (var drive in drives)
            DriveProgressItems.Add(new DriveProgress(drive));

        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        int count = _drives.Count;
        HeaderText.Text = $"Copying to {count} drive{(count != 1 ? "s" : "")}...";
        SubHeaderText.Text = $"Source: {_sourceDescription}";
        SummaryText.Text = $"0 of {count} complete";

        try
        {
            var tasks = _drives.Select((drive, i) => RunCopyForDrive(drive, DriveProgressItems[i])).ToList();

            await Task.WhenAll(tasks);

            _finished = true;

            if (_setName && !_cts.IsCancellationRequested)
            {
                for (int i = 0; i < _drives.Count; i++)
                {
                    var dp = DriveProgressItems[i];
                    if (dp.Status != CopyStatus.Done) continue;
                    try
                    {
                        _drives[i].VolumeLabel = _volumeName;
                        dp.RefreshVolumeLabel();
                    }
                    catch (Exception ex)
                    {
                        dp.Warning = $"label not set ({ex.Message})";
                    }
                }
            }

            int done      = DriveProgressItems.Count(d => d.Status == CopyStatus.Done);
            int errors    = DriveProgressItems.Count(d => d.Status == CopyStatus.Error);
            int cancelled = DriveProgressItems.Count(d => d.Status == CopyStatus.Cancelled);

            HeaderText.Text = errors == 0 && cancelled == 0
                ? $"Complete — {done} drive{(done != 1 ? "s" : "")} copied"
                : $"Finished with issues";

            SummaryText.Text = $"{done}/{count} done" +
                (errors > 0    ? $"  ·  {errors} error{(errors != 1 ? "s" : "")}" : "") +
                (cancelled > 0 ? $"  ·  {cancelled} cancelled" : "");
        }
        catch (Exception ex)
        {
            _finished = true;
            HeaderText.Text = "Error during copy";
            SummaryText.Text = ex.Message;
        }
        finally
        {
            CancelBtn.IsEnabled = false;
            CloseBtn.IsEnabled = true;
        }
    }

    private async Task RunCopyForDrive(DriveInfo drive, DriveProgress dp)
    {
        dp.Status = CopyStatus.Copying;

        var progress = new Progress<CopyProgressUpdate>(update =>
        {
            try
            {
                dp.BytesCopied = update.BytesCopied;
                dp.TotalBytes  = update.TotalBytes;
                dp.SpeedMbps   = update.SpeedBytesPerSecond / 1_048_576.0;
                dp.Eta         = update.Eta;
                dp.CurrentFile = update.CurrentFile;

                int done = DriveProgressItems.Count(d => d.Status is CopyStatus.Done or CopyStatus.Error or CopyStatus.Cancelled);
                SummaryText.Text = $"{done} of {_drives.Count} complete";
            }
            catch { /* progress display failed; copy continues */ }
        });

        try
        {
            await CopyService.CopyToDriveAsync(_sources, drive, _eraseBefore, _overwrite, progress, _cts.Token);
            dp.BytesCopied = dp.TotalBytes;
            dp.CurrentFile = "";
            dp.Status = CopyStatus.Done;
        }
        catch (OperationCanceledException)
        {
            dp.CurrentFile = "";
            dp.Status = CopyStatus.Cancelled;
        }
        catch (Exception ex)
        {
            dp.CurrentFile = "";
            dp.Status = CopyStatus.Error;
            dp.ErrorMessage = ex.Message;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        _cts.Cancel();
        CancelBtn.IsEnabled = false;
        HeaderText.Text = "Cancelling...";
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (_finished || _cts.IsCancellationRequested) return;

        var result = MessageBox.Show(
            "A copy is in progress. Cancel all drives and close?",
            "Multicopy",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        _cts.Cancel();
    }
}
