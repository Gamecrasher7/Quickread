using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using QuickRead.Utils;

namespace Quickread
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Global exception handling
            SetupExceptionHandling();

            // Initialize default language
            InitializeLanguage();

            // Set up HTTP client defaults
            SetupHttpDefaults();

            base.OnStartup(e);
        }

        private void SetupExceptionHandling()
        {
            // Handle unhandled exceptions on UI thread
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            // Handle unhandled exceptions on background threads
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Handle unhandled exceptions in async/await operations
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void InitializeLanguage()
        {
            try
            {
                // Set default language to English
                LanguageManager.ChangeLanguage("en");
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                LogError("Failed to initialize language", ex);
            }
        }

        private void SetupHttpDefaults()
        {
            // Set up default HTTP client settings
            System.Net.ServicePointManager.DefaultConnectionLimit = 10;
            System.Net.ServicePointManager.Expect100Continue = false;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogError("Unhandled UI exception", e.Exception);

            // Show user-friendly error message
            MessageBox.Show(
                $"An unexpected error occurred:\n{e.Exception.Message}\n\nThe application will continue running.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            // Mark as handled to prevent crash
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogError("Unhandled domain exception", ex);

                // Show critical error message
                MessageBox.Show(
                    $"A critical error occurred:\n{ex.Message}\n\nThe application will exit.",
                    "Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogError("Unhandled task exception", e.Exception);

            // Mark as observed to prevent crash
            e.SetObserved();
        }

        private void LogError(string message, Exception ex)
        {
            try
            {
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "QuickRead", "Logs");

                Directory.CreateDirectory(logDir);

                var logFile = Path.Combine(logDir, $"error_{DateTime.Now:yyyyMMdd}.log");
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n{ex}\n\n";

                File.AppendAllText(logFile, logEntry);
            }
            catch
            {
                // Ignore logging errors to prevent infinite loops
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Clean up resources here if needed
            base.OnExit(e);
        }
    }
}