using System.Windows;

namespace SolarCleaningSimulation1.Classes
{
    // Represents a roof area on which solar panels can be placed.
    // Provides methods for dimension conversions and grid calculations.
    internal class Roof
    {
        // Input dimensions
        public double WidthM { get; set; }
        public double LengthM { get; set; }

        // Derived dimensions in millimeters
        public double WidthMm => WidthM * 1000;
        public double LengthMm => LengthM * 1000;

        // Computed layout results
        public double ScaleFactor { get; private set; }
        public Rect RoofRect { get; private set; }
        public List<Rect> PanelRects { get; private set; } = new List<Rect>();

        // Initializes a new instance of Roof with given dimensions in meters.
        public Roof(double widthM, double lengthM)
        {
            WidthM = widthM;
            LengthM = lengthM;
        }

        // Calculates layout for roof and panels given canvas size and padding parameters.
        public void CalculateLayout(double canvasWidth, double canvasHeight, double canvasPadding, double panelWidthMm, double panelLengthMm, double panelPaddingMm)
        {
            // Compute available area after padding
            double availableWidth = canvasWidth - 2 * canvasPadding;
            double availableHeight = canvasHeight - 2 * canvasPadding;

            // Scale factor to fit roof
            ScaleFactor = Math.Min(availableWidth / WidthMm, availableHeight / LengthMm);

            // Roof rectangle in pixel coords
            double WidthPx = WidthMm * ScaleFactor;
            double LengthPx = LengthMm * ScaleFactor;
            RoofRect = new Rect(canvasPadding, canvasPadding, WidthPx, LengthPx);

            // Generate panel positions in mm
            int cols = CalculateColumns(panelWidthMm, panelPaddingMm);
            int rows = CalculateRows(panelLengthMm, panelPaddingMm);
            double totalGridWmm = cols * panelWidthMm + (cols - 1) * panelPaddingMm;
            double totalGridHmm = rows * panelLengthMm + (rows - 1) * panelPaddingMm;

            // Compute centering offsets (in mm)
            double offsetGridXmm = (WidthMm - totalGridWmm) / 2;
            double offsetGridYmm = (LengthMm - totalGridHmm) / 2;

            // Convert each panel into a pixel Rect, now centered
            PanelRects.Clear();
            double panelPxW = panelWidthMm * ScaleFactor;
            double panelPxH = panelLengthMm * ScaleFactor;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    // position in mm relative to roof
                    double xMm = offsetGridXmm + c * (panelWidthMm + panelPaddingMm);
                    double yMm = offsetGridYmm + r * (panelLengthMm + panelPaddingMm);

                    // convert to pixel coords atop RoofRect.X/Y
                    double xPx = RoofRect.X + xMm * ScaleFactor;
                    double yPx = RoofRect.Y + yMm * ScaleFactor;

                    PanelRects.Add(new Rect(xPx, yPx, panelPxW, panelPxH));
                }
            }
        }

        // Calculates how many columns of panels fit given panel width and padding in mm.
        public int CalculateColumns(double panelWidthMm, double panelPaddingMm = 0)
        {
            return (int)Math.Floor((WidthMm + panelPaddingMm) / (panelWidthMm + panelPaddingMm));
        }

        // Calculates how many rows of panels fit given panel length and padding in mm.
        public int CalculateRows(double panelLengthMm, double panelPaddingMm = 0)
        {
            return (int)Math.Floor((LengthMm + panelPaddingMm) / (panelLengthMm + panelPaddingMm));
        }
    }
}
