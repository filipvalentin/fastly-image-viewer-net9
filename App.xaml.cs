using System.Windows;

namespace fastly_image_viewer_net9 {
	public partial class App : Application/*, ISingleInstanceApp*/ {

		protected override void OnStartup(StartupEventArgs e) {
			//if (!SingleInstance<App>.InitializeAsFirstInstance("Fastly_Image_Viewer")) {
			//	Shutdown(); // Exit this second instance
			//	return;
			//}

			base.OnStartup(e);
			// your normal startup logic continues here
		}
		//public bool SignalExternalCommandLineArgs(IList<string> args) {
		//	if (args.Count > 1 && MainWindow is MainWindow mainWindow) {
		//		mainWindow.OpenImage(args[1]);
		//		if (!mainWindow.IsActive)
		//			mainWindow.Show();
		//	}

		//	return true;
		//}
	}
}
