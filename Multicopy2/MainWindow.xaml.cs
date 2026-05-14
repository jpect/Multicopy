using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using Multicopy2.Models;

namespace Multicopy2;

public partial class MainWindow : Window
{
    public ObservableCollection<DriveSelectionItem> DriveItems { get; } = [];
    public ObservableCollection<string> FileItems { get; } = [];
    public ObservableCollection<SourceDriveItem> SourceDriveItems { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshDrives();
        RefreshSourceDrives();
    }

    // ── Mode switching ─────────────────────────────────────────────────────────

    private void SourceMode_Changed(object sender, RoutedEventArgs e)
    {
        if (FolderModePanel is null) return; // can fire before InitializeComponent finishes

        FolderModePanel.Visibility = ModeFolder.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        FilesModePanel.Visibility  = ModeFiles.IsChecked  == true ? Visibility.Visible : Visibility.Collapsed;
        DriveModePanel.Visibility  = ModeDrive.IsChecked  == true ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Folder mode ────────────────────────────────────────────────────────────

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Select the source folder" };
        if (dialog.ShowDialog(this) == true)
            SourceBox.Text = dialog.FolderName;
    }

    // ── Files mode ─────────────────────────────────────────────────────────────

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Select files to copy",
            Multiselect = true
        };
        if (dialog.ShowDialog(this) == true)
        {
            foreach (var path in dialog.FileNames)
                if (!FileItems.Contains(path))
                    FileItems.Add(path);
        }
    }

    private void ClearFiles_Click(object sender, RoutedEventArgs e) => FileItems.Clear();

    // ── Drive mode ─────────────────────────────────────────────────────────────

    private void RescanSourceDrives_Click(object sender, RoutedEventArgs e) => RefreshSourceDrives();

    private void RefreshSourceDrives()
    {
        SourceDriveItems.Clear();
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
            SourceDriveItems.Add(new SourceDriveItem(drive));

        if (SourceDriveItems.Count > 0 && SourceDriveCombo.SelectedIndex < 0)
            SourceDriveCombo.SelectedIndex = 0;
    }

    // ── Destination drives ─────────────────────────────────────────────────────

    private void Rescan_Click(object sender, RoutedEventArgs e) => RefreshDrives();

    private void OptionSetName_Changed(object sender, RoutedEventArgs e)
        => OptionVolumeNameBox.IsEnabled = OptionSetName.IsChecked == true;

    private void RefreshDrives()
    {
        var previouslySelected = new HashSet<string>(
            DriveItems.Where(d => d.IsSelected).Select(d => d.Drive.Name));

        DriveItems.Clear();

        var removable = DriveInfo.GetDrives()
            .Where(d => d.DriveType == DriveType.Removable && d.IsReady);

        foreach (var drive in removable)
        {
            DriveItems.Add(new DriveSelectionItem(drive)
            {
                IsSelected = previouslySelected.Count == 0 || previouslySelected.Contains(drive.Name)
            });
        }
    }

    // ── Start copy ─────────────────────────────────────────────────────────────

    private void StartCopy_Click(object sender, RoutedEventArgs e)
    {
        var errors = new List<string>();
        var selectedDrives = DriveItems.Where(d => d.IsSelected).Select(d => d.Drive).ToList();
        if (selectedDrives.Count == 0)
            errors.Add("• No destination drives selected.");

        bool setName  = OptionSetName.IsChecked == true;
        string volName = OptionVolumeNameBox.Text.Trim();
        if (setName && string.IsNullOrWhiteSpace(volName))
            errors.Add("• Volume name is empty — enter a name or uncheck the option.");

        List<CopySource> sources;
        string description;

        try
        {
            (sources, description) = BuildSources(errors, selectedDrives);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Source error:\n\n{ex.Message}", "Multicopy", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (errors.Count > 0)
        {
            MessageBox.Show(string.Join("\n", errors), "Please fix the following",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        bool erase     = OptionEraseBefore.IsChecked == true;
        bool overwrite = OptionOverwrite.IsChecked  == true;

        if (erase)
        {
            var confirm = MessageBox.Show(
                $"This will permanently delete all contents from {selectedDrives.Count} drive(s) before copying.\n\nAre you sure?",
                "Confirm Erase", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
        }

        if (setName && volName.Length > 11)
        {
            var lengthWarn = MessageBox.Show(
                $"Volume name '{volName}' is {volName.Length} characters.\n\n" +
                "Most USB drives use FAT32 or exFAT, which only support up to 11 characters " +
                "for volume names. Drives that reject the name will keep their existing label " +
                "(the copy still succeeds).\n\nContinue anyway?",
                "Volume name may be too long", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (lengthWarn != MessageBoxResult.Yes) return;
        }

        var copyWindow = new CopyWindow(sources, description, selectedDrives, erase, overwrite, setName, volName, this);
        copyWindow.ShowDialog();
    }

    private (List<CopySource> sources, string description) BuildSources(List<string> errors, List<DriveInfo> destDrives)
    {
        if (ModeFolder.IsChecked == true)
        {
            string path = SourceBox.Text;
            if (string.IsNullOrWhiteSpace(path) || path == "(no folder selected)")
            {
                errors.Add("• No source folder selected.");
                return ([], "");
            }
            if (!Directory.Exists(path))
            {
                errors.Add("• Source folder does not exist.");
                return ([], "");
            }

            bool includeName = FolderIncludeName.IsChecked == true;
            string description = includeName ? $"folder '{new DirectoryInfo(path).Name}' (preserved)" : $"contents of {path}";
            return ([new CopySource(path, IsFolder: true, IncludeFolderName: includeName)], description);
        }

        if (ModeFiles.IsChecked == true)
        {
            if (FileItems.Count == 0)
            {
                errors.Add("• No files added — click '+ Add files...' to pick some.");
                return ([], "");
            }
            var sources = FileItems.Select(p => new CopySource(p, IsFolder: false)).ToList();
            return (sources, $"{FileItems.Count} file(s)");
        }

        // Drive mode
        if (SourceDriveCombo.SelectedItem is not SourceDriveItem item)
        {
            errors.Add("• No source drive selected.");
            return ([], "");
        }
        string driveRoot = item.Drive.RootDirectory.FullName;

        if (destDrives.Any(d => string.Equals(d.Name, item.Drive.Name, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add($"• Source drive {item.Drive.Name.TrimEnd('\\')} is also a destination — uncheck it on the right.");
            return ([], "");
        }

        return (
            [new CopySource(driveRoot, IsFolder: true, IncludeFolderName: false)],
            $"entire drive {item.Drive.Name.TrimEnd('\\')}");
    }
}
