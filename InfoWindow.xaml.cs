using System.Windows;
using System.Windows.Input;

namespace fastly_image_viewer_net9 {
	public partial class InfoWindow : Window {
		private readonly MainWindow mainWindow;

		public InfoWindow(MainWindow mW) {
			InitializeComponent();
			mainWindow = mW;

			VersionLabel.Content = Properties.Settings.Default.Version;

			CloseButton.Click += (s, e) => Hide();
		}

		protected override void OnClosed(EventArgs e) {
			Application.Current.Shutdown();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			if (mainWindow.fileInfoStr == null) return;

			FileInfoHeaderLabel.Visibility = Visibility.Visible;
			FileInfoLabel.Content = mainWindow.fileInfoStr;
		}


		private void Window_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			if (e.ButtonState == MouseButtonState.Pressed) {
				DragMove();
			}
		}
	}
}