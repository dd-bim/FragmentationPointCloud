﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Autodesk.Revit.DB.ExtensibleStorage;
using Autodesk.Revit.DB;
using Serilog;
using S = ScantraIO.Data;
using D3 = GeometryLib.Double.D3;
using Autodesk.Revit.UI;

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
        public static Transform GetTransformation(in Document doc, in SettingsJson set, out D3.CoordinateSystem crs)
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
            
            crs = new D3.CoordinateSystem(new D3.Vector(easting, northing, elevation), new D3.Direction(angle, GeometryLib.Double.Constants.HALFPI), D3.Axes.X, new D3.Direction(angle + GeometryLib.Double.Constants.HALFPI, GeometryLib.Double.Constants.HALFPI));
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
        public class Paint
        {
            public static void ColourFace(Document doc, List<S.Id> ids, ElementId colourId)
            {
                foreach (var id in ids)
                {
                    // letzte Stelle entfernen, oder schon vorher entfernen, wenn ergebnisse zusammengefasst werden
                    Reference refFace = Reference.ParseFromStableRepresentation(doc, id.FaceId);
                    Face face = doc.GetElement(refFace).GetGeometryObjectFromReference(refFace) as Face;

                    try
                    {
                        using (Transaction t1 = new Transaction(doc, "Painting"))
                        {
                            t1.Start();
                            doc.Paint(refFace.ElementId, face, colourId);
                            t1.Commit();
                        }
                    }
                    catch
                    {
                        Log.Information("Fehler beim Einfärben");
                    }
                }
            }
        }
        public static Schema GetSchemaByName(string schemaName)
        {
            var schemaList = Schema.ListSchemas();
            foreach (var schema in schemaList)
            {
                if (schema.SchemaName == schemaName)
                {
                    return schema;
                }
            }
            return null;
        }
        public static ElementId[] ReadMaterialsDS(Document doc)
        {
            var mat = new ElementId[12];
            using Transaction trans = new Transaction(doc, "Read Materials");
            trans.Start();
            Schema ppSchema = GetSchemaByName("Green3DScanMaterials");

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> dataStorageList = collector.OfClass(typeof(DataStorage)).ToElements();

            foreach (var ds in dataStorageList)
            {
                Entity ent = ds.GetEntity(ppSchema);
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
            Color dRed = new Color(150, 20, 0);
            Color red = new Color(190, 70, 0);
            Color lRed = new Color(210, 120, 0);
            Color dOra = new Color(225, 160, 0);
            Color ora = new Color(240, 200, 0);
            Color yel = new Color(230, 220, 0);
            Color yelGre = new Color(130, 150, 0);
            Color gre = new Color(130, 150, 0);
            Color dGre = new Color(70, 120, 0);
            Color ddGre = new Color(20, 70, 0);
            Color grey = new Color(105, 105, 105);
            Color blue = new Color(0, 120, 200);

            var colorArr = new ElementId[12];

            using Transaction t = new Transaction(doc, "AddMaterials");
            t.Start();

            // materials

            var matDRed = Material.Create(doc, "CPM_darkred");
            Material mat0 = doc.GetElement(matDRed) as Material;
            mat0.Color = dRed;
            colorArr[0] = matDRed;

            var matRed = Material.Create(doc, "CPM_red");
            Material mat1 = doc.GetElement(matRed) as Material;
            mat1.Color = red;
            colorArr[1] = matRed;

            var matLRed = Material.Create(doc, "CPM_lightred");
            Material mat2 = doc.GetElement(matLRed) as Material;
            mat2.Color = lRed;
            colorArr[2] = matLRed;

            var matDOra = Material.Create(doc, "CPM_darkorange");
            Material mat3 = doc.GetElement(matDOra) as Material;
            mat3.Color = dOra;
            colorArr[3] = matDOra;

            var matOra = Material.Create(doc, "CPM_orange");
            Material mat4 = doc.GetElement(matOra) as Material;
            mat4.Color = ora;
            colorArr[4] = matOra;

            var matLOra = Material.Create(doc, "CPM_ligthorange");
            Material mat5 = doc.GetElement(matLOra) as Material;
            mat5.Color = yel;
            colorArr[5] = matLOra;

            var matYel = Material.Create(doc, "CPM_yellow");
            Material mat6 = doc.GetElement(matYel) as Material;
            mat6.Color = yelGre;
            colorArr[6] = matYel;

            var matYelGre = Material.Create(doc, "CPM_yellowgreen");
            Material mat7 = doc.GetElement(matYelGre) as Material;
            mat7.Color = gre;
            colorArr[7] = matYelGre;

            var matGre = Material.Create(doc, "CPM_green");
            Material mat8 = doc.GetElement(matGre) as Material;
            mat8.Color = dGre;
            colorArr[8] = matGre;

            var matDGre = Material.Create(doc, "CPM_darkgreen");
            Material mat9 = doc.GetElement(matDGre) as Material;
            mat9.Color = ddGre;
            colorArr[9] = matDGre;

            var matGrey = Material.Create(doc, "CPM_grey");
            Material mat10 = doc.GetElement(matGrey) as Material;
            mat10.Color = grey;
            colorArr[10] = matGrey;

            var matBlue = Material.Create(doc, "CPM_blue");
            Material mat11 = doc.GetElement(matBlue) as Material;
            mat11.Color = blue;
            colorArr[11] = matBlue;

            //DataStorage
            Schema progressPatchMaterials = GetSchemaByName("Green3DScanMaterials");

            if (progressPatchMaterials == null)
            {
                SchemaBuilder sb = new SchemaBuilder(Guid.NewGuid());
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
            Entity ent = new Entity(progressPatchMaterials);
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

            DataStorage materialsIdStorage = DataStorage.Create(doc);
            materialsIdStorage.SetEntity(ent);

            t.Commit();

            return colorArr;
        }

        //public static List<D3.Vector> ReadStationsDS(Document doc)
        //{
        //    var mat = new List<D3.Vector>();
        //    using Transaction trans = new Transaction(doc, "Read Stations");
        //    trans.Start();
        //    Schema schema = GetSchemaByName("Green3DScanStations");

        //    FilteredElementCollector collector = new FilteredElementCollector(doc);
        //    IList<Element> dataStorageList = collector.OfClass(typeof(DataStorage)).ToElements();

        //    foreach (var ds in dataStorageList)
        //    {
        //        Entity ent = ds.GetEntity(schema);
        //        if (ent.IsValid())
        //        {
        //            foreach (var x in ent.Get<List<D3.Vector>>("Coordinates"))
        //            {
        //                mat.Add(x);
        //            }

        //            trans.Commit();
        //        }
        //    }
        //    return mat;
        //}
        //public static void AddStation(Document doc, List<XYZ> coordinates)
        //{
        //    using (Transaction t = new Transaction(doc, "Save XYZ Coordinates"))
        //    {
        //        t.Start();

        //        // Define or get the schema for storing XYZ coordinates
        //        Schema schema = GetSchemaByName("Green3DScanStations");
        //        if (schema == null)
        //        {
        //            SchemaBuilder sb = new SchemaBuilder(Guid.NewGuid());
        //            sb.SetSchemaName("Green3DScanStations");
        //            sb.SetReadAccessLevel(AccessLevel.Public);
        //            sb.SetWriteAccessLevel(AccessLevel.Public);

        //            // Add a field for storing XYZ coordinates as a list
        //            sb.AddArrayField("Coordinates", typeof(IList<XYZ>));

        //            schema = sb.Finish();
        //        }
        //        else
        //        {
        //            TaskDialog.Show("Message", "gibs schon");
        //        }

        //        //// Convert D3.Vector to XYZ
        //        //List<XYZ> xyzCoordinates = new List<XYZ>();
        //        //foreach (var coord in coordinates)
        //        //{
        //        //    XYZ xyz = new XYZ(coord.x, coord.y, coord.z);
        //        //    xyzCoordinates.Add(xyz);
        //        //}

        //        //// Create a new entity to store the coordinates
        //        Entity entity = new Entity(schema);
        //        entity.Set<IList<XYZ>>("Coordinates", coordinates);

        //        // Create a DataStorage element and set the entity to it
        //        DataStorage dataStorage = DataStorage.Create(doc);
        //        dataStorage.SetEntity(entity);

        //        //// Commit the transaction
        //        t.Commit();
        //        TaskDialog.Show("Message", "schema anlegen klappt");

        //        //IList<XYZ> xyzCoordinates = default;
        //        //return xyzCoordinates;
        //    }
        //    //using Transaction t = new Transaction(doc, "Save XYZ Coordinates");
        //    //t.Start();

        //    //// Define or get the schema for storing XYZ coordinates
        //    //Schema schema = GetSchemaByName("Green3DScanStations");
        //    //if (schema == null)
        //    //{
        //    //    SchemaBuilder sb = new SchemaBuilder(Guid.NewGuid());
        //    //    sb.SetSchemaName("Green3DScanStations");
        //    //    sb.SetReadAccessLevel(AccessLevel.Public);
        //    //    sb.SetWriteAccessLevel(AccessLevel.Public);

        //    //    // Add a field for storing XYZ coordinates as a list
        //    //    sb.AddArrayField("Coordinates", typeof(List<D3.Vector>));

        //    //    schema = sb.Finish();
        //    //}

        //    //// Create a new entity to store the coordinates
        //    //Entity entity = new Entity(schema);

        //    //// Set the coordinates in the entity
        //    //entity.Set<List<D3.Vector>>("Coordinates", coordinates);

        //    //// Create a DataStorage element and set the entity to it
        //    //DataStorage dataStorage = DataStorage.Create(doc);
        //    //dataStorage.SetEntity(entity);

        //    //// Commit the transaction
        //    //t.Commit();
        //    //return coordinates;
        //}



        ////public static Schema AddSchemaForUsageType(string schemaName)
        ////{
        ////    SchemaBuilder sb = new SchemaBuilder(Guid.NewGuid());
        ////    sb.SetSchemaName(schemaName);
        ////    sb.SetReadAccessLevel(AccessLevel.Public);
        ////    sb.SetWriteAccessLevel(AccessLevel.Public);
        ////    sb.SetVendorId("HTWDresden");

        ////    sb.AddArrayField("Coordinates", typeof(XYZ));

        ////    return sb.Finish();
        ////}
        ////public static DataStorage GetOrCreateDataStorage(Document doc, string storageName, string levelName, Schema schema)
        ////{
        ////    FilteredElementCollector collector = new FilteredElementCollector(doc)
        ////        .OfClass(typeof(DataStorage));

        ////    foreach (DataStorage ds in collector)
        ////    {
        ////        if (ds.Name == storageName)
        ////        {
        ////            return ds;
        ////        }
        ////    }

        ////    using (Transaction trans = new Transaction(doc, "Create DataStorage"))
        ////    {
        ////        trans.Start();
        ////        DataStorage dataStorage = DataStorage.Create(doc);
        ////        dataStorage.Name = storageName;

        ////        Entity entity = new Entity(schema);
        ////        entity.Set("Level", levelName);
        ////        entity.Set<IList<XYZ>>("Coordinates", new List<XYZ>()); // Initial leere Liste
        ////        dataStorage.SetEntity(entity);

        ////        trans.Commit();
        ////        return dataStorage;
        ////    }
        ////}
        ////public static void AddCoordinates(Document doc, DataStorage dataStorage, IList<XYZ> coordinates)
        ////{
        ////    if (schema == null)
        ////    {
        ////        Log.Error("Schema not found with the provided GUID.");
        ////    }
        ////    else
        ////    {
        ////        // Get the Entity from the DataStorage
        ////        Entity entity = dataStorage.GetEntity(schema);

        ////        if (!entity.IsValid())
        ////        {
        ////            Log.Error("Entity is not valid for the given schema.");
        ////        }
        ////        else
        ////        {
        ////            // Access the stored data using the schema and entity
        ////            // Example: assuming the schema has a field named "MyField"
        ////            string myFieldValue = entity.Get<string>("MyField");
        ////            Log.Information("MyField Value: " + myFieldValue);
        ////        }
        ////    }

        ////    using (Transaction trans = new Transaction(doc, "Add Coordinates"))
        ////    {
        ////        trans.Start();

        ////        Schema schema = Schema.Lookup(dataStorage.GetEntity().SchemaGUID);
        ////        Entity entity = dataStorage.GetEntity(schema);

        ////        IList<XYZ> existingCoordinates = entity.Get<IList<XYZ>>("Coordinates");
        ////        foreach (var coord in coordinates)
        ////        {
        ////            existingCoordinates.Add(coord);
        ////        }

        ////        entity.Set("Coordinates", existingCoordinates);
        ////        dataStorage.SetEntity(entity);

        ////        trans.Commit();
        ////    }
        ////}
        //public static DataStorage GetOrCreateDataStorage(Document doc, string storageName, string levelName)
        //{
        //    // Suche nach bestehendem DataStorage-Element
        //    FilteredElementCollector collector = new FilteredElementCollector(doc)
        //        .OfClass(typeof(DataStorage));

        //    foreach (DataStorage ds in collector)
        //    {
        //        if (ds.Name == storageName)
        //        {
        //            return ds;
        //        }
        //    }

        //    // Erstelle neues DataStorage-Element
        //    using (Transaction trans = new Transaction(doc, "Create DataStorage"))
        //    {
        //        trans.Start();
        //        DataStorage dataStorage = DataStorage.Create(doc);
        //        dataStorage.Name = storageName;

        //        // Füge zusätzliche Daten hinzu
        //        List<string> fieldList = new List<string> { "Level", "Coordinates" };
        //        Schema schema = AddSchemaForUsageType(storageName, fieldList);
        //        Entity entity = new Entity(schema);
        //        entity.Set("Level", levelName);
        //        entity.Set<IList<string>>("Coordinates", new List<string>());
        //        dataStorage.SetEntity(entity);

        //        trans.Commit();
        //        return dataStorage;
        //    }
        //}
        //public static void AddSphereCoordinates(Document doc, DataStorage dataStorage, XYZ coordinates)
        //{
        //    using (Transaction trans = new Transaction(doc, "Add Coordinates"))
        //    {
        //        trans.Start();

        //        Schema schema = GetSchemaByName(dataStorage.Name);
        //        if (schema != null)
        //        {
        //            Entity entity = dataStorage.GetEntity(schema);
        //            IList<string> coordList = entity.Get<IList<string>>("Coordinates");
        //            string coordString = $"{coordinates.X},{coordinates.Y},{coordinates.Z}";
        //            coordList.Add(coordString);

        //            entity.Set("Coordinates", coordList);
        //            dataStorage.SetEntity(entity);
        //        }

        //        trans.Commit();
        //    }
        //}
        //public static void CreateAndStoreSphereData(Document doc, XYZ coordinates, string levelName)
        //{
        //    string storageName = "Stations";
        //    DataStorage dataStorage = GetOrCreateDataStorage(doc, storageName, levelName);
        //    AddSphereCoordinates(doc, dataStorage, coordinates);
        //}
    }
}
