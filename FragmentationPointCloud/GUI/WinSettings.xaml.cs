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
                { "BBox_Buffer", set.BBox_Buffer.ToString() },
                { "OnlyPlanarFaces", set.OnlyPlanarFaces.ToString() },
                { "CoordinatesReduction", set.CoordinatesReduction.ToString() },
                { "PgmHeightOfLevel_Meter", set.PgmHeightOfLevel_Meter.ToString() },
                { "PgmImageExpansion_Px", set.PgmImageExpansion_Px.ToString() },
                { "PgmImageResolution_Meter", set.PgmImageResolution_Meter.ToString() },
                { "VerbosityLevel", set.VerbosityLevel },
                { "PathPointCloud", set.PathPointCloud },
                { "FragmentationVoxelResulution_Meter", set.FragmentationVoxelResolution_Meter.ToString() },
                { "StepsPerFullTurn", set.StepsPerFullTurn.ToString() },
                { "SphereDiameter_Meter", set.SphereDiameter_Meter.ToString() },
                { "HeightOfScanner_Meter", set.HeightOfScanner_Meter.ToString() },
                { "Beta_Degree", set.Beta_Degree.ToString() },
                { "MinDF_Meter", set.MinDF_Meter.ToString() },
                { "MaxDF_Meter", set.MaxDF_Meter.ToString() },
                { "MaxPlaneDist_Meter", set.MaxPlaneDist_Meter.ToString() }
            };

            var currentTabItem = new TabItem
            {
                Header = "Green3DScan"
            };
            var attrList = AttributeContainer.GetAttrContainerFromDict(x);
            currentTabItem.Content = attrList;

            Data.Add("Green3DScan", attrList);
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

            var pathPointCloudAttr = Data["Green3DScan"].FirstOrDefault(attr => attr.AttributName == "PathPointCloud");
            if (pathPointCloudAttr != null)
            {
                pathPointCloudAttr.AttributValue = newPath;
            }
        }
    }
}
