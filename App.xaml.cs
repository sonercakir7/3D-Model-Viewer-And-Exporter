// Soner Çakır tarafından yapıldı
// MIT License

using System;
using System.Windows;

namespace ThreeDModelViewerAndExporter
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            try {
                SetDefaultLanguage();
            } catch (Exception ex) {
                // Hata olsa bile uygulamanın açılmasını engelleme
                Console.WriteLine("Dil yükleme hatası: " + ex.Message);
            }
        }

        private void SetDefaultLanguage()
        {
            try {
                string culture = System.Globalization.CultureInfo.CurrentCulture.Name;
                if (culture.StartsWith("tr")) ChangeLanguage("tr-TR");
                else if (culture.StartsWith("de")) ChangeLanguage("de-DE");
                else if (culture.StartsWith("fr")) ChangeLanguage("fr-FR");
                else ChangeLanguage("en-US");
            } catch {
                ChangeLanguage("en-US");
            }
        }

        public void ChangeLanguage(string cultureCode)
        {
            try {
                ResourceDictionary dict = new ResourceDictionary();
                dict.Source = new Uri($"/ThreeDModelViewerAndExporter;component/Resources/Languages/{cultureCode}.xaml", UriKind.RelativeOrAbsolute);

                ResourceDictionary? oldDict = null;
                foreach (var d in Resources.MergedDictionaries)
                {
                    if (d.Source != null && d.Source.OriginalString.Contains("Resources/Languages/"))
                    {
                        oldDict = d;
                        break;
                    }
                }

                if (oldDict != null)
                {
                    int index = Resources.MergedDictionaries.IndexOf(oldDict);
                    Resources.MergedDictionaries[index] = dict;
                }
                else
                {
                    Resources.MergedDictionaries.Add(dict);
                }
            } catch { }
        }

        public void ChangeTheme(bool isDark)
        {
            try {
                string themeName = isDark ? "Dark" : "Light";
                ResourceDictionary dict = new ResourceDictionary();
                dict.Source = new Uri($"/ThreeDModelViewerAndExporter;component/Resources/Themes/{themeName}.xaml", UriKind.RelativeOrAbsolute);

                ResourceDictionary? oldTheme = null;
                foreach (var d in Resources.MergedDictionaries)
                {
                    if (d.Source != null && d.Source.OriginalString.Contains("Resources/Themes/"))
                    {
                        oldTheme = d;
                        break;
                    }
                }

                if (oldTheme != null)
                {
                    int index = Resources.MergedDictionaries.IndexOf(oldTheme);
                    Resources.MergedDictionaries[index] = dict;
                }
                else
                {
                    Resources.MergedDictionaries.Add(dict);
                }
            } catch { }
        }
    }
}
