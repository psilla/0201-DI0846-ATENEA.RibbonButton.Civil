using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.Main;
using static TYPSA.PS.RibbonButton.Civil.cls_00_MainAteneaModelChecker;
using static TYPSA.SharedLib.Autocad.Main.cls_00_MainCheckAcadVersion;
using static TYPSA.SharedLib.Autocad.Main.cls_00_MainCheckBlockRefsInLayouts;
using static TYPSA.SharedLib.Autocad.Main.cls_00_MainCheckBlocksInUse;
using static TYPSA.SharedLib.Autocad.Main.cls_00_MainCheckEntInLayerZero;
using static TYPSA.SharedLib.Autocad.Main.cls_00_MainCheckEntityTypes;
using static TYPSA.SharedLib.Autocad.Main.cls_00_MainCheckFileSize;
using static TYPSA.SharedLib.Autocad.Main.cls_00_MainCheckLayersInUse;
using static TYPSA.SharedLib.Autocad.Main.cls_00_MainCheckPaperTextFont;
using static TYPSA.SharedLib.Autocad.Main.cls_00_MainCheckProjUnits;
using static TYPSA.SharedLib.Autocad.Main.cls_00_MainCheckXrefs;
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

        public static void ProcessProjectUnits(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Database db,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResults resultsfromcad,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.ProjectUnits))
            {
                return;
            }

            // Analizamos
            ProjectUnitsResult units = AnalyzeUnits(db, fileName);

            // Validamos
            if (units == null)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.ProjectUnits,
                    Message = "Selected check was executed, but no project units information was detected."
                });
            }
            else
            {
                // Validamos que sean metros
                if (!units.IsMeters)
                {
                    warningChecksLog.Add(new WarningCheckLogResult
                    {
                        FileName = fileName,
                        CheckName = keys.ProjectUnits,
                        Message = $"Project units are set to '{units.Units}' instead of meters."
                    });
                }

                // Almacenamos
                resultsfromcad.ProjectUnits.Add(units);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.ProjectUnits), units }
            });
        }

        public static void ProcessLayersInUse(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Transaction tr,
            Database db,
            BlockTable bt,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResults resultsfromcad,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.LayersInUse))
            {
                return;
            }

            // Analizamos
            List<LayerUsageResult> layersInUse = AnalyzeLayers(
                tr, db, bt, fileName
            );

            // Validamos
            if (layersInUse == null || layersInUse.Count == 0)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.LayersInUse,
                    Message = "Selected check was executed, but no layers in use were detected."
                });
            }
            else
            {
                // Buscar capas sin uso
                bool hasUnusedLayers = layersInUse.Any(
                    x => x != null && !x.IsUsed
                );

                // Validamos
                if (hasUnusedLayers)
                {
                    warningChecksLog.Add(new WarningCheckLogResult
                    {
                        FileName = fileName,
                        CheckName = keys.LayersInUse,
                        Message = "One or more layers exist in the drawing but are not in use."
                    });
                }

                // Almacenamos
                resultsfromcad.LayersInUse.AddRange(layersInUse);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.LayersInUse), layersInUse }
            });
        }

        public static void ProcessLayerZero(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Transaction tr,
            Database db,
            BlockTable bt,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResults resultsfromcad,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.LayerZero))
            {
                return;
            }

            // Analizamos
            LayerZeroUsageResult entLayerZero = AnalyzeLayerZero(
                tr, db, bt, fileName
            );

            // Validamos
            if (entLayerZero == null)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.LayerZero,
                    Message = "Selected check was executed, but no layer zero information was detected."
                });
            }
            else
            {
                // Validamos si hay entidades en capa 0
                if (entLayerZero.IsUsed)
                {
                    warningChecksLog.Add(new WarningCheckLogResult
                    {
                        FileName = fileName,
                        CheckName = keys.LayerZero,
                        Message = "One or more entities exist in layer 0."
                    });
                }

                // Almacenamos
                resultsfromcad.LayerZero.Add(entLayerZero);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.LayerZero), entLayerZero }
            });
        }

        public static void ProcessVersion(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Database db,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResults resultsfromcad,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.Version))
            {
                return;
            }

            // Analizamos
            AcadVersionResult version = AnalyzeVersionMapped(
                db, fileName
            );

            // Validamos
            if (version == null)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.Version,
                    Message = "Selected check was executed, but no AutoCAD version information was detected."
                });
            }
            else
            {
                // Almacenamos
                resultsfromcad.Version.Add(version);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.Version), version }
            });
        }

        public static void ProcessXrefs(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Transaction tr,
            Database db,
            BlockTable bt,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResults resultsfromcad,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.Xrefs))
            {
                return;
            }

            // Analizamos
            List<XrefStatusResult> xrefs = AnalyzeXrefs(
                db, bt, tr, fileName
            );

            // Validamos que existan Xrefs
            if (xrefs != null && xrefs.Count > 0)
            {
                // Verificar si alguna Xref está descargada
                bool hasUnloadedXrefs = xrefs.Any(
                    x => x != null && !x.IsLoaded
                );

                // Añadimos un único warning
                if (hasUnloadedXrefs)
                {
                    warningChecksLog.Add(new WarningCheckLogResult
                    {
                        FileName = fileName,
                        CheckName = keys.Xrefs,
                        Message = "One or more external references are unloaded in this file."
                    });
                }

                // Almacenamos
                resultsfromcad.Xrefs.AddRange(xrefs);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.Xrefs), xrefs }
            });
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

        public static void ProcessPaperTextFont(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Transaction tr,
            Database db,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResults resultsfromcad,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.PaperTextFont))
            {
                return;
            }

            // Analizamos
            List<PaperTextFontResult> paperFonts = AnalyzeTextFont(
                tr, db, fileName, AteneaModelCheckerDefaults.ExpectedPaperTextFont
            );

            // Validamos
            if (paperFonts == null || paperFonts.Count == 0)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.PaperTextFont,
                    Message = "Selected check was executed, but no paper space text entities were detected."
                });
            }
            else
            {
                // Buscar fuentes distintas a la requerida
                bool hasUnexpectedFonts = paperFonts.Any(
                    x => x != null && !x.IsExpected
                );

                // Validamos
                if (hasUnexpectedFonts)
                {
                    warningChecksLog.Add(new WarningCheckLogResult
                    {
                        FileName = fileName,
                        CheckName = keys.PaperTextFont,
                        Message = "One or more paper space text entities use a font different from the required font."
                    });
                }

                // Almacenamos
                resultsfromcad.PaperTextFont.AddRange(paperFonts);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.PaperTextFont), paperFonts }
            });
        }

        public static void ProcessFileSize(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            string file,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResults resultsfromcad,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.FileSize))
            {
                return;
            }

            // Analizamos
            List<FileSizeResult> fileSize = AnalyzeFileSize(
                file, fileName
            );

            // Validamos
            if (fileSize != null && fileSize.Any(x => x != null && x.ExceedsLimit))
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.FileSize,
                    Message = "The file size exceeds the 200 MB limit."
                });
            }

            // Almacenamos
            if (fileSize != null && fileSize.Count > 0)
            {
                resultsfromcad.FileSize.AddRange(fileSize);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.FileSize), fileSize }
            });
        }

        public static void ProcessBlocksInUse(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Transaction tr,
            Database db,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResults resultsfromcad,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.BlocksInUse))
            {
                return;
            }

            // Analizamos
            List<BlockUsageResult> blocksInUse = AnalyzeBlocks(
                tr, db, fileName
            );

            // Validamos
            if (blocksInUse == null || blocksInUse.Count == 0)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.BlocksInUse,
                    Message = "Selected check was executed, but no blocks were detected."
                });
            }
            else
            {
                // Buscar bloques sin uso
                bool hasUnusedBlocks = blocksInUse.Any(
                    x => x != null && !x.IsUsed
                );

                // Validamos
                if (hasUnusedBlocks)
                {
                    warningChecksLog.Add(new WarningCheckLogResult
                    {
                        FileName = fileName,
                        CheckName = keys.BlocksInUse,
                        Message = "One or more block definitions exist in the drawing but are not in use."
                    });
                }

                // Almacenamos
                resultsfromcad.BlocksInUse.AddRange(blocksInUse);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.BlocksInUse), blocksInUse }
            });
        }

        public static void ProcessEntityTypes(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Transaction tr,
            Database db,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResults resultsfromcad,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.EntityTypes))
            {
                return;
            }

            // Analizamos
            List<EntityTypeResult> entityTypes = AnalyzeEntityTypes(
                tr, db, fileName
            );

            // Validamos
            if (entityTypes == null || entityTypes.Count == 0)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.EntityTypes,
                    Message = "Selected check was executed, but no entity types were detected."
                });
            }
            else
            {
                // Almacenamos
                resultsfromcad.EntityTypes.AddRange(entityTypes);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.EntityTypes), entityTypes }
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

        public static void ProcessBlockRefsInLayouts(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Transaction tr,
            Database db,
            BlockTable bt,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResults resultsfromcad,
            List<Dictionary<string, object>> extractedData
        )
        {
            // Validamos selección
            if (!selectedOptions.Contains(keys.BlockRefsInLayouts))
            {
                return;
            }

            // Analizamos
            List<BlockRefLayoutResult> blockRefsInLayouts = AnalyzeBlockRefsInLayouts(
                tr, db, bt, fileName
            );

            // Validamos
            if (blockRefsInLayouts == null || blockRefsInLayouts.Count == 0)
            {
                warningChecksLog.Add(new WarningCheckLogResult
                {
                    FileName = fileName,
                    CheckName = keys.BlockRefsInLayouts,
                    Message = "Selected check was executed, but no block references were detected in layouts."
                });
            }
            else
            {
                // Almacenamos
                resultsfromcad.BlockRefsInLayouts.AddRange(blockRefsInLayouts);
            }

            // Almacenamos
            extractedData.Add(new Dictionary<string, object>
            {
                { GetCleanCheckName(keys.BlockRefsInLayouts), blockRefsInLayouts }
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
