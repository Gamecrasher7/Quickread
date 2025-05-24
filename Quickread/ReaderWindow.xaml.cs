using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Threading.Tasks;

namespace QuickRead
{
    public partial class ReaderWindow : Window
    {
        private readonly List<string> _imageUrls;
        private int _currentIndex = 0;
        private double _zoomLevel = 1.0;
        private bool _uiVisible = true;

        public ReaderWindow(List<string> imageUrls)
        {
            InitializeComponent();
            _imageUrls = imageUrls ?? new List<string>();

            if (_imageUrls.Any())
            {
                LoadImageAsync().ConfigureAwait(false);
                Title = $"Reader - Page {_currentIndex + 1} of {_imageUrls.Count}";
            }
            else
            {
                Title = "Reader - No Pages";
                MessageBox.Show("No pages to display!", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            // Keyboard shortcuts
            KeyDown += ReaderWindow_KeyDown;
        }

        private void ReaderWindow_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                case Key.A:
                    Prev_Click(this, new RoutedEventArgs());
                    break;
                case Key.Right:
                case Key.D:
                    Next_Click(this, new RoutedEventArgs());
                    break;
                case Key.Add:
                case Key.OemPlus:
                    ZoomIn_Click(this, new RoutedEventArgs());
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    ZoomOut_Click(this, new RoutedEventArgs());
                    break;
                case Key.Escape:
                    Close();
                    break;
                case Key.F11:
                    ToggleFullscreen();
                    break;
            }
        }

        private void ToggleFullscreen()
        {
            if (WindowState == WindowState.Maximized && WindowStyle == WindowStyle.None)
            {
                WindowState = WindowState.Normal;
                WindowStyle = WindowStyle.SingleBorderWindow;
            }
            else
            {
                WindowState = WindowState.Maximized;
                WindowStyle = WindowStyle.None;
            }
        }

             private async Task LoadImageAsync()
        {
            if (_imageUrls == null || !_imageUrls.Any() || _currentIndex < 0 || _currentIndex >= _imageUrls.Count)
                return;

            try
            {
                var imageUrl = _imageUrls[_currentIndex];

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imageUrl, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bitmap.EndInit();

                MangaImage.Source = bitmap;
                Title = $"Reader - Page {_currentIndex + 1} of {_imageUrls.Count}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Try to skip to next image if current one fails
                if (_currentIndex < _imageUrls.Count - 1)
                {
                    _currentIndex++;
                    await LoadImageAsync();
                }
            }
        }

        private async void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                await LoadImageAsync();
            }
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _imageUrls.Count - 1)
            {
                _currentIndex++;
                await LoadImageAsync();
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            if (_zoomLevel < 5.0) // Max zoom limit
            {
                _zoomLevel += 0.2;
                ImageScale.ScaleX = _zoomLevel;
                ImageScale.ScaleY = _zoomLevel;
            }
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (_zoomLevel > 0.2) // Min zoom limit
            {
                _zoomLevel -= 0.2;
                ImageScale.ScaleX = _zoomLevel;
                ImageScale.ScaleY = _zoomLevel;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _uiVisible = !_uiVisible;
            TopBar.Visibility = _uiVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        protected override void OnClosed(EventArgs e)
        {
            // Clean up resources
            MangaImage.Source = null;
            base.OnClosed(e);
        }
    }
}