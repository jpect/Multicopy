using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Multicopy2.Models;

public enum CopyStatus { Waiting, Copying, Done, Cancelled, Error }

public class DriveProgress : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private long _bytesCopied;
    private long _totalBytes;
    private double _speedMbps;
    private TimeSpan _eta;
    private CopyStatus _status = CopyStatus.Waiting;
    private string _errorMessage = "";
    private string _currentFile = "";

    public DriveProgress(DriveInfo drive)
    {
        Drive = drive;
        DriveLetter = drive.Name.TrimEnd('\\');
        VolumeLabel = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "No Label" : drive.VolumeLabel;
    }

    public DriveInfo Drive { get; }
    public string DriveLetter { get; }
    public string VolumeLabel { get; }

    public long BytesCopied
    {
        get => _bytesCopied;
        set
        {
            _bytesCopied = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(BytesCopiedDisplay));
        }
    }

    public long TotalBytes
    {
        get => _totalBytes;
        set
        {
            _totalBytes = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressPercent));
            OnPropertyChanged(nameof(TotalBytesDisplay));
        }
    }

    public double ProgressPercent => _totalBytes > 0 ? (double)_bytesCopied / _totalBytes * 100.0 : 0;

    public double SpeedMbps
    {
        get => _speedMbps;
        set { _speedMbps = value; OnPropertyChanged(); OnPropertyChanged(nameof(SpeedDisplay)); }
    }

    public TimeSpan Eta
    {
        get => _eta;
        set { _eta = value; OnPropertyChanged(); OnPropertyChanged(nameof(EtaDisplay)); }
    }

    public CopyStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusDisplay));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(IsActive));
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); OnPropertyChanged(nameof(StatusDisplay)); }
    }

    public string CurrentFile
    {
        get => _currentFile;
        set { _currentFile = value; OnPropertyChanged(); }
    }

    public bool IsActive => _status == CopyStatus.Copying;

    public string BytesCopiedDisplay => FormatBytes(_bytesCopied);
    public string TotalBytesDisplay => FormatBytes(_totalBytes);

    public string SpeedDisplay => _status == CopyStatus.Copying ? $"{_speedMbps:F1} MB/s" : "";

    public string EtaDisplay => _status == CopyStatus.Copying && _eta > TimeSpan.Zero
        ? $"ETA {FormatEta(_eta)}"
        : "";

    public string StatusDisplay => _status switch
    {
        CopyStatus.Waiting   => "Waiting...",
        CopyStatus.Copying   => $"{_bytesCopied:N0} / {_totalBytes:N0} bytes",
        CopyStatus.Done      => $"Done  ({BytesCopiedDisplay})",
        CopyStatus.Cancelled => "Cancelled",
        CopyStatus.Error     => $"Error: {_errorMessage}",
        _                    => ""
    };

    public string StatusColor => _status switch
    {
        CopyStatus.Done      => "#388E3C",
        CopyStatus.Error     => "#C62828",
        CopyStatus.Cancelled => "#F57C00",
        _                    => "#555555"
    };

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824L) return $"{bytes / 1_073_741_824.0:F2} GB";
        if (bytes >= 1_048_576L)     return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1_024L)         return $"{bytes / 1_024.0:F0} KB";
        return $"{bytes} B";
    }

    private static string FormatEta(TimeSpan eta)
    {
        if (eta.TotalHours >= 1)   return $"{(int)eta.TotalHours}h {eta.Minutes:D2}m";
        if (eta.TotalMinutes >= 1) return $"{(int)eta.TotalMinutes}m {eta.Seconds:D2}s";
        return $"{eta.Seconds}s";
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
