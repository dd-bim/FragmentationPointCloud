using System;
using System.IO;
using Newtonsoft.Json;

namespace Revit
{
    public class SettingsJson
    {
        public double BBox_Buffer { get; set; }
        public bool OnlyPlanarFaces { get; set; }
        public bool CoordinatesReduction { get; set; }
        public double PgmHeightOfLevel_Meter { get; set; }
        public double PgmImageExpansion_Px { get; set; }
        public double PgmImageResolution_Meter { get; set; }
        public string VerbosityLevel { get; set; }
        public string PathPointCloud { get; set; }
        public string PathCloudCompare { get; set; }
        public string PathDecap { get; set; }
        public string ServerUuid { get; set; }
        public double FragmentationVoxelResolution_Meter { get; set; }
        public int StepsPerFullTurn { get; set; }
        public double SphereDiameter_Meter { get; set; }
        public double HeightOfScanner_Meter { get; set; }
        public double NoiceOfScanner_Meter { get; set; }
        public double Beta_Degree { get; set; }
        public double MinDF_Meter { get; set; }
        public double MaxDF_Meter { get; set; }
        public double MaxPlaneDist_Meter { get; set; }
        public double GridSpacing_Meter { get; set; }
        public int GridColumns { get; set; }
        public int GridRows { get; set; }

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
