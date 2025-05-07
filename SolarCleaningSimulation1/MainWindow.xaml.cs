using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SolarCleaningSimulation1.Classes;

namespace SolarCleaningSimulation1
{
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
            robot_speed_input_mm_s.Text = "10000";  // Robot Speed (mm/s)
        }

        // Variables
        private double _currentScaleFactor; // Variable for converting mm to pixels - calculated in the GenerateGrid_Click() method
        public const int CanvasPadding = 20; //value for padding the canvases for solar panels and for the roof

        private double _numRows, _numCols;
        private double _gridOffsetX, _gridOffsetY; // Offsets for the grid in pixels
        private double _panelWidthPx, _panelHeightPx; // Panel dimensions in pixels
        public const double panelPaddingMm = 2;

        private int robot_width_mm = 1200, robot_height_mm = 1450; // Robot dimensions in milimiters

        public double panel_inclination = 0;
        
        private Robot robot;
        private Roof roof;

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
                roof = new Roof(roofWidthM, roofLengthM);
                

                roof.CalculateLayout(canvasWidth: solar_panel_canvas.ActualWidth, canvasHeight: solar_panel_canvas.ActualHeight,
                                        canvasPadding: CanvasPadding, panelWidthMm: panelWidthMm, panelLengthMm: panelLengthMm, panelPaddingMm: panelPaddingMm);

                // Clear the canvas
                solar_panel_canvas.Children.Clear();

                // Draw roof rectangle
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

                // Draw each panel
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
                solar_panel_canvas.Width = roof.RoofRect.Width + 2 * CanvasPadding;
                solar_panel_canvas.Height = roof.RoofRect.Height + 2 * CanvasPadding;

                // Store for the robot placement logic
                _numCols = roof.CalculateColumns(panelWidthMm, panelPaddingMm);
                _numRows = roof.CalculateRows(panelLengthMm, panelPaddingMm);
                _currentScaleFactor = roof.ScaleFactor;
                _gridOffsetX = roof.RoofRect.X;
                _gridOffsetY = roof.RoofRect.Y;
                _panelWidthPx = panelWidthMm * _currentScaleFactor;
                _panelHeightPx = panelLengthMm * _currentScaleFactor;

                // Display the robot placement button
                place_robot_button.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Please enter valid numeric values for roof and panel dimensions.",
                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
      
        private void place_robot_button_Click(object sender, RoutedEventArgs e)
        {
            robot = new Robot(solar_panel_canvas, animation_canvas,
                                    widthMm: robot_width_mm, heightMm: robot_height_mm,
                                    imageUri: "pack://application:,,,/Resources/robot-picture-01.png");

            robot.Configure(gridOffsetX: _gridOffsetX,
                gridOffsetY: _gridOffsetY,
                numCols: _numCols,
                numRows: _numRows,
                panelWidthPx: _panelWidthPx,
                panelHeightPx: _panelHeightPx,
                startPaddingPx: 10,
                panelRectsPx: roof.PanelRects);

            robot.PlaceOnRoof(_currentScaleFactor);
            robot.BuildCoveragePath(panelPaddingPx: panelPaddingMm * _currentScaleFactor);

            start_simulation_button.Visibility = Visibility.Visible;
            stop_simulation_button.Visibility = Visibility.Visible;
        }

        private void start_simulation_button_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(robot_speed_input_mm_s.Text, out double robot_speed_mm_s))
            {
                robot.AnimationStart(robot_speed_mm_s, _currentScaleFactor);

                // User display
                error_label.Content = "Animation Started!";
            }
            else
            {
                MessageBox.Show("Please enter a valid numeric value for robot speed in mm/s.",
                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void stop_simulation_button_Click(object sender, RoutedEventArgs e)
        {
            robot.AnimationStop();
            error_label.Content = "Animation Ended!";
        }
    }

}
