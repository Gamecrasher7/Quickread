using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace QuickRead
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer spinnerTimer;

        public MainWindow()
        {
            InitializeComponent();

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
                SearchButton_Click(null, null);
            }
        }

        private double spinnerAngle = 0;
        private void SpinnerTimer_Tick(object sender, EventArgs e)
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

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var query = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                SetStatus("Please enter a search term.");
                return;
            }

            SetStatus($"Searching for \"{query}\" in {((ComboBoxItem)SourceComboBox.SelectedItem).Content}...");
            ShowLoading(true);

            // Beispiel: Async Suche simulieren (hier ersetzt du mit deinem Suchcode)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // TODO: Hier tatsächliche Suche starten

                ShowLoading(false);
                SetStatus($"Search finished. Results for \"{query}\".");
            }), DispatcherPriority.Background);
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selected)
            {
                string lang = selected.Tag.ToString();
                SetStatus($"Language changed to {selected.Content} ({lang}).");

                // TODO: Sprachwechsel-Logik hier implementieren
            }
        }

        private void MangaList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Manga Auswahl behandeln
        }

        private void ChapterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // TODO: Chapter Auswahl behandeln
        }

        private void SetStatus(string message)
        {
            StatusText.Text = message;
        }
    }
}
