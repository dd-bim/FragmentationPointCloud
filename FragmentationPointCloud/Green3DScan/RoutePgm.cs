using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Except = Autodesk.Revit.Exceptions;
using Serilog;

namespace Revit.Green3DScan
{
    [Transaction(TransactionMode.Manual)]
    public class RoutePgm : IExternalCommand
    {
        #region Execute
        string path;
        string dateBimLastModified;
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region setup
            // settings json
            SettingsJson set = SettingsJson.ReadSettingsJson(Constants.pathSettings);

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            UIApplication uiapp = commandData.Application;
            Document doc = uidoc.Document;
            try
            {
                path = Path.GetDirectoryName(doc.PathName);
                FileInfo fileInfo = new FileInfo(path);
                var date = fileInfo.LastWriteTime;
                dateBimLastModified = date.Year + "-" + date.Month + "-" + date.Day + "-" + date.Hour + "-" + date.Minute;
            }
            catch (Exception)
            {
                TaskDialog.Show("Message", "The file has not been saved yet.");
                return Result.Failed;
            }

            // logger
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File(Path.Combine(path, "LogFile_"), rollingInterval: RollingInterval.Day)
               .CreateLogger();
            Log.Information("start");
            Log.Information(set.BBox_Buffer.ToString());
            #endregion setup

            try
            {
                Result resCreateLevel = CreateLevel(doc, set, out Level newLevel);
                if (resCreateLevel != Result.Succeeded)
                {
                    TaskDialog.Show("Message", "Create level not successful!");
                    return Result.Failed;
                }

                Result resCreateViewPlan = CreateViewPlan(doc, uidoc, newLevel);
                if (resCreateViewPlan != Result.Succeeded)
                {
                    TaskDialog.Show("Message", "Creating viewplan not successful!");
                    return Result.Failed;
                }
                TaskDialog.Show("Message", "Creating viewplan successful!");

                Result resRoomColorScheme = RoomColorScheme(doc, uiapp);
                if (resRoomColorScheme != Result.Succeeded)
                {
                    TaskDialog.Show("Message", "Creating ColorScheme not successfull!");
                    return Result.Succeeded;
                }
                TaskDialog.Show("Message", "Creating ColorScheme successfull!");

                return Result.Succeeded;
            }
            #region catch
            catch (Except.OperationCanceledException)
            {
                TaskDialog.Show("Message", "Error 1: Command canceled.");
                return Result.Failed;
            }
            catch (Except.ForbiddenForDynamicUpdateException)
            {
                TaskDialog.Show("Message", "Error 2");
                return Result.Failed;
            }
            catch (Exception ex)
            {
                message += "Error message:" + ex.ToString();
                TaskDialog.Show("Message", message);
                return Result.Failed;
            }
            #endregion catch
        }
        #endregion execute
        static Result CreateLevel(Document doc, SettingsJson set, out Level newLevel)
        {
            try
            {
                using Transaction tx = new Transaction(doc, "Create Level");
                tx.Start();
                double elevation = set.PgmHeightOfLevel_Meter * Constants.meter2Feet;
                newLevel = Level.Create(doc, elevation);
                string baseName = "Route_PGM_High_" + set.PgmHeightOfLevel_Meter.ToString();
                string levelName = baseName;
                int suffix = 1;

                while (LevelNameExists(doc, levelName))
                {
                    levelName = baseName + "_" + suffix;
                    suffix++;
                }
                newLevel.Name = levelName;
                ElementId newLevelId = newLevel.Id;
                tx.Commit();
                return Result.Succeeded;
            }
            catch (Exception)
            {
                newLevel = default;
                return Result.Failed;
            }

            static bool LevelNameExists(Document doc, string name)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(Level));

