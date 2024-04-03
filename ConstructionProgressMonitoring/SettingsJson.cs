using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Revit
{
    public class SettingsJson
    {
        public double ConstructionTolerance_Meter { get; set; }
        public double AngleDeviation_Degree { get; set; }
        public double BBox_Buffer { get; set; }
        public bool VisibilityAnalysis { get; set; }
        public bool OnlyPlanarFaces { get; set; }
        public bool CoordinatesReduction { get; set; }
        public bool CalculateShift{ get; set; }
        public bool CalculateVertexDistance { get; set; }
        public double PgmHeightOfLevel_Meter { get; set; }
        public double PgmImageExpansion_Px { get; set; }
        public double PgmImageResolution_Meter { get; set; }
        public string VerbosityLevel { get; set; }
        public string PathPointCloud { get; set; }
        public double FragmentationVoxelResolution_Meter { get; set; }

        #region file handling
        /// <summary>
        /// file names
        /// </summary>
        public string CsvR { get; set; }
        public string CsvRRef { get; set; }
        public string CsvVisibleFaces { get; set; }
        public string CsvVisibleFacesRef { get; set; }
        public string CsvS { get; set; }
        public string CsvSRef { get; set; }
        public string CsvMatchesR { get; set; }
        public string CsvMatchesRRef { get; set; }
        public string CsvMatchesS { get; set; }
        public string CsvMatchesSRef { get; set; }
        public string ObjR { get; set; }
        public string ObjS { get; set; }

        public double StepsPerFullTurn { get; set; }
        public string Pointcloud { get; set; }

        #endregion file handling
        public double FilterAngle_Degree { get; set; }
        public double FilterD_Meter { get; set; }
        public double FilterBuffer_Meter { get; set; }

        #region constants
        public double MaxPlaneTol_Degree { get; set; }
        public double MaxPlaneDist_Meter { get; set; }

        public double MinDistToStation_Meter { get; set; }
        public double MaxDistToStation_Meter { get; set; }

        public double MinCoverage_Percent { get; set; }
        public double MinPatchLength_Meter { get; set; }
        public double MaxPatchLength_Meter { get; set; }

        public double MaxDistToArc_Meter { get; set; }
        public double MaxRayPlaneDiffCos { get; set; }

        public double QuantilAlpha2_5 { get; set; }
        public double QuantilAlpha5 { get; set; }

        #endregion constants

        /// <summary>
        /// deserialize json file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static SettingsJson ReadSettingsJson(string path)
        {
            string jText = File.ReadAllText(path);

            //create collection from each json file
            SettingsJson settings = JsonConvert.DeserializeObject<SettingsJson>(jText);
            return settings;
        }
        /// <summary>
        /// serialize json file
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static void WriteSettingsJson(SettingsJson json, string path)
        {
            try
            {
                string jExportText = JsonConvert.SerializeObject(json, Formatting.Indented);
                File.WriteAllText(path, jExportText);
                Console.WriteLine("write settings");
            }
            catch
            {
                Console.WriteLine(" Fail");
            }
        }

    }
}
