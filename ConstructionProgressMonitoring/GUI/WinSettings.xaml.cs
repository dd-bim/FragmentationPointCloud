using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Serilog;
using System.Linq;

namespace Revit.GUI
{
    /// <summary>
    /// Interaktionslogik für WinSettings.xaml
    /// </summary>
    public partial class WinSettings : Window
    {
        public Dictionary<string, ObservableCollection<AttributeContainer>> Data { get; set; }
        public SettingsJson Json { get; set; }
        public bool SaveChanges { get; set; }
        public WinSettings(SettingsJson set)
        {
            Data = new Dictionary<string, ObservableCollection<AttributeContainer>>();
            SaveChanges = false;
            InitializeComponent();

            var x = new Dictionary<string, string>
            {
                { "ConstructionTolerance_Meter", set.ConstructionTolerance_Meter.ToString() },
                { "AngleDeviation_Degree", set.AngleDeviation_Degree.ToString() },
                { "BBox_Buffer", set.BBox_Buffer.ToString() },
                { "VisibilityAnalysis", set.VisibilityAnalysis.ToString() },
                { "OnlyPlanarFaces", set.OnlyPlanarFaces.ToString() },
                { "CoordinatesReduction", set.CoordinatesReduction.ToString() },
                { "CalculateShift", set.CalculateShift.ToString() },
                { "CalculateVertexDistance", set.CalculateVertexDistance.ToString() },
                { "PgmHeightOfLevel_Meter", set.PgmHeightOfLevel_Meter.ToString() },
                { "PgmImageExpansion_Px", set.PgmImageExpansion_Px.ToString() },
                { "PgmImageResolution_Meter", set.PgmImageResolution_Meter.ToString() },
                { "VerbosityLevel", set.VerbosityLevel },
                { "PathPointCloud", set.PathPointCloud },
                { "FragmentationVoxelResulution_Meter", set.FragmentationVoxelResolution_Meter.ToString() },
                { "CsvR", set.CsvR },
                { "CsvRRef", set.CsvRRef },
                { "CsvVisibleFaces", set.CsvVisibleFaces },
                { "CsvVisibleFacesRef", set.CsvVisibleFacesRef },
                { "CsvS", set.CsvS },
                { "CsvSRef", set.CsvSRef },
                { "CsvMatchesR", set.CsvMatchesR },
                { "CsvMatchesRRef", set.CsvMatchesRRef },
                { "CsvMatchesS", set.CsvMatchesS },
                { "CsvMatchesSRef", set.CsvMatchesSRef },
                { "ObjR", set.ObjR },
                { "ObjS", set.ObjS },
                { "StepsPerFullTurn", set.StepsPerFullTurn.ToString() },
                { "Pointcloud", set.Pointcloud },
                { "FilterAngle_Degree", set.FilterAngle_Degree.ToString() },
                { "FilterD_Meter", set.FilterD_Meter.ToString() },
                { "FilterBuffer_Meter", set.FilterBuffer_Meter.ToString() },
                { "MaxPlaneTol_Degree", set.MaxPlaneTol_Degree.ToString() },
                { "MaxPlaneDist_Meter", set.MaxPlaneDist_Meter.ToString() },
                { "MinDistToStation_Meter", set.MinDistToStation_Meter.ToString() },
                { "MaxDistToStation_Meter", set.MaxDistToStation_Meter.ToString() },
                { "MinCoverage_Percent", set.MinCoverage_Percent.ToString() },
                { "MinPatchLength_Meter", set.MinPatchLength_Meter.ToString() },
                { "MaxPatchLength_Meter", set.MaxPatchLength_Meter.ToString() },
                { "MaxDistToArc_Meter", set.MaxDistToArc_Meter.ToString() },
                { "MaxRayPlaneDiffCos", set.MaxRayPlaneDiffCos.ToString() },
                { "QuantilAlpha2_5", set.QuantilAlpha2_5.ToString() },
                { "QuantilAlpha5", set.QuantilAlpha5.ToString() },
            };

            var currentTabItem = new TabItem
            {
                Header = "CPM"
            };
            var attrList = AttributeContainer.GetAttrContainerFromDict(x);
            currentTabItem.Content = attrList;

            Data.Add("CPM", attrList);
            tabs.Items.Add(currentTabItem);
        }
        private void SaveClick(object sender, RoutedEventArgs e)
        {
            SaveChanges = true;
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            SaveChanges = false;
            Close();
        }
        public class AttributeContainer : INotifyPropertyChanged
        {
            public string AttributName { get; set; }
            private string attrvalue;
            public string AttributValue
            {
                get { return attrvalue; }
                set
                {
                    if (attrvalue != value)
                    {
                        attrvalue = value;
                        NotifyPropertyChanged("attrValue");
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public void NotifyPropertyChanged(string propName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
            }
            public static ObservableCollection<AttributeContainer> GetAttrContainerFromDict(Dictionary<string, string> attributes)
            {
                ObservableCollection<AttributeContainer> collection = new ObservableCollection<AttributeContainer>();

                foreach (KeyValuePair<string, string> entry in attributes)
                {
                    var attrCont = new AttributeContainer
                    {
                        AttributName = entry.Key,
                        AttributValue = entry.Value
                    };
                    collection.Add(attrCont);
                }

                return collection;
            }
        }
        public void ChangePathPointCloud(string newPath)
        {
            Json.PathPointCloud = newPath;

            var pathPointCloudAttr = Data["CPM"].FirstOrDefault(attr => attr.AttributName == "PathPointCloud");
            if (pathPointCloudAttr != null)
            {
                pathPointCloudAttr.AttributValue = newPath;
            }
        }
    }
}
