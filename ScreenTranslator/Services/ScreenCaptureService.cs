using System.Drawing;
using System.Drawing.Imaging;


namespace ScreenTranslator.Services
{
	public class ScreenCaptureService
	{
		public static Bitmap CaptureScreen(Rectangle area)
		{
			Bitmap bitmap = new Bitmap(area.Width, area.Height);
			using (Graphics g = Graphics.FromImage(bitmap))
			{
				g.CopyFromScreen(area.Location, Point.Empty, area.Size);
			}
			return bitmap;
		}
		public void SaveBitmapToFile(Bitmap bitmap, string filePath)
		{
			bitmap.Save(filePath, ImageFormat.Png);
		}
	}
}