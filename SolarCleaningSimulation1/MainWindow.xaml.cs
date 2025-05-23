﻿using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using SolarCleaningSimulation1.Classes;

namespace SolarCleaningSimulation1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetDefaultValues();  // Automatically set default values and generate the grid

            this.SizeChanged += Window_SizeChanged;

            // fill with the enum values and pick a default
            CoveragePathComboBox.ItemsSource = Enum.GetValues(typeof(RobotPath.CoveragePathType));
            CoveragePathComboBox.SelectedItem = RobotPath.CoveragePathType.ZigZag;
        }

        private void SetDefaultValues()
        {
            // Predefined dimensions (change these values as needed)
            RoofWidthInput.Text = "20";             // Roof Width (m)
            RoofLengthInput.Text = "8";             // Roof Length (m)
            WidthInput.Text = "1100";               // Solar Panel Width (mm)
            LengthInput.Text = "2000";              // Solar Panel Length (mm)
            PanelInclinationInput.Text = "25";      // Solar panel/roof inclination in degrees
            robot_speed_input_mm_s.Text = "1000";   // Default Robot Speed (mm/s) => 1 m/s
            speed_multiplier_input.Text = "5";      // Default 5x speed for animation
        }

        // Variables
        private double _currentScaleFactor; // Variable for converting mm to pixels - calculated in the GenerateGrid_Click() method
        public const int CanvasPadding = 20; //value for padding the canvases for solar panels and for the roof

        private double _numRows, _numCols;
        private double _gridOffsetX, _gridOffsetY; // Offsets for the grid in pixels
        private double _panelWidthPx, _panelHeightPx; // Panel dimensions in pixels
        public const double panelPaddingMm = 2;
        private bool _gridInitialized = false;

        private int robot_width_mm = 1200, robot_height_mm = 1450; // Robot dimensions in milimiters

        public double panel_inclination = 0; 
        private double _speedMultiplier;

        private Robot robot;
        private Roof roof;

        // Data Saving
        private int _runCount = 0;
        private bool _runRecorded = false;

        private void CoveragePathComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (robot == null) return;
            RegenerateCoveragePath();
        }

        // shared logic to build & hand off the path
        private void RegenerateCoveragePath()
        {
            var selected = (RobotPath.CoveragePathType)CoveragePathComboBox.SelectedItem;
            var waypoints = RobotPath.GenerateCoveragePath(
                selected,
                panelPaddingPx: panelPaddingMm * _currentScaleFactor,
                robotBrushPx: robot_width_mm * _currentScaleFactor,
                numCols: (int)_numCols,
                numRows: (int)_numRows,
                panelWidthPx: _panelWidthPx,
                panelHeightPx: _panelHeightPx
            );
            robot.SetCoveragePath(waypoints);
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // only regenerate if we’ve already drawn the grid once
            if (!_gridInitialized) return;

            // force a fresh grid
            GenerateGrid_Click(generate_grid_button, null);
        }

        private void saveFilesToCSVButton_Click(object sender, RoutedEventArgs e)
        {
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            DataRecorder.SaveAllToCsv(desktop);
            MessageBox.Show($"All {_runCount} runs saved to:\n{desktop}", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Generating the grid based on the width and length of the solar panels that
        // were introduced by the user.
        private void GenerateGrid_Click(object sender, RoutedEventArgs e)
        {
            // Clean up old robot
            if (robot != null)
            {
                // stop any animation
                robot.AnimationStop();

                // remove the animation canvas (and any robot image in it) from the roof canvas
                if (animation_canvas.Parent is Panel parent)
                    parent.Children.Remove(animation_canvas);

                // clear out the animation canvas children
                animation_canvas.Children.Clear();

                // drop the reference so we can make a new robot later
                robot = null;

                // make buttons invisible so player cannot access them
                start_simulation_button.Visibility = Visibility.Hidden;
                stop_simulation_button.Visibility= Visibility.Hidden;
            }

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
                _gridInitialized = true;
            }
            else
            {
                MessageBox.Show("Please enter valid numeric values for roof and panel dimensions.",
                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void place_robot_button_Click(object sender, RoutedEventArgs e)
        {
            // if we already have a robot, do nothing
            if (robot != null)
                return;

            robot = new Robot(solar_panel_canvas, animation_canvas,
                                    widthMm: robot_width_mm, heightMm: robot_height_mm,
                                    imageUri: "pack://application:,,,/Resources/robot-picture-01.png");

            // Configure inclination of panels into robot algorithms
            if (double.TryParse(PanelInclinationInput.Text, out double inclDeg))
                robot.PanelInclinationDeg = inclDeg;
            else robot.PanelInclinationDeg = 0; // default is 0 degrees

            robot.Configure(gridOffsetX: _gridOffsetX, gridOffsetY: _gridOffsetY, numCols: _numCols, numRows: _numRows,
                panelWidthPx: _panelWidthPx, panelHeightPx: _panelHeightPx, startPaddingPx: 10, panelRectsPx: roof.PanelRects);

            robot.PlaceOnRoof(_currentScaleFactor);

            // reset our flag and hook the event so we record if the robot
            // ever stops itself (i.e. reaches the end of the path)
            _runRecorded = false;
            robot.AnimationStopped += (s, elapsed) => Dispatcher.Invoke(() => RecordRun(elapsed));

            // Choose the path type
            CoveragePathComboBox.Visibility = Visibility.Visible;
            dropbox_path_label.Visibility = Visibility.Visible;

            // get their selection
            var selectedPattern = (RobotPath.CoveragePathType)CoveragePathComboBox.SelectedItem;

            RegenerateCoveragePath();

            // now safely show & enable Start/Stop
            start_simulation_button.Visibility = Visibility.Visible;
            stop_simulation_button.Visibility = Visibility.Visible;

        }

        private void RecordRun(TimeSpan elapsed)
        {
            if (_runRecorded) return;       // only once per run
            _runRecorded = true;
            _runCount++;

            DataRecorder.AddRun(
                animationNumber: _runCount,
                elapsedTime: elapsed * _speedMultiplier,
                roofLength_m: double.Parse(RoofLengthInput.Text),
                roofWidth_m: double.Parse(RoofWidthInput.Text),
                panelWidth_mm: double.Parse(WidthInput.Text),
                panelLength_mm: double.Parse(LengthInput.Text),
                panelInclination_deg: double.Parse(PanelInclinationInput.Text),
                robotSpeed_mmPerSec: double.Parse(robot_speed_input_mm_s.Text),
                speedMultiplier: double.Parse(speed_multiplier_input.Text),
                pathType: (RobotPath.CoveragePathType)CoveragePathComboBox.SelectedItem
            );

            error_label.Content = $" Animation Ended!\n Elapsed: {elapsed * _speedMultiplier:mm\\:ss}" +
                    $"\n Run #{_runCount} recorded.\n";
        }

        private void start_simulation_button_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(robot_speed_input_mm_s.Text, out double robot_speed_mm_s) && double.TryParse(speed_multiplier_input.Text, out double speed_multiplier))
            {
                // allow recording again on each new run
                _runRecorded = false;

                _speedMultiplier = speed_multiplier;
                robot.AnimationStart(robot_speed_mm_s, speed_multiplier, _currentScaleFactor);

                // User display
                error_label.Content = " Animation Started!";
            }
            else
            {
                MessageBox.Show("Please enter a valid numeric value for robot speed in mm/s and/or speed multiplier.",
                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void stop_simulation_button_Click(object sender, RoutedEventArgs e)
        {
            if (!robot.IsRunning) return;

            robot.AnimationStop();

            RecordRun(robot.ElapsedTime * _speedMultiplier);

            // var elapsed = robot.ElapsedTime * _speedMultiplier;
            // error_label.Content = $"Animation Ended!\n Elapsed: " + elapsed.ToString(@"mm\:ss");
        }
    }

}
