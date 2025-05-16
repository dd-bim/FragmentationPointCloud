using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;

namespace Revit.GUI
{
    /// <summary>
    ///     Interaktionslogik für WinSettings.xaml
    /// </summary>
    public partial class WinSettings : Window
    {
        public WinSettings(SettingsJson set)
        {
            Data = new Dictionary<string, ObservableCollection<AttributeContainer>>();
            SaveChanges = false;
            InitializeComponent();

            var x = new Dictionary<string, string>();
            var properties = typeof(SettingsJson).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                object value = prop.GetValue(set);

                string stringValue = value switch
                {
                    double d => d.ToString(CultureInfo.InvariantCulture),
                    int i => i.ToString(CultureInfo.InvariantCulture),
                    bool b => b.ToString(),
                    _ => value?.ToString() ?? string.Empty
                };

                x.Add(prop.Name, stringValue);
            }

            var currentTabItem = new TabItem
            {
                Header = "Green3DScan"
            };
            var attrList = AttributeContainer.GetAttrContainerFromDict(x);
            currentTabItem.Content = attrList;

            Data.Add("Green3DScan", attrList);
            tabs.Items.Add(currentTabItem);
        }

        public Dictionary<string, ObservableCollection<AttributeContainer>> Data { get; set; }
        public SettingsJson Json { get; set; }
        public bool SaveChanges { get; set; }

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

        public void ChangePathPointCloud(string newPath)
        {
            Json.PathPointCloud = newPath;

            var pathPointCloudAttr =
                Data["Green3DScan"].FirstOrDefault(attr => attr.AttributeName == "PathPointCloud");
            if (pathPointCloudAttr != null) pathPointCloudAttr.AttributeValue = newPath;
        }

        public class AttributeContainer : INotifyPropertyChanged
        {
            private string _attrValue;
            public string AttributeName { get; private init; }

            public event PropertyChangedEventHandler PropertyChanged;

            public string AttributeValue
            {
                get => _attrValue;
                set
                {
                    if (_attrValue == value) return;
                    _attrValue = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("attrValue"));
                }
            }

            public static ObservableCollection<AttributeContainer> GetAttrContainerFromDict(
                Dictionary<string, string> attributes)
            {
                var collection = new ObservableCollection<AttributeContainer>();

                foreach (var attrCont in attributes.Select(entry => new AttributeContainer
                         {
                             AttributeName = entry.Key,
                             AttributeValue = entry.Value
                         }))
                {
                    collection.Add(attrCont);
                }

                return collection;
            }
        }
    }
}