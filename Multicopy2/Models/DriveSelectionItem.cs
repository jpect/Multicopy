using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Multicopy2.Models;

public class DriveSelectionItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private bool _isSelected = true;

    public DriveInfo Drive { get; }

    public DriveSelectionItem(DriveInfo drive) => Drive = drive;

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public string DisplayText
    {
        get
        {
            try
            {
                string letter = Drive.Name.TrimEnd('\\');
                string label = string.IsNullOrWhiteSpace(Drive.VolumeLabel) ? "No Label" : Drive.VolumeLabel;
                string free = FormatBytes(Drive.AvailableFreeSpace);
                string total = FormatBytes(Drive.TotalSize);
                return $"{letter}  —  {label}  ({free} free of {total})";
            }
            catch
            {
                return $"{Drive.Name.TrimEnd('\\')}  —  (not ready)";
            }
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes >= 1_073_741_824L) return $"{bytes / 1_073_741_824.0:F1} GB";
        if (bytes >= 1_048_576L) return $"{bytes / 1_048_576.0:F0} MB";
        return $"{bytes / 1024.0:F0} KB";
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
