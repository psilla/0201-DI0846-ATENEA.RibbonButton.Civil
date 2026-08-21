using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.Main;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckAlignStyles;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckAssemblies;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckBodies;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckCoordSystem;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckCorridorStyles;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckFeatureLineStyles;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckPipesStyles;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckPressNetworkStyles;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckPurgeableStyles;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckSites;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckStructures;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckSubassemblies;
using static TYPSA.SharedLib.Civil.Main.cls_00_MainCheckSurfaces;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_GetDataModelChecker
    {
        private static string GetCleanCheckName(string stringKey)
        {
            return stringKey.Contains(":")
                ? stringKey.Split(':')[0].Trim()
                : stringKey;
        }

        public static void ProcessCoordinateSystem(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResultsCivil resultsFromCivil,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.CoordSystem))
            {
                return;
            }

            // Analizamos
            CoordinateSystemResult coordSystem = AnalyzeCoordSystem(
                fileName
            );

            // Validamos
            if (coordSystem == null)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.CoordSystem,
                    Message = "Selected check was executed, but no coordinate system was detected."
                });
            }
            else
            {
                // Almacenamos
                resultsFromCivil.CoordSystem.Add(coordSystem);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.CoordSystem), coordSystem }
            });
        }

        public static void ProcessPurgeableStyles(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Transaction tr,
            Database db,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResultsCivil resultsFromCivil,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.PurgeableStyles))
            {
                return;
            }

            // Analizamos
            List<CivilStyleUsageResult> civilStylesUsage = AnalyzeAllCivilStyles(
                tr, db, fileName
            );

            // Validamos
            if (civilStylesUsage == null || civilStylesUsage.Count == 0)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.PurgeableStyles,
                    Message = "Selected check was executed, but no Civil 3D styles were detected."
                });
            }
            else
            {
                // Almacenamos
                resultsFromCivil.PurgeableStyles.AddRange(civilStylesUsage);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.PurgeableStyles), civilStylesUsage }
            });
        }

        public static void ProcessCivilStyles(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Transaction tr,
            Database db,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResultsCivil resultsFromCivil,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.CivilStyles))
            {
                return;
            }

            // Analizamos
            List<AlignmentResultStyles> alignments = AnalyzeAlignmentsStyles(tr, db, fileName);
            List<CorridorResultStyles> corridors = AnalyzeCorridorsStyles(tr, db, fileName);
            List<FeatureLineResultStyles> featureLines = AnalyzeFeatureLinesStyles(tr, db, fileName);
            List<PipeResultStyles> pipes = AnalyzePipesStyles(tr, db, fileName);
            List<PressureNetworkResultStyles> pressureNetworks = AnalyzePressureNetworksStyles(tr, db, fileName);

            // Lista unificada
            List<object> civilStyles = new List<object>();

            // Validamos
            if (alignments != null && alignments.Count > 0)
            {
                resultsFromCivil.Alignments.AddRange(alignments);
                civilStyles.AddRange(alignments);
            }

            // Validamos
            if (corridors != null && corridors.Count > 0)
            {
                resultsFromCivil.Corridors.AddRange(corridors);
                civilStyles.AddRange(corridors);
            }

            // Validamos
            if (featureLines != null && featureLines.Count > 0)
            {
                resultsFromCivil.FeatureLines.AddRange(featureLines);
                civilStyles.AddRange(featureLines);
            }

            // Validamos
            if (pipes != null && pipes.Count > 0)
            {
                resultsFromCivil.Pipes.AddRange(pipes);
                civilStyles.AddRange(pipes);
            }

            // Validamos
            if (pressureNetworks != null && pressureNetworks.Count > 0)
            {
                resultsFromCivil.PressureNetworks.AddRange(pressureNetworks);
                civilStyles.AddRange(pressureNetworks);
            }

            // Validamos si no se encontró ningún elemento
            if (civilStyles.Count == 0)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.CivilStyles,
                    Message = "Selected check was executed, but no Civil 3D styled objects were detected."
                });
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.CivilStyles), civilStyles }
            });
        }

        public static void ProcessCivilObjects(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Transaction tr,
            Database db,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResultsCivil resultsFromCivil,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.CivilObjects))
            {
                return;
            }

            // Analizamos
            List<AssemblyResult> assemblies = AnalyzeAssemblies(tr, db, fileName);
            List<SubassemblyResult> subassemblies = AnalyzeSubassemblies(tr, db, fileName);
            List<SurfaceResult> surfaces = AnalyzeSurfaces(tr, db, fileName);
            List<SiteResult> sites = AnalyzeSites(tr, db, fileName);
            List<BodyResult> bodies = AnalyzeBodies(tr, db, fileName);
            List<StructureResult> structures = AnalyzeStructures(tr, db, fileName);

            // Lista unificada
            List<object> civilObjects = new List<object>();

            // Validamos
            if (assemblies != null && assemblies.Count > 0)
            {
                resultsFromCivil.Assemblies.AddRange(assemblies);
                civilObjects.AddRange(assemblies);
            }

            // Validamos
            if (subassemblies != null && subassemblies.Count > 0)
            {
                resultsFromCivil.Subassemblies.AddRange(subassemblies);
                civilObjects.AddRange(subassemblies);
            }

            // Validamos
            if (surfaces != null && surfaces.Count > 0)
            {
                resultsFromCivil.Surfaces.AddRange(surfaces);
                civilObjects.AddRange(surfaces);
            }

            // Validamos
            if (sites != null && sites.Count > 0)
            {
                resultsFromCivil.Sites.AddRange(sites);
                civilObjects.AddRange(sites);
            }

            // Validamos
            if (bodies != null && bodies.Count > 0)
            {
                resultsFromCivil.Bodies.AddRange(bodies);
                civilObjects.AddRange(bodies);
            }

            // Validamos
            if (structures != null && structures.Count > 0)
            {
                resultsFromCivil.Structures.AddRange(structures);
                civilObjects.AddRange(structures);
            }

            // Validamos si no se encontró ningún elemento
            if (civilObjects.Count == 0)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.CivilObjects,
                    Message = "Selected check was executed, but no Civil 3D objects were detected."
                });
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.CivilObjects), civilObjects }
            });
        }
    }
}
