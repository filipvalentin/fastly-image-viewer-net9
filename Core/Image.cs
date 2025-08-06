using Openize.Heic.Decoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace fastly_image_viewer_net9.Core {
    public class Image
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(nint hObject);

        public Bitmap Bitmap { get; }

        public Image(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(path);

			if(path.Contains(".heic", StringComparison.InvariantCultureIgnoreCase)) {
				using var fs = new FileStream(path, FileMode.Open);
				HeicImage image = HeicImage.Load(fs);

				var pixels = image.GetInt32Array(Openize.Heic.Decoder.PixelFormat.Argb32);
				var width = (int)image.Width;
				var height = (int)image.Height;
				var i = 0;

				Bitmap toConstruct = new(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				Rectangle rect = new(0, 0, width, height);
				BitmapData bmpData = toConstruct.LockBits(rect, ImageLockMode.WriteOnly, toConstruct.PixelFormat);

				unsafe {
					byte* ptr = (byte*)bmpData.Scan0;
					int stride = bmpData.Stride;

					Parallel.For(0, height, y =>
					{
						for (int x = 0; x < width; x++) {
							int pixel = pixels[y * width + x];
							byte a = (byte)((pixel >> 24) & 0xFF);
							byte r = (byte)((pixel >> 16) & 0xFF);
							byte g = (byte)((pixel >> 8) & 0xFF);
							byte b = (byte)(pixel & 0xFF);

							byte* pixelPtr = ptr + y * stride + x * 4;
							pixelPtr[0] = b;
							pixelPtr[1] = g;
							pixelPtr[2] = r;
							pixelPtr[3] = a;
						}
					});
				}
				toConstruct.UnlockBits(bmpData);
				Bitmap = toConstruct;
			}
        }

        public BitmapSource GetBitmapSource()
        {
			nint bitmapHandle = Bitmap.GetHbitmap();
            BitmapSource source = Imaging.CreateBitmapSourceFromHBitmap(bitmapHandle, nint.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());

            DeleteObject(bitmapHandle);

            return source;
        }
    }
}
