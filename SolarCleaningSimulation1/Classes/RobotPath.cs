using System.Windows;

namespace SolarCleaningSimulation1.Classes
{
    internal class RobotPath
    {
        public enum CoveragePathType
        {
            ZigZag,
            RowWise
        }

        // Generates a list of waypoints for the given path type and grid parameters
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
                _ => throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null)
            };
        }

        private static List<Point> GenerateZigZagPath(double panelPaddingPx, double robotBrushPx, int numCols, int numRows, double panelWidthPx, double panelHeightPx)
        {
            var coveragePath = new List<Point>();
            double xStep = panelWidthPx + panelPaddingPx;
            double yStep = panelHeightPx + panelPaddingPx;

            // total grid height (edge-to-edge)
            double totalHeight = numRows * panelHeightPx
                               + (numRows - 1) * panelPaddingPx;

            // inset by half the robot’s brush width
            double halfBrush = robotBrushPx / 2;
            double yTop = halfBrush;
            double yBottom = totalHeight - halfBrush;

            // start at the rightmost column, bottom edge
            double xCurrent = (numCols - 1) * xStep + panelWidthPx / 2;
            coveragePath.Add(new Point(xCurrent, yBottom));

            bool nextGoesDown = false;
            // snake leftward across columns
            for (int col = 1; col < numCols; col++)
            {
                // move up or down to the opposite inset-edge
                double yHere = nextGoesDown ? yTop : yBottom;
                xCurrent -= xStep;
                coveragePath.Add(new Point(xCurrent, yHere));

                // then sweep to the other edge of this column
                double yThere = nextGoesDown ? yBottom : yTop;
                coveragePath.Add(new Point(xCurrent, yThere));

                nextGoesDown = !nextGoesDown;
            }

            return coveragePath;
        }

        private static List<Point> GenerateRowWisePath(double panelPaddingPx, double robotBrushPx, int numCols, int numRows, double panelWidthPx, double panelHeightPx)
        {
            var coveragePath = new List<Point>();

            // compute grid steps
            double xStep = panelWidthPx + panelPaddingPx;
            double yStep = panelHeightPx + panelPaddingPx;
            double halfBrush = robotBrushPx / 2;

            // inset extremes
            double totalWidth = numCols * panelWidthPx + (numCols - 1) * panelPaddingPx;
            double totalHeight = numRows * panelHeightPx + (numRows - 1) * panelPaddingPx;

            // yTop is at the very top of the first stripe
            double yTop = totalHeight - halfBrush;
            double yBottom = halfBrush;
            double xLeft = halfBrush;
            double xRight = totalWidth - halfBrush;

            // build stripe Y positions by exactly brush‐width
            var stripeYs = new List<double>();
            for (double y = yTop; y > yBottom; y -= robotBrushPx)
                stripeYs.Add(y);
            stripeYs.Add(yBottom);

            // start at top-right
            coveragePath.Add(new Point(xRight, stripeYs[0]));

            bool goingLeft = true;
            // for each stripe: horizontal sweep then drop down one brush-width
            for (int i = 0; i < stripeYs.Count; i++)
            {
                double y = stripeYs[i];
                double xTarget = goingLeft ? xLeft : xRight;

                // horizontal move
                coveragePath.Add(new Point(xTarget, y));

                // vertical drop to next stripe (if any)
                if (i < stripeYs.Count - 1)
                    coveragePath.Add(new Point(xTarget, stripeYs[i + 1]));

                goingLeft = !goingLeft;
            }

            return coveragePath;
        }
    }
}
