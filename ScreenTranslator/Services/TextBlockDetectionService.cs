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

    /// <summary>
    /// Groups individual OCR lines (as returned by Windows.Media.Ocr) into logical text blocks
    /// (paragraphs / speech bubbles), handling both horizontal and vertical (Japanese) text layouts.
    /// </summary>
    public static class TextBlockDetectionService
    {
        /// <summary>
        /// A line is classified as vertical text when its height exceeds its width by this factor.
        /// Typical for Japanese vertical writing (columns read top-to-bottom, right-to-left).
        /// </summary>
        private const double VerticalAspectRatioThreshold = 1.8;

        /// <summary>
        /// Minimum overlap (as a fraction of the smaller dimension) required between two lines
        /// to be considered part of the same block, in addition to the gap check.
        /// </summary>
        private const double MinOverlapFraction = 0.1;

        /// <summary>
        /// Absolute floor for the required overlap in pixels, so tiny lines don't get an
        /// unreasonably small (near-zero) overlap requirement.
        /// </summary>
        private const double MinOverlapPixelsFloor = 10.0;

        /// <summary>
        /// Fallback tolerance (in pixels) used when there isn't enough data (fewer than 2 lines
        /// of a given orientation) to compute an adaptive gap threshold.
        /// </summary>
        private const double FallbackToleranceWhenInsufficientData = 15.0;

        /// <summary>
        /// A gap between two consecutive lines is considered a "jump" (i.e. likely a boundary
        /// between two separate paragraphs) when it is at least this many times larger than
        /// the previous gap in the sorted distribution.
        /// </summary>
        private const double ParagraphBoundaryJumpRatio = 1.6;

        /// <summary>
        /// Safety margin multiplier applied to the median gap when no clear paragraph
        /// boundary jump is found in the distribution (i.e. everything looks like one block).
        /// </summary>
        private const double NoJumpFoundMedianMultiplier = 1.8;

        public static List<TextBlockResult> GroupLinesIntoBlocks(List<OcrLineResult> lines)
        {
            var textBlocks = new List<TextBlockResult>();
            if (lines == null || lines.Count == 0) return textBlocks;

            bool[] isLineVertical = lines.Select(IsVerticalLine).ToArray();

            double horizontalTolerance = ComputeAdaptiveTolerance(
                lines.Where((line, index) => !isLineVertical[index]).ToList(), isVertical: false);
            double verticalTolerance = ComputeAdaptiveTolerance(
                lines.Where((line, index) => isLineVertical[index]).ToList(), isVertical: true);

            int lineCount = lines.Count;

            // Union-Find (disjoint set) structure: each line starts as its own group,
            // and gets merged with adjacent lines regardless of processing order.
            var parentIndex = Enumerable.Range(0, lineCount).ToArray();

            int FindRoot(int index) =>
                parentIndex[index] == index ? index : (parentIndex[index] = FindRoot(parentIndex[index]));

            void UnionGroups(int indexA, int indexB)
            {
                int rootA = FindRoot(indexA);
                int rootB = FindRoot(indexB);
                if (rootA != rootB) parentIndex[rootA] = rootB;
            }

            // Compare every pair of lines using their ORIGINAL bounding boxes (never an
            // accumulated/union box), so the result doesn't depend on processing order.
            for (int i = 0; i < lineCount; i++)
            {
                for (int j = i + 1; j < lineCount; j++)
                {
                    if (isLineVertical[i] != isLineVertical[j]) continue; // don't mix orientations

                    double toleranceForPair = isLineVertical[i] ? verticalTolerance : horizontalTolerance;

                    if (AreLinesAdjacent(lines[i].BoundingBox, lines[j].BoundingBox, toleranceForPair, isLineVertical[i]))
                        UnionGroups(i, j);
                }
            }

            foreach (var group in Enumerable.Range(0, lineCount).GroupBy(FindRoot))
            {
                var linesInGroup = group.Select(index => lines[index]).ToList();
                bool groupIsVertical = isLineVertical[group.First()];

                // Reading order: vertical Japanese text reads right-to-left column by column,
                // top-to-bottom within each column. Horizontal text reads top-to-bottom.
                var orderedLines = groupIsVertical
                    ? linesInGroup.OrderByDescending(line => line.BoundingBox.X).ThenBy(line => line.BoundingBox.Y).ToList()
                    : linesInGroup.OrderBy(line => line.BoundingBox.Y).ToList();

                textBlocks.Add(new TextBlockResult
                {
                    Lines = orderedLines,
                    Text = groupIsVertical
                        ? string.Join("", orderedLines.Select(line => line.Text))   // no spaces: not used in Japanese
                        : string.Join(" ", orderedLines.Select(line => line.Text)),
                    BoundingBox = orderedLines.Select(line => line.BoundingBox).Aggregate(Rect.Union)
                });
            }

            return textBlocks;
        }

        /// <summary>
        /// Computes a merge tolerance based on the ACTUAL gaps between consecutive lines in this
        /// specific capture, instead of a fixed multiplier of line height. This adapts naturally to
        /// both tightly-spaced manga speech bubbles and normally-spaced prose paragraphs.
        /// </summary>
        private static double ComputeAdaptiveTolerance(List<OcrLineResult> linesOfOneOrientation, bool isVertical)
        {
            if (linesOfOneOrientation.Count < 2)
                return FallbackToleranceWhenInsufficientData;

            var sortedLines = isVertical
                ? linesOfOneOrientation.OrderBy(line => line.BoundingBox.X).ToList()
                : linesOfOneOrientation.OrderBy(line => line.BoundingBox.Y).ToList();

            var consecutiveGaps = new List<double>();
            for (int i = 0; i < sortedLines.Count - 1; i++)
            {
                double gap = isVertical
                    ? sortedLines[i + 1].BoundingBox.X - (sortedLines[i].BoundingBox.X + sortedLines[i].BoundingBox.Width)
                    : sortedLines[i + 1].BoundingBox.Y - (sortedLines[i].BoundingBox.Y + sortedLines[i].BoundingBox.Height);

                if (gap > 0) consecutiveGaps.Add(gap);
            }

            if (consecutiveGaps.Count == 0)
                return FallbackToleranceWhenInsufficientData;

            consecutiveGaps.Sort();

            // Look for the biggest relative jump between sorted gaps: this jump is the natural
            // boundary between "same paragraph line spacing" and "gap between separate paragraphs".
            double biggestJumpRatioFound = 1.0;
            int jumpBoundaryIndex = -1;

            for (int i = 0; i < consecutiveGaps.Count - 1; i++)
            {
                if (consecutiveGaps[i] <= 0) continue;

                double jumpRatio = consecutiveGaps[i + 1] / consecutiveGaps[i];

                if (jumpRatio > biggestJumpRatioFound && jumpRatio > ParagraphBoundaryJumpRatio)
                {
                    biggestJumpRatioFound = jumpRatio;
                    jumpBoundaryIndex = i;
                }
            }

            if (jumpBoundaryIndex >= 0)
            {
                // Tolerance sits right between the last "normal" gap and the first "paragraph break" gap.
                return (consecutiveGaps[jumpBoundaryIndex] + consecutiveGaps[jumpBoundaryIndex + 1]) / 2.0;
            }

            // No clear paragraph separation found in this capture -> treat all lines as one block,
            // using a generous margin above the median gap.
            double medianGap = consecutiveGaps[consecutiveGaps.Count / 2];
            return medianGap * NoJumpFoundMedianMultiplier;
        }

        /// <summary>
        /// Determines whether two line bounding boxes are close enough (gap) and aligned enough
        /// (overlap on the perpendicular axis) to belong to the same text block.
        /// </summary>
        private static bool AreLinesAdjacent(Rect boxA, Rect boxB, double tolerance, bool isVertical)
        {
            if (!isVertical)
            {
                // Horizontal text: lines stack top-to-bottom, so check the vertical gap
                // between them and require horizontal (X-axis) overlap.
                double verticalGap = Math.Max(boxA.Y, boxB.Y) - Math.Min(boxA.Y + boxA.Height, boxB.Y + boxB.Height);

                double overlapLeft = Math.Max(boxA.X, boxB.X);
                double overlapRight = Math.Min(boxA.X + boxA.Width, boxB.X + boxB.Width);
                double horizontalOverlap = overlapRight - overlapLeft;

                double minWidth = Math.Min(boxA.Width, boxB.Width);
                double requiredOverlap = Math.Min(MinOverlapPixelsFloor, minWidth * MinOverlapFraction);

                return verticalGap <= tolerance && horizontalOverlap > requiredOverlap;
            }
            else
            {
                // Vertical text: columns stack right-to-left, so check the horizontal gap
                // between them and require vertical (Y-axis) overlap.
                double horizontalGap = Math.Max(boxA.X, boxB.X) - Math.Min(boxA.X + boxA.Width, boxB.X + boxB.Width);

                double overlapTop = Math.Max(boxA.Y, boxB.Y);
                double overlapBottom = Math.Min(boxA.Y + boxA.Height, boxB.Y + boxB.Height);
                double verticalOverlap = overlapBottom - overlapTop;

                double minHeight = Math.Min(boxA.Height, boxB.Height);
                double requiredOverlap = Math.Min(MinOverlapPixelsFloor, minHeight * MinOverlapFraction);

                return horizontalGap <= tolerance && verticalOverlap > requiredOverlap;
            }
        }

        /// <summary>
        /// Classifies a line as vertical text based on its aspect ratio (tall and narrow = vertical
        /// Japanese column). Note: this heuristic can misclassify near-square single-character lines;
        /// worth validating against real vertical-text captures if edge cases appear.
        /// </summary>
        private static bool IsVerticalLine(OcrLineResult line)
        {
            var box = line.BoundingBox;
            return box.Height > box.Width * VerticalAspectRatioThreshold;
        }
    }
}