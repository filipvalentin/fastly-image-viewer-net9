using System.Windows;

namespace fastly_image_viewer_net9
{
    public partial class ColorPickerWindow : Window
    {
        public ColorPickerWindow()
        {
            InitializeComponent();

            titleLbl.MouseDown += (s, e) => DragMove();
            //closeLbl.Click += (s, e) => Hide();
        }
    }
}
