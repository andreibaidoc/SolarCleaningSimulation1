using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using SolarCleaningSimulation1.Classes;

namespace SolarCleaningSimulation1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetDefaultValues();  // Automatically set default values and generate the grid
        }

        private void SetDefaultValues()
        {
            // Predefined dimensions (change these values as needed)
            RoofWidthInput.Text = "20";             // Roof Width (m)
            RoofLengthInput.Text = "8";             // Roof Length (m)
            WidthInput.Text = "1100";               // Solar Panel Width (mm)
            LengthInput.Text = "2000";              // Solar Panel Length (mm)
            robot_speed_input_mm_s.Text = "100";    // Robot Speed (mm/s)
        }

        private double currentScaleFactor; // Variable for converting mm to pixels - calculated in the GenerateGrid_Click() method
        public const int CanvasPadding = 10; //value for padding the canvases for solar panels and for the roof
        double panelWidth, panelLength; // Panel dimensions in pixels

        private double _gridPaddingX, _gridPaddingY, _numRows, _numCols, _panelHeight;

        private double robot_startX, robot_startY; // Robot starting position in pixels

        Image robot_image = new Image
        {
            Height = 155,
            Width = 135,
            Source = new BitmapImage(new Uri("pack://application:,,,/Resources/robot-picture-01.png")),
            Stretch = System.Windows.Media.Stretch.Fill,
            Visibility = Visibility.Collapsed
        };

        

        // Generating the grid based on the width and length of the solar panels that
        // were introduced by the user.
        private void GenerateGrid_Click(object sender, RoutedEventArgs e)
        {
            // Further down, we will change the size of the solar panel canvas
            // this means that now whenever the user clicks, the canvas needs to 
            // return to initial state => this is why we have the following 3 lines

            solar_panel_canvas.Width = double.NaN;
            solar_panel_canvas.Height = double.NaN;
            solar_panel_canvas.UpdateLayout();

            if (double.TryParse(RoofWidthInput.Text, out double roofWidthM) &&
                double.TryParse(RoofLengthInput.Text, out double roofLengthM) &&
                double.TryParse(WidthInput.Text, out double panelWidthMm) &&
                double.TryParse(LengthInput.Text, out double panelLengthMm))
            {
                Roof roof = new Roof(roofWidthM, roofLengthM);
                const double extraPaddingX = 50, extraPaddingY = 50;
                const double panelPaddingMm = 2;

                roof.CalculateLayout(canvasWidth: solar_panel_canvas.ActualWidth, canvasHeight: solar_panel_canvas.ActualHeight,
                                        canvasPadding: CanvasPadding, extraPaddingX: extraPaddingX, extraPaddingY: extraPaddingY,
                                        panelWidthMm: panelWidthMm, panelLengthMm: panelLengthMm, panelPaddingMm: panelPaddingMm);

                // Clear the canvas
                solar_panel_canvas.Children.Clear();

                // 1) Draw roof rectangle
                var r = roof.RoofRect;
                var roofRect = new Rectangle
                {
                    Width = r.Width,
                    Height = r.Height,
                    Fill = Brushes.LightGray,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(roofRect, r.X);
                Canvas.SetTop(roofRect, r.Y);
                solar_panel_canvas.Children.Add(roofRect);

                // 2) Draw each panel
                foreach (var pr in roof.PanelRects)
                {
                    var panel = new Rectangle
                    {
                        Width = pr.Width,
                        Height = pr.Height,
                        Fill = Brushes.DarkBlue,
                        Stroke = Brushes.LightGray,
                        StrokeThickness = 1
                    };
                    Canvas.SetLeft(panel, pr.X);
                    Canvas.SetTop(panel, pr.Y);
                    solar_panel_canvas.Children.Add(panel);
                }

                // Resize canvas to fit everything (roof + paddings)
                solar_panel_canvas.Width = roof.RoofRect.Width + 2 * (CanvasPadding + extraPaddingX);
                solar_panel_canvas.Height = roof.RoofRect.Height + 2 * (CanvasPadding + extraPaddingY);

                // Store for the robot placement logic
                _numCols = roof.CalculateColumns(panelWidthMm, panelPaddingMm);
                _numRows = roof.CalculateRows(panelLengthMm, panelPaddingMm);
                currentScaleFactor = roof.ScaleFactor;

                // Display the robot placement button
                place_robot_button.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Please enter valid numeric values for roof and panel dimensions.",
                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        private void add_animation_canvas()
        {
            if (animation_canvas.Parent != null)
            {
                var parent = (Panel)robot_image.Parent;
                parent.Children.Remove(robot_image);
            }
            solar_panel_canvas.Children.Add(animation_canvas);

            // Find the coordinates of the starting position - bottom left corner (relative to solar panel canvas)
            robot_startX = _gridPaddingX + 10;
            robot_startY = _gridPaddingY + 10;

            animation_canvas.Width = solar_panel_canvas.Width - robot_startX * 2;
            animation_canvas.Height = solar_panel_canvas.Height - robot_startY * 2;
            Canvas.SetRight(animation_canvas, robot_startX);
            Canvas.SetBottom(animation_canvas, robot_startY);
        }

        private List<Point> _coveragePath;
        private int _currentWaypoint;
        private void build_coverage_path_snake1()
        {
            _coveragePath = new List<Point>();

            double pad = 2;                         // panel-to-panel padding
            double xStep = panelWidth + pad;        // horizontal step
            double yStep = panelLength + pad;        // vertical   step

            // compute the two extreme Y centers
            double yTop = panelLength / 2;
            double yBottom = yTop + (_numRows - 1) * yStep;

            // starting X in the rightmost column
            double xStart = (_numCols - 1) * xStep + panelWidth / 2;
            double xCurrent = xStart;

            // 1) FULL first climb: bottom → top
            //    (robot is already placed at (xStart, yBottom))
            _coveragePath.Add(new Point(xCurrent, yBottom));

            // we just went “up,” so next vertical should be “down”
            bool nextGoesDown = false;

            // 2) Snake leftward through the remaining columns
            for (int stripe = 1; stripe < _numCols; stripe++)
            {
                // a) shift left one column at the current row
                double yHere = nextGoesDown ? yTop : yBottom;
                xCurrent -= xStep;
                _coveragePath.Add(new Point(xCurrent, yHere));

                // b) full vertical leg: if nextGoesDown, go to yBottom; otherwise go to yTop
                double yThere = nextGoesDown ? yBottom : yTop;
                _coveragePath.Add(new Point(xCurrent, yThere));

                // flip direction for the next stripe
                nextGoesDown = !nextGoesDown;
            }

            _currentWaypoint = 0;
        }
      
        private void place_robot_button_Click(object sender, RoutedEventArgs e)
        {

            // Add and resize the animation canvas
            add_animation_canvas();

            build_coverage_path_snake1();

            error_label.Content = "Error Displaying Label: " + $"\n startX = {robot_startX}" + $"\n startY = {robot_startY}"
                                    + $"\n _gridPaddingX = {_gridPaddingX}" + $"\n _gridPaddingY = {_gridPaddingY}" + $"\n _numRows = {_numRows}" + $"\n _numCols = {_numCols}"
                                    + $"\n animation_canvas.ActualWidth = {animation_canvas.ActualWidth}" + $"\n animation_canvas.Height = {animation_canvas.Height}";

            // Remove the robot_image from its current parent if it has one
            if (robot_image.Parent != null)
            {
                var parent = (Panel)robot_image.Parent;
                parent.Children.Remove(robot_image);
            }
            animation_canvas.Children.Add(robot_image);

            // Change the robot image scale
            robot_image.Width = robot_width_mm * currentScaleFactor;
            robot_image.Height = robot_height_mm * currentScaleFactor;

            // Place the robot at the starting position
            Canvas.SetRight(robot_image, 0);
            Canvas.SetBottom(robot_image, 0);

            robot_image.Visibility = Visibility.Visible; // Display the robot picture
            start_simulation_button.Visibility = Visibility.Visible; // Show the start simulation button
            stop_simulation_button.Visibility = Visibility.Visible; // Show the stop simulation button
        }

        // Variables used for simulation
        private double robotSpeedPxPerTick;
        private DateTime simulationStartTime;
        private DispatcherTimer simulationTimer = new DispatcherTimer();

        private void stop_simulation_button_Click(object sender, RoutedEventArgs e)
        {
            if (simulationTimer != null && simulationTimer.IsEnabled)
            {
                simulationTimer.Stop();
                MessageBox.Show("Simulation stopped by user!");
            }
        }

        private int robot_width_mm = 1200, robot_height_mm = 1450; // Robot dimensions in milimiters

        private void start_simulation_button_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(robot_speed_input_mm_s.Text, out double robot_speed_mm_s))
            {
                simulationStartTime = DateTime.Now;

                // Calculate real-time movement speed (mm/s → px/tick)
                double tickRate = 60;
                double tickIntervalMs = 1000 / tickRate;
                robotSpeedPxPerTick = (robot_speed_mm_s * currentScaleFactor) / tickRate;

                // Init and start simulation timer
                simulationTimer.Interval = TimeSpan.FromMilliseconds(tickIntervalMs);
                simulationTimer.Tick += MoveRobotStep_Tick;
                simulationTimer.Start();

                // Error display
                error_label.Content = "Error Displaying Label: " + $"\n robotSpeedPxPerTick = {robotSpeedPxPerTick}" + $"\n robot_speed_mm_s = {robot_speed_mm_s}" +
                   $"\n solar_panel_canvas.ActualWidth = {solar_panel_canvas.ActualWidth}" + $"\n solar_panel_canvas.Height = {solar_panel_canvas.Height}";
            }
            else
            {
                MessageBox.Show("Please enter a valid numeric value for robot speed in mm/s.",
                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void MoveRobotStep_Tick(object sender, EventArgs e)
        {
            if (_currentWaypoint >= _coveragePath.Count)
            {
                simulationTimer.Stop();
                error_label.Content = "Done!";
                return;
            }

            // 1) grab robot’s current offsets (within animation_canvas)
            double cr = Canvas.GetRight(robot_image);
            double cb = Canvas.GetBottom(robot_image);

            // 2) compute its center in local coords
            double cx = animation_canvas.ActualWidth - cr - robot_image.Width / 2;
            double cy = cb + robot_image.Height / 2;

            // 3) target waypoint (also in animation_canvas coords)
            Point target = _coveragePath[_currentWaypoint];
            Vector toT = target - new Point(cx, cy);
            double dist = toT.Length;

            if (dist < robotSpeedPxPerTick)
            {
                // snap onto the point
                double newBottom = target.Y - robot_image.Height / 2;
                double newRight = animation_canvas.ActualWidth
                                   - (target.X + robot_image.Width / 2);

                Canvas.SetBottom(robot_image, newBottom);
                Canvas.SetRight(robot_image, newRight);

                _currentWaypoint++;
                RotateTowardsNext();
            }
            else
            {
                // move a step toward it
                toT.Normalize();
                double nx = cx + toT.X * robotSpeedPxPerTick;
                double ny = cy + toT.Y * robotSpeedPxPerTick;

                double newBottom = ny - robot_image.Height / 2;
                double newRight = animation_canvas.ActualWidth
                                   - (nx + robot_image.Width / 2);

                Canvas.SetBottom(robot_image, newBottom);
                Canvas.SetRight(robot_image, newRight);
            }
        }

        private void RotateTowardsNext()
        {
            if (_currentWaypoint >= _coveragePath.Count) return;

            // Robot’s current center in animation_canvas coords
            double cr = Canvas.GetRight(robot_image);
            double cb = Canvas.GetBottom(robot_image);
            double cx = animation_canvas.ActualWidth - cr - robot_image.Width / 2;
            double cy = cb + robot_image.Height / 2;

            // Vector to the next waypoint
            Point next = _coveragePath[_currentWaypoint];
            Vector v = next - new Point(cx, cy);

            double angle;

            // decide whether this is primarily vertical or horizontal
            if (Math.Abs(v.Y) > Math.Abs(v.X))
            {
                // vertical move
                angle = v.Y < 0 ? 0   // up
                               : 180; // down
            }
            else
            {
                // horizontal move
                angle = v.X < 0 ? -90  // left
                               : 90;  // right  (you probably never go right, but just in case)
            }

            robot_image.RenderTransform = new RotateTransform(
                angle,
                robot_image.Width / 2,
                robot_image.Height / 2
            );
        }


        public double panel_inclination = 0;
    }

}