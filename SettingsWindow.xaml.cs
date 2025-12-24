using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ThreeDModelViewerAndExporter
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            SetCurrentLanguageInComboBox();
            LoadCurrentSettings();
        }

        private void LoadCurrentSettings()
        {
            var mw = Owner as MainWindow;
            if (mw != null)
            {
                FPSCheckBox.IsChecked = mw.ShowFPS;
                SenseSlider.Value = mw.viewPort.RotationSensitivity / 1.0; // Varsayılan 1.0 varsayarak
                GridOpacitySlider.Value = mw.gridLines.Fill.Opacity;
            }
        }

        private void SetCurrentLanguageInComboBox()
        {
            var currentDict = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Resources/Languages/"));

            if (currentDict != null)
            {
                string cultureCode = currentDict.Source.OriginalString
                    .Replace("Resources/Languages/", "")
                    .Replace(".xaml", "");

                foreach (ComboBoxItem item in LanguageComboBox.Items)
                {
                    if (item.Tag.ToString() == cultureCode)
                    {
                        LanguageComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string cultureCode = selectedItem.Tag.ToString()!;
                ((App)Application.Current).ChangeLanguage(cultureCode);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var mw = Owner as MainWindow;
            if (mw != null)
            {
                mw.ShowFPS = FPSCheckBox.IsChecked ?? false;
                mw.viewPort.RotationSensitivity = SenseSlider.Value;
                
                if (mw.gridLines != null && mw.gridLines.Fill != null)
                {
                    // Brushes.Gray is frozen, we must clone it to change opacity
                    var brush = mw.gridLines.Fill.Clone();
                    brush.Opacity = GridOpacitySlider.Value;
                    mw.gridLines.Fill = brush;
                }
                
                mw.OnPropertyChanged("ShowFPS");
            }
            this.Close();
        }
    }
}