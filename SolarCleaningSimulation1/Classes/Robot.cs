using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;

namespace SolarCleaningSimulation1.Classes
{
    internal class Robot
    {
        // Robot dimensions (mm)
        public int WidthMm { get; set; }
        public int HeightMm { get; set; }

        // Canvases and image
        private readonly Canvas _solarCanvas;
        private readonly Canvas _animCanvas;
        private readonly Image _robotImage;

        // Layout parameters
        private double _gridOffsetX;
        private double _gridOffsetY;
        private double _numRows;
        private double _numCols;
        private double _panelWidthPx;
        private double _panelHeightPx;
        private double _startPaddingPx;
        private List<Rect> _panelRects;

        // Animation state
        private readonly DispatcherTimer _timer;
        private List<Point> _coveragePath = new List<Point>();
        private int _currentWaypoint;
        private double _speedPxPerTick;

        // Turning‐in‐place state
        private bool _isTurning = false;
        private double _turnTickElapsed = 0;
        private double _turnTicks = 0;
        private double _initialAngle = 0;
        private double _targetAngle = 0;
        private double _currentAngle = 0;

        public Robot(Canvas solarPanelCanvas, Canvas animationCanvas,
                     int widthMm, int heightMm, string imageUri)
        {
            _solarCanvas = solarPanelCanvas;
            _animCanvas = animationCanvas;
            WidthMm = widthMm;
            HeightMm = heightMm;

            // Setup image
            _robotImage = new Image
            {
                Source = new BitmapImage(new Uri(imageUri, UriKind.Absolute)),
                Stretch = Stretch.Fill,
                Visibility = Visibility.Collapsed
            };

            // Timer
            _timer = new DispatcherTimer();
            _timer.Tick += MoveStep;
        }

        // Configures layout parameters before placing robot: grid offsets, panel sizes, grid counts, padding.
        public void Configure(double gridOffsetX, double gridOffsetY,
                              double numCols, double numRows,
                              double panelWidthPx, double panelHeightPx,
                              double startPaddingPx, List<Rect> panelRectsPx)
        {
            _gridOffsetX = gridOffsetX;
            _gridOffsetY = gridOffsetY;
            _numCols = numCols;
            _numRows = numRows;
            _panelWidthPx = panelWidthPx;
            _panelHeightPx = panelHeightPx;
            _startPaddingPx = startPaddingPx;
            _panelRects = panelRectsPx;
        }

        /// <summary>
        /// Positions the animation canvas over the panel grid and places the robot at bottom-right.
        /// </summary>
        public void PlaceOnRoof(double scaleFactor)
        {
            // Remove robot image from any parent
            if (_robotImage.Parent is Panel oldParent)
                oldParent.Children.Remove(_robotImage);

            // Add animation canvas atop solar canvas
            if (_animCanvas.Parent is Panel p) p.Children.Remove(_animCanvas);

            // Compute the panel‐grid bounding box in the roof‐canvas coordinates
            double minX = _panelRects.Min(r => r.X);
            double minY = _panelRects.Min(r => r.Y);
            double maxX = _panelRects.Max(r => r.X + r.Width);
            double maxY = _panelRects.Max(r => r.Y + r.Height);

            // Size & place the animation canvas exactly over that box
            double gridW = maxX - minX;
            double gridH = maxY - minY;
            _animCanvas.Width = gridW;
            _animCanvas.Height = gridH;
            Canvas.SetLeft(_animCanvas, minX);
            Canvas.SetTop(_animCanvas, minY);

            // Add it into the view
            _solarCanvas.Children.Add(_animCanvas);

            // Add robot image into animation canvas
            _animCanvas.Children.Add(_robotImage);

            // Scale robot image
            _robotImage.Width = WidthMm * scaleFactor;
            _robotImage.Height = HeightMm * scaleFactor;

            // Position at bottom-right of animation canvas
            Canvas.SetRight(_robotImage, 0);
            Canvas.SetBottom(_robotImage, 0);
            _robotImage.Visibility = Visibility.Visible;
        }

