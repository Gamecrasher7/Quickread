using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace QuickRead.Views.Controls
{
    /// <summary>
    /// Interaction logic for LoadingSpinner.xaml
    /// </summary>
    public partial class LoadingSpinner : UserControl
    {
        private Storyboard? _spinnerStoryboard;
        private bool _isSpinning;

        public LoadingSpinner()
        {
            InitializeComponent();
            Loaded += LoadingSpinner_Loaded;
            Unloaded += LoadingSpinner_Unloaded;
        }

        /// <summary>
        /// Gets or sets whether the spinner is currently spinning
        /// </summary>
        public bool IsSpinning
        {
            get => _isSpinning;
            set
            {
                if (_isSpinning != value)
                {
                    _isSpinning = value;
                    UpdateSpinnerState();
                }
            }
        }

        /// <summary>
        /// Gets or sets the spin speed in seconds for a full rotation
        /// </summary>
        public double SpinDuration { get; set; } = 1.0;

        private void LoadingSpinner_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isSpinning)
            {
                StartSpinner();
            }
        }

        private void LoadingSpinner_Unloaded(object sender, RoutedEventArgs e)
        {
            StopSpinner();
        }

        private void UpdateSpinnerState()
        {
            if (_isSpinning && IsLoaded)
            {
                StartSpinner();
            }
            else
            {
                StopSpinner();
            }
        }

        private void StartSpinner()
        {
            if (_spinnerStoryboard != null)
                return;

            // Create rotation animation
            var rotateAnimation = new DoubleAnimation
            {
                From = 0,
                To = 360,
                Duration = new Duration(System.TimeSpan.FromSeconds(SpinDuration)),
                RepeatBehavior = RepeatBehavior.Forever
            };

            // Set animation target
            Storyboard.SetTargetName(rotateAnimation, "SpinnerRotate");
            Storyboard.SetTargetProperty(rotateAnimation, new PropertyPath("Angle"));

            // Create and start storyboard
            _spinnerStoryboard = new Storyboard();
            _spinnerStoryboard.Children.Add(rotateAnimation);
            _spinnerStoryboard.Begin(this);
        }

        private void StopSpinner()
        {
            if (_spinnerStoryboard != null)
            {
                _spinnerStoryboard.Stop(this);
                _spinnerStoryboard = null;
            }
        }

        /// <summary>
        /// Manually start the spinner animation
        /// </summary>
        public void Start()
        {
            IsSpinning = true;
        }

        /// <summary>
        /// Manually stop the spinner animation
        /// </summary>
        public void Stop()
        {
            IsSpinning = false;
        }
    }
}