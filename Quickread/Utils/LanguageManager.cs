using System;
using System.Collections.Generic;
using System.Windows;

namespace QuickRead.Utils
{
    public static class LanguageManager
    {
        private static readonly Dictionary<string, Uri> _languageDictionaries = new()
        {
            { "de", new Uri("/Resources/StringResources.de.xaml", UriKind.Relative) },
            { "en", new Uri("/Resources/StringResources.en.xaml", UriKind.Relative) }
        };

        public static void ChangeLanguage(string cultureCode)
        {
            if (!_languageDictionaries.ContainsKey(cultureCode))
                throw new ArgumentException("Unsupported language code");

            var dictUri = _languageDictionaries[cultureCode];

            // Entferne alte Sprachdateien
            var existingDict = FindLanguageDictionary();
            if (existingDict != null)
                Application.Current.Resources.MergedDictionaries.Remove(existingDict);

            // Lade neue Sprachdatei
            var newDict = new ResourceDictionary() { Source = dictUri };
            Application.Current.Resources.MergedDictionaries.Add(newDict);
        }

        private static ResourceDictionary? FindLanguageDictionary()
        {
            foreach (var dict in Application.Current.Resources.MergedDictionaries)
            {
                if (dict.Source != null && dict.Source.OriginalString.Contains("StringResources"))
                    return dict;
            }
            return null;
        }
    }
}
