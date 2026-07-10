using System.Windows;
using System.Windows.Media;

namespace ScreenTranslator.Helpers
{
	public static class RectangleHelper
	{
		public static System.Drawing.Rectangle ToPhysicalRectangle(this Rect rect, Visual visual)
		{
			var source = PresentationSource.FromVisual(visual);
			double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
			double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

			return new System.Drawing.Rectangle(
				(int)(rect.X * dpiX),
				(int)(rect.Y * dpiY),
				(int)(rect.Width * dpiX),
				(int)(rect.Height * dpiY));
		}

		public static Rect ToDeviceIndependentRect(this System.Drawing.Rectangle physRect, Visual visual)
		{
			var source = PresentationSource.FromVisual(visual);
			double dpiX = source?.CompositionTarget?.TransformToDevice.M11 ?? 1.0;
			double dpiY = source?.CompositionTarget?.TransformToDevice.M22 ?? 1.0;

			return new Rect(
				physRect.X / dpiX,
				physRect.Y / dpiY,
				physRect.Width / dpiX,
				physRect.Height / dpiY);
		}
	}
}