using System;
using System.Drawing;
using System.Windows;

namespace ScreenTranslator.Models
{
    public class CaptureLogEntry
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string DetectedText { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public System.Drawing.Rectangle PhysicalRect { get; set; }
        public Rect DipRect { get; set; }
    }
}
