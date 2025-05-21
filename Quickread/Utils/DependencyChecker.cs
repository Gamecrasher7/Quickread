using System;
using System.Diagnostics;
using System.Windows;

namespace QuickRead.Utils
{
    public static class DependencyChecker
    {
        // Beispiel: Prüfe ob .NET Framework 4.8 oder höher installiert ist
        // (Hier kann man auch andere Checks einbauen wie z.B. für bestimmte Programme)

        public static bool CheckDotNetVersion()
        {
            // Für Demo: Einfach true zurückgeben
            // In real: Prüfung via Registry oder API

            // Beispiel: Hinweis ausgeben
            if (!IsDotNet48Installed())
            {
                MessageBox.Show("Bitte .NET Framework 4.8 oder höher installieren.", "Abhängigkeit fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private static bool IsDotNet48Installed()
        {
            // Dummy-Implementierung, hier kannst du Registry-Abfragen oder andere Methoden nutzen
            // z.B. Registry-Key prüfen:
            // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full Release >= 528040

            return true;
        }
    }
}
