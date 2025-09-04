using ImageMagick;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace fastly_image_viewer_net9 {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public string[] Args = Environment.GetCommandLineArgs();
		public BitmapImage? bitmap;

		private SettingsWindow settingsWindow;
		private InfoWindow infoWindow;

		private Point startPoint;
		private bool isDragging = false;

		public MainWindow() {
			InitializeComponent();

			settingsWindow = new SettingsWindow();
			infoWindow = new InfoWindow();

			//settingsBtn.Click += (s, e) => settingsWindow.Show();
			infoBtn.Click += (s, e) => infoWindow.Show();


			if (Args.Length > 1)
				OpenImage(Args[1]);
#if DEBUG
			OpenImage("W:\\WDownloads\\20250803_175728.heic");
#endif
		}

		

		public async void OpenImage(string path) {
			Stopwatch s = new(); s.Start();

			if (!File.Exists(path))
				throw new FileNotFoundException(path);

			using var mimage = new MagickImage(path);
			mimage.Format = MagickFormat.Bmp;
			using var ms = new MemoryStream();
			mimage.Write(ms);
			var bitmapImage = new BitmapImage();
			bitmapImage.BeginInit();
			bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
			bitmapImage.StreamSource = ms;
			bitmapImage.EndInit();
			bitmapImage.Freeze();
			await this.Dispatcher.InvokeAsync(() => {
				DisplayImageControl.Source = bitmapImage;
			});
			var width = mimage.Width;
			var height = mimage.Height;

			var fileInfo = new FileInfo(path);

			infoLbl.Visibility = Visibility.Visible;
			infoLbl2.Content = $"{fileInfo.Name}\n{string.Format("{0:f2}", (double)fileInfo.Length / 1024)} KB\n{width} x {height}";

			while (width > SystemParameters.PrimaryScreenWidth * 70 / 100) {
				width = width * 70 / 100;
			}

			while (height > SystemParameters.PrimaryScreenHeight * 70 / 100) {
				height = height * 70 / 100;
			}

			DisplayImageControl.Width = width;
			DisplayImageControl.Height = height;
			grid.Visibility = Visibility.Hidden;

			saveAsBtn.IsEnabled = true;
			zoomInBtn.IsEnabled = true;
			zoomReloadBtn.IsEnabled = true;
			zoomOutBtn.IsEnabled = true;

			s.Stop();
			await this.Dispatcher.InvokeAsync(() => {
				PerformanceLabel.Content = $"Image displayed in {s.ElapsedMilliseconds} ms";
			});
#if DEBUG
			Debug.WriteLine($"Image displayed in {s.ElapsedMilliseconds} ms");
#endif
		}

		private void Window_Drop(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
				string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
				OpenImage(files[0]);
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			settingsWindow.Close();
			infoWindow.Close();

			if (WindowState == WindowState.Maximized) {
				// Use the RestoreBounds as the current values will be 0, 0 and the size of the screen
				Properties.Settings.Default.Top = RestoreBounds.Top;
				Properties.Settings.Default.Left = RestoreBounds.Left;
				Properties.Settings.Default.Height = RestoreBounds.Height;
				Properties.Settings.Default.Width = RestoreBounds.Width;
				Properties.Settings.Default.Maximized = true;
			}
			else {
				Properties.Settings.Default.Top = this.Top;
				Properties.Settings.Default.Left = this.Left;
				Properties.Settings.Default.Height = this.Height;
				Properties.Settings.Default.Width = this.Width;
				Properties.Settings.Default.Maximized = false;
			}

			Properties.Settings.Default.Save();
		}

		private void Image_MouseMove(object sender, MouseEventArgs e) {
			if (isDragging) {
				Point pos = e.GetPosition(this);
				Vector delta = pos - startPoint;

				ImageTranslate.X += delta.X;
				ImageTranslate.Y += delta.Y;

				startPoint = pos;
			}
		}

		private void Image_MouseWheel(object sender, MouseWheelEventArgs e) {
			if (DisplayImageControl.Source == null) return;

			double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
			Point mousePos = e.GetPosition(DisplayImageControl);

			// current scale
			double currentScale = ImageScale.ScaleX;

			// apply zoom
			ImageScale.ScaleX *= zoomFactor;
			ImageScale.ScaleY *= zoomFactor;

			// adjust translation so the zoom centers on the mouse
			ImageTranslate.X = (ImageTranslate.X - mousePos.X) * zoomFactor + mousePos.X;
			ImageTranslate.Y = (ImageTranslate.Y - mousePos.Y) * zoomFactor + mousePos.Y;
		}


		private void openBtn_Click(object sender, RoutedEventArgs e) {
			var dialog = new OpenFileDialog();
			dialog.Filter =
				"All supported (*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.ico;*.tiff;*.wmf;*.heic)|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.ico;*.tiff;*.wmf;*.heic|" +
				"JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
				"Portable Network Graphic (*.png)|*.png|" +
				"Graphics Interchange Format (*.gif)|*.gif|" +
				"Icon (*.ico)|*.ico|" +
				"Other (*.tiff;*.wmf)|*.tiff;*.wmf|" +
				"All files (*.*)|*.*";

			if (dialog.ShowDialog() is true)
				OpenImage(dialog.FileName);
		}

		private void saveAsBtn_Click(object sender, RoutedEventArgs e) {
			var dialog = new SaveFileDialog();
			dialog.Filter =
				"JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
				"Portable Network Graphic (*.png)|*.png|" +
				"Graphics Interchange Format (*.gif)|*.gif|" +
				"Icon (*.ico)|*.ico";

			//if (dialog.ShowDialog() is true) {
			//	switch (dialog.FilterIndex) {
			//		case 1:
			//			bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Jpeg);
			//			break;
			//		case 2:
			//			bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
			//			break;
			//		case 3:
			//			_image.Bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Gif);
			//			break;
			//		case 4:
			//			_image.Bitmap.Save(dialog.FileName, System.Drawing.Imaging.ImageFormat.Icon);
			//			break;
			//	}
			//}
		}

		private void closeBtn_Click(object sender, RoutedEventArgs e) {
			Application.Current.Shutdown();
			//Hide();
			//pickerWindow.Hide();
			//settingsWindow.Hide();
			//infoWindow.Hide();
		}

		private void zoomInBtn_Click(object sender, RoutedEventArgs e) {
			if (DisplayImageControl.Source == null) return;

			var width = DisplayImageControl.Width;
			var height = DisplayImageControl.Height;

			while (width > SystemParameters.PrimaryScreenWidth * 70 / 100) {
				width = width * 70 / 100;
			}

			while (height > SystemParameters.PrimaryScreenHeight * 70 / 100) {
				height = height * 70 / 100;
			}

			//if (bitmap.Width > (SystemParameters.PrimaryScreenWidth * 70 / 100) || bitmap.Height > (SystemParameters.PrimaryScreenHeight * 70 / 100))
			//	return;

			DisplayImageControl.Width += width * 5 / 100;
			DisplayImageControl.Height += height * 5 / 100;
		}

		private void zoomReloadBtn_Click(object sender, RoutedEventArgs e) {
			if (DisplayImageControl.Source == null) return;

			var width = DisplayImageControl.Width;
			var height = DisplayImageControl.Height;

			while (width > SystemParameters.PrimaryScreenWidth * 70 / 100) {
				width = width * 70 / 100;
			}

			while (height > SystemParameters.PrimaryScreenHeight * 70 / 100) {
				height = height * 70 / 100;
			}

			DisplayImageControl.Width = width;
			DisplayImageControl.Height = height;
		}

		private void zoomOutBtn_Click(object sender, RoutedEventArgs e) {
			if (DisplayImageControl.Source == null) return;

			var width = DisplayImageControl.Width;
			var height = DisplayImageControl.Height;

			while (width > SystemParameters.PrimaryScreenWidth * 70 / 100) {
				width = width * 70 / 100;
			}

			while (height > SystemParameters.PrimaryScreenHeight * 70 / 100) {
				height = height * 70 / 100;
			}

			if (DisplayImageControl.Width - width * 5 / 100 <= 0 || DisplayImageControl.Height - height * 5 / 10 <= 0)
				return;

			DisplayImageControl.Width -= width * 5 / 100;
			DisplayImageControl.Height -= height * 5 / 100;
		}

		private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			startPoint = e.GetPosition(scrollViewer);
			isDragging = true;
			DisplayImageControl.CaptureMouse();
		}

		private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			isDragging = false;
			DisplayImageControl.ReleaseMouseCapture();
		}

		private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
			if (e.Key == System.Windows.Input.Key.Escape) {
				Application.Current.Shutdown();
			}
		}

		private void Window_SourceInitialized(object sender, EventArgs e) {
			this.Top = Properties.Settings.Default.Top;
			this.Left = Properties.Settings.Default.Left;
			this.Height = Properties.Settings.Default.Height;
			this.Width = Properties.Settings.Default.Width;
			// Very quick and dirty - but it does the job
			if (Properties.Settings.Default.Maximized) {
				WindowState = WindowState.Maximized;
			}
		}
	}
}