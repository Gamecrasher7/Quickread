using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace QuickRead.Utils
{
    public static class LanguageManager
    {
        private static readonly Dictionary<string, Uri> _languageDictionaries = new()
        {
            { "de", new Uri("/Resources/StringResources.xaml", UriKind.Relative) },
            { "en", new Uri("/Resources/StringResources.xaml", UriKind.Relative) }
        };

        public static void ChangeLanguage(string cultureCode)
        {
            try
            {
                if (string.IsNullOrEmpty(cultureCode))
                    cultureCode = "en";

                // Set thread culture
                var culture = new CultureInfo(cultureCode);
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;

                // For now, we only have one string resource file
                // In a full implementation, you would have separate files for each language
                var dictUri = new Uri("/Resources/StringResources.xaml", UriKind.Relative);

                // Remove existing language dictionaries
                var existingDict = FindLanguageDictionary();
                if (existingDict != null)
                {
                    Application.Current.Resources.MergedDictionaries.Remove(existingDict);
                }

                // Load new language dictionary
                try
                {
                    var newDict = new ResourceDictionary() { Source = dictUri };
                    Application.Current.Resources.MergedDictionaries.Add(newDict);
                }
                catch (Exception ex)
                {
                    // If loading fails, continue without crashing
                    Console.WriteLine($"Could not load language dictionary: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error changing language: {ex.Message}");
            }
        }

        private static ResourceDictionary? FindLanguageDictionary()
        {
            try
            {
                foreach (var dict in Application.Current.Resources.MergedDictionaries)
                {
                    if (dict.Source != null && dict.Source.OriginalString.Contains("StringResources"))
                        return dict;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finding language dictionary: {ex.Message}");
            }
            return null;
        }

        public static string GetString(string key)
        {
            try
            {
                if (Application.Current.Resources.Contains(key))
                {
                    return Application.Current.Resources[key] as string ?? key;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting string resource: {ex.Message}");
            }
            return key; // Return key if resource not found
        }
    }
}