using NoorAhlulBayt.Common.Data;
using NoorAhlulBayt.Common.Models;
using NoorAhlulBayt.Common.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NoorAhlulBayt.Browser;

public partial class HistoryWindow : Window
{
    private readonly ApplicationDbContext _context;
    private readonly HistoryService _historyService;
    private readonly int _userProfileId;
    private ObservableCollection<BrowsingHistory> _history;
    private ObservableCollection<BrowsingHistory> _filteredHistory;
    private HistoryStatistics? _statistics;

    public HistoryWindow(ApplicationDbContext context, int userProfileId)
    {
        InitializeComponent();
        _context = context;
        _historyService = new HistoryService(context);
        _userProfileId = userProfileId;
        _history = new ObservableCollection<BrowsingHistory>();
        _filteredHistory = new ObservableCollection<BrowsingHistory>();

        HistoryDataGrid.ItemsSource = _filteredHistory;
        
        InitializeFilters();
        Loaded += HistoryWindow_Loaded;
    }

    private void InitializeFilters()
    {
        // Date filter options
        DateFilterComboBox.Items.Add("All Time");
        DateFilterComboBox.Items.Add("Today");
        DateFilterComboBox.Items.Add("Yesterday");
        DateFilterComboBox.Items.Add("Last 7 Days");
        DateFilterComboBox.Items.Add("Last 30 Days");
        DateFilterComboBox.SelectedIndex = 0;

        // Type filter options
        TypeFilterComboBox.Items.Add("All");
        TypeFilterComboBox.Items.Add("Normal");
        TypeFilterComboBox.Items.Add("Blocked");
        TypeFilterComboBox.Items.Add("Incognito");
        TypeFilterComboBox.SelectedIndex = 0;
    }

    private async void HistoryWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadHistoryAsync();
        await LoadStatisticsAsync();
        UpdateStatus("Ready");
    }

    #region Data Loading

    private async Task LoadHistoryAsync()
    {
        try
        {
            var history = await _historyService.GetHistoryAsync(_userProfileId, 1000, 1, true); // Load up to 1000 entries
            _history.Clear();
            
            foreach (var entry in history)
            {
                _history.Add(entry);
            }

            ApplyFilters();
            UpdateStatus($"Loaded {history.Count} history entries");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading history: {ex.Message}", "Error", 
                          MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("Error loading history");
        }
    }

    private async Task LoadStatisticsAsync()
    {
        try
        {
            _statistics = await _historyService.GetHistoryStatisticsAsync(_userProfileId, true);
            UpdateStatisticsDisplay();
        }
        catch (Exception ex)
        {
            StatisticsTextBlock.Text = "Error loading statistics";
        }
    }

    private void UpdateStatisticsDisplay()
    {
        if (_statistics != null)
        {
            var dateRange = "";
            if (_statistics.OldestEntry.HasValue && _statistics.NewestEntry.HasValue)
            {
                dateRange = $" • From {_statistics.OldestEntry.Value:MM/dd/yyyy} to {_statistics.NewestEntry.Value:MM/dd/yyyy}";
            }

            StatisticsTextBlock.Text = $"Total: {_statistics.TotalEntries} entries • " +
                                     $"Visits: {_statistics.TotalVisits} • " +
                                     $"Unique URLs: {_statistics.UniqueUrls} • " +
                                     $"Blocked: {_statistics.BlockedEntries}{dateRange}";
        }
    }

    #endregion

    #region Filtering and Search

    private void ApplyFilters()
    {
        _filteredHistory.Clear();

        var searchTerm = SearchTextBox.Text?.ToLower().Trim() ?? "";
        var dateFilter = DateFilterComboBox.SelectedItem?.ToString() ?? "All Time";
        var typeFilter = TypeFilterComboBox.SelectedItem?.ToString() ?? "All";

        foreach (var entry in _history)
        {
            bool matchesSearch = string.IsNullOrEmpty(searchTerm) ||
                               entry.Title.ToLower().Contains(searchTerm) ||
                               entry.Url.ToLower().Contains(searchTerm);

            bool matchesDate = MatchesDateFilter(entry, dateFilter);
            bool matchesType = MatchesTypeFilter(entry, typeFilter);

            if (matchesSearch && matchesDate && matchesType)
            {
                _filteredHistory.Add(entry);
            }
        }

        UpdateStatus($"Showing {_filteredHistory.Count} of {_history.Count} history entries");
    }

    private bool MatchesDateFilter(BrowsingHistory entry, string dateFilter)
    {
        var now = DateTime.Now;
        var entryDate = entry.VisitedAt;

        return dateFilter switch
        {
            "Today" => entryDate.Date == now.Date,
            "Yesterday" => entryDate.Date == now.Date.AddDays(-1),
            "Last 7 Days" => entryDate >= now.AddDays(-7),
            "Last 30 Days" => entryDate >= now.AddDays(-30),
            _ => true // All Time
        };
    }

    private bool MatchesTypeFilter(BrowsingHistory entry, string typeFilter)
    {
        return typeFilter switch
        {
            "Normal" => !entry.WasBlocked && !entry.IsIncognito,
            "Blocked" => entry.WasBlocked,
            "Incognito" => entry.IsIncognito,
            _ => true // All
        };
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void DateFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilters();
    }

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Text = "";
        DateFilterComboBox.SelectedIndex = 0;
        TypeFilterComboBox.SelectedIndex = 0;
        ApplyFilters();
    }

    #endregion

    #region History Management

    private async void DeleteSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedEntries = HistoryDataGrid.SelectedItems.Cast<BrowsingHistory>().ToList();
        
        if (!selectedEntries.Any())
        {
            MessageBox.Show("Please select history entries to delete.", "No Selection", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var result = MessageBox.Show($"Are you sure you want to delete {selectedEntries.Count} history entries?", 
                                   "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                int deletedCount = 0;
                foreach (var entry in selectedEntries)
                {
                    if (await _historyService.DeleteHistoryEntryAsync(entry.Id))
                    {
                        deletedCount++;
                    }
                }

                await LoadHistoryAsync();
                await LoadStatisticsAsync();
                UpdateStatus($"Deleted {deletedCount} history entries");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting history entries: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ClearTodayButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to clear today's browsing history?", 
                                   "Confirm Clear", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var deletedCount = await _historyService.ClearHistoryByDateRangeAsync(_userProfileId, today, tomorrow, true);

                await LoadHistoryAsync();
                await LoadStatisticsAsync();
                UpdateStatus($"Cleared {deletedCount} entries from today's history");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing today's history: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to clear ALL browsing history?\n\nThis action cannot be undone.", 
                                   "Confirm Clear All", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        
        if (result == MessageBoxResult.Yes)
        {
            // Double confirmation for clearing all history
            var confirmResult = MessageBox.Show("This will permanently delete all browsing history.\n\nAre you absolutely sure?", 
                                              "Final Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (confirmResult == MessageBoxResult.Yes)
            {
                try
                {
                    var deletedCount = await _historyService.ClearAllHistoryAsync(_userProfileId, true);

                    await LoadHistoryAsync();
                    await LoadStatisticsAsync();
                    UpdateStatus($"Cleared all history - {deletedCount} entries deleted");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing all history: {ex.Message}", "Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void HistoryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DeleteSelectedButton.IsEnabled = HistoryDataGrid.SelectedItems.Count > 0;
    }

    private void HistoryDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (HistoryDataGrid.SelectedItem is BrowsingHistory selectedEntry)
        {
            // Navigate to history URL in main browser window
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = selectedEntry.Url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening URL: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadHistoryAsync();
        await LoadStatisticsAsync();
        UpdateStatus("History refreshed");
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