        // Builds the back‐and‐forth path, inset by half the robot’s width so panels get fully covered.
        public void BuildCoveragePath(double panelPaddingPx, double robotWidthPx)
        {
            _coveragePath.Clear();

            double xStep = _panelWidthPx + panelPaddingPx;
            double yStep = _panelHeightPx + panelPaddingPx;

            // total grid height (edge-to-edge)
            double totalHeight = _numRows * _panelHeightPx
                               + (_numRows - 1) * panelPaddingPx;

            // inset by half the robot’s width so it brushes to the panel edges
            double halfBrush = robotWidthPx / 2;
            double yTop = halfBrush;
            double yBottom = totalHeight - halfBrush;

            // start at the rightmost column
            double xStart = (_numCols - 1) * xStep + _panelWidthPx / 2;
            double xCurrent = xStart;

            // first drop-in at bottom edge
            _coveragePath.Add(new Point(xCurrent, yBottom));
            bool nextGoesDown = false;

            // snake leftward
            for (int col = 1; col < _numCols; col++)
            {
                // move up or down to the opposite edge‐inset
                double yHere = nextGoesDown ? yTop : yBottom;
                xCurrent -= xStep;
                _coveragePath.Add(new Point(xCurrent, yHere));

                // then sweep back to the other edge‐inset
                double yThere = nextGoesDown ? yBottom : yTop;
                _coveragePath.Add(new Point(xCurrent, yThere));

                nextGoesDown = !nextGoesDown;
            }

            _currentWaypoint = 0;
        }

        // Begins animation at the given speed.
        public void AnimationStart(double speedMmPerSec, double scaleFactor, int tickRate = 60)
        {
            _speedPxPerTick = (speedMmPerSec * scaleFactor) / tickRate;
            _timer.Interval = TimeSpan.FromMilliseconds(1000.0 / tickRate);
            _timer.Start();
        }

        /// <summary>
        /// Stops any movement or turning.
        /// </summary>
        public void AnimationStop()
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
                _isTurning = false;
            }
        }

        /// <summary>
        /// Called each tick: handles turning‐in‐place or straight movement.
        /// </summary>
        private void MoveStep(object sender, EventArgs e)
        {
            // 1) Turning state
            if (_isTurning)
            {
                _turnTickElapsed++;
                double t = Math.Min(1.0, _turnTickElapsed / _turnTicks);
                double ang = _initialAngle + (_targetAngle - _initialAngle) * t;
                _robotImage.RenderTransform = new RotateTransform(
                    ang,
                    _robotImage.Width / 2,
                    _robotImage.Height / 2
                );
                if (t >= 1.0)
                {
                    _isTurning = false;
                    _currentAngle = _targetAngle;
                }
                return;
            }

            // 2) Completed path?
            if (_currentWaypoint >= _coveragePath.Count)
            {
                _timer.Stop();
                return;
            }

            // 3) Compute robot center
            double cr = Canvas.GetRight(_robotImage);
            double cb = Canvas.GetBottom(_robotImage);
            double cx = _animCanvas.ActualWidth - cr - _robotImage.Width / 2;
            double cy = cb + _robotImage.Height / 2;

            // 4) Vector to waypoint
            Point target = _coveragePath[_currentWaypoint];
            Vector vec = target - new Point(cx, cy);
            double dist = vec.Length;

            // 5) Snap & initiate turn
            if (dist < _speedPxPerTick)
            {
                // snap
                double nb = target.Y - _robotImage.Height / 2;
                double nr = _animCanvas.ActualWidth
                          - (target.X + _robotImage.Width / 2);
                Canvas.SetBottom(_robotImage, nb);
                Canvas.SetRight(_robotImage, nr);

                // compute next heading
                if (_currentWaypoint + 1 < _coveragePath.Count)
                {
                    Point nxt = _coveragePath[_currentWaypoint + 1];
                    Vector v2 = nxt - target;
                    double ar = Math.Atan2(v2.Y, v2.X);
                    double raw = -ar * 180 / Math.PI + 90;
                    double delta = ((raw - _currentAngle + 540) % 360) - 180;
                    _targetAngle = _currentAngle + delta;
                }
                else
                {
                    _targetAngle = _currentAngle;
                }

                // compute ticks for 90° turn
                double radiusPx = _panelWidthPx * 0.3;
                _turnTicks = (Math.PI / 2 * radiusPx) / _speedPxPerTick;
                _turnTickElapsed = 0;
                _initialAngle = _currentAngle;
                _isTurning = true;

                _currentWaypoint++;
            }
            else
            {
                // 6) Move step
                vec.Normalize();
                double nx = cx + vec.X * _speedPxPerTick;
                double ny = cy + vec.Y * _speedPxPerTick;
                double nb = ny - _robotImage.Height / 2;
                double nr = _animCanvas.ActualWidth
                          - (nx + _robotImage.Width / 2);
                Canvas.SetBottom(_robotImage, nb);
                Canvas.SetRight(_robotImage, nr);
            }
        }
    }
}
