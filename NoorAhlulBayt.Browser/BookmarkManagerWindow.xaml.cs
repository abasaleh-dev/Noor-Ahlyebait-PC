using Microsoft.Win32;
using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;
using NoorAhlulBayt.Common.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NoorAhlulBayt.Browser;

public partial class BookmarkManagerWindow : Window
{
    private readonly ApplicationDbContext _context;
    private readonly BookmarkService _bookmarkService;
    private readonly int _userProfileId;
    private ObservableCollection<Bookmark> _bookmarks;
    private ObservableCollection<Bookmark> _filteredBookmarks;
    private List<string> _folders;
    private string? _selectedFolder;

    public BookmarkManagerWindow(ApplicationDbContext context, int userProfileId)
    {
        InitializeComponent();
        _context = context;
        _bookmarkService = new BookmarkService(context);
        _userProfileId = userProfileId;
        _bookmarks = new ObservableCollection<Bookmark>();
        _filteredBookmarks = new ObservableCollection<Bookmark>();
        _folders = new List<string>();

        BookmarkDataGrid.ItemsSource = _filteredBookmarks;
        
        Loaded += BookmarkManagerWindow_Loaded;
    }

    private async void BookmarkManagerWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadBookmarksAsync();
        await LoadFoldersAsync();
        UpdateStatus("Ready");
    }

    #region Data Loading

    private async Task LoadBookmarksAsync()
    {
        try
        {
            var bookmarks = await _bookmarkService.GetBookmarksAsync(_userProfileId);
            _bookmarks.Clear();
            
            foreach (var bookmark in bookmarks)
            {
                _bookmarks.Add(bookmark);
            }

            ApplyFilters();
            UpdateStatus($"Loaded {bookmarks.Count} bookmarks");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading bookmarks: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("Error loading bookmarks");
        }
    }

    private async Task LoadFoldersAsync()
    {
        try
        {
            _folders = await _bookmarkService.GetFoldersAsync(_userProfileId);
            
            // Update folder filter combobox
            FolderFilterComboBox.Items.Clear();
            FolderFilterComboBox.Items.Add("All Folders");
            
            foreach (var folder in _folders)
            {
                FolderFilterComboBox.Items.Add(folder);
            }
            
            FolderFilterComboBox.SelectedIndex = 0;

            // Update folder tree
            UpdateFolderTree();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading folders: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateFolderTree()
    {
        FolderTreeView.Items.Clear();

        // Add "All Bookmarks" root item
        var allBookmarksItem = new TreeViewItem
        {
            Header = "All Bookmarks",
            Tag = null,
            IsExpanded = true
        };
        FolderTreeView.Items.Add(allBookmarksItem);

        // Add folder items
        foreach (var folder in _folders.OrderBy(f => f))
        {
            var folderItem = new TreeViewItem
            {
                Header = $"{folder} ({_bookmarks.Count(b => b.FolderPath == folder)})",
                Tag = folder
            };
            allBookmarksItem.Items.Add(folderItem);
        }

        // Select "All Bookmarks" by default
        allBookmarksItem.IsSelected = true;
    }

    #endregion

    #region Filtering and Search

    private void ApplyFilters()
    {
        _filteredBookmarks.Clear();

        var searchTerm = SearchTextBox.Text?.ToLower().Trim() ?? "";
        var selectedFolderFilter = FolderFilterComboBox.SelectedItem?.ToString();

        foreach (var bookmark in _bookmarks)
        {
            bool matchesSearch = string.IsNullOrEmpty(searchTerm) ||
                               bookmark.Title.ToLower().Contains(searchTerm) ||
                               bookmark.Url.ToLower().Contains(searchTerm) ||
                               (bookmark.Description?.ToLower().Contains(searchTerm) ?? false);

            bool matchesFolder = selectedFolderFilter == "All Folders" ||
                               selectedFolderFilter == null ||
                               bookmark.FolderPath == selectedFolderFilter;

            bool matchesSelectedFolder = _selectedFolder == null ||
                                       bookmark.FolderPath == _selectedFolder;

            if (matchesSearch && matchesFolder && matchesSelectedFolder)
            {
                _filteredBookmarks.Add(bookmark);
            }
        }

        UpdateStatus($"Showing {_filteredBookmarks.Count} of {_bookmarks.Count} bookmarks");
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void FolderFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = "";
        FolderFilterComboBox.SelectedIndex = 0;
        _selectedFolder = null;
        
        // Clear tree selection
        foreach (TreeViewItem item in FolderTreeView.Items)
        {
            item.IsSelected = false;
            foreach (TreeViewItem subItem in item.Items)
            {
                subItem.IsSelected = false;
            }
        }
        
        ApplyFilters();
    }

    private void FolderTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeViewItem selectedItem)
        {
            _selectedFolder = selectedItem.Tag as string;
            ApplyFilters();
        }
    }

    #endregion

    #region Bookmark Management

    private async void AddBookmarkButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new BookmarkEditDialog(_folders, _selectedFolder);
        if (dialog.ShowDialog() == true)
        {
            try
            {
                await _bookmarkService.AddBookmarkAsync(_userProfileId, dialog.BookmarkTitle, 
                    dialog.BookmarkUrl, dialog.BookmarkDescription, dialog.BookmarkFolder);
                
                await LoadBookmarksAsync();
                await LoadFoldersAsync();
                UpdateStatus("Bookmark added successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding bookmark: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void EditBookmarkButton_Click(object sender, RoutedEventArgs e)
    {
        if (BookmarkDataGrid.SelectedItem is Bookmark selectedBookmark)
        {
            var dialog = new BookmarkEditDialog(_folders, selectedBookmark);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _bookmarkService.UpdateBookmarkAsync(selectedBookmark.Id, 
                        dialog.BookmarkTitle, dialog.BookmarkUrl, 
                        dialog.BookmarkDescription, dialog.BookmarkFolder);
                    
                    await LoadBookmarksAsync();
                    await LoadFoldersAsync();
                    UpdateStatus("Bookmark updated successfully");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating bookmark: {ex.Message}", "Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Please select a bookmark to edit.", "No Selection", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void DeleteBookmarkButton_Click(object sender, RoutedEventArgs e)
    {
        if (BookmarkDataGrid.SelectedItem is Bookmark selectedBookmark)
        {
            var result = MessageBox.Show($"Are you sure you want to delete the bookmark '{selectedBookmark.Title}'?", 
                                       "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _bookmarkService.DeleteBookmarkAsync(selectedBookmark.Id);
                    await LoadBookmarksAsync();
                    await LoadFoldersAsync();
                    UpdateStatus("Bookmark deleted successfully");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting bookmark: {ex.Message}", "Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Please select a bookmark to delete.", "No Selection", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BookmarkDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = BookmarkDataGrid.SelectedItem != null;
        EditBookmarkButton.IsEnabled = hasSelection;
        DeleteBookmarkButton.IsEnabled = hasSelection;
    }

    private void BookmarkDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (BookmarkDataGrid.SelectedItem is Bookmark selectedBookmark)
        {
            // Navigate to bookmark URL in main browser window
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = selectedBookmark.Url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening bookmark: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Folder Management

    private async void AddFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FolderEditDialog();
        if (dialog.ShowDialog() == true)
        {
            var folderName = dialog.FolderName.Trim();
            if (!_folders.Contains(folderName))
            {
                // Add a placeholder bookmark to create the folder
                try
                {
                    await _bookmarkService.AddBookmarkAsync(_userProfileId, "New Bookmark", 
                        "https://example.com", "Placeholder bookmark", folderName);
                    
                    await LoadFoldersAsync();
                    UpdateStatus($"Folder '{folderName}' created successfully");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating folder: {ex.Message}", "Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("A folder with this name already exists.", "Duplicate Folder", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private async void RenameFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (FolderTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is string folderName)
        {
            var dialog = new FolderEditDialog(folderName);
            if (dialog.ShowDialog() == true)
            {
                var newFolderName = dialog.FolderName.Trim();
                if (newFolderName != folderName && !_folders.Contains(newFolderName))
                {
                    try
                    {
                        await _bookmarkService.RenameFolderAsync(_userProfileId, folderName, newFolderName);
                        await LoadBookmarksAsync();
                        await LoadFoldersAsync();
                        UpdateStatus($"Folder renamed to '{newFolderName}' successfully");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming folder: {ex.Message}", "Error", 
                                      MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if (_folders.Contains(newFolderName))
                {
                    MessageBox.Show("A folder with this name already exists.", "Duplicate Folder", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
        else
        {
            MessageBox.Show("Please select a folder to rename.", "No Selection", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private async void DeleteFolderButton_Click(object sender, RoutedEventArgs e)
    {
        if (FolderTreeView.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag is string folderName)
        {
            var bookmarksInFolder = _bookmarks.Where(b => b.FolderPath == folderName).Count();
            
            var result = MessageBox.Show($"Are you sure you want to delete the folder '{folderName}'?\n" +
                                       $"This will move {bookmarksInFolder} bookmark(s) to the 'Default' folder.", 
                                       "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _bookmarkService.DeleteFolderAsync(_userProfileId, folderName, "Default");
                    await LoadBookmarksAsync();
                    await LoadFoldersAsync();
                    UpdateStatus($"Folder '{folderName}' deleted successfully");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting folder: {ex.Message}", "Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("Please select a folder to delete.", "No Selection", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    #endregion

    #region Import/Export

    private async void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Import Bookmarks",
            Filter = "HTML Files (*.html)|*.html|JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                var content = await File.ReadAllTextAsync(openFileDialog.FileName);
                ImportResult result;

                if (openFileDialog.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    result = await _bookmarkService.ImportBookmarksFromJsonAsync(_userProfileId, content);
                }
                else
                {
                    result = await _bookmarkService.ImportBookmarksFromHtmlAsync(_userProfileId, content);
                }

                var message = $"Import completed:\n" +
                            $"• Imported: {result.ImportedCount}\n" +
                            $"• Duplicates skipped: {result.DuplicateCount}\n" +
                            $"• Errors: {result.ErrorCount}";

                if (result.HasErrors)
                {
                    message += $"\n\nErrors:\n{string.Join("\n", result.Errors)}";
                }

                MessageBox.Show(message, "Import Results", MessageBoxButton.OK, 
                              result.HasErrors ? MessageBoxImage.Warning : MessageBoxImage.Information);

                await LoadBookmarksAsync();
                await LoadFoldersAsync();
                UpdateStatus($"Import completed: {result.ImportedCount} bookmarks imported");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing bookmarks: {ex.Message}", "Import Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Title = "Export Bookmarks",
            Filter = "HTML Files (*.html)|*.html|JSON Files (*.json)|*.json",
            FilterIndex = 1,
            FileName = $"bookmarks_export_{DateTime.Now:yyyyMMdd_HHmmss}"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                string content;

                if (saveFileDialog.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    content = await _bookmarkService.ExportBookmarksToJsonAsync(_userProfileId);
                }
                else
                {
                    content = await _bookmarkService.ExportBookmarksToHtmlAsync(_userProfileId);
                }

                await File.WriteAllTextAsync(saveFileDialog.FileName, content);

                MessageBox.Show($"Bookmarks exported successfully to:\n{saveFileDialog.FileName}", 
                              "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                
                UpdateStatus($"Bookmarks exported to {Path.GetFileName(saveFileDialog.FileName)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting bookmarks: {ex.Message}", "Export Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region UI Helpers

    private void UpdateStatus(string message)
    {
        StatusTextBlock.Text = message;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion
}
