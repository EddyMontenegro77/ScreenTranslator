using System.Windows;

namespace ScreenTranslator.Models
{
	public class OcrTextResults
	{
		public string FullText { get; set; } = string.Empty;
		public List<OcrLineResult> Lines { get; set; } = new();
	}

	public class OcrLineResult
	{
		public string Text { get; set; } = string.Empty;
		public Rect BoundingBox { get; set; }
	}
}