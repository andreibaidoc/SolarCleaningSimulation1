using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Media.Animation;

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
        private DateTime _lastRenderTime;
        private double _speedPxPerSec;

        // Coverage path and waypoints
        private List<Point> _coveragePath = new List<Point>();
        private int _currentWaypoint;

        // Turning‐in‐place state
        private bool _isTurning = false;
        private double _turnElapsedSec = 0;
        private double _turnDurationSec = 0;
        private double _initialAngle = 0;
        private double _targetAngle = 0;
        private double _currentAngle = 0;

        private DateTime _simStartTime;
        
        private TimeSpan _elapsedTime;
        public TimeSpan ElapsedTime => _elapsedTime;

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

        // Positions the animation canvas over the panel grid and places the robot at bottom-right.
        public void PlaceOnRoof(double scaleFactor)
        {
            // Remove robot image from any parent
            if (_robotImage.Parent is Panel oldParent)
                oldParent.Children.Remove(_robotImage);

            // Add animation canvas atop solar canvas
            if (_animCanvas.Parent is Panel p) p.Children.Remove(_animCanvas);

            // Compute the panel‐grid bounding box in the roof‐canvas coordinates
            // This one basically takes the first and the last panels and memorizes their coordinates 
            // such that animation canvas is exactly the size of the panel grid.
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
            _speedPxPerSec = speedMmPerSec * scaleFactor;
            _lastRenderTime = DateTime.Now;
            _simStartTime = DateTime.Now;

            // Unsubscribe first, then subscribe exactly once
            CompositionTarget.Rendering -= OnRendering;
            CompositionTarget.Rendering += OnRendering;
        }

        public void AnimationStop()
        {
            CompositionTarget.Rendering -= OnRendering;
            _isTurning = false;
            BackToOrigin();
            _elapsedTime = DateTime.Now - _simStartTime;
        }

        private void BackToOrigin()
        {
            // Stop receiving per-frame updates
            CompositionTarget.Rendering -= OnRendering;

            // Cancel any in-place turn
            _isTurning = false;

            // Reset path position
            _currentWaypoint = 0;

            // Reset orientation
            _currentAngle = 0;
            _robotImage.RenderTransform = new RotateTransform(
                0,
                _robotImage.Width / 2,
                _robotImage.Height / 2
            );

            // Snap back to top-left of animation canvas
            Canvas.SetRight(_robotImage, 0);
            Canvas.SetBottom(_robotImage, 0);
        }

        private void OnRendering(object sender, EventArgs e)
        {
            // Calculate seconds since last frame
            var now = DateTime.Now;
            double dt = (now - _lastRenderTime).TotalSeconds;
            _lastRenderTime = now;

            // Call your modified MoveStep that takes dt
            MoveStep(dt);
        }

        private void MoveStep(double dt)
        {
            // Turning‐in‐place
            if (_isTurning)
            {
                _turnElapsedSec += dt;
                double t = Math.Min(1.0, _turnElapsedSec / _turnDurationSec);
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

            // Completed path
            if (_currentWaypoint >= _coveragePath.Count)
            {
                AnimationStop();
                return;
            }

            // Compute center (use Left/Top now)
            double cr = Canvas.GetRight(_robotImage);
            double cb = Canvas.GetBottom(_robotImage);
            double cx = _animCanvas.ActualWidth - cr - _robotImage.Width / 2;
            double cy = cb + _robotImage.Height / 2;

            // Vector - target
            Point target = _coveragePath[_currentWaypoint];
            Vector vec = target - new Point(cx, cy);
            double dist = vec.Length;
            vec.Normalize();

            // Arrived? then snap + initiate turn
            if (dist < _speedPxPerSec * dt)
            {
                // snap to exact center‐line
                double newRight = _animCanvas.ActualWidth - (target.X + _robotImage.Width / 2);
                double newBottom = target.Y - _robotImage.Height / 2;
                Canvas.SetRight(_robotImage, newRight);
                Canvas.SetBottom(_robotImage, newBottom);

                // compute next heading and turnDurationSec:
                if (_currentWaypoint + 1 < _coveragePath.Count)
                {
                    var next = _coveragePath[_currentWaypoint + 1];
                    var v2 = next - target;
                    double rawRad = Math.Atan2(v2.Y, v2.X);
                    double rawDeg = -rawRad * 180 / Math.PI + 90;
                    double delta = ((rawDeg - _currentAngle + 540) % 360) - 180;
                    _targetAngle = _currentAngle + delta;
                }
                else _targetAngle = _currentAngle;

                // turnDuration = arcLength / speed = (π/2 * r) / (px/sec)
                double radiusPx = _panelWidthPx * 0.3;
                _turnDurationSec = (Math.PI / 2 * radiusPx) / _speedPxPerSec;
                _turnElapsedSec = 0;
                _initialAngle = _currentAngle;
                _isTurning = true;

                _currentWaypoint++;
            }
            else
            {
                // Move by (speed * dt)
                double move = _speedPxPerSec * dt;
                double nx = cx + vec.X * move;
                double ny = cy + vec.Y * move;

                double newRight = _animCanvas.ActualWidth - (nx + _robotImage.Width / 2);
                double newBottom = ny - _robotImage.Height / 2;
                Canvas.SetRight(_robotImage, newRight);
                Canvas.SetBottom(_robotImage, newBottom);
            }
        }
    }
}
