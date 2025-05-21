using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

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
            _imageUrls = imageUrls;
            LoadImage();
        }

        private void LoadImage()
        {
            if (_imageUrls == null || _imageUrls.Count == 0) return;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new System.Uri(_imageUrls[_currentIndex], System.UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            MangaImage.Source = bitmap;
        }

        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                LoadImage();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _imageUrls.Count - 1)
            {
                _currentIndex++;
                LoadImage();
            }
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            _zoomLevel += 0.1;
            ImageScale.ScaleX = _zoomLevel;
            ImageScale.ScaleY = _zoomLevel;
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            if (_zoomLevel > 0.2)
            {
                _zoomLevel -= 0.1;
                ImageScale.ScaleX = _zoomLevel;
                ImageScale.ScaleY = _zoomLevel;
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _uiVisible = !_uiVisible;
            TopBar.Visibility = _uiVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
