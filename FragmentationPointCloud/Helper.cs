using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB;
using Serilog;
using D3 = GeometryLib.Double.D3;

namespace Revit
{
    public class Helper
    {
        /// <summary>
        /// transformation from internal Revit crs to user crs
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="set"></param>
        /// <returns></returns>
        public static Transform GetTransformation(in Document doc, in SettingsJson set)
        {
            ProjectLocation projloc = doc.ActiveProjectLocation;
            ProjectPosition position_data = projloc.GetProjectPosition(XYZ.Zero);
            double angle = position_data.Angle;
            double elevation = 0;
            double easting = 0;
            double northing = 0;
            // Differentiation whether a reduction is to be calculated or not
            if (set.CoordinatesReduction == false)
            {
                elevation = position_data.Elevation;
                easting = position_data.EastWest;
                northing = position_data.NorthSouth;
            }
            Transform tRotation = Transform.CreateRotation(XYZ.BasisZ, angle);
            XYZ vectorTranslation = new XYZ(easting, northing, elevation);
            Transform tTranslation = Transform.CreateTranslation(vectorTranslation);
            Transform transformation = tTranslation.Multiply(tRotation);

            return transformation;
        }
        public class BoundingBox
        {
            public XYZ Min { get; set; }
            public XYZ Max { get; set; }

            public BoundingBox(XYZ min, XYZ max)
            {
                Min = min;
                Max = max;
            }
        }
        public class OrientedBoundingBox
        {
            public bool Oriented { get; }
            public string StateId { get; }
            public string ObjectGuid { get; }
            public string ElementId { get; }
            public XYZ Center { get; }
            public XYZ XDirection { get; }
            public XYZ YDirection { get; }
            public XYZ ZDirection { get; }
            public double HalfLength { get; }
            public double HalfWidth { get; }
            public double HalfHeight { get; }
            public XYZ Min { get; }
            public XYZ Max { get; }

            public OrientedBoundingBox(bool oriented, string stateId, string objectGuid, string elementId, XYZ center, XYZ xDir, XYZ yDir, XYZ zDir, double halfLength, double halfWidth, double halfHeight, XYZ min = default, XYZ max= default)
            {
                Oriented = oriented;
                StateId = stateId;
                ObjectGuid = objectGuid;
                ElementId = elementId;
                Center = center;
                XDirection = xDir;
                YDirection = yDir;
                ZDirection = zDir;
                HalfLength = halfLength;
                HalfWidth = halfWidth;
                HalfHeight = halfHeight;
                Min = min;
                Max = max;
            }
        }

        public static bool Fragmentation2Pcd(string exeGreen3DPath, string command)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo(exeGreen3DPath, command);
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.CreateNoWindow = true;
                Process process = new Process();
                process.StartInfo = processInfo;

                process.Start();

                while (!process.StandardOutput.EndOfStream)
                {
                    string outputLine = process.StandardOutput.ReadLine();
                    Log.Information(outputLine);
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
        public static bool Pcd2e57(string pcdFilePath, string e57FilePath)
        {
            try
            {
                Process cloudCompareProcess = new Process();
                cloudCompareProcess.StartInfo.FileName = Constants.cloudComparePath;
                cloudCompareProcess.StartInfo.Arguments = "-SILENT -O \"" + pcdFilePath + "\" -C_EXPORT_FMT E57 -SAVE_CLOUDS FILE \"" + e57FilePath + "\"";
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
                ProcessStartInfo cmdInfo = new ProcessStartInfo
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    FileName = "cmd.exe",
                };

                Process cmd = new Process();
                cmd.StartInfo = cmdInfo;
                cmd.Start();

                StreamWriter inStream = cmd.StandardInput;
                inStream.WriteLine(Constants.directory);
                inStream.WriteLine(Constants.lineDecap);
                string outputPath = System.IO.Path.Combine(path, "07_FragmentationBBox");

                if (File.Exists(System.IO.Path.Combine(outputPath, guid + ".rcp")))
                {
                    File.Delete(System.IO.Path.Combine(outputPath, guid + ".rcp"));
                }
                inStream.WriteLine("{0}decap.exe{0} --importWithLicense {0}{1}{0} {0}{2}{0} {0}{3}{0}", '"', outputPath, guid, e57FilePath);
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

        /// <summary>
        ///     Conversion methods between an IFC
        ///     encoded GUID string and a .NET GUID.
        ///     https://github.com/hakonhc/IfcGuid/blob/master/IfcGuid/IfcGuid.cs
        /// </summary>
        private static readonly char[] Base64Chars =
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C',
            'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c',
            'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p',
            'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '_', '$'
        };
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
            var num = new uint[6];
            var str = new char[22];
            byte[] b = guid.ToByteArray();

            // Creation of six 32 Bit integers from the components of the GUID structure
            num[0] = BitConverter.ToUInt32(b, 0) / 16777216;
            num[1] = BitConverter.ToUInt32(b, 0) % 16777216;
            num[2] = (uint)(BitConverter.ToUInt16(b, 4) * 256 + BitConverter.ToUInt16(b, 6) / 256);
            num[3] = (uint)(BitConverter.ToUInt16(b, 6) % 256 * 65536 + b[8] * 256 + b[9]);
            num[4] = (uint)(b[10] * 65536 + b[11] * 256 + b[12]);
            num[5] = (uint)(b[13] * 65536 + b[14] * 256 + b[15]);

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
            if (uniqueId.Length != 45)
            {
                throw new Exception("the given string isn't revit unique id");
            }
            int elementId = int.Parse(uniqueId.Substring(37), NumberStyles.AllowHexSpecifier);
            int tempId = int.Parse(uniqueId.Substring(28, 8), NumberStyles.AllowHexSpecifier);
            int xor = tempId ^ elementId;
            return new Guid(uniqueId.Substring(0, 28) + xor.ToString("x8"));
        }

    }
}
