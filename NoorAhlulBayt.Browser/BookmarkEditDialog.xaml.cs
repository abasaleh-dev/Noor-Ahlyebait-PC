using NoorAhlulBayt.Common.Models;
using System.Windows;

namespace NoorAhlulBayt.Browser;

public partial class BookmarkEditDialog : Window
{
    public string BookmarkTitle => TitleTextBox.Text.Trim();
    public string BookmarkUrl => UrlTextBox.Text.Trim();
    public string? BookmarkDescription => string.IsNullOrWhiteSpace(DescriptionTextBox.Text) ? null : DescriptionTextBox.Text.Trim();
    public string BookmarkFolder => string.IsNullOrWhiteSpace(FolderComboBox.Text) ? "Default" : FolderComboBox.Text.Trim();

    private readonly bool _isEditMode;

    // Constructor for adding new bookmark
    public BookmarkEditDialog(List<string> folders, string? defaultFolder = null)
    {
        InitializeComponent();
        _isEditMode = false;
        HeaderTextBlock.Text = "Add Bookmark";
        Title = "Add Bookmark";

        InitializeFolders(folders, defaultFolder ?? "Default");

        // Wire up event handlers
        TitleTextBox.TextChanged += TitleTextBox_TextChanged;
        UrlTextBox.TextChanged += UrlTextBox_TextChanged;
        FolderComboBox.SelectionChanged += FolderComboBox_SelectionChanged;
    }

    // Constructor for editing existing bookmark
    public BookmarkEditDialog(List<string> folders, Bookmark bookmark)
    {
        InitializeComponent();
        _isEditMode = true;
        HeaderTextBlock.Text = "Edit Bookmark";
        Title = "Edit Bookmark";

        InitializeFolders(folders, bookmark.FolderPath);

        // Wire up event handlers
        TitleTextBox.TextChanged += TitleTextBox_TextChanged;
        UrlTextBox.TextChanged += UrlTextBox_TextChanged;
        FolderComboBox.SelectionChanged += FolderComboBox_SelectionChanged;

        // Populate fields with existing bookmark data
        TitleTextBox.Text = bookmark.Title;
        UrlTextBox.Text = bookmark.Url;
        DescriptionTextBox.Text = bookmark.Description ?? "";
        FolderComboBox.Text = bookmark.FolderPath ?? "Default";
    }

    private void InitializeFolders(List<string> folders, string selectedFolder)
    {
        // Add existing folders to combobox
        FolderComboBox.Items.Clear();
        
        // Add default folders
        var defaultFolders = new[] { "Default", "Islamic", "News", "Education", "Entertainment", "Work" };
        foreach (var folder in defaultFolders)
        {
            if (!FolderComboBox.Items.Contains(folder))
            {
                FolderComboBox.Items.Add(folder);
            }
        }

        // Add existing folders
        foreach (var folder in folders.OrderBy(f => f))
        {
            if (!FolderComboBox.Items.Contains(folder))
            {
                FolderComboBox.Items.Add(folder);
            }
        }

        // Set selected folder
        FolderComboBox.Text = selectedFolder;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (ValidateInput())
        {
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private bool ValidateInput()
    {
        var errors = new List<string>();

        // Validate title
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
        {
            errors.Add("Title is required.");
        }

        // Validate URL
        if (string.IsNullOrWhiteSpace(UrlTextBox.Text))
        {
            errors.Add("URL is required.");
        }
        else if (!IsValidUrl(UrlTextBox.Text.Trim()))
        {
            errors.Add("Please enter a valid URL.");
        }

        // Validate folder name
        var folderName = FolderComboBox.Text.Trim();
        if (!string.IsNullOrEmpty(folderName) && !IsValidFolderName(folderName))
        {
            errors.Add("Folder name contains invalid characters.");
        }

        if (errors.Any())
        {
            ValidationTextBlock.Text = string.Join("\n", errors);
            ValidationTextBlock.Visibility = Visibility.Visible;
            return false;
        }
        else
        {
            ValidationTextBlock.Visibility = Visibility.Collapsed;
            return true;
        }
    }

    private bool IsValidUrl(string url)
    {
        try
        {
            // Add protocol if missing
            if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("ftp://"))
            {
                url = "https://" + url;
                UrlTextBox.Text = url; // Update the textbox with the corrected URL
            }

            return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps || result.Scheme == Uri.UriSchemeFtp);
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidFolderName(string folderName)
    {
        // Check for invalid characters in folder names
        var invalidChars = new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
        return !invalidChars.Any(c => folderName.Contains(c)) && folderName.Length <= 100;
    }

    private void TitleTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Hide validation message when user starts typing
        if (ValidationTextBlock.Visibility == Visibility.Visible)
        {
            ValidationTextBlock.Visibility = Visibility.Collapsed;
        }
    }

    private void UrlTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Hide validation message when user starts typing
        if (ValidationTextBlock.Visibility == Visibility.Visible)
        {
            ValidationTextBlock.Visibility = Visibility.Collapsed;
        }
    }

    private void FolderComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Hide validation message when user changes selection
        if (ValidationTextBlock.Visibility == Visibility.Visible)
        {
            ValidationTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}
