using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SolarCleaningSimulation1.Classes
{
    internal class DataRecorder
    {
        // static list of all runs
        private static readonly List<DataRecorder> _runs = new List<DataRecorder>();

        // instance‐level properties (one per run)
        public int AnimationNumber { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public double RoofLength_m { get; set; }
        public double RoofWidth_m { get; set; }
        public double PanelWidth_mm { get; set; }
        public double PanelLength_mm { get; set; }
        public double PanelInclination_deg { get; set; }
        public double RobotSpeed_mmPerSec { get; set; }
        public double SpeedMultiplier { get; set; }
        public RobotPath.CoveragePathType PathType { get; set; }

        // private ctor: captures one run and adds to the static list
        private DataRecorder(int animationNumber, TimeSpan elapsedTime, double roofLength_m, double roofWidth_m, double panelWidth_mm,
            double panelLength_mm, double panelInclination_deg, double robotSpeed_mmPerSec, double speedMultiplier, RobotPath.CoveragePathType pathType)
        {
            AnimationNumber = animationNumber;
            ElapsedTime = elapsedTime;
            RoofLength_m = roofLength_m;
            RoofWidth_m = roofWidth_m;
            PanelWidth_mm = panelWidth_mm;
            PanelLength_mm = panelLength_mm;
            PanelInclination_deg = panelInclination_deg;
            RobotSpeed_mmPerSec = robotSpeed_mmPerSec;
            SpeedMultiplier = speedMultiplier;
            PathType = pathType;

            _runs.Add(this);
        }

        /// <summary>
        /// Call this once per run (e.g. in your stop handler) to record a new run.
        /// </summary>
        public static void AddRun(
            int animationNumber,
            TimeSpan elapsedTime,
            double roofLength_m,
            double roofWidth_m,
            double panelWidth_mm,
            double panelLength_mm,
            double panelInclination_deg,
            double robotSpeed_mmPerSec,
            double speedMultiplier,
            RobotPath.CoveragePathType pathType
        )
        {
            new DataRecorder(
                animationNumber,
                elapsedTime,
                roofLength_m,
                roofWidth_m,
                panelWidth_mm,
                panelLength_mm,
                panelInclination_deg,
                robotSpeed_mmPerSec,
                speedMultiplier,
                pathType
            );
        }

        /// <summary>
        /// Write a single CSV containing all runs.  The filename is timestamped
        /// (at the moment of saving) and dropped into outputDirectory.
        /// </summary>
        public static void SaveAllToCsv(string outputDirectory)
        {
            // build filename: SolarSim_Runs_yyyyMMdd_HHmmss.csv
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"SolarSim_Runs_{ts}.csv";
            string fullPath = Path.Combine(outputDirectory, filename);

            var sb = new StringBuilder();
            // header
            sb.AppendLine(
                "Run," +
                "Elapsed Time [s]," +
                "Roof Length [m]," +
                "Roof Width [m]," +
                "Panel Width [mm]," +
                "Panel Length [mm]," +
                "Inclination Angle [deg.]," +
                "Speed [mm/s]," +
                "Multiplier," +
                "PathType"
            );

            // one CSV row per run
            foreach (var r in _runs)
            {
                sb.AppendLine(
                    $"{r.AnimationNumber}," +
                    $"{r.ElapsedTime:mm\\:ss}," +
                    $"{r.RoofLength_m}," +
                    $"{r.RoofWidth_m}," +
                    $"{r.PanelWidth_mm}," +
                    $"{r.PanelLength_mm}," +
                    $"{r.PanelInclination_deg}," +
                    $"{r.RobotSpeed_mmPerSec}," +
                    $"{r.SpeedMultiplier}," +
                    $"{r.PathType}"
                );
            }

            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        }
    }
}
