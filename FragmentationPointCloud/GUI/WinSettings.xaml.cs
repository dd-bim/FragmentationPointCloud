using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Revit.GUI
{
    /// <summary>
    ///     Interaktionslogik für WinSettings.xaml
    /// </summary>
    public partial class WinSettings : Window
    {
        public WinSettings(SettingsJson set)
        {
            Data = [];
            SaveChanges = false;
            InitializeComponent();

            Json = set;

            var x = new Dictionary<string, string>();
            var properties = typeof(SettingsJson).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                object? value = prop.GetValue(set);

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
            pathPointCloudAttr?.AttributeValue = newPath;
        }

        public class AttributeContainer : INotifyPropertyChanged
        {
            private string _attrValue = string.Empty;
            public string AttributeName { get; private init; } = string.Empty;

            // Declare the event as nullable to match the nullability of the interface member
            public event PropertyChangedEventHandler? PropertyChanged;

            public string AttributeValue
            {
                get => _attrValue;
                set
                {
                    if (_attrValue == value) return;
                    _attrValue = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AttributeValue)));
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