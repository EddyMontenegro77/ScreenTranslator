using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using ScreenTranslator.Models;

namespace ScreenTranslator.Services
{
    public class TextBlockResult
    {
        public string Text { get; set; } = string.Empty;
        public Rect BoundingBox { get; set; }
        public List<OcrLineResult> Lines { get; set; } = new();
    }

    public static class TextBlockDetectionService
    {
        public static List<TextBlockResult> GroupLinesIntoBlocks(List<OcrLineResult> lines, double verticalTolerance = 8.0)
        {
            var blocks = new List<TextBlockResult>();
            if (lines == null || lines.Count == 0) return blocks;

            var sorted = lines.OrderBy(l => l.BoundingBox.Y).ToList();

            foreach (var line in sorted)
            {
                bool added = false;

                foreach (var block in blocks)
                {
                    var blockRect = block.BoundingBox;
                    double verticalGap = line.BoundingBox.Y - (blockRect.Y + blockRect.Height);

                    double left = Math.Max(blockRect.X, line.BoundingBox.X);
                    double right = Math.Min(blockRect.X + blockRect.Width, line.BoundingBox.X + line.BoundingBox.Width);
                    double horizOverlap = right - left;

                    if (verticalGap <= verticalTolerance && horizOverlap > Math.Min(10, Math.Min(blockRect.Width, line.BoundingBox.Width) * 0.1))
                    {
                        block.BoundingBox = Rect.Union(block.BoundingBox, line.BoundingBox);
                        if (!string.IsNullOrEmpty(block.Text)) block.Text += " ";
                        block.Text += line.Text;
                        block.Lines.Add(line);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    blocks.Add(new TextBlockResult
                    {
                        BoundingBox = line.BoundingBox,
                        Text = line.Text,
                        Lines = new List<OcrLineResult> { line }
                    });
                }
            }

            return blocks;
        }
    }
}
