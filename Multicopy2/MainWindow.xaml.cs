using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Multicopy2.Models;
using MessageBox = System.Windows.MessageBox;

namespace Multicopy2;

public partial class MainWindow : Window
{
    public ObservableCollection<DriveSelectionItem> DriveItems { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) => RefreshDrives();

    private void BrowseSource_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select the source folder to copy",
            ShowNewFolderButton = false
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            SourceBox.Text = dialog.SelectedPath;
    }

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

    private void StartCopy_Click(object sender, RoutedEventArgs e)
    {
        var errors = Validate(out var selectedDrives, out string sourcePath);
        if (errors.Count > 0)
        {
            MessageBox.Show(
                string.Join("\n", errors),
                "Please fix the following",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        bool erase   = OptionEraseBefore.IsChecked == true;
        bool overwrite = OptionOverwrite.IsChecked == true;
        bool setName = OptionSetName.IsChecked == true;
        string volName = OptionVolumeNameBox.Text.Trim();

        if (erase)
        {
            var confirm = MessageBox.Show(
                $"This will permanently delete all contents from {selectedDrives.Count} drive(s) before copying.\n\nAre you sure?",
                "Confirm Erase",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;
        }

        var copyWindow = new CopyWindow(sourcePath, selectedDrives, erase, overwrite, setName, volName, this);
        copyWindow.ShowDialog();
    }

    private List<string> Validate(out List<DriveInfo> selectedDrives, out string sourcePath)
    {
        var errors = new List<string>();
        sourcePath = SourceBox.Text;
        selectedDrives = DriveItems.Where(d => d.IsSelected).Select(d => d.Drive).ToList();

        if (string.IsNullOrWhiteSpace(sourcePath) || sourcePath == "(no folder selected)")
            errors.Add("• No source folder selected.");
        else if (!Directory.Exists(sourcePath))
            errors.Add("• Source folder does not exist.");

        if (selectedDrives.Count == 0)
            errors.Add("• No drives selected.");

        if (OptionSetName.IsChecked == true && string.IsNullOrWhiteSpace(OptionVolumeNameBox.Text))
            errors.Add("• Volume name is empty — enter a name or uncheck the option.");

        return errors;
    }
}
