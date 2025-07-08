using System.Windows;

namespace NoorAhlulBayt.Browser;

public partial class FolderEditDialog : Window
{
    public string FolderName => FolderNameTextBox.Text.Trim();

    private readonly bool _isEditMode;

    // Constructor for adding new folder
    public FolderEditDialog()
    {
        InitializeComponent();
        _isEditMode = false;
        HeaderTextBlock.Text = "Add Folder";
        Title = "Add Folder";
    }

    // Constructor for editing existing folder
    public FolderEditDialog(string existingFolderName)
    {
        InitializeComponent();
        _isEditMode = true;
        HeaderTextBlock.Text = "Rename Folder";
        Title = "Rename Folder";
        
        FolderNameTextBox.Text = existingFolderName;
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

        // Validate folder name
        if (string.IsNullOrWhiteSpace(FolderNameTextBox.Text))
        {
            errors.Add("Folder name is required.");
        }
        else if (!IsValidFolderName(FolderNameTextBox.Text.Trim()))
        {
            errors.Add("Folder name contains invalid characters or is too long.");
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

    private bool IsValidFolderName(string folderName)
    {
        // Check for invalid characters in folder names
        var invalidChars = new[] { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
        return !invalidChars.Any(c => folderName.Contains(c)) && 
               folderName.Length <= 100 && 
               folderName.Length > 0;
    }

    private void FolderNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // Hide validation message when user starts typing
        if (ValidationTextBlock.Visibility == Visibility.Visible)
        {
            ValidationTextBlock.Visibility = Visibility.Collapsed;
        }
    }
}
