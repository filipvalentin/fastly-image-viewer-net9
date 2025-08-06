using System.Windows;

namespace fastly_image_viewer_net9 {
    public partial class InfoWindow : Window
    {
        public InfoWindow()
        {
            InitializeComponent();

            versionLbl.Content = $"{Properties.Settings.Default.Version}";

            closeBtn.Click += (s, e) => Hide();
            //githubLbl.Click += (s, e) => System.Diagnostics.Process.Start("https://github.com/Rebzzel/Fastly-Image-Viewer");
        }

		protected override void OnClosed(EventArgs e) {
			Application.Current.Shutdown();
		}
    }
}