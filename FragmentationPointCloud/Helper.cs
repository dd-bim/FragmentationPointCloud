using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

using Revit.Data;

using Serilog;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Revit
{
    public static class Helper
    {



        /// <summary>
        ///     Conversion methods between an IFC
        ///     encoded GUID string and a .NET GUID.
        ///     https://github.com/hakonhc/IfcGuid/blob/master/IfcGuid/IfcGuid.cs
        /// </summary>
        private static readonly char[] Base64Chars =
        [
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C',
            'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c',
            'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
            'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '_', '$'
        ];

        /// <summary>
        /// Creates a transformation matrix based on the specified document's project location and settings.
        /// </summary>
        /// <remarks>If coordinate reduction is enabled in the <paramref name="settings"/>, the
        /// transformation will only include rotation without translation. Otherwise, the transformation includes both
        /// rotation and translation based on the project's position data.</remarks>
        /// <param name="document">The document containing the active project location used to calculate the transformation.</param>
        /// <param name="settings">The settings that determine whether coordinate reduction is applied during the transformation calculation.</param>
        /// <returns>A <see cref="Transform"/> object representing the combined translation and rotation transformation based on
        /// the project's position and the specified settings.</returns>
        internal static Transform GetTransformation(in Document document, in SettingsJson settings)
        {
            var projectLocation = document.ActiveProjectLocation;
            var positionData = projectLocation.GetProjectPosition(XYZ.Zero);

            // Differentiation whether a reduction is to be calculated or not
            (double angle, double elevation, double easting, double northing) = settings.CoordinatesReduction == false
                ? (positionData.Angle, positionData.Elevation, positionData.EastWest, positionData.NorthSouth)
                : (0, 0, 0, 0);

            var rotation = Transform.CreateRotation(XYZ.BasisZ, angle);
            var origin = new XYZ(easting, northing, elevation);
            var translation = Transform.CreateTranslation(origin);
            var transformation = translation.Multiply(rotation);

            return transformation;
        }

        public static bool Fragmentation2Pcd(string exeGreen3DPath, string command)
        {
            try
            {
                var processInfo = new ProcessStartInfo(exeGreen3DPath, command)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                var process = new Process
                {
                    StartInfo = processInfo
                };

                process.Start();

                while (!process.StandardOutput.EndOfStream)
                {
                    string? outputLine = process.StandardOutput.ReadLine();
                    Log.Information("{outputLine}", outputLine);
                }

                process.WaitForExit();

                string output = process.StandardOutput.ReadToEnd();
                Console.WriteLine(output);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool Pcd2e57(string pcdFilePath, string e57FilePath, SettingsJson set)
        {
            try
            {
                var cloudCompareProcess = new Process();
                cloudCompareProcess.StartInfo.FileName = set.PathCloudCompare;
                cloudCompareProcess.StartInfo.Arguments = "-SILENT -O \"" + pcdFilePath +
                                                          "\" -C_EXPORT_FMT E57 -SAVE_CLOUDS FILE \"" + e57FilePath +
                                                          "\"";
                cloudCompareProcess.Start();
                cloudCompareProcess.WaitForExit();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool DeCap(string path, string guid, string e57FilePath)
        {
            try
            {
                var cmdInfo = new ProcessStartInfo
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = "cmd.exe"
                };

                var cmd = new Process
                {
                    StartInfo = cmdInfo
                };
                cmd.Start();

                var inStream = cmd.StandardInput;
                inStream.WriteLine(Constants.directory);
                inStream.WriteLine(Constants.lineDecap);

                if (File.Exists(Path.Combine(path, guid + ".rcp"))) File.Delete(Path.Combine(path, guid + ".rcp"));
                inStream.WriteLine("{0}decap.exe{0} --importWithLicense {0}{1}{0} {0}{2}{0} {0}{3}{0}", '"', path, guid,
                    e57FilePath);
                inStream.Close();
                cmd.WaitForExit();
                cmd.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void CvTo64(uint number, ref char[] result, int start, int len)
        {
            int digit;

            Debug.Assert(len <= 4, "Length must be equal or lett than 4");

            uint act = number;
            int digits = len;

            for (digit = 0; digit < digits; digit++)
            {
                result[start + len - digit - 1] = Base64Chars[(int)(act % 64)];
                act /= 64;
            }

            Debug.Assert(act == 0, "Logic failed, act was not null: " + act);
        }

        public static string ToIfcGuid(Guid guid)
        {
            uint[] num = new uint[6];
            char[] str = new char[22];
            byte[] b = guid.ToByteArray();

            // Creation of six 32 Bit integers from the components of the GUID structure
            num[0] = BitConverter.ToUInt32(b, 0) / 16777216;
            num[1] = BitConverter.ToUInt32(b, 0) % 16777216;
            num[2] = (uint)((BitConverter.ToUInt16(b, 4) * 256) + (BitConverter.ToUInt16(b, 6) / 256));
            num[3] = (uint)((BitConverter.ToUInt16(b, 6) % 256 * 65536) + (b[8] * 256) + b[9]);
            num[4] = (uint)((b[10] * 65536) + (b[11] * 256) + b[12]);
            num[5] = (uint)((b[13] * 65536) + (b[14] * 256) + b[15]);

            // Conversion of the numbers into a system using a base of 64
            int n = 2;
            int pos = 0;
            for (int i = 0; i < 6; i++)
            {
                CvTo64(num[i], ref str, pos, n);
                pos += n;
                n = 4;
            }

            return new string(str);
        }

        public static Guid ToGuid(string uniqueId)
        {
            var uid = uniqueId.AsSpan();
            if (uid.Length != 45) throw new Exception("the given string isn't revit unique id");
            int elementId = int.Parse(uid[37..], NumberStyles.AllowHexSpecifier);
            int tempId = int.Parse(uid.Slice(28, 8), NumberStyles.AllowHexSpecifier);
            int xor = tempId ^ elementId;
            return new Guid(string.Concat(uid[..28], xor.ToString("x8")));
        }

        public static Schema? GetSchemaByName(string schemaName)
        {
            var schemaList = Schema.ListSchemas();
            foreach (var schema in schemaList)
            {
                if (schema.SchemaName == schemaName)
                    return schema;
            }

            return null;
        }

        public static ElementId[] ReadMaterialsDS(Document doc)
        {
            var mat = new ElementId[12];
            using var trans = new Transaction(doc, "Read Materials");
            trans.Start();
            var ppSchema = GetSchemaByName("Green3DScanMaterials");
            if (ppSchema == null)
            {
                Log.Error("Schema Green3DScanMaterials not found.");
                return mat;
            }

            var collector = new FilteredElementCollector(doc);
            var dataStorageList = collector.OfClass(typeof(DataStorage)).ToElements();

            foreach (var ds in dataStorageList)
            {
                var ent = ds.GetEntity(ppSchema);
                if (ent.IsValid())
                {
                    var m0 = ent.Get<ElementId>(ppSchema.GetField("M0"));
                    var m1 = ent.Get<ElementId>(ppSchema.GetField("M1"));
                    var m2 = ent.Get<ElementId>(ppSchema.GetField("M2"));
                    var m3 = ent.Get<ElementId>(ppSchema.GetField("M3"));
                    var m4 = ent.Get<ElementId>(ppSchema.GetField("M4"));
                    var m5 = ent.Get<ElementId>(ppSchema.GetField("M5"));
                    var m6 = ent.Get<ElementId>(ppSchema.GetField("M6"));
                    var m7 = ent.Get<ElementId>(ppSchema.GetField("M7"));
                    var m8 = ent.Get<ElementId>(ppSchema.GetField("M8"));
                    var m9 = ent.Get<ElementId>(ppSchema.GetField("M9"));
                    var m10 = ent.Get<ElementId>(ppSchema.GetField("M10"));
                    var m11 = ent.Get<ElementId>(ppSchema.GetField("M11"));

                    trans.Commit();
                    mat[0] = m0;
                    mat[1] = m1;
                    mat[2] = m2;
                    mat[3] = m3;
                    mat[4] = m4;
                    mat[5] = m5;
                    mat[6] = m6;
                    mat[7] = m7;
                    mat[8] = m8;
                    mat[9] = m9;
                    mat[10] = m10;
                    mat[11] = m11;
                }
            }
            return mat;
        }

        public static ElementId[] AddMaterials(Document doc)
        {
            var dRed = new Color(150, 20, 0);
            var red = new Color(190, 70, 0);
            var lRed = new Color(210, 120, 0);
            var dOra = new Color(225, 160, 0);
            var ora = new Color(240, 200, 0);
            var yel = new Color(230, 220, 0);
            var yelGre = new Color(130, 150, 0);
            var gre = new Color(130, 150, 0);
            var dGre = new Color(70, 120, 0);
            var ddGre = new Color(20, 70, 0);
            var grey = new Color(105, 105, 105);
            var blue = new Color(0, 120, 200);

            var colorArr = new ElementId[12];

            using var t = new Transaction(doc, "AddMaterials");
            t.Start();

            void addMat(int index, string name, Color color)
            {
                var matDRed = Material.Create(doc, name);
                if (doc.GetElement(matDRed) is not Material mat0)
                {
                    Log.Error("Material {name} could not be created.", name);
                    return;
                }
                mat0.Color = color;
                colorArr[index] = matDRed;
            }

            // materials

            addMat(0, "CPM_darkred", dRed);
            addMat(1, "CPM_red", red);
            addMat(2, "CPM_lightred", lRed);
            addMat(3, "CPM_darkorange", dOra);
            addMat(4, "CPM_orange", ora);
            addMat(5, "CPM_ligthorange", yel);
            addMat(6, "CPM_yellow", yelGre);
            addMat(7, "CPM_yellowgreen", gre);
            addMat(8, "CPM_green", dGre);
            addMat(9, "CPM_darkgreen", ddGre);
            addMat(10, "CPM_grey", grey);
            addMat(11, "CPM_blue", blue);

            //DataStorage
            var progressPatchMaterials = GetSchemaByName("Green3DScanMaterials");

            if (progressPatchMaterials == null)
            {
                var sb = new SchemaBuilder(Guid.NewGuid());
                sb.SetSchemaName("Green3DScanMaterials");
                sb.SetReadAccessLevel(AccessLevel.Public);
                sb.SetWriteAccessLevel(AccessLevel.Public);

                sb.AddSimpleField("M0", typeof(ElementId));
                sb.AddSimpleField("M1", typeof(ElementId));
                sb.AddSimpleField("M2", typeof(ElementId));
                sb.AddSimpleField("M3", typeof(ElementId));
                sb.AddSimpleField("M4", typeof(ElementId));
                sb.AddSimpleField("M5", typeof(ElementId));
                sb.AddSimpleField("M6", typeof(ElementId));
                sb.AddSimpleField("M7", typeof(ElementId));
                sb.AddSimpleField("M8", typeof(ElementId));
                sb.AddSimpleField("M9", typeof(ElementId));
                sb.AddSimpleField("M10", typeof(ElementId));
                sb.AddSimpleField("M11", typeof(ElementId));

                progressPatchMaterials = sb.Finish();
            }

            var ent = new Entity(progressPatchMaterials);
            ent.Set("M0", colorArr[0]);
            ent.Set("M1", colorArr[1]);
            ent.Set("M2", colorArr[2]);
            ent.Set("M3", colorArr[3]);
            ent.Set("M4", colorArr[4]);
            ent.Set("M5", colorArr[5]);
            ent.Set("M6", colorArr[6]);
            ent.Set("M7", colorArr[7]);
            ent.Set("M8", colorArr[8]);
            ent.Set("M9", colorArr[9]);
            ent.Set("M10", colorArr[10]);
            ent.Set("M11", colorArr[11]);

            var materialsIdStorage = DataStorage.Create(doc);
            materialsIdStorage.SetEntity(ent);

            t.Commit();

            return colorArr;
        }

        public sealed record BoundingBox(XYZ Min, XYZ Max);

        public sealed record OrientedBoundingBox(
            bool Oriented, string StateId, string ObjectGuid, string ElementId, XYZ Center, XYZ XDirection, XYZ YDirection,
            XYZ ZDirection, double HalfLength, double HalfWidth, double HalfHeight,
            XYZ Min, XYZ Max);

        public class Paint
        {
            public static void ColourFace(Document doc, List<Id> ids, ElementId colourId)
            {
                foreach (var id in ids)
                {
                    // letzte Stelle entfernen, oder schon vorher entfernen, wenn ergebnisse zusammengefasst werden
                    var refFace = Reference.ParseFromStableRepresentation(doc, id.FaceId);
                    var face = doc.GetElement(refFace).GetGeometryObjectFromReference(refFace) as Face;

                    try
                    {
                        using var t1 = new Transaction(doc, "Painting");
                        t1.Start();
                        doc.Paint(refFace.ElementId, face, colourId);
                        t1.Commit();
                    }
                    catch
                    {
                        Log.Information("Error during coloring");
                    }
                }
            }
        }
    }
}