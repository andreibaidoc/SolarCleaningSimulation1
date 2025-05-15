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

        private static List<Point> GenerateZigZagPath(double panelPaddingPx, double robotBrushPx, int numCols, 
                                                        int numRows, double panelWidthPx, double panelHeightPx)
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
