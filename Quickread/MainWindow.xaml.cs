using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using QuickRead.Models;
using QuickRead.Sources;

namespace QuickRead
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer spinnerTimer;
        private readonly Dictionary<string, ISourceService> _sources;
        private List<Manga> _currentMangaList = new();
        private List<Chapter> _currentChapterList = new();

        public MainWindow()
        {
            InitializeComponent();

            // Initialize services
            _sources = new Dictionary<string, ISourceService>
            {
                { "Comick", new ComickService() },
                { "MangaDX", new MangaDexService() }
            };

            // Placeholder-Text verstecken, wenn TextBox gefüllt ist
            SearchBox.TextChanged += SearchBox_TextChanged;

            // Enter-Taste im Suchfeld auslösen
            SearchBox.KeyDown += SearchBox_KeyDown;

            // Spinner Animation Timer
            spinnerTimer = new DispatcherTimer();
            spinnerTimer.Interval = TimeSpan.FromMilliseconds(30);
            spinnerTimer.Tick += SpinnerTimer_Tick;

            HidePlaceholderIfNeeded();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            HidePlaceholderIfNeeded();
        }

        private void HidePlaceholderIfNeeded()
        {
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(SearchBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SearchButton_Click(this, new RoutedEventArgs());
            }
        }

        private double spinnerAngle = 0;
        private void SpinnerTimer_Tick(object? sender, EventArgs e)
        {
            spinnerAngle += 10;
            if (spinnerAngle >= 360) spinnerAngle = 0;
            SpinnerRotate.Angle = spinnerAngle;
        }

        private void ShowLoading(bool show)
        {
            LoadingGrid.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (show) spinnerTimer.Start();
            else spinnerTimer.Stop();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var query = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                SetStatus("Please enter a search term.");
                return;
            }

            var selectedSource = ((ComboBoxItem)SourceComboBox.SelectedItem)?.Content?.ToString();
            if (string.IsNullOrEmpty(selectedSource))
            {
                SetStatus("Please select a valid source.");
                return;
            }

            SetStatus($"Searching for \"{query}\" in {selectedSource}...");
            ShowLoading(true);

            try
            {
                if (_sources.TryGetValue(selectedSource!, out var service)) // "!" added to ensure non-null
                {
                    _currentMangaList = await service.SearchMangaAsync(query);

                    Dispatcher.Invoke(() =>
                    {
                        MangaList.ItemsSource = null;
                        MangaList.ItemsSource = _currentMangaList;

                        ChapterList.ItemsSource = null;
                        _currentChapterList.Clear();

                        ShowLoading(false);
                        SetStatus($"Found {_currentMangaList.Count} results for \"{query}\".");
                    });
                }
                else
                {
                    ShowLoading(false);
                    SetStatus("Unknown source selected.");
                }
            }
            catch (Exception ex)
            {
                ShowLoading(false);
                SetStatus($"Error during search: {ex.Message}");
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selected && selected.Tag is not null)
            {
                string lang = selected.Tag.ToString() ?? string.Empty; // Sicherstellen, dass kein NULL-Wert zugewiesen wird
                SetStatus($"Language changed to {selected.Content} ({lang}).");

                // Filter current chapter list by language if available
                if (_currentChapterList.Any())
                {
                    var filteredChapters = _currentChapterList.Where(c =>
                        string.IsNullOrEmpty(c.Language) || c.Language == lang).ToList();
                    ChapterList.ItemsSource = null;
                    ChapterList.ItemsSource = filteredChapters;
                }
            }
        }

        private async void MangaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MangaList.SelectedItem is Manga selectedManga)
            {
                SetStatus($"Loading chapters for {selectedManga.Title}...");
                ShowLoading(true);

                try
                {
                    var selectedSource = ((ComboBoxItem)SourceComboBox.SelectedItem)?.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedSource) && _sources.TryGetValue(selectedSource!, out var service)) // "!" added
                    {
                        _currentChapterList = await service.GetChaptersAsync(selectedManga);

                        var selectedLang = ((ComboBoxItem)LanguageComboBox.SelectedItem)?.Tag?.ToString();
                        var filteredChapters = _currentChapterList.Where(c =>
                            string.IsNullOrEmpty(c.Language) || c.Language == selectedLang).ToList();

                        Dispatcher.Invoke(() =>
                        {
                            ChapterList.ItemsSource = null;
                            ChapterList.ItemsSource = filteredChapters;

                            ShowLoading(false);
                            SetStatus($"Loaded {filteredChapters.Count} chapters for {selectedManga.Title}.");
                        });
                    }
                }
                catch (Exception ex)
                {
                    ShowLoading(false);
                    SetStatus($"Error loading chapters: {ex.Message}");
                }
            }
        }

        private async void ChapterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChapterList.SelectedItem is Chapter selectedChapter)
            {
                SetStatus($"Loading pages for {selectedChapter.Title}...");
                ShowLoading(true);

                try
                {
                    var selectedSource = ((ComboBoxItem)SourceComboBox.SelectedItem)?.Content?.ToString();
                    if (!string.IsNullOrEmpty(selectedSource) && _sources.TryGetValue(selectedSource, out var service))
                    {
                        var pageImages = await service.GetPageImagesAsync(selectedChapter);

                        if (pageImages.Any())
                        {
                            Dispatcher.Invoke(() =>
                            {
                                ShowLoading(false);
                                SetStatus($"Opening reader with {pageImages.Count} pages...");

                                var readerWindow = new ReaderWindow(pageImages);
                                readerWindow.Show();
                            });
                        }
                        else
                        {
                            ShowLoading(false);
                            SetStatus("No pages found for this chapter.");
                        }
                    }
                    else
                    {
                        ShowLoading(false);
                        SetStatus("Invalid or unknown source selected.");
                    }
                }
                catch (Exception ex)
                {
                    ShowLoading(false);
                    SetStatus($"Error loading pages: {ex.Message}");
                }
            }
        }

        private void SetStatus(string message)
        {
            if (StatusText != null)
            {
                StatusText.Text = message;
            }
        }

        private void SourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}