using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolarCleaningSimulation1.Classes
{
    internal class Path
    {
        public enum CoveragePathType
        {
            ZigZag,
            RowWise,
            Loop
        }

        /// <summary>
        /// Generates a list of waypoints for the given path type and grid parameters.
        /// </summary>
        public static List<Point> GenerateCoveragePath(
            CoveragePathType pathType,
            double panelPaddingPx,
            double robotBrushPx,
            int numCols,
            int numRows,
            double panelWidthPx,
            double panelHeightPx)
        {
            return pathType switch
            {
                CoveragePathType.ZigZag => GenerateZigZagPath(panelPaddingPx, robotBrushPx, numCols, numRows, panelWidthPx, panelHeightPx),
                CoveragePathType.RowWise => GenerateRowWisePath(panelPaddingPx, robotBrushPx, numCols, numRows, panelWidthPx, panelHeightPx),
                CoveragePathType.Loop => GenerateLoopPath(panelPaddingPx, robotBrushPx, numCols, numRows, panelWidthPx, panelHeightPx),
                _ => throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null)
            };
        }

        private static List<Point> GenerateZigZagPath(
            double panelPaddingPx,
            double robotBrushPx,
            int numCols,
            int numRows,
            double panelWidthPx,
            double panelHeightPx)
        {
            var coveragePath = new List<Point>();
            double xStep = panelWidthPx + panelPaddingPx;
            double yStep = panelHeightPx + panelPaddingPx;
            double halfBrush = robotBrushPx / 2;

            // Y-extremes inset by half the brush
            double yTop = halfBrush;
            double yBottom = numRows * panelHeightPx
                           + (numRows - 1) * panelPaddingPx
                           - halfBrush;

            // start at bottom-right
            double x = (numCols - 1) * xStep + panelWidthPx / 2;
            coveragePath.Add(new Point(x, yBottom));

            bool goingUp = true;
            for (int col = numCols - 1; col >= 0; col--)
            {
                // vertical leg
                double yTarget = goingUp ? yTop : yBottom;
                coveragePath.Add(new Point(x, yTarget));

                // horizontal shift (unless last column)
                if (col > 0)
                {
                    x -= xStep;
                    coveragePath.Add(new Point(x, yTarget));
                }
                goingUp = !goingUp;
            }

            return coveragePath;
        }

        private static List<Point> GenerateRowWisePath(
            double panelPaddingPx,
            double robotBrushPx,
            int numCols,
            int numRows,
            double panelWidthPx,
            double panelHeightPx)
        {
            var coveragePath = new List<Point>();
            double xStep = panelWidthPx + panelPaddingPx;
            double yStep = panelHeightPx + panelPaddingPx;
            double halfBrush = robotBrushPx / 2;

            double xLeft = halfBrush;
            double xRight = numCols * panelWidthPx
                          + (numCols - 1) * panelPaddingPx
                          - halfBrush;

            // start at top-left
            double y = halfBrush;
            coveragePath.Add(new Point(xLeft, y));

            bool goingRight = true;
            for (int row = 0; row < numRows; row++)
            {
                // horizontal sweep
                double xTarget = goingRight ? xRight : xLeft;
                coveragePath.Add(new Point(xTarget, y));

                // drop down one row (unless last)
                if (row < numRows - 1)
                {
                    y += yStep;
                    coveragePath.Add(new Point(xTarget, y));
                }
                goingRight = !goingRight;
            }

            return coveragePath;
        }

        private static List<Point> GenerateLoopPath(
            double panelPaddingPx,
            double robotBrushPx,
            int numCols,
            int numRows,
            double panelWidthPx,
            double panelHeightPx)
        {
            var coveragePath = new List<Point>();
            double xStep = panelWidthPx + panelPaddingPx;
            double yStep = panelHeightPx + panelPaddingPx;
            double halfBrush = robotBrushPx / 2;

            double xRight = (numCols - 1) * xStep + panelWidthPx / 2;
            double xLeft = panelWidthPx / 2;
            double yBottom = numRows * panelHeightPx
                           + (numRows - 1) * panelPaddingPx
                           - halfBrush;

            // loop start at bottom-right
            var start = new Point(xRight, yBottom);
            coveragePath.Add(start);
            // climb up
            coveragePath.Add(new Point(xRight, halfBrush));
            // left
            coveragePath.Add(new Point(xLeft, halfBrush));
            // down small (one brush-width)
            coveragePath.Add(new Point(xLeft, halfBrush + robotBrushPx));
            // right
            coveragePath.Add(new Point(xRight, halfBrush + robotBrushPx));
            // back down
            coveragePath.Add(start);
            // left to finish loop
            coveragePath.Add(new Point(xLeft, yBottom));
            // return to start
            coveragePath.Add(start);

            return coveragePath;
        }
    }
}
