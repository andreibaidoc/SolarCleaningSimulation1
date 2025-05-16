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
        public double PanelInclinationDeg { get; set; }
        private double _gridOffsetX;
        private double _gridOffsetY;
        private double _numRows;
        private double _numCols;
        private double _panelWidthPx;
        private double _panelHeightPx;
        private double _startPaddingPx;
        private List<Rect> _panelRects;

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

        // Animation state
        private DateTime _lastRenderTime;
        private double _speedPxPerSec;

        // Simulation parameters
        private DateTime _simStartTime;
        private TimeSpan _elapsedTime;
        public TimeSpan ElapsedTime => _elapsedTime;
        public event EventHandler<TimeSpan> AnimationStopped;
        public bool IsRunning { get; private set; }

        public Robot(Canvas solarPanelCanvas, Canvas animationCanvas, int widthMm, int heightMm, string imageUri)
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
        public void Configure(double gridOffsetX, double gridOffsetY, double numCols, double numRows, double panelWidthPx, double panelHeightPx, double startPaddingPx, List<Rect> panelRectsPx)
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

        /// <summary>
        /// Sets a new coverage path for the robot and resets its progress to the start.
        /// </summary>
        /// <param name="coveragePath">List of Points (canvas coords) the robot will follow.</param>
        public void SetCoveragePath(List<Point> coveragePath)
        {
            _coveragePath = coveragePath ?? throw new ArgumentNullException(nameof(coveragePath));
            _currentWaypoint = 0;
        }


        // Begins animation at the given speed.
        public void AnimationStart(double speedMmPerSec, double scaleFactor, int tickRate = 60)
        {
            IsRunning = true;
            _speedPxPerSec = speedMmPerSec * scaleFactor;
            _lastRenderTime = DateTime.Now;
            _simStartTime = DateTime.Now;

            // Unsubscribe first, then subscribe exactly once
            CompositionTarget.Rendering -= OnRendering;
            CompositionTarget.Rendering += OnRendering;
        }

        public void AnimationStop()
        {
            if (!IsRunning) return; 

            IsRunning = false;
            CompositionTarget.Rendering -= OnRendering;
            _isTurning = false;
            BackToOrigin();
            _elapsedTime = DateTime.Now - _simStartTime;

            // Fire the event, showing the animation has stopped
            AnimationStopped?.Invoke(this, _elapsedTime);
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

            // Call modified MoveStep that takes dt
            MoveStep(dt);
        }

        private void MoveStep(double dt)
        {
            // Turning in place
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

                // turnDuration = arcLength / speed = (pi/2 * r) / (px/sec)
                double radiusPx = _panelWidthPx * 0.3;
                _turnDurationSec = (Math.PI / 2 * radiusPx) / _speedPxPerSec;
                _turnElapsedSec = 0;
                _initialAngle = _currentAngle;
                _isTurning = true;

                _currentWaypoint++;
            }
            else
            {
                // detect if this segment is mostly vertical
                bool isClimbing = vec.Y > 0 && Math.Abs(vec.Y) > Math.Abs(vec.X);

                // convert to radians
                double inclineRad = PanelInclinationDeg * Math.PI / 180.0;

                // only slow down on climbs
                double gradeFactor = isClimbing ? Math.Cos(inclineRad) : 1.0;

                // final movement this tick
                double move = _speedPxPerSec * dt * gradeFactor;
                double nx = cx + vec.X * move;
                double ny = cy + vec.Y * move;

                // Debug for panel inclination
                // System.Diagnostics.Debug.WriteLine($"[MoveStep] move={move}, gradeFactor={gradeFactor}");

                double newRight = _animCanvas.ActualWidth - (nx + _robotImage.Width / 2);
                double newBottom = ny - _robotImage.Height / 2;
                Canvas.SetRight(_robotImage, newRight);
                Canvas.SetBottom(_robotImage, newBottom);
            }
        }
    }
}
