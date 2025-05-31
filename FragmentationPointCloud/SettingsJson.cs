using System;
using System.IO;
using System.Text.Json;

namespace Revit
{
    /// <summary>
    /// Represents the configuration settings for an application, typically loaded from or saved to a JSON file.
    /// </summary>
    /// <remarks>This record encapsulates various configuration parameters, such as file paths, geometric
    /// settings,  and application-specific options. It provides methods to read settings from a JSON file and write 
    /// settings back to a JSON file. Use this type to manage and persist application settings in a structured
    /// way.</remarks>
    public record SettingsJson
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true,
            //PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public double BBox_Buffer { get; set; }
        public bool OnlyPlanarFaces { get; set; }
        public bool CoordinatesReduction { get; set; }
        public double PgmHeightOfLevel_Meter { get; set; }
        public double PgmImageExpansion_Px { get; set; }
        public double PgmImageResolution_Meter { get; set; }
        public string VerbosityLevel { get; set; } = string.Empty;
        public string PathPointCloud { get; set; } = string.Empty;
        public string PathCloudCompare { get; set; } = string.Empty;
        public string PathDecap { get; set; } = string.Empty;
        public string ServerUuid { get; set; } = string.Empty;
        public double FragmentationVoxelResolution_Meter { get; set; }
        public int StepsPerFullTurn { get; set; }
        public double SphereDiameter_Meter { get; set; }
        public double HeightOfScanner_Meter { get; set; }
        public double NoiseOfScanner_Meter { get; set; }
        public double Beta_Degree { get; set; }
        public double MinDF_Meter { get; set; }
        public double MaxDF_Meter { get; set; }
        public double MaxPlaneDist_Meter { get; set; }
        public double GridSpacing_Meter { get; set; }
        public int GridColumns { get; set; }
        public int GridRows { get; set; }

        /// <summary>
        /// Reads a JSON file from the specified path and deserializes its content into a <see cref="SettingsJson"/>
        /// object.
        /// </summary>
        /// <remarks>This method reads the entire content of the specified JSON file into memory. Ensure
        /// the file size is manageable to avoid excessive memory usage. The caller is responsible for ensuring the file
        /// exists and contains valid JSON.</remarks>
        /// <param name="path">The file path of the JSON file to read. Must be a valid, non-null, and non-empty string.</param>
        /// <returns>A <see cref="SettingsJson"/> object containing the deserialized data from the JSON file.</returns>
        public static SettingsJson ReadSettingsJson(string path)
        {
            string jText = File.ReadAllText(path);

            // Ensure the deserialization result is not null
            return JsonSerializer.Deserialize<SettingsJson>(jText)
                   ?? throw new InvalidOperationException("Failed to deserialize the JSON file into a SettingsJson object.");
        }

        /// <summary>
        /// Writes the specified <see cref="SettingsJson"/> object to a file in JSON format at the specified path.
        /// </summary>
        /// <remarks>The JSON output is formatted with indented styling for readability. If the operation
        /// fails, an error message is written to the console.</remarks>
        /// <param name="json">The <see cref="SettingsJson"/> object to serialize and write to the file.</param>
        /// <param name="path">The file path where the JSON representation of the object will be written. Must not be null or empty.</param>
        public static void WriteSettingsJson(SettingsJson json, string path)
        {
            try
            {
                string jExportText = JsonSerializer.Serialize(json, _options);
                File.WriteAllText(path, jExportText);
                Console.WriteLine(@"write settings");
            }
            catch
            {
                Console.WriteLine(@" Fail");
            }
        }
    }
}