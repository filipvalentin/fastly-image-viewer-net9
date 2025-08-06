using Microsoft.Win32;
using System.Windows;

namespace fastly_image_viewer_net9
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();

            autoRunCheckBox.IsChecked =
                Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "FastlyImageViewer", string.Empty) != string.Empty;

            titleLbl.MouseDown += (s, e) => DragMove();
            cancelBtn.Click += (s, e) => Hide();
        }

        private void applyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (autoRunCheckBox.IsChecked is true)
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "FastlyImageViewer", MainWindow.Args[0]);
            else
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run", "FastlyImageViewer", string.Empty);

            Hide();
        }
    }
}