                foreach (Level level in collector.ToElements())
                {
                    if (level.Name == name)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        static Result CreateViewPlan(Document doc, UIDocument uidoc, Level newLevel)
        {
            try
            {
                ViewPlan newView;
                using (Transaction tx = new Transaction(doc, "Create ViewPlan"))
                {
                    tx.Start();
                    newView = ViewCreation.NewViewPlan(newLevel, ViewType.FloorPlan);
                    tx.Commit();
                }
                uidoc.ActiveView = newView;
                // set ViewDetailLevel coarse
                using (Transaction tx = new Transaction(doc, "Change ViewDetailLevel"))
                {
                    tx.Start();
                    doc.ActiveView.DetailLevel = ViewDetailLevel.Coarse;
                    doc.ActiveView.DisplayStyle = DisplayStyle.ShadingWithEdges;
                    tx.Commit();
                }
                
                // get all categories
                List<BuiltInCategory> cat = GetBuiltInCategory();
                foreach (BuiltInCategory category in cat)
                {
                    // get all existing elements to the categories
                    ElementCategoryFilter categoryFilter = new ElementCategoryFilter(category);
                    FilteredElementCollector filteredLabels = new FilteredElementCollector(doc, newView.Id);
                    filteredLabels.WherePasses(categoryFilter);
                    List<ElementId> listElementId = new List<ElementId>();
                    foreach (Element element in filteredLabels)
                    {
                        listElementId.Add(element.Id);
                    }
                    if (listElementId.Count == 0)
                    {
                        continue;
                    }

                    // hide elements
                    Transaction tx = new Transaction(doc, "Hide Elements");
                    tx.Start();
                    try
                    {
                        newView.HideElements(listElementId);
                        tx.Commit();
                    }
                    catch
                    {
                        tx.RollBack();
                        TaskDialog.Show("Message", category.ToString());
                    }
                }
                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }

            static List<BuiltInCategory> GetBuiltInCategory()
            {
                List<BuiltInCategory> cat = new List<BuiltInCategory>
            {
                BuiltInCategory.OST_StackedWalls_Obsolete_IdInWrongRange,
                BuiltInCategory.OST_MassTags_Obsolete_IdInWrongRange,
                BuiltInCategory.OST_MassSurface_Obsolete_IdInWrongRange,
                BuiltInCategory.OST_MassFloor_Obsolete_IdInWrongRange,
                BuiltInCategory.OST_Mass_Obsolete_IdInWrongRange,
                BuiltInCategory.OST_WallRefPlanes_Obsolete_IdInWrongRange,
                BuiltInCategory.OST_StickSymbols_Obsolete_IdInWrongRange,
                BuiltInCategory.OST_RemovedGridSeg_Obsolete_IdInWrongRange,
                BuiltInCategory.OST_PointClouds,
                BuiltInCategory.OST_AnalyticalPanelLocalCoordSys,
                BuiltInCategory.OST_AnalyticalMemberLocalCoordSys,
                BuiltInCategory.OST_AnalyticalOpening,
                BuiltInCategory.OST_AnalyticalPanel,
                BuiltInCategory.OST_AnalyticalMemberTags,
                BuiltInCategory.OST_AnalyticalMember,
                BuiltInCategory.OST_AssemblyOrigin_Lines,
                BuiltInCategory.OST_AssemblyOrigin_Planes,
                BuiltInCategory.OST_AssemblyOrigin_Points,
                BuiltInCategory.OST_AssemblyOrigin,
                BuiltInCategory.OST_LinksAnalytical,
                BuiltInCategory.OST_FoundationSlabAnalyticalTags,
                BuiltInCategory.OST_WallFoundationAnalyticalTags,
                BuiltInCategory.OST_IsolatedFoundationAnalyticalTags,
                BuiltInCategory.OST_WallAnalyticalTags,
                BuiltInCategory.OST_FloorAnalyticalTags,
                BuiltInCategory.OST_ColumnAnalyticalTags,
                BuiltInCategory.OST_BraceAnalyticalTags,
                BuiltInCategory.OST_BeamAnalyticalTags,
                BuiltInCategory.OST_AnalyticalNodes_Lines,
                BuiltInCategory.OST_AnalyticalNodes_Planes,
                BuiltInCategory.OST_AnalyticalNodes_Points,
                BuiltInCategory.OST_AnalyticalNodes,
                BuiltInCategory.OST_RigidLinksAnalytical,
                BuiltInCategory.OST_FoundationSlabAnalytical,
                BuiltInCategory.OST_WallFoundationAnalytical,
                BuiltInCategory.OST_IsolatedFoundationAnalytical,
                BuiltInCategory.OST_WallAnalytical,
                BuiltInCategory.OST_FloorAnalytical,
                BuiltInCategory.OST_ColumnEndSegment,
                BuiltInCategory.OST_ColumnStartSegment,
                BuiltInCategory.OST_ColumnAnalytical,
                BuiltInCategory.OST_BraceEndSegment,
                BuiltInCategory.OST_BraceStartSegment,
                BuiltInCategory.OST_BraceAnalytical,
                BuiltInCategory.OST_BeamEndSegment,
                BuiltInCategory.OST_BeamStartSegment,
                BuiltInCategory.OST_BeamAnalytical,
                BuiltInCategory.OST_CompassSecondaryMonth,
                BuiltInCategory.OST_CompassPrimaryMonth,
                BuiltInCategory.OST_CompassSectionFilled,
                BuiltInCategory.OST_LightLine,
                BuiltInCategory.OST_MultiSurface,
                BuiltInCategory.OST_SunSurface,
                BuiltInCategory.OST_Analemma,
                BuiltInCategory.OST_SunsetText,
                BuiltInCategory.OST_CompassSection,
                BuiltInCategory.OST_CompassOuter,
                BuiltInCategory.OST_SunriseText,
                BuiltInCategory.OST_CompassInner,
                BuiltInCategory.OST_SunPath2,
                BuiltInCategory.OST_SunPath1,
                BuiltInCategory.OST_Sun,
                BuiltInCategory.OST_SunStudy,
                BuiltInCategory.OST_StructuralTrussStickSymbols,
                BuiltInCategory.OST_StructuralTrussHiddenLines,
                BuiltInCategory.OST_TrussChord,
                BuiltInCategory.OST_TrussWeb,
                BuiltInCategory.OST_TrussBottomChordCurve,
                BuiltInCategory.OST_TrussTopChordCurve,
                BuiltInCategory.OST_TrussVertWebCurve,
                BuiltInCategory.OST_TrussDiagWebCurve,
                BuiltInCategory.OST_Truss,
                BuiltInCategory.OST_PlumbingEquipmentHiddenLines,
                BuiltInCategory.OST_MechanicalControlDevicesHiddenLines,
                BuiltInCategory.OST_RailingSystemTransitionHiddenLines_Deprecated,
                BuiltInCategory.OST_RailingSystemTerminationHiddenLines_Deprecated,
                BuiltInCategory.OST_RailingSystemRailHiddenLines_Deprecated,
                BuiltInCategory.OST_RailingSystemTopRailHiddenLines_Deprecated,
                BuiltInCategory.OST_RailingSystemHandRailBracketHiddenLines_Deprecated,
                BuiltInCategory.OST_RailingSystemHandRailHiddenLines_Deprecated,
                BuiltInCategory.OST_RailingSystemPanelBracketHiddenLines_Deprecated,
                BuiltInCategory.OST_RailingSystemPanelHiddenLines_Deprecated,
                BuiltInCategory.OST_RailingSystemBalusterHiddenLines_Deprecated,
                BuiltInCategory.OST_RailingSystemPostHiddenLines_Deprecated,
                BuiltInCategory.OST_RailingSystemSegmentHiddenLines_Deprecated,
                BuiltInCategory.OST_RailingSystemHiddenLines_Deprecated,
                BuiltInCategory.OST_StairStringer2012HiddenLines_Deprecated,
                BuiltInCategory.OST_StairTread2012HiddenLines_Deprecated,
                BuiltInCategory.OST_StairLanding2012HiddenLines_Deprecated,
                BuiltInCategory.OST_StairRun2012HiddenLines_Deprecated,
                BuiltInCategory.OST_Stairs2012HiddenLines_Deprecated,
                BuiltInCategory.OST_MassHiddenLines,
                BuiltInCategory.OST_CurtaSystemHiddenLines,
                BuiltInCategory.OST_OBSOLETE_ElemArrayHiddenLines,
                BuiltInCategory.OST_EntourageHiddenLines,
                BuiltInCategory.OST_PlantingHiddenLines,
                BuiltInCategory.OST_SpecialityEquipmentHiddenLines,
                BuiltInCategory.OST_TopographyHiddenLines,
                BuiltInCategory.OST_StructuralFramingSystemHiddenLines_Obsolete,
                BuiltInCategory.OST_SiteHiddenLines,
                BuiltInCategory.OST_RoadsHiddenLines,
                BuiltInCategory.OST_ParkingHiddenLines,
                BuiltInCategory.OST_PlumbingFixturesHiddenLines,
                BuiltInCategory.OST_MechanicalEquipmentHiddenLines,
                BuiltInCategory.OST_LightingFixturesHiddenLines,
                BuiltInCategory.OST_FurnitureSystemsHiddenLines,
                BuiltInCategory.OST_ElectricalFixturesHiddenLines,
                BuiltInCategory.OST_ElectricalEquipmentHiddenLines,
                BuiltInCategory.OST_CaseworkHiddenLines,
                BuiltInCategory.OST_DetailComponentsHiddenLines,
                BuiltInCategory.OST_ShaftOpeningHiddenLines,
                BuiltInCategory.OST_GenericModelHiddenLines,
                //BuiltInCategory.OST_CurtainWallMullionsHiddenLines,
                //BuiltInCategory.OST_CurtainWallPanelsHiddenLines,
                BuiltInCategory.OST_RampsHiddenLines,
                BuiltInCategory.OST_StairsRailingHiddenLines,
                BuiltInCategory.OST_StairsHiddenLines,
                BuiltInCategory.OST_ColumnsHiddenLines,
                BuiltInCategory.OST_FurnitureHiddenLines,
                BuiltInCategory.OST_LinesHiddenLines,
                BuiltInCategory.OST_CeilingsHiddenLines,
                BuiltInCategory.OST_RoofsHiddenLines,
                //BuiltInCategory.OST_DoorsHiddenLines,
                BuiltInCategory.OST_WindowsHiddenLines,
                BuiltInCategory.OST_StructConnectionProfilesTags,
                BuiltInCategory.OST_StructConnectionHoleTags,
                BuiltInCategory.OST_CouplerHiddenLines,
                BuiltInCategory.OST_CouplerTags,
                BuiltInCategory.OST_Coupler,
                BuiltInCategory.OST_StructConnectionWeldTags,
                BuiltInCategory.OST_StructConnectionShearStudTags,
                BuiltInCategory.OST_StructConnectionAnchorTags,
                BuiltInCategory.OST_StructConnectionBoltTags,
                BuiltInCategory.OST_StructConnectionPlateTags,
                BuiltInCategory.OST_RebarHiddenLines,
                BuiltInCategory.OST_StructSubConnections,
                BuiltInCategory.OST_SteelElementStale,
                BuiltInCategory.OST_StructConnectionModifiers,
                BuiltInCategory.OST_StructConnectionWelds,
                BuiltInCategory.OST_StructConnectionHoles,
                BuiltInCategory.OST_StructConnectionShearStuds,
                BuiltInCategory.OST_StructConnectionNobleWarning,
                BuiltInCategory.OST_StructConnectionOthers,
                BuiltInCategory.OST_StructConnectionBolts,
                BuiltInCategory.OST_StructConnectionTags,
                BuiltInCategory.OST_StructConnectionAnchors,
                BuiltInCategory.OST_StructConnectionPlates,
                BuiltInCategory.OST_StructConnectionProfiles,
                BuiltInCategory.OST_StructConnectionReference,
                BuiltInCategory.OST_StructConnectionFailed,
                BuiltInCategory.OST_StructConnectionStale,
                BuiltInCategory.OST_StructConnectionSymbol,
                BuiltInCategory.OST_StructConnectionHiddenLines,
                BuiltInCategory.OST_StructWeldLines,
                BuiltInCategory.OST_StructConnections,
                BuiltInCategory.OST_FabricAreaBoundary,
                BuiltInCategory.OST_FabricReinSpanSymbol,
                BuiltInCategory.OST_FabricReinforcementWire,
                BuiltInCategory.OST_FabricReinforcementBoundary,
                BuiltInCategory.OST_RebarSetToggle,
                BuiltInCategory.OST_FabricAreaTags,
                BuiltInCategory.OST_FabricReinforcementTags,
                BuiltInCategory.OST_AreaReinTags,
                BuiltInCategory.OST_RebarTags,
                BuiltInCategory.OST_FabricAreaSketchSheetsLines,
                BuiltInCategory.OST_FabricAreaSketchEnvelopeLines,
                BuiltInCategory.OST_FabricAreas,
                BuiltInCategory.OST_FabricReinforcement,
                BuiltInCategory.OST_RebarCover,
                BuiltInCategory.OST_CoverType,
                BuiltInCategory.OST_RebarShape,
                BuiltInCategory.OST_PathReinBoundary,
                BuiltInCategory.OST_PathReinTags,
                BuiltInCategory.OST_PathReinSpanSymbol,
                BuiltInCategory.OST_PathRein,
                BuiltInCategory.OST_Cage,
                BuiltInCategory.OST_AreaReinXVisibility,
                BuiltInCategory.OST_AreaReinBoundary,
                BuiltInCategory.OST_AreaReinSpanSymbol,
                BuiltInCategory.OST_AreaReinSketchOverride,
                BuiltInCategory.OST_AreaRein,
                BuiltInCategory.OST_RebarLines,
                BuiltInCategory.OST_RebarSketchLines,
                BuiltInCategory.OST_Rebar,
                BuiltInCategory.OST_MEPAncillaryFramingTags,
                BuiltInCategory.OST_PlumbingEquipmentTags,
                BuiltInCategory.OST_PlumbingEquipment,
                BuiltInCategory.OST_MechanicalControlDeviceTags,
                BuiltInCategory.OST_MechanicalControlDevices,
                BuiltInCategory.OST_MEPAncillaryFraming,
                BuiltInCategory.OST_MEPAncillaries_Obsolete,
                BuiltInCategory.OST_FabricationDuctworkStiffenerTags,
                BuiltInCategory.OST_FabricationDuctworkStiffeners,
                BuiltInCategory.OST_ELECTRICAL_AreaBasedLoads_Reference_Visibility,
                BuiltInCategory.OST_ELECTRICAL_AreaBasedLoads_InteriorFill_Visibility,
                BuiltInCategory.OST_ELECTRICAL_AreaBasedLoads_ColorFill_Obsolete,
                BuiltInCategory.OST_ELECTRICAL_AreaBasedLoads_Reference,
                BuiltInCategory.OST_ELECTRICAL_AreaBasedLoads_InteriorFill,
                BuiltInCategory.OST_ELECTRICAL_AreaBasedLoads_Boundary,
                BuiltInCategory.OST_FabricationPipeworkInsulation,
                BuiltInCategory.OST_FabricationDuctworkLining,
                BuiltInCategory.OST_FabricationContainmentDrop,
                BuiltInCategory.OST_FabricationContainmentRise,
                BuiltInCategory.OST_FabricationPipeworkDrop,
                BuiltInCategory.OST_FabricationPipeworkRise,
                BuiltInCategory.OST_FabricationContainmentSymbology,
                BuiltInCategory.OST_FabricationContainmentCenterLine,
                BuiltInCategory.OST_FabricationContainmentTags,
                BuiltInCategory.OST_FabricationContainment,
                BuiltInCategory.OST_FabricationPipeworkSymbology,
                BuiltInCategory.OST_FabricationPipeworkCenterLine,
                BuiltInCategory.OST_FabricationPipeworkTags,
                BuiltInCategory.OST_FabricationPipework,
                BuiltInCategory.OST_FabricationDuctworkSymbology,
                BuiltInCategory.OST_FabricationDuctworkDrop,
                BuiltInCategory.OST_FabricationDuctworkRise,
                BuiltInCategory.OST_FabricationHangerTags,
                BuiltInCategory.OST_FabricationHangers,
                BuiltInCategory.OST_OBSOLETE_FabricationPartsTmpGraphicDropDrag,
                BuiltInCategory.OST_FabricationPartsTmpGraphicDrag,
                BuiltInCategory.OST_OBSOLETE_FabricationPartsTmpGraphicDrop,
                BuiltInCategory.OST_FabricationPartsTmpGraphicEnd,
                BuiltInCategory.OST_FabricationDuctworkInsulation,
                BuiltInCategory.OST_LayoutNodes,
                BuiltInCategory.OST_FabricationDuctworkCenterLine,
                BuiltInCategory.OST_FabricationServiceElements,
                BuiltInCategory.OST_FabricationDuctworkTags,
                BuiltInCategory.OST_FabricationDuctwork,
                BuiltInCategory.OST_LayoutPathBase_Pipings,
                BuiltInCategory.OST_NumberingSchemas,
                BuiltInCategory.OST_DivisionRules,
                BuiltInCategory.OST_gbXML_Shade,
                BuiltInCategory.OST_AnalyticSurfaces,
                BuiltInCategory.OST_AnalyticSpaces,
                BuiltInCategory.OST_gbXML_OpeningAir,
                BuiltInCategory.OST_gbXML_NonSlidingDoor,
                BuiltInCategory.OST_gbXML_SlidingDoor,
                BuiltInCategory.OST_gbXML_OperableSkylight,
                BuiltInCategory.OST_gbXML_FixedSkylight,
                BuiltInCategory.OST_gbXML_OperableWindow,
                BuiltInCategory.OST_gbXML_FixedWindow,
                BuiltInCategory.OST_gbXML_UndergroundCeiling,
                BuiltInCategory.OST_gbXML_UndergroundSlab,
                BuiltInCategory.OST_gbXML_UndergroundWall,
                BuiltInCategory.OST_gbXML_SurfaceAir,
                BuiltInCategory.OST_gbXML_Ceiling,
                BuiltInCategory.OST_gbXML_InteriorFloor,
                BuiltInCategory.OST_gbXML_InteriorWall,
                BuiltInCategory.OST_gbXML_SlabOnGrade,
                BuiltInCategory.OST_gbXML_RaisedFloor,
                BuiltInCategory.OST_gbXML_Roof,
                BuiltInCategory.OST_gbXML_ExteriorWall,
                BuiltInCategory.OST_DivisionProfile,
                BuiltInCategory.OST_SplitterProfile,
                BuiltInCategory.OST_PipeSegments,
                BuiltInCategory.OST_GraphicalWarning_OpenConnector,
                BuiltInCategory.OST_PlaceHolderPipes,
                BuiltInCategory.OST_PlaceHolderDucts,
                BuiltInCategory.OST_PipingSystem_Reference_Visibility,
                BuiltInCategory.OST_PipingSystem_Reference,
                BuiltInCategory.OST_DuctSystem_Reference_Visibility,
                BuiltInCategory.OST_DuctSystem_Reference,
                BuiltInCategory.OST_PipeInsulationsTags,
                BuiltInCategory.OST_DuctLiningsTags,
                BuiltInCategory.OST_DuctInsulationsTags,
                BuiltInCategory.OST_ElectricalInternalCircuits,
                BuiltInCategory.OST_PanelScheduleGraphics,
                BuiltInCategory.OST_CableTrayRun,
                BuiltInCategory.OST_ConduitRun,
                BuiltInCategory.OST_ParamElemElectricalLoadClassification,
                BuiltInCategory.OST_DataPanelScheduleTemplates,
                BuiltInCategory.OST_SwitchboardScheduleTemplates,
                BuiltInCategory.OST_BranchPanelScheduleTemplates,
                BuiltInCategory.OST_ConduitStandards,
                BuiltInCategory.OST_ElectricalLoadClassifications,
                BuiltInCategory.OST_ElectricalDemandFactorDefinitions,
                BuiltInCategory.OST_ConduitFittingCenterLine,
                BuiltInCategory.OST_CableTrayFittingCenterLine,
                BuiltInCategory.OST_ConduitCenterLine,
                BuiltInCategory.OST_ConduitDrop,
                BuiltInCategory.OST_ConduitRiseDrop,
                BuiltInCategory.OST_CableTrayCenterLine,
                BuiltInCategory.OST_CableTrayDrop,
                BuiltInCategory.OST_CableTrayRiseDrop,
                BuiltInCategory.OST_ConduitTags,
                BuiltInCategory.OST_Conduit,
                BuiltInCategory.OST_CableTrayTags,
                BuiltInCategory.OST_CableTray,
                BuiltInCategory.OST_ConduitFittingTags,
                BuiltInCategory.OST_ConduitFitting,
                BuiltInCategory.OST_CableTrayFittingTags,
                BuiltInCategory.OST_CableTrayFitting,
                BuiltInCategory.OST_RoutingPreferences,
                BuiltInCategory.OST_DuctLinings,
                BuiltInCategory.OST_DuctInsulations,
                BuiltInCategory.OST_PipeInsulations,
                BuiltInCategory.OST_HVAC_Load_Schedules,
                BuiltInCategory.OST_HVAC_Load_Building_Types,
                BuiltInCategory.OST_HVAC_Load_Space_Types,
                BuiltInCategory.OST_HVAC_Zones_Reference_Visibility,
                BuiltInCategory.OST_HVAC_Zones_InteriorFill_Visibility,
                BuiltInCategory.OST_HVAC_Zones_ColorFill,
                BuiltInCategory.OST_ZoneTags,
                BuiltInCategory.OST_LayoutPath_Bases,
                BuiltInCategory.OST_WireTemperatureRatings,
                BuiltInCategory.OST_WireInsulations,
                BuiltInCategory.OST_WireMaterials,
                BuiltInCategory.OST_HVAC_Zones_Reference,
                BuiltInCategory.OST_HVAC_Zones_InteriorFill,
                BuiltInCategory.OST_HVAC_Zones_Boundary,
                BuiltInCategory.OST_HVAC_Zones,
                BuiltInCategory.OST_Fluids,
                BuiltInCategory.OST_PipeSchedules,
                BuiltInCategory.OST_PipeMaterials,
                BuiltInCategory.OST_PipeConnections,
                BuiltInCategory.OST_EAConstructions,
                BuiltInCategory.OST_SwitchSystem,
                BuiltInCategory.OST_SprinklerTags,
                BuiltInCategory.OST_Sprinklers,
                BuiltInCategory.OST_RouteCurveBranch,
                BuiltInCategory.OST_RouteCurveMain,
                BuiltInCategory.OST_RouteCurve,
                BuiltInCategory.OST_GbXML_Opening,
                BuiltInCategory.OST_GbXML_SType_Underground,
                BuiltInCategory.OST_GbXML_SType_Shade,
                BuiltInCategory.OST_GbXML_SType_Exterior,
                BuiltInCategory.OST_GbXML_SType_Interior,
                BuiltInCategory.OST_GbXMLFaces,
                BuiltInCategory.OST_WireHomeRunArrows,
                BuiltInCategory.OST_LightingDeviceTags,
                BuiltInCategory.OST_LightingDevices,
                BuiltInCategory.OST_FireAlarmDeviceTags,
                BuiltInCategory.OST_FireAlarmDevices,
                BuiltInCategory.OST_DataDeviceTags,
                BuiltInCategory.OST_DataDevices,
                BuiltInCategory.OST_CommunicationDeviceTags,
                BuiltInCategory.OST_CommunicationDevices,
                BuiltInCategory.OST_SecurityDeviceTags,
                BuiltInCategory.OST_SecurityDevices,
                BuiltInCategory.OST_NurseCallDeviceTags,
                BuiltInCategory.OST_NurseCallDevices,
                BuiltInCategory.OST_TelephoneDeviceTags,
                BuiltInCategory.OST_TelephoneDevices,
                BuiltInCategory.OST_WireTickMarks,
                BuiltInCategory.OST_PipeFittingInsulation,
                BuiltInCategory.OST_PipeFittingCenterLine,
                BuiltInCategory.OST_FlexPipeCurvesInsulation,
                BuiltInCategory.OST_PipeCurvesInsulation,
                BuiltInCategory.OST_PipeCurvesDrop,
                BuiltInCategory.OST_DuctFittingLining,
                BuiltInCategory.OST_DuctFittingInsulation,
                BuiltInCategory.OST_DuctFittingCenterLine,
                BuiltInCategory.OST_FlexDuctCurvesInsulation,
                BuiltInCategory.OST_DuctCurvesLining,
                BuiltInCategory.OST_DuctCurvesInsulation,
                BuiltInCategory.OST_DuctCurvesDrop,
                BuiltInCategory.OST_DuctFittingTags,
                BuiltInCategory.OST_PipeFittingTags,
                BuiltInCategory.OST_PipeColorFills,
                BuiltInCategory.OST_PipeColorFillLegends,
                BuiltInCategory.OST_WireTags,
                BuiltInCategory.OST_PipeAccessoryTags,
                BuiltInCategory.OST_PipeAccessory,
                BuiltInCategory.OST_PipeCurvesRiseDrop,
                BuiltInCategory.OST_FlexPipeCurvesPattern,
                BuiltInCategory.OST_FlexPipeCurvesContour,
                BuiltInCategory.OST_FlexPipeCurvesCenterLine,
                BuiltInCategory.OST_FlexPipeCurves,
                BuiltInCategory.OST_PipeFitting,
                BuiltInCategory.OST_FlexPipeTags,
                BuiltInCategory.OST_PipeTags,
                BuiltInCategory.OST_PipeCurvesContour,
                BuiltInCategory.OST_PipeCurvesCenterLine,
                BuiltInCategory.OST_PipeCurves,
                BuiltInCategory.OST_PipingSystem,
                BuiltInCategory.OST_ElectricalDemandFactor,
                BuiltInCategory.OST_ElecDistributionSys,
                BuiltInCategory.OST_ElectricalVoltage,
                BuiltInCategory.OST_Wire,
                BuiltInCategory.OST_ElectricalCircuitTags,
                BuiltInCategory.OST_ElectricalCircuit,
                BuiltInCategory.OST_DuctCurvesRiseDrop,
                BuiltInCategory.OST_FlexDuctCurvesPattern,
                BuiltInCategory.OST_FlexDuctCurvesContour,
                BuiltInCategory.OST_FlexDuctCurvesCenterLine,
                BuiltInCategory.OST_FlexDuctCurves,
                BuiltInCategory.OST_DuctAccessoryTags,
                BuiltInCategory.OST_DuctAccessory,
                BuiltInCategory.OST_DuctSystem,
                BuiltInCategory.OST_DuctTerminalTags,
                BuiltInCategory.OST_DuctTerminal,
                BuiltInCategory.OST_DuctFitting,
                BuiltInCategory.OST_DuctColorFills,
                BuiltInCategory.OST_FlexDuctTags,
                BuiltInCategory.OST_DuctTags,
                BuiltInCategory.OST_DuctCurvesContour,
                BuiltInCategory.OST_DuctCurvesCenterLine,
                BuiltInCategory.OST_DuctCurves,
                BuiltInCategory.OST_DuctColorFillLegends,
                BuiltInCategory.OST_ConnectorElemZAxis,
                BuiltInCategory.OST_ConnectorElemYAxis,
                BuiltInCategory.OST_ConnectorElemXAxis,
                BuiltInCategory.OST_ConnectorElem,
                BuiltInCategory.OST_VibrationManagementTags,
                BuiltInCategory.OST_BridgeFramingTrussTags,
                BuiltInCategory.OST_BridgeFramingDiaphragmTags,
                BuiltInCategory.OST_BridgeFramingCrossBracingTags,
                BuiltInCategory.OST_StructuralTendonTags,
                BuiltInCategory.OST_StructuralTendonHiddenLines,
                BuiltInCategory.OST_StructuralTendons,
                BuiltInCategory.OST_ExpansionJointTags,
                BuiltInCategory.OST_ExpansionJointHiddenLines,
                BuiltInCategory.OST_ExpansionJoints,
                BuiltInCategory.OST_VibrationIsolatorTags,
                BuiltInCategory.OST_VibrationIsolators,
                BuiltInCategory.OST_VibrationDamperTags,
                BuiltInCategory.OST_VibrationDampers,
                BuiltInCategory.OST_VibrationManagementHiddenLines,
                BuiltInCategory.OST_VibrationManagement,
                BuiltInCategory.OST_BridgeFramingTrusses,
                BuiltInCategory.OST_BridgeFramingDiaphragms,
                BuiltInCategory.OST_BridgeFramingCrossBracing,
                BuiltInCategory.OST_BridgeFramingTags,
                BuiltInCategory.OST_BridgeFramingHiddenLines,
                BuiltInCategory.OST_BridgeFraming,
                BuiltInCategory.OST_PierWallTags,
                BuiltInCategory.OST_PierWalls,
                BuiltInCategory.OST_PierPileTags,
                BuiltInCategory.OST_PierPiles,
                BuiltInCategory.OST_PierColumnTags,
                BuiltInCategory.OST_PierColumns,
                BuiltInCategory.OST_PierCapTags,
                BuiltInCategory.OST_PierCaps,
                BuiltInCategory.OST_ApproachSlabTags,
                BuiltInCategory.OST_AbutmentWallTags,
                BuiltInCategory.OST_AbutmentPileTags,
                BuiltInCategory.OST_AbutmentFoundationTags,
                BuiltInCategory.OST_ApproachSlabs,
                BuiltInCategory.OST_AbutmentWalls,
                BuiltInCategory.OST_AbutmentPiles,
                BuiltInCategory.OST_AbutmentFoundations,
                BuiltInCategory.OST_BridgeBearingTags,
                BuiltInCategory.OST_BridgeGirderTags,
                BuiltInCategory.OST_BridgeFoundationTags,
                BuiltInCategory.OST_BridgeDeckTags,
                BuiltInCategory.OST_BridgeArchTags,
                BuiltInCategory.OST_BridgeCableTags,
                BuiltInCategory.OST_BridgeTowerTags,
                BuiltInCategory.OST_BridgePierTags,
                BuiltInCategory.OST_BridgeAbutmentTags,
                BuiltInCategory.OST_BridgeBearingHiddenLines,
                BuiltInCategory.OST_BridgeGirderHiddenLines2021_Deprecated,
                BuiltInCategory.OST_BridgeFoundationHiddenLines2021_Deprecated,
                BuiltInCategory.OST_BridgeDeckHiddenLines,
                BuiltInCategory.OST_BridgeArchHiddenLines2021_Deprecated,
                BuiltInCategory.OST_BridgeCableHiddenLines2021_Deprecated,
                BuiltInCategory.OST_BridgeTowerHiddenLines2021_Deprecated,
                BuiltInCategory.OST_BridgePierHiddenLines,
                BuiltInCategory.OST_BridgeAbutmentHiddenLines,
                BuiltInCategory.OST_BridgeBearings,
                BuiltInCategory.OST_BridgeGirders,
                BuiltInCategory.OST_BridgeFoundations,
                BuiltInCategory.OST_BridgeDecks,
                BuiltInCategory.OST_BridgeArches,
                BuiltInCategory.OST_BridgeCables,
                BuiltInCategory.OST_BridgeTowers,
                BuiltInCategory.OST_BridgePiers,
                BuiltInCategory.OST_BridgeAbutments,
                BuiltInCategory.OST_DesignOptions,
                BuiltInCategory.OST_DesignOptionSets,
                BuiltInCategory.OST_StructuralBracePlanReps,
                BuiltInCategory.OST_StructConnectionSymbols,
                BuiltInCategory.OST_StructuralAnnotations,
                BuiltInCategory.OST_RevisionCloudTags,
                BuiltInCategory.OST_RevisionNumberingSequences,
                BuiltInCategory.OST_Revisions,
                BuiltInCategory.OST_RevisionClouds,
                BuiltInCategory.OST_EditCutProfile,
                BuiltInCategory.OST_ElevationMarks,
                BuiltInCategory.OST_GridHeads,
                BuiltInCategory.OST_LevelHeads,
                BuiltInCategory.OST_DecalType,
                BuiltInCategory.OST_DecalElement,
                BuiltInCategory.OST_VolumeOfInterest,
                BuiltInCategory.OST_BoundaryConditions,
                BuiltInCategory.OST_InternalAreaLoadTags,
                BuiltInCategory.OST_InternalLineLoadTags,
                BuiltInCategory.OST_InternalPointLoadTags,
                BuiltInCategory.OST_AreaLoadTags,
                BuiltInCategory.OST_LineLoadTags,
                BuiltInCategory.OST_PointLoadTags,
                BuiltInCategory.OST_LoadCasesSeismic,
                BuiltInCategory.OST_LoadCasesTemperature,
                BuiltInCategory.OST_LoadCasesAccidental,
                BuiltInCategory.OST_LoadCasesRoofLive,
                BuiltInCategory.OST_LoadCasesSnow,
                BuiltInCategory.OST_LoadCasesWind,
                BuiltInCategory.OST_LoadCasesLive,
                BuiltInCategory.OST_LoadCasesDead,
                BuiltInCategory.OST_LoadCases,
                BuiltInCategory.OST_InternalAreaLoads,
                BuiltInCategory.OST_InternalLineLoads,
                BuiltInCategory.OST_InternalPointLoads,
                BuiltInCategory.OST_InternalLoads,
                BuiltInCategory.OST_AreaLoads,
                BuiltInCategory.OST_LineLoads,
                BuiltInCategory.OST_PointLoads,
                BuiltInCategory.OST_Loads,
                BuiltInCategory.OST_BeamSystemTags,
                BuiltInCategory.OST_FootingSpanDirectionSymbol,
                BuiltInCategory.OST_SpanDirectionSymbol,
                BuiltInCategory.OST_SpotSlopesSymbols,
                BuiltInCategory.OST_SpotCoordinateSymbols,
                BuiltInCategory.OST_SpotElevSymbols,
                BuiltInCategory.OST_MultiLeaderTag,
                BuiltInCategory.OST_CurtainWallMullionTags,
                BuiltInCategory.OST_StructuralConnectionHandlerTags_Deprecated,
                BuiltInCategory.OST_TrussTags,
                BuiltInCategory.OST_KeynoteTags,
                BuiltInCategory.OST_DetailComponentTags,
                BuiltInCategory.OST_MaterialTags,
                BuiltInCategory.OST_FloorTags,
                BuiltInCategory.OST_CurtaSystemTags,
                BuiltInCategory.OST_HostFinTags,
                BuiltInCategory.OST_StairsTags,
                BuiltInCategory.OST_MultiCategoryTags,
                BuiltInCategory.OST_PlantingTags,
                BuiltInCategory.OST_AreaTags,
                BuiltInCategory.OST_StructuralFoundationTags,
                BuiltInCategory.OST_StructuralColumnTags,
                BuiltInCategory.OST_ParkingTags,
                BuiltInCategory.OST_SiteTags,
                BuiltInCategory.OST_StructuralFramingTags,
                BuiltInCategory.OST_SpecialityEquipmentTags,
                BuiltInCategory.OST_GenericModelTags,
                BuiltInCategory.OST_CurtainWallPanelTags,
                BuiltInCategory.OST_WallTags,
                BuiltInCategory.OST_PlumbingFixtureTags,
                BuiltInCategory.OST_MechanicalEquipmentTags,
                BuiltInCategory.OST_LightingFixtureTags,
                BuiltInCategory.OST_FurnitureSystemTags,
                BuiltInCategory.OST_FurnitureTags,
                BuiltInCategory.OST_ElectricalFixtureTags,
                BuiltInCategory.OST_ElectricalEquipmentTags,
                BuiltInCategory.OST_CeilingTags,
                BuiltInCategory.OST_CaseworkTags,
                BuiltInCategory.OST_Tags,
                BuiltInCategory.OST_MEPSpaceColorFill,
                BuiltInCategory.OST_MEPSpaceReference,
                BuiltInCategory.OST_MEPSpaceInteriorFill,
                BuiltInCategory.OST_MEPSpaceReferenceVisibility,
                BuiltInCategory.OST_MEPSpaceInteriorFillVisibility,
                BuiltInCategory.OST_MEPSpaces,
                BuiltInCategory.OST_StackedWalls,
                BuiltInCategory.OST_MassGlazingAll,
                BuiltInCategory.OST_MassFloorsAll,
                BuiltInCategory.OST_MassWallsAll,
                BuiltInCategory.OST_MassExteriorWallUnderground,
                BuiltInCategory.OST_MassSlab,
                BuiltInCategory.OST_MassShade,
                BuiltInCategory.OST_MassOpening,
                BuiltInCategory.OST_MassSkylights,
                BuiltInCategory.OST_MassGlazing,
                BuiltInCategory.OST_MassRoof,
                BuiltInCategory.OST_MassExteriorWall,
                BuiltInCategory.OST_MassInteriorWall,
                BuiltInCategory.OST_MassZone,
                BuiltInCategory.OST_MassAreaFaceTags,
                BuiltInCategory.OST_HostTemplate,
                BuiltInCategory.OST_MassFaceSplitter,
                BuiltInCategory.OST_MassCutter,
                BuiltInCategory.OST_ZoningEnvelope,
                BuiltInCategory.OST_MassTags,
                BuiltInCategory.OST_MassForm,
                BuiltInCategory.OST_MassFloor,
                BuiltInCategory.OST_Mass,
                BuiltInCategory.OST_DividedSurface_DiscardedDivisionLines,
                BuiltInCategory.OST_DividedSurfaceBelt,
                BuiltInCategory.OST_TilePatterns,
                BuiltInCategory.OST_AlwaysExcludedInAllViews,
                BuiltInCategory.OST_DividedSurface_TransparentFace,
                BuiltInCategory.OST_DividedSurface_PreDividedSurface,
                BuiltInCategory.OST_DividedSurface_PatternFill,
                BuiltInCategory.OST_DividedSurface_PatternLines,
                BuiltInCategory.OST_DividedSurface_Gridlines,
                BuiltInCategory.OST_DividedSurface_Nodes,
                BuiltInCategory.OST_DividedSurface,
                BuiltInCategory.OST_RepeatingDetailLines,
                BuiltInCategory.OST_RampsDownArrow,
                BuiltInCategory.OST_RampsUpArrow,
                BuiltInCategory.OST_RampsDownText,
                BuiltInCategory.OST_RampsUpText,
                BuiltInCategory.OST_RampsStringerAboveCut,
                BuiltInCategory.OST_RampsStringer,
                BuiltInCategory.OST_RampsAboveCut,
                BuiltInCategory.OST_RampsIncomplete,
                BuiltInCategory.OST_TrussDummy,
                BuiltInCategory.OST_ZoneSchemes,
                BuiltInCategory.OST_AreaSchemes,
                BuiltInCategory.OST_Areas,
                BuiltInCategory.OST_ProjectInformation,
                BuiltInCategory.OST_Sheets,
                BuiltInCategory.OST_ProfileFamilies,
                BuiltInCategory.OST_DetailComponents,
                BuiltInCategory.OST_RoofSoffit,
                BuiltInCategory.OST_EdgeSlab,
                BuiltInCategory.OST_Gutter,
                BuiltInCategory.OST_Fascia,
                BuiltInCategory.OST_Entourage,
                BuiltInCategory.OST_Planting,
                BuiltInCategory.OST_Blocks,
                BuiltInCategory.OST_StructuralStiffenerHiddenLines,
                BuiltInCategory.OST_StructuralColumnLocationLine,
                BuiltInCategory.OST_StructuralFramingLocationLine,
                BuiltInCategory.OST_StructuralStiffenerTags,
                BuiltInCategory.OST_StructuralStiffener,
                BuiltInCategory.OST_FootingAnalyticalGeometry,
                BuiltInCategory.OST_RvtLinks,
                BuiltInCategory.OST_Automatic,
                BuiltInCategory.OST_SpecialityEquipment,
                BuiltInCategory.OST_ColumnAnalyticalRigidLinks,
                BuiltInCategory.OST_SecondaryTopographyContours,
                BuiltInCategory.OST_TopographyContours,
                BuiltInCategory.OST_TopographySurface,
                BuiltInCategory.OST_Topography,
                BuiltInCategory.OST_TopographyLink,
                BuiltInCategory.OST_StructuralTruss,
                BuiltInCategory.OST_StructuralColumnStickSymbols,
                BuiltInCategory.OST_HiddenStructuralColumnLines,
                BuiltInCategory.OST_AnalyticalRigidLinks,
                BuiltInCategory.OST_ColumnAnalyticalGeometry,
                BuiltInCategory.OST_FramingAnalyticalGeometry,
                //BuiltInCategory.OST_StructuralColumns,
                BuiltInCategory.OST_HiddenStructuralFramingLines,
                BuiltInCategory.OST_KickerBracing,
                BuiltInCategory.OST_StructuralFramingSystem,
                BuiltInCategory.OST_VerticalBracing,
                BuiltInCategory.OST_HorizontalBracing,
                BuiltInCategory.OST_Purlin,
                BuiltInCategory.OST_Joist,
                BuiltInCategory.OST_Girder,
                BuiltInCategory.OST_StructuralFramingOther,
                BuiltInCategory.OST_StructuralFraming,
                BuiltInCategory.OST_HiddenStructuralFoundationLines,
                BuiltInCategory.OST_StructuralFoundation,
                BuiltInCategory.OST_LinkBasePoint,
                BuiltInCategory.OST_BasePointAxisZ,
                BuiltInCategory.OST_BasePointAxisY,
                BuiltInCategory.OST_BasePointAxisX,
                BuiltInCategory.OST_SharedBasePoint,
                BuiltInCategory.OST_ProjectBasePoint,
                BuiltInCategory.OST_SiteRegion,
                BuiltInCategory.OST_SitePropertyLineSegmentTags,
                BuiltInCategory.OST_SitePropertyLineSegment,
                BuiltInCategory.OST_SitePropertyTags,
                BuiltInCategory.OST_SitePointBoundary,
                BuiltInCategory.OST_SiteProperty,
                BuiltInCategory.OST_BuildingPad,
                BuiltInCategory.OST_SitePoint,
                BuiltInCategory.OST_SiteSurface,
                BuiltInCategory.OST_Site,
                BuiltInCategory.OST_Sewer,
                BuiltInCategory.OST_RoadTags,
                BuiltInCategory.OST_Roads,
                BuiltInCategory.OST_Property,
                BuiltInCategory.OST_Parking,
                BuiltInCategory.OST_PlumbingFixtures,
                BuiltInCategory.OST_MechanicalEquipment,
                BuiltInCategory.OST_LightingFixtureSource,
                BuiltInCategory.OST_LightingFixtures,
                BuiltInCategory.OST_FurnitureSystems,
                BuiltInCategory.OST_ElectricalLoadSet,
                BuiltInCategory.OST_ToposolidLink,
                BuiltInCategory.OST_ElectricalAnalyticalFeeder,
                BuiltInCategory.OST_ToposolidOpening,
                BuiltInCategory.OST_ToposolidTags,
                BuiltInCategory.OST_ToposolidInsulation,
                BuiltInCategory.OST_ToposolidSurfacePattern,
                BuiltInCategory.OST_ToposolidFinish2,
                BuiltInCategory.OST_ToposolidFinish1,
                BuiltInCategory.OST_ToposolidSubstrate,
                BuiltInCategory.OST_ToposolidStructure,
                BuiltInCategory.OST_ToposolidMembrane,
                BuiltInCategory.OST_ToposolidCutPattern,
                BuiltInCategory.OST_ToposolidDefault,
                BuiltInCategory.OST_ToposolidSplitLines,
                BuiltInCategory.OST_ToposolidFoldingLines,
                BuiltInCategory.OST_ToposolidSecondaryContours,
                BuiltInCategory.OST_ToposolidContours,
                BuiltInCategory.OST_ToposolidHiddenLines,
                BuiltInCategory.OST_Toposolid,
                BuiltInCategory.OST_ELECTRICAL_AreaBasedLoads_Tags,
                BuiltInCategory.OST_ElectricalAnalyticalTransformer,
                BuiltInCategory.OST_FloorsSplitLines,
                BuiltInCategory.OST_AnalyticalMemberCrossSection,
                BuiltInCategory.OST_RvtLinksTags,
                BuiltInCategory.OST_ModelGroupTags,
                BuiltInCategory.OST_WallSweepTags,
                BuiltInCategory.OST_TopRailTags,
                BuiltInCategory.OST_SlabEdgeTags,
                BuiltInCategory.OST_RoofSoffitTags,
                BuiltInCategory.OST_RampTags,
                BuiltInCategory.OST_PadTags,
                BuiltInCategory.OST_HandrailTags,
                BuiltInCategory.OST_GutterTags,
                BuiltInCategory.OST_EntourageTags,
                BuiltInCategory.OST_ColumnTags,
                BuiltInCategory.OST_FasciaTags,
                BuiltInCategory.OST_SignageTags,
                BuiltInCategory.OST_ElectricalFixtures,
                BuiltInCategory.OST_SignageHiddenLines,
                BuiltInCategory.OST_Signage,
                BuiltInCategory.OST_AudioVisualDeviceTags,
                BuiltInCategory.OST_AudioVisualDevicesHiddenLines,
                BuiltInCategory.OST_AudioVisualDevices,
                BuiltInCategory.OST_VerticalCirculationTags,
                BuiltInCategory.OST_VerticalCirculationHiddenLines,
                BuiltInCategory.OST_VerticalCirculation,
                BuiltInCategory.OST_FireProtectionTags,
                BuiltInCategory.OST_FireProtectionHiddenLines,
                BuiltInCategory.OST_FireProtection,
                BuiltInCategory.OST_MedicalEquipmentTags,
                BuiltInCategory.OST_MedicalEquipmentHiddenLines,
                BuiltInCategory.OST_MedicalEquipment,
                BuiltInCategory.OST_FoodServiceEquipmentTags,
                BuiltInCategory.OST_FoodServiceEquipmentHiddenLines,
                BuiltInCategory.OST_FoodServiceEquipment,
                BuiltInCategory.OST_TemporaryStructureTags,
                BuiltInCategory.OST_TemporaryStructureHiddenLines,
                BuiltInCategory.OST_ElectricalEquipment,
                BuiltInCategory.OST_TemporaryStructure,
                BuiltInCategory.OST_HardscapeTags,
                BuiltInCategory.OST_HardscapeHiddenLines,
                BuiltInCategory.OST_Hardscape,
                BuiltInCategory.OST_WallCoreLayer,
                BuiltInCategory.OST_WallNonCoreLayer,
                BuiltInCategory.OST_MEPLoadAreaSeparationLines,
                BuiltInCategory.OST_MEPLoadAreaReferenceVisibility,
                BuiltInCategory.OST_MEPLoadAreaInteriorFillVisibility,
                BuiltInCategory.OST_MEPLoadAreaReference,
                BuiltInCategory.OST_MEPLoadAreaInteriorFill,
                BuiltInCategory.OST_MEPLoadAreaColorFill,
                BuiltInCategory.OST_ElectricalPowerSource,
                BuiltInCategory.OST_MEPLoadAreaTags_OBSOLETE,
                BuiltInCategory.OST_MEPLoadAreas,
                BuiltInCategory.OST_MEPAnalyticalTransferSwitch,
                BuiltInCategory.OST_OBSOLETE_MEPAnalyticalElectricalBranch,
                BuiltInCategory.OST_MEPAnalyticalBus,
                BuiltInCategory.OST_ElectricalLoadZoneInstance,
                BuiltInCategory.OST_ElectricalLoadZoneType,
                BuiltInCategory.OST_ElectricalZoneEquipment_Obsolete,
                BuiltInCategory.OST_AlignmentStationLabels,
                BuiltInCategory.OST_AlignmentStationLabelSets,
                BuiltInCategory.OST_AlignmentsTags,
                BuiltInCategory.OST_MinorStations_Deprecated,
                BuiltInCategory.OST_MajorStations_Deprecated,
                BuiltInCategory.OST_Alignments,
                BuiltInCategory.OST_ElectricalCircuitNaming,
                BuiltInCategory.OST_ZoneEquipment,
                BuiltInCategory.OST_MEPAnalyticalWaterLoop,
                BuiltInCategory.OST_MEPAnalyticalAirLoop,
                BuiltInCategory.OST_MEPSystemZoneTags,
                BuiltInCategory.OST_MEPSystemZoneReferenceLinesVisibility,
                BuiltInCategory.OST_MEPSystemZoneInteriorFillVisibility,
                BuiltInCategory.OST_MEPSystemZoneReferenceLines,
                BuiltInCategory.OST_MEPSystemZoneInteriorFill,
                BuiltInCategory.OST_MEPSystemZoneBoundary,
                BuiltInCategory.OST_MEPSystemZone,
                BuiltInCategory.OST_Casework,
                BuiltInCategory.OST_ArcWallRectOpening,
                BuiltInCategory.OST_DormerOpeningIncomplete,
                BuiltInCategory.OST_SWallRectOpening,
                BuiltInCategory.OST_ShaftOpening,
                BuiltInCategory.OST_StructuralFramingOpening,
                BuiltInCategory.OST_ColumnOpening,
                BuiltInCategory.OST_RiseDropSymbols,
                BuiltInCategory.OST_PipeHydronicSeparationSymbols,
                BuiltInCategory.OST_MechanicalEquipmentSetBoundaryLines,
                BuiltInCategory.OST_MechanicalEquipmentSetTags,
                BuiltInCategory.OST_MechanicalEquipmentSet,
                BuiltInCategory.OST_AnalyticalPipeConnectionLineSymbol,
                BuiltInCategory.OST_AnalyticalPipeConnections,
                BuiltInCategory.OST_Coordination_Model,
                BuiltInCategory.OST_MultistoryStairs,
                BuiltInCategory.OST_HiddenStructuralConnectionLines_Deprecated,
                BuiltInCategory.OST_StructuralConnectionHandler_Deprecated,
                BuiltInCategory.OST_CoordinateSystem,
                BuiltInCategory.OST_FndSlabLocalCoordSys,
                BuiltInCategory.OST_FloorLocalCoordSys,
                BuiltInCategory.OST_WallLocalCoordSys,
                BuiltInCategory.OST_BraceLocalCoordSys,
                BuiltInCategory.OST_ColumnLocalCoordSys,
                BuiltInCategory.OST_BeamLocalCoordSys,
                BuiltInCategory.OST_MultiReferenceAnnotations,
                BuiltInCategory.OST_DSR_LeaderTickMarkStyleId,
                BuiltInCategory.OST_DSR_InteriorTickMarkStyleId,
                BuiltInCategory.OST_DSR_ArrowHeadStyleId,
                BuiltInCategory.OST_DSR_CenterlineTickMarkStyleId,
                BuiltInCategory.OST_DSR_CenterlinePatternCatId,
                BuiltInCategory.OST_DSR_DimStyleHeavyEndCategoryId,
                BuiltInCategory.OST_DSR_DimStyleHeavyEndCatId,
                BuiltInCategory.OST_DSR_DimStyleTickCategoryId,
                BuiltInCategory.OST_DSR_LineAndTextAttrFontId,
                BuiltInCategory.OST_DSR_LineAndTextAttrCategoryId,
                BuiltInCategory.OST_AnalyticalOpeningTags,
                BuiltInCategory.OST_AnalyticalPanelTags,
                BuiltInCategory.OST_NodeAnalyticalTags,
                BuiltInCategory.OST_LinkAnalyticalTags,
                BuiltInCategory.OST_RailingRailPathExtensionLines,
                BuiltInCategory.OST_RailingRailPathLines,
                BuiltInCategory.OST_StairsSupports,
                BuiltInCategory.OST_RailingHandRailAboveCut,
                BuiltInCategory.OST_RailingTopRailAboveCut,
                BuiltInCategory.OST_RailingTermination,
                BuiltInCategory.OST_RailingSupport,
                BuiltInCategory.OST_RailingHandRail,
                BuiltInCategory.OST_RailingTopRail,
                BuiltInCategory.OST_StairsSketchPathLines,
                BuiltInCategory.OST_StairsTriserNumbers,
                BuiltInCategory.OST_StairsTriserTags,
                BuiltInCategory.OST_StairsSupportTags,
                BuiltInCategory.OST_StairsLandingTags,
                BuiltInCategory.OST_StairsRunTags,
                BuiltInCategory.OST_StairsPathsAboveCut,
                BuiltInCategory.OST_StairsPaths,
                BuiltInCategory.OST_StairsRiserLinesAboveCut,
                BuiltInCategory.OST_StairsRiserLines,
                BuiltInCategory.OST_StairsOutlinesAboveCut,
                BuiltInCategory.OST_StairsOutlines,
                BuiltInCategory.OST_StairsNosingLinesAboveCut,
                BuiltInCategory.OST_StairsNosingLines,
                BuiltInCategory.OST_StairsCutMarksAboveCut,
                BuiltInCategory.OST_StairsCutMarks,
                BuiltInCategory.OST_ComponentRepeaterSlot,
                BuiltInCategory.OST_ComponentRepeater,
                BuiltInCategory.OST_DividedPath,
                BuiltInCategory.OST_IOSRoomCalculationPoint,
                BuiltInCategory.OST_PropertySet,
                BuiltInCategory.OST_AppearanceAsset,
                BuiltInCategory.OST_StairStringer2012_Deprecated,
                BuiltInCategory.OST_StairsTrisers,
                //BuiltInCategory.OST_StairsLandings,
                //BuiltInCategory.OST_StairsRuns,
                BuiltInCategory.OST_Stair2012_Deprecated,
                BuiltInCategory.OST_RailingSystemTags,
                BuiltInCategory.OST_RailingSystemTransition,
                BuiltInCategory.OST_RailingSystemTermination,
                BuiltInCategory.OST_RailingSystemRail,
                BuiltInCategory.OST_RailingSystemTopRail,
                BuiltInCategory.OST_RailingSystemHandRailBracket,
                BuiltInCategory.OST_RailingSystemHandRail,
                BuiltInCategory.OST_RailingSystemHardware,
                BuiltInCategory.OST_RailingSystemPanel,
                BuiltInCategory.OST_RailingSystemBaluster,
                BuiltInCategory.OST_RailingSystemPost,
                BuiltInCategory.OST_RailingSystemSegment,
                BuiltInCategory.OST_RailingSystem,
                BuiltInCategory.OST_AdaptivePoints_HiddenLines,
                BuiltInCategory.OST_AdaptivePoints_Lines,
                BuiltInCategory.OST_AdaptivePoints_Planes,
                BuiltInCategory.OST_AdaptivePoints_Points,
                BuiltInCategory.OST_AdaptivePoints,
                BuiltInCategory.OST_CeilingOpening,
                BuiltInCategory.OST_FloorOpening,
                BuiltInCategory.OST_RoofOpening,
                BuiltInCategory.OST_WallRefPlanes,
                BuiltInCategory.OST_StructLocationLineControl,
                BuiltInCategory.OST_PathOfTravelTags,
                BuiltInCategory.OST_PathOfTravelLines,
                BuiltInCategory.OST_DimLockControlLeader,
                BuiltInCategory.OST_MEPSpaceSeparationLines,
                BuiltInCategory.OST_AreaPolylines,
                BuiltInCategory.OST_RoomPolylines,
                BuiltInCategory.OST_InstanceDrivenLineStyle,
                BuiltInCategory.OST_RemovedGridSeg,
                BuiltInCategory.OST_IOSOpening,
                BuiltInCategory.OST_IOSTilePatternGrid,
                BuiltInCategory.OST_ControlLocal,
                BuiltInCategory.OST_ControlAxisZ,
                BuiltInCategory.OST_ControlAxisY,
                BuiltInCategory.OST_ControlAxisX,
                BuiltInCategory.OST_XRayConstrainedProfileEdge,
                BuiltInCategory.OST_XRayImplicitPathCurve,
                BuiltInCategory.OST_XRayPathPoint,
                BuiltInCategory.OST_XRayPathCurve,
                BuiltInCategory.OST_XRaySideEdge,
                BuiltInCategory.OST_XRayProfileEdge,
                BuiltInCategory.OST_ReferencePoints_HiddenLines,
                BuiltInCategory.OST_ReferencePoints_Lines,
                BuiltInCategory.OST_ReferencePoints_Planes,
                BuiltInCategory.OST_ReferencePoints_Points,
                BuiltInCategory.OST_ReferencePoints,
                BuiltInCategory.OST_Materials,
                BuiltInCategory.OST_CeilingsCutPattern,
                BuiltInCategory.OST_CeilingsDefault,
                BuiltInCategory.OST_CeilingsFinish2,
                BuiltInCategory.OST_CeilingsFinish1,
                BuiltInCategory.OST_CeilingsSubstrate,
                BuiltInCategory.OST_CeilingsInsulation,
                BuiltInCategory.OST_CeilingsStructure,
                BuiltInCategory.OST_CeilingsMembrane,
                BuiltInCategory.OST_FloorsInteriorEdges,
                BuiltInCategory.OST_FloorsCutPattern,
                BuiltInCategory.OST_HiddenFloorLines,
                BuiltInCategory.OST_FloorsDefault,
                BuiltInCategory.OST_FloorsFinish2,
                BuiltInCategory.OST_FloorsFinish1,
                BuiltInCategory.OST_FloorsSubstrate,
                BuiltInCategory.OST_FloorsInsulation,
                BuiltInCategory.OST_FloorsStructure,
                BuiltInCategory.OST_FloorsMembrane,
                BuiltInCategory.OST_RoofsInteriorEdges,
                BuiltInCategory.OST_RoofsCutPattern,
                BuiltInCategory.OST_RoofsDefault,
                BuiltInCategory.OST_RoofsFinish2,
                BuiltInCategory.OST_RoofsFinish1,
                BuiltInCategory.OST_RoofsSubstrate,
                BuiltInCategory.OST_RoofsInsulation,
                BuiltInCategory.OST_RoofsStructure,
                BuiltInCategory.OST_RoofsMembrane,
                BuiltInCategory.OST_WallsCutPattern,
                BuiltInCategory.OST_HiddenWallLines,
                BuiltInCategory.OST_WallsDefault,
                BuiltInCategory.OST_WallsFinish2,
                BuiltInCategory.OST_WallsFinish1,
                BuiltInCategory.OST_WallsSubstrate,
                BuiltInCategory.OST_WallsInsulation,
                BuiltInCategory.OST_WallsStructure,
                BuiltInCategory.OST_WallsMembrane,
                BuiltInCategory.OST_PreviewLegendComponents,
                BuiltInCategory.OST_LegendComponents,
                BuiltInCategory.OST_Schedules,
                BuiltInCategory.OST_ScheduleGraphics,
                BuiltInCategory.OST_RasterImages,
                BuiltInCategory.OST_ColorFillSchema,
                BuiltInCategory.OST_RoomColorFill,
                BuiltInCategory.OST_ColorFillLegends,
                BuiltInCategory.OST_AnnotationCropSpecial,
                BuiltInCategory.OST_CropBoundarySpecial,
                BuiltInCategory.OST_AnnotationCrop,
                BuiltInCategory.OST_FloorsAnalyticalGeometry,
                BuiltInCategory.OST_WallsAnalyticalGeometry,
                BuiltInCategory.OST_CalloutLeaderLine,
                BuiltInCategory.OST_CeilingsSurfacePattern,
                BuiltInCategory.OST_RoofsSurfacePattern,
                BuiltInCategory.OST_FloorsSurfacePattern,
                BuiltInCategory.OST_WallsSurfacePattern,
                BuiltInCategory.OST_CalloutBoundary,
                BuiltInCategory.OST_CalloutHeads,
                BuiltInCategory.OST_Callouts,
                BuiltInCategory.OST_CropBoundary,
                BuiltInCategory.OST_Elev,
                BuiltInCategory.OST_AxisZ,
                BuiltInCategory.OST_AxisY,
                BuiltInCategory.OST_AxisX,
                BuiltInCategory.OST_CLines,
                BuiltInCategory.OST_Lights,
                BuiltInCategory.OST_ViewportLabel,
                BuiltInCategory.OST_Viewports,
                BuiltInCategory.OST_Camera_Lines,
                //BuiltInCategory.OST_Cameras,
                BuiltInCategory.OST_MEPSpaceTags,
                BuiltInCategory.OST_RoomTags,
                BuiltInCategory.OST_DoorTags,
                BuiltInCategory.OST_WindowTags,
                BuiltInCategory.OST_SectionHeadWideLines,
                BuiltInCategory.OST_SectionHeadMediumLines,
                BuiltInCategory.OST_SectionHeadThinLines,
                BuiltInCategory.OST_SectionHeads,
                BuiltInCategory.OST_ContourLabels,
                BuiltInCategory.OST_CurtaSystemFaceManager,
                BuiltInCategory.OST_CurtaSystem,
                BuiltInCategory.OST_AreaReport_Arc_Minus,
                BuiltInCategory.OST_AreaReport_Arc_Plus,
                BuiltInCategory.OST_AreaReport_Boundary,
                BuiltInCategory.OST_AreaReport_Triangle,
                //BuiltInCategory.OST_CurtainGridsCurtaSystem,
                //BuiltInCategory.OST_CurtainGridsSystem,
                //BuiltInCategory.OST_CurtainGridsWall,
                //BuiltInCategory.OST_CurtainGridsRoof,
                BuiltInCategory.OST_HostFinHF,
                BuiltInCategory.OST_HostFinWall,
                BuiltInCategory.OST_HostFinCeiling,
                BuiltInCategory.OST_HostFinRoof,
                BuiltInCategory.OST_HostFinFloor,
                BuiltInCategory.OST_HostFin,
                BuiltInCategory.OST_AnalysisDisplayStyle,
                BuiltInCategory.OST_AnalysisResults,
                BuiltInCategory.OST_RenderRegions,
                //BuiltInCategory.OST_SectionBox,
                BuiltInCategory.OST_TextNotes,
                BuiltInCategory.OST_Divisions,
                BuiltInCategory.OST_Catalogs,
                BuiltInCategory.OST_DirectionEdgeLines,
                BuiltInCategory.OST_CenterLines,
                BuiltInCategory.OST_LinesBeyond,
                BuiltInCategory.OST_HiddenLines,
                BuiltInCategory.OST_DemolishedLines,
                BuiltInCategory.OST_OverheadLines,
                BuiltInCategory.OST_TitleBlockWideLines,
                BuiltInCategory.OST_TitleBlockMediumLines,
                BuiltInCategory.OST_TitleBlockThinLines,
                BuiltInCategory.OST_TitleBlocks,
                BuiltInCategory.OST_Views,
                BuiltInCategory.OST_Viewers,
                BuiltInCategory.OST_PartHiddenLines,
                BuiltInCategory.OST_PartTags,
                BuiltInCategory.OST_Parts,
                BuiltInCategory.OST_AssemblyTags,
                BuiltInCategory.OST_Assemblies,
                BuiltInCategory.OST_RoofTags,
                BuiltInCategory.OST_SpotSlopes,
                BuiltInCategory.OST_SpotCoordinates,
                BuiltInCategory.OST_SpotElevations,
                BuiltInCategory.OST_Constraints,
                BuiltInCategory.OST_WeakDims,
                BuiltInCategory.OST_Dimensions,
                BuiltInCategory.OST_Levels,
                BuiltInCategory.OST_DisplacementPath,
                BuiltInCategory.OST_DisplacementElements,
                BuiltInCategory.OST_GridChains,
                BuiltInCategory.OST_Grids,
                BuiltInCategory.OST_BrokenSectionLine,
                BuiltInCategory.OST_SectionLine,
                BuiltInCategory.OST_Sections,
                BuiltInCategory.OST_ReferenceViewer,
                BuiltInCategory.OST_ReferenceViewerSymbol,
                BuiltInCategory.OST_ImportObjectStyles,
                BuiltInCategory.OST_ModelText,
                BuiltInCategory.OST_MaskingRegion,
                BuiltInCategory.OST_Matchline,
                BuiltInCategory.OST_FaceSplitter,
                BuiltInCategory.OST_PlanRegion,
                BuiltInCategory.OST_FilledRegion,
                BuiltInCategory.OST_MassingProjectionOutlines,
                BuiltInCategory.OST_MassingCutOutlines,
                BuiltInCategory.OST_Massing,
                BuiltInCategory.OST_Reveals,
                BuiltInCategory.OST_Cornices,
                BuiltInCategory.OST_Ramps,
                BuiltInCategory.OST_RailingBalusterRailCut,
                BuiltInCategory.OST_RailingBalusterRail,
                BuiltInCategory.OST_Railings,
                //BuiltInCategory.OST_CurtainGrids,
                //BuiltInCategory.OST_CurtainWallMullionsCut,
                //BuiltInCategory.OST_CurtainWallMullions,
                //BuiltInCategory.OST_CurtainWallPanels,
                BuiltInCategory.OST_AreaReference,
                BuiltInCategory.OST_AreaInteriorFill,
                BuiltInCategory.OST_RoomReference,
                BuiltInCategory.OST_RoomInteriorFill,
                BuiltInCategory.OST_AreaColorFill,
                BuiltInCategory.OST_AreaReferenceVisibility,
                BuiltInCategory.OST_AreaInteriorFillVisibility,
                BuiltInCategory.OST_RoomReferenceVisibility,
                BuiltInCategory.OST_RoomInteriorFillVisibility,
                //BuiltInCategory.OST_Rooms,
                //BuiltInCategory.OST_GenericModel,
                BuiltInCategory.OST_GenericAnnotation,
                BuiltInCategory.OST_Fixtures,
                BuiltInCategory.OST_StairsRailingTags,
                BuiltInCategory.OST_StairsRailingAboveCut,
                BuiltInCategory.OST_StairsDownArrows,
                BuiltInCategory.OST_StairsUpArrows,
                BuiltInCategory.OST_StairsDownText,
                BuiltInCategory.OST_StairsRailingRail,
                BuiltInCategory.OST_StairsRailingBaluster,
                BuiltInCategory.OST_StairsRailing,
                BuiltInCategory.OST_StairsUpText,
                BuiltInCategory.OST_StairsSupportsAboveCut,
                //BuiltInCategory.OST_StairsStringerCarriage,
                BuiltInCategory.OST_StairsAboveCut_ToBeDeprecated,
                BuiltInCategory.OST_StairsIncomplete_Deprecated,
                BuiltInCategory.OST_Stairs,
                BuiltInCategory.OST_IOSNavWheelPivotBall,
                BuiltInCategory.OST_IOSRoomComputationHeight,
                BuiltInCategory.OST_IOSRoomUpperLowerLines,
                BuiltInCategory.OST_IOSDragBoxInverted,
                BuiltInCategory.OST_IOSDragBox,
                BuiltInCategory.OST_Phases,
                BuiltInCategory.OST_IOS_GeoSite,
                BuiltInCategory.OST_IOS_GeoLocations,
                BuiltInCategory.OST_IOSFabricReinSpanSymbolCtrl,
                BuiltInCategory.OST_GuideGrid,
                BuiltInCategory.OST_EPS_Future,
                BuiltInCategory.OST_EPS_Temporary,
                BuiltInCategory.OST_EPS_New,
                BuiltInCategory.OST_EPS_Demolished,
                BuiltInCategory.OST_EPS_Existing,
                BuiltInCategory.OST_IOSMeasureLineScreenSize,
                //BuiltInCategory.OST_Columns,
                BuiltInCategory.OST_IOSRebarSystemSpanSymbolCtrl,
                BuiltInCategory.OST_IOSRoomTagToRoomLines,
                BuiltInCategory.OST_IOSAttachedDetailGroups,
                BuiltInCategory.OST_IOSDetailGroups,
                BuiltInCategory.OST_IOSModelGroups,
                BuiltInCategory.OST_IOSSuspendedSketch,
                BuiltInCategory.OST_IOSWallCoreBoundary,
                BuiltInCategory.OST_IOSMeasureLine,
                BuiltInCategory.OST_IOSArrays,
                //BuiltInCategory.OST_Curtain_Systems,
                BuiltInCategory.OST_IOSBBoxScreenSize,
                BuiltInCategory.OST_IOSSlabShapeEditorPointInterior,
                BuiltInCategory.OST_IOSSlabShapeEditorPointBoundary,
                BuiltInCategory.OST_IOSSlabShapeEditorBoundary,
                BuiltInCategory.OST_IOSSlabShapeEditorAutoCrease,
                BuiltInCategory.OST_IOSSlabShapeEditorExplitCrease,
                BuiltInCategory.OST_ReferenceLines,
                BuiltInCategory.OST_IOSNotSilhouette,
                BuiltInCategory.OST_FillPatterns,
                BuiltInCategory.OST_Furniture,
                BuiltInCategory.OST_AreaSchemeLines,
                BuiltInCategory.OST_GenericLines,
                BuiltInCategory.OST_InsulationLines,
                BuiltInCategory.OST_CloudLines,
                BuiltInCategory.OST_IOSRoomPerimeterLines,
                BuiltInCategory.OST_IOSCuttingGeometry,
                BuiltInCategory.OST_IOSCrashGraphics,
                BuiltInCategory.OST_IOSGroups,
                BuiltInCategory.OST_IOSGhost,
                BuiltInCategory.OST_StairsSketchLandingCenterLines,
                BuiltInCategory.OST_StairsSketchRunLines,
                BuiltInCategory.OST_StairsSketchRiserLines,
                BuiltInCategory.OST_StairsSketchBoundaryLines,
                BuiltInCategory.OST_RoomSeparationLines,
                BuiltInCategory.OST_AxisOfRotation,
                BuiltInCategory.OST_InvisibleLines,
                BuiltInCategory.OST_IOSThinPixel_DashDot,
                BuiltInCategory.OST_IOSThinPixel_Dash,
                BuiltInCategory.OST_IOSThinPixel_Dot,
                BuiltInCategory.OST_Extrusions,
                BuiltInCategory.OST_IOS,
                BuiltInCategory.OST_CutOutlines,
                BuiltInCategory.OST_IOSThinPixel,
                BuiltInCategory.OST_IOSFlipControl,
                BuiltInCategory.OST_IOSSketchGrid,
                BuiltInCategory.OST_IOSSuspendedSketch_obsolete,
                BuiltInCategory.OST_IOSFreeSnapLine,
                BuiltInCategory.OST_IOSDatumPlane,
                BuiltInCategory.OST_Lines,
                BuiltInCategory.OST_IOSConstructionLine,
                BuiltInCategory.OST_IOSAlignmentGraphics,
                BuiltInCategory.OST_IOSAligningLine,
                BuiltInCategory.OST_IOSBackedUpElements,
                BuiltInCategory.OST_IOSRegeneratedElements,
                BuiltInCategory.OST_SketchLines,
                BuiltInCategory.OST_CurvesWideLines,
                BuiltInCategory.OST_CurvesMediumLines,
                BuiltInCategory.OST_CurvesThinLines,
                BuiltInCategory.OST_Curves,
                BuiltInCategory.OST_CeilingsProjection,
                BuiltInCategory.OST_CeilingsCut,
                BuiltInCategory.OST_Ceilings,
                BuiltInCategory.OST_RoofsProjection,
                BuiltInCategory.OST_RoofsCut,
                BuiltInCategory.OST_Roofs,
                BuiltInCategory.OST_FloorsProjection,
                BuiltInCategory.OST_FloorsCut,
                BuiltInCategory.OST_Floors,
                //BuiltInCategory.OST_DoorsGlassProjection,
                //BuiltInCategory.OST_DoorsGlassCut,
                //BuiltInCategory.OST_DoorsFrameMullionProjection,
                //BuiltInCategory.OST_DoorsFrameMullionCut,
                //BuiltInCategory.OST_DoorsOpeningProjection,
                //BuiltInCategory.OST_DoorsOpeningCut,
                //BuiltInCategory.OST_DoorsPanelProjection,
                //BuiltInCategory.OST_DoorsPanelCut,
                //BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_WindowsOpeningProjection,
                BuiltInCategory.OST_WindowsOpeningCut,
                BuiltInCategory.OST_WindowsSillHeadProjection,
                BuiltInCategory.OST_WindowsSillHeadCut,
                BuiltInCategory.OST_WindowsFrameMullionProjection,
                BuiltInCategory.OST_WindowsFrameMullionCut,
                BuiltInCategory.OST_WindowsGlassProjection,
                BuiltInCategory.OST_WindowsGlassCut,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_WallsProjectionOutlines,
                BuiltInCategory.OST_WallsCutOutlines,
                //BuiltInCategory.OST_Walls,
                //BuiltInCategory.OST_IOSRegenerationFailure,
                //BuiltInCategory.OST_ScheduleViewParamGroup,
                //BuiltInCategory.OST_MatchSiteComponent,
                //BuiltInCategory.OST_MatchProfile,
                //BuiltInCategory.OST_MatchDetail,
                //BuiltInCategory.OST_MatchAnnotation,
                //BuiltInCategory.OST_MatchModel,
                //BuiltInCategory.OST_MatchAll
            };
                return cat;
            }
        }
        static Result RoomColorScheme(Document doc, UIApplication uiapp)
        {
            try
            {
                Category roomCategory = Category.GetCategory(doc, BuiltInCategory.OST_Rooms);

                if (roomCategory == null)
                {
                    TaskDialog.Show("Message", "RoomCategory is not used in model.");
                    return Result.Failed;
                }
                FilteredElementCollector roomCollector = new FilteredElementCollector(doc);
                roomCollector.OfCategory(BuiltInCategory.OST_Rooms);

                if (roomCollector.Count() == 0)
                {
                    TaskDialog.Show("Message", "No rooms found.");
                    return Result.Failed;
                }

                ElementId schemeId = default;

                using (Transaction tx = new Transaction(doc, "Create ColorScheme"))
                {
                    tx.Start();

                    // Create Color Scheme by duplicating an existing one 
                    ColorFillScheme firstOrDefaultColorScheme = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_ColorFillSchema)
                        .Cast<ColorFillScheme>()
                        .Where(c => c.CategoryId == roomCategory.Id)
                        .FirstOrDefault();

                    string baseName = "RouteColorRoom";
                    string newName = baseName;
                    int suffix = 1;
                    while (SchemeNameExists(doc, newName))
                    {
                        newName = baseName + "_" + suffix;
                        suffix++;
                    }

                    ElementId newColorSchemeId = firstOrDefaultColorScheme.Duplicate(newName);
                    ColorFillScheme colorScheme = doc.GetElement(newColorSchemeId) as ColorFillScheme;
                    schemeId = colorScheme.Id;
                    tx.Commit();
                }

                ColorFillScheme colorSchemeChange = doc.GetElement(schemeId) as ColorFillScheme;

                using (Transaction tx = new Transaction(doc, "Change Color"))
                {
                    tx.Start();
                    List<ColorFillSchemeEntry> list = new List<ColorFillSchemeEntry>();
                    foreach (ColorFillSchemeEntry entry in colorSchemeChange.GetEntries())
                    {
                        entry.Color = new Color(150, 150, 150);
                        list.Add(entry);
                    }
                    colorSchemeChange.SetEntries(list);
                    tx.Commit();
                }
                using (Transaction tx = new Transaction(doc, "Set ColorScheme"))
                {
                    tx.Start();
                    View currentView = uiapp.ActiveUIDocument.ActiveView;
                    currentView.SetColorFillSchemeId(roomCategory.Id, colorSchemeChange.Id);
                    tx.Commit();
                }
                return Result.Succeeded;
            }
            catch (Exception)
            {
                return Result.Failed;
            }
            static bool SchemeNameExists(Document doc, string name)
            {
                Category roomCategory = Category.GetCategory(doc, BuiltInCategory.OST_Rooms);
                List<ColorFillScheme> listColorScheme = new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_ColorFillSchema)
                        .Cast<ColorFillScheme>()
                        .Where(c => c.CategoryId == roomCategory.Id)
                        .ToList();
                List<string> listName = new List<string>();  
                
                foreach (ColorFillScheme scheme in listColorScheme)
                {
                    listName.Add(scheme.Name);
                }

                foreach (string existingName in listName)
                {
                    if (existingName == name)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public class ViewCreation
        {
            public static IEnumerable<ViewFamilyType> FindViewTypes(Document doc, ViewType viewType)
            {
                IEnumerable<ViewFamilyType> ret = new FilteredElementCollector(doc).WherePasses(new ElementClassFilter(typeof(ViewFamilyType), false)).Cast<ViewFamilyType>();

                return viewType switch
                {
                    ViewType.AreaPlan => ret.Where(e => e.ViewFamily == ViewFamily.AreaPlan),
                    ViewType.CeilingPlan => ret.Where(e => e.ViewFamily == ViewFamily.CeilingPlan),
                    ViewType.CostReport => ret.Where(e => e.ViewFamily == ViewFamily.CostReport),
                    ViewType.Detail => ret.Where(e => e.ViewFamily == ViewFamily.Detail),
                    ViewType.DraftingView => ret.Where(e => e.ViewFamily == ViewFamily.Drafting),
                    ViewType.DrawingSheet => ret.Where(e => e.ViewFamily == ViewFamily.Sheet),
                    ViewType.Elevation => ret.Where(e => e.ViewFamily == ViewFamily.Elevation),
                    ViewType.FloorPlan => ret.Where(e => e.ViewFamily == ViewFamily.FloorPlan),
                    ViewType.Legend => ret.Where(e => e.ViewFamily == ViewFamily.Legend),
                    ViewType.LoadsReport => ret.Where(e => e.ViewFamily == ViewFamily.LoadsReport),
                    ViewType.PanelSchedule => ret.Where(e => e.ViewFamily == ViewFamily.PanelSchedule),
                    ViewType.PresureLossReport => ret.Where(e => e.ViewFamily == ViewFamily.PressureLossReport),
                    ViewType.Rendering => ret.Where(e => e.ViewFamily == ViewFamily.ImageView),
                    ViewType.Schedule => ret.Where(e => e.ViewFamily == ViewFamily.Schedule),
                    ViewType.Section => ret.Where(e => e.ViewFamily == ViewFamily.Section),
                    ViewType.ThreeD => ret.Where(e => e.ViewFamily == ViewFamily.ThreeDimensional),
                    ViewType.Walkthrough => ret.Where(e => e.ViewFamily == ViewFamily.Walkthrough),
                    _ => ret,
                };
            }
            public static ViewPlan NewViewPlan(Level level, ViewType viewType)
            {
                ElementId viewTypeId = FindViewTypes(level.Document, viewType).First().Id;
                ViewPlan view = ViewPlan.Create(level.Document, viewTypeId, level.Id);
                return view;
            }
        }
    }
}