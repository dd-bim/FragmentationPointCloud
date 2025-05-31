using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

using Revit.Green3DScan;

using Serilog;

using System;
using System.Collections.Generic;

namespace Revit.GUI;

[Transaction(TransactionMode.Manual)]
public class CmdShowSettings : IExternalCommand
{

    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        if (!ExternalCommandHelper.GetProjectPath(commandData, out string projectPath, out _, out _))
        {
            TaskDialog.Show("Message", "The project file has not been saved yet.");
            return Result.Failed;
        }
        ExternalCommandHelper.InitLogger(projectPath);
        Log.Information("start CmdShowSettings");

        // settings json
        SettingsJson settings;
        try
        {
            settings = SettingsJson.ReadSettingsJson(Constants.pathSettings);
        }
        catch
        {
            settings = SettingsJson.ReadSettingsJson(Constants.readPathSettings);
        }

        try
        {
            var propUI = new WinSettings(settings);
            propUI.ShowDialog();

            if (propUI.SaveChanges)
            {
                var modified = propUI.Data;
                var j = modified["Green3DScan"];
                // Dictionary: PropertyName -> AttributeValue
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var attr in j)
                    dict[attr.AttributeName] = attr.AttributeValue;

                var errors = new List<string>();
                var newSettings = new SettingsJson();
                var properties = typeof(SettingsJson).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                foreach (var prop in properties)
                {
                    if (!dict.TryGetValue(prop.Name, out string? val))
                    {
                        errors.Add(prop.Name + " (fehlt)");
                        continue;
                    }

                    object? parsed = null;
                    bool valid = true;

                    if (prop.PropertyType == typeof(double))
                        valid = val.TryParseInvariant(out double d) && (parsed = d) != null;
                    else if (prop.PropertyType == typeof(int))
                        valid = val.TryParseInvariant(out int n) && (parsed = n) != null;
                    else if (prop.PropertyType == typeof(bool))
                        valid = bool.TryParse(val, out bool b) && (parsed = b) != null;
                    else
                        parsed = val;

                    if (!valid)
                    {
                        errors.Add(prop.Name);
                    }
                    else
                    {
                        prop.SetValue(newSettings, parsed);
                    }
                }

                if (errors.Count > 0)
                {
                    TaskDialog.Show("Fehlerhafte Eingabe",
                        "Folgende Felder sind ungültig oder fehlen:\n" +
                        string.Join(", ", errors) +
                        "\nBitte korrigieren Sie die Eingaben (Dezimalpunkt beachten) und versuchen Sie es erneut.");
                    return Result.Failed;
                }

                SettingsJson.WriteSettingsJson(newSettings, Constants.pathSettings);
            }
        }
        catch (Exception e)
        {
            TaskDialog.Show("Exception", e.ToString());
            return Result.Failed;
        }

        return Result.Succeeded;
    }

}