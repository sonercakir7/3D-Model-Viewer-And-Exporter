using System.Windows;

namespace ThreeDModelViewerAndExporter
{
    public partial class ModelInfoWindow : Window
    {
        public ModelInfoWindow(string info)
        {
            InitializeComponent();
            InfoTextBox.Text = info;
        }
    }
}