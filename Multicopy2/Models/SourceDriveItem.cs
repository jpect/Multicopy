using System.IO;

namespace Multicopy2.Models;

public class SourceDriveItem
{
    public DriveInfo Drive { get; }

    public SourceDriveItem(DriveInfo drive) => Drive = drive;

    public string DisplayText
    {
        get
        {
            try
            {
                string letter = Drive.Name.TrimEnd('\\');
                string label = string.IsNullOrWhiteSpace(Drive.VolumeLabel) ? "No Label" : Drive.VolumeLabel;
                string type = Drive.DriveType.ToString();
                string used = FormatBytes(Drive.TotalSize - Drive.AvailableFreeSpace);
                return $"{letter}  —  {label}  ({type}, {used} used)";
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
        if (bytes >= 1_048_576L)     return $"{bytes / 1_048_576.0:F0} MB";
        return $"{bytes / 1024.0:F0} KB";
    }
}
