using ImageMagick;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Matrix = System.Windows.Media.Matrix;

namespace fastly_image_viewer_net9 {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		public string[] Args = Environment.GetCommandLineArgs();
		public MagickImage? mimage;

		private readonly InfoWindow infoWindow;

		private Matrix matrix = Matrix.Identity;
		private bool isDragging = false;
		private bool isMouseOverImage = false;
		private Point startPoint;

		private readonly DispatcherTimer inactivityTimer;
		private bool isHidden = false;
		private readonly Storyboard fadeOut;
		private readonly Storyboard fadeIn;
		public string? fileInfoStr = null;

		public MainWindow() {
			InitializeComponent();

			this.SizeChanged += (s, e) => FitToWindow();

			if (Args.Length > 1)
				_ = Task.Run(async () => await OpenImage(Args[1]));

			infoWindow = new InfoWindow(this);
			InfoButton.Click += (s, e) => infoWindow.Show();

			fadeOut = (Storyboard)FindResource("FadeOutStoryboard");
			fadeIn = (Storyboard)FindResource("FadeInStoryboard");
			inactivityTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
			inactivityTimer.Tick += InactivityTimer_Tick;
			inactivityTimer.Start();
		}

		#region WINDOW stuff
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			mimage?.Dispose();
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

		private void Window_KeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Escape) {
				Application.Current.Shutdown();
			}
			if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
				switch (e.Key) {
					case Key.OemPlus:
						ZoomAtCenter(1.2);
						break;
					case Key.OemMinus:
						ZoomAtCenter(0.8);
						break;
				}

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
		#endregion

		public async Task OpenImage(string path) {
			var s = Stopwatch.StartNew();

			if (!File.Exists(path))
				throw new FileNotFoundException(path);

			mimage?.Dispose();

			mimage = new MagickImage(path) { Format = MagickFormat.Bmp };

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

				FitToWindow();

				SaveAsButton.IsEnabled = true;
				ZoomInButton.IsEnabled = true;
				ZoomOutButton.IsEnabled = true;
			});

			s.Stop();

			var fileInfo = new FileInfo(path);
			fileInfoStr = $"File name: {fileInfo.Name}\nDisk size: {string.Format("{0:f2}", (double)fileInfo.Length / 1024)} KB\nSize: {mimage.Width} x {mimage.Height} px\nImage displayed in {s.ElapsedMilliseconds} ms";
		}

		#region Panning, Clamping, Zooming
		private void FitToWindow() {
			if (DisplayImageControl.Source == null) return;

			double windowWidth = ImageCanvas.ActualWidth;
			double windowHeight = ImageCanvas.ActualHeight;

			double imgWidth = DisplayImageControl.Source.Width;
			double imgHeight = DisplayImageControl.Source.Height;

			double scale = Math.Min(windowWidth / imgWidth, windowHeight / imgHeight);

			matrix = Matrix.Identity;
			matrix.Scale(scale, scale);
			matrix.Translate((windowWidth - imgWidth * scale) / 2, (windowHeight - imgHeight * scale) / 2);

			ImageTransform.Matrix = matrix;
		}

		private void ClampPan() {
			if (DisplayImageControl.Source == null) return;

			double imgWidth = DisplayImageControl.Source.Width * matrix.M11;
			double imgHeight = DisplayImageControl.Source.Height * matrix.M22;

			double windowWidth = ImageCanvas.ActualWidth;
			double windowHeight = ImageCanvas.ActualHeight;

			double offsetX = matrix.OffsetX;
			double offsetY = matrix.OffsetY;

			// If the image is smaller than window → center it
			if (imgWidth <= windowWidth) {
				offsetX = (windowWidth - imgWidth) / 2;
			}
			else {
				// Clamp so the image does not leave gaps
				if (offsetX > 0) offsetX = 0; // left edge
				if (offsetX + imgWidth < windowWidth) offsetX = windowWidth - imgWidth; // right edge
			}

			if (imgHeight <= windowHeight) {
				offsetY = (windowHeight - imgHeight) / 2;
			}
			else {
				// Clamp so the image does not leave gaps
				if (offsetY > 0) offsetY = 0; // top edge
				if (offsetY + imgHeight < windowHeight) offsetY = windowHeight - imgHeight; // bottom edge
			}

			matrix.OffsetX = offsetX;
			matrix.OffsetY = offsetY;
			ImageTransform.Matrix = matrix;
		}

		private void Image_MouseWheel(object sender, MouseWheelEventArgs e) {
			if (!isMouseOverImage || DisplayImageControl.Source == null) return;

			double zoom = e.Delta > 0 ? 1.1 : 0.9;
			Point mousePos = e.GetPosition(ImageCanvas);

			matrix.Translate(-mousePos.X, -mousePos.Y);
			matrix.Scale(zoom, zoom);
			matrix.Translate(mousePos.X, mousePos.Y);

			ClampPan();
		}

		private void Image_MouseMove(object sender, MouseEventArgs e) {
			if (isDragging && isMouseOverImage) {
				Point pos = e.GetPosition(this);
				Vector delta = pos - startPoint;

				matrix.Translate(delta.X, delta.Y);
				ClampPan();

				startPoint = pos;
			}
		}

		private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			if (!isMouseOverImage) return;
			startPoint = e.GetPosition(this);
			isDragging = true;
			DisplayImageControl.CaptureMouse();
		}

		private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
			isDragging = false;
			DisplayImageControl.ReleaseMouseCapture();
		}

		private void Image_MouseEnter(object sender, MouseEventArgs e) => isMouseOverImage = true;
		private void Image_MouseLeave(object sender, MouseEventArgs e) => isMouseOverImage = false;
		#endregion

		private void OpenButton_Click(object sender, RoutedEventArgs e) {
			var dialog = new OpenFileDialog {
				Filter =
					"All supported (*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.ico;*.tiff;*.wmf;*.heic)|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.ico;*.tiff;*.wmf;*.heic|" +
					"JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
					"Portable Network Graphic (*.png)|*.png|" +
					"Graphics Interchange Format (*.gif)|*.gif|" +
					"Icon (*.ico)|*.ico|" +
					"Other (*.tiff;*.wmf)|*.tiff;*.wmf|" +
					"All files (*.*)|*.*"
			};

			if (dialog.ShowDialog() is true)
				_ = Task.Run(async () => await OpenImage(dialog.FileName));
		}

		private void SaveAsButton_Click(object sender, RoutedEventArgs e) {
			var dialog = new SaveFileDialog {
				Filter =
					"JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
					"Portable Network Graphic (*.png)|*.png|" +
					"Graphics Interchange Format (*.gif)|*.gif|" +
					"Bitmap (*.bmp)|*.bmp\\"
			};


			if (dialog.ShowDialog() is true) {
				switch (dialog.FilterIndex) {
					case 1:
						mimage?.Write(dialog.FileName, MagickFormat.Jpg);
						break;
					case 2:
						mimage?.Write(dialog.FileName, MagickFormat.Png);
						break;
					case 3:
						mimage?.Write(dialog.FileName, MagickFormat.Gif);
						break;
					case 4:
						mimage?.Write(dialog.FileName, MagickFormat.Bmp);
						break;
				}
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e) {
			Application.Current.Shutdown();
			Hide();
			infoWindow.Hide();
		}

		private void ZoomAtCenter(double zoomFactor) {
			if (DisplayImageControl.Source == null) return;

			double centerX = ImageCanvas.ActualWidth / 2;
			double centerY = ImageCanvas.ActualHeight / 2;

			matrix.Translate(-centerX, -centerY);
			matrix.Scale(zoomFactor, zoomFactor);
			matrix.Translate(centerX, centerY);

			ClampPan();
		}

		private void ZoomInButton_Click(object sender, RoutedEventArgs e) => ZoomAtCenter(1.1);

		private void ZoomOutButton_Click(object sender, RoutedEventArgs e) => ZoomAtCenter(0.9);

		private void InactivityTimer_Tick(object? sender, EventArgs e) {
			fadeOut.Begin(OpenButton);
			fadeOut.Begin(SaveAsButton);
			fadeOut.Begin(CloseButton);
			fadeOut.Begin(ZoomInButton);
			fadeOut.Begin(ZoomOutButton);
			fadeOut.Begin(InfoButton);
			isHidden = true;
			inactivityTimer.Stop();
		}

		private void Window_MouseMove(object sender, MouseEventArgs e) {
			if (isHidden) {
				fadeIn.Begin(OpenButton);
				fadeIn.Begin(SaveAsButton);
				fadeIn.Begin(CloseButton);
				fadeIn.Begin(ZoomInButton);
				fadeIn.Begin(ZoomOutButton);
				fadeIn.Begin(InfoButton);
				isHidden = false;
			}

			inactivityTimer.Stop();
			inactivityTimer.Start();
		}
	}
}