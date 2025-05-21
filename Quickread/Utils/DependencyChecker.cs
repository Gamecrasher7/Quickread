using System;
using System.Diagnostics;
using System.Windows;

namespace QuickRead.Utils
{
    public static class DependencyChecker
    {
        // Beispiel: Pr�fe ob .NET Framework 4.8 oder h�her installiert ist
        // (Hier kann man auch andere Checks einbauen wie z.B. f�r bestimmte Programme)

        public static bool CheckDotNetVersion()
        {
            // F�r Demo: Einfach true zur�ckgeben
            // In real: Pr�fung via Registry oder API

            // Beispiel: Hinweis ausgeben
            if (!IsDotNet48Installed())
            {
                MessageBox.Show("Bitte .NET Framework 4.8 oder h�her installieren.", "Abh�ngigkeit fehlt", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private static bool IsDotNet48Installed()
        {
            // Dummy-Implementierung, hier kannst du Registry-Abfragen oder andere Methoden nutzen
            // z.B. Registry-Key pr�fen:
            // HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full Release >= 528040

            return true;
        }
    }
}
