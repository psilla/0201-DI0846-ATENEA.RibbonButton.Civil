using System.Collections.Generic;
using System.Linq;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.Main;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_GetExportDataModelChecker
    {
        public static Dictionary<string, object> GetExportData(
            ModelCheckerKeys keys,
            List<string> selectedOptions,
            ModelCheckerResults resultsfromcad,
            ModelCheckerResultsCivil resultsFromCivil
        )
        {
            Dictionary<string, object> exportData = new Dictionary<string, object>();

            // -----------------------------
            // Project Units
            // -----------------------------

            if (selectedOptions.Contains(keys.ProjectUnits) && resultsfromcad.ProjectUnits.Any())
            {
                exportData.Add(keys.ProjectUnits, resultsfromcad.ProjectUnits);
            }

            // -----------------------------
            // Version
            // -----------------------------

            if (selectedOptions.Contains(keys.Version) && resultsfromcad.Version.Any())
            {
                exportData.Add(keys.Version, resultsfromcad.Version);
            }

            // -----------------------------
            // Coord system
            // -----------------------------

            if (selectedOptions.Contains(keys.CoordSystem) && resultsFromCivil.CoordSystem.Any())
            {
                exportData.Add(keys.CoordSystem, resultsFromCivil.CoordSystem);
            }

            // -----------------------------
            // Xrefs
            // -----------------------------

            if (selectedOptions.Contains(keys.Xrefs) && resultsfromcad.Xrefs.Any())
            {
                exportData.Add(keys.Xrefs, resultsfromcad.Xrefs);
            }

            // -----------------------------
            // Layer Zero
            // -----------------------------

            if (selectedOptions.Contains(keys.LayerZero) && resultsfromcad.LayerZero.Any())
            {
                exportData.Add(keys.LayerZero, resultsfromcad.LayerZero);
            }

            // -----------------------------
            // Layers In Use
            // -----------------------------

            if (selectedOptions.Contains(keys.LayersInUse) && resultsfromcad.LayersInUse.Any())
            {
                exportData.Add(keys.LayersInUse, resultsfromcad.LayersInUse);
            }

            // -----------------------------
            // Paper Text Font
            // -----------------------------

            if (selectedOptions.Contains(keys.PaperTextFont) && resultsfromcad.PaperTextFont.Any())
            {
                exportData.Add(keys.PaperTextFont, resultsfromcad.PaperTextFont);
            }

            // -----------------------------
            // File Size
            // -----------------------------

            if (selectedOptions.Contains(keys.FileSize) && resultsfromcad.FileSize.Any())
            {
                exportData.Add(keys.FileSize, resultsfromcad.FileSize);
            }

            // -----------------------------
            // Blocks in Use
            // -----------------------------

            if (selectedOptions.Contains(keys.BlocksInUse) && resultsfromcad.BlocksInUse.Any())
            {
                exportData.Add(keys.BlocksInUse, resultsfromcad.BlocksInUse);
            }

            //// -----------------------------
            //// Entity Types
            //// -----------------------------

            //if (selectedOptions.Contains(keys.EntityTypes) && resultsfromcad.EntityTypes.Any())
            //{
            //    exportData.Add(keys.EntityTypes, resultsfromcad.EntityTypes);
            //}

            // -----------------------------
            // Entity Types
            // -----------------------------

            if (selectedOptions.Contains(keys.EntityTypesCount) && resultsfromcad.EntityTypesCount.Any())
            {
                exportData.Add(keys.EntityTypesCount, resultsfromcad.EntityTypesCount);
            }

            // -----------------------------
            // Purgeable Styles
            // -----------------------------

            if (selectedOptions.Contains(keys.PurgeableStyles) && resultsFromCivil.PurgeableStyles.Any())
            {
                exportData.Add(keys.PurgeableStyles, resultsFromCivil.PurgeableStyles);
            }

            //// -----------------------------
            //// Block References in Layout
            //// -----------------------------

            //if (selectedOptions.Contains(keys.BlockRefsInLayouts) && resultsfromcad.BlockRefsInLayouts.Any())
            //{
            //    exportData.Add(keys.BlockRefsInLayouts, resultsfromcad.BlockRefsInLayouts);
            //}

            // -----------------------------
            // Block References in Layout
            // -----------------------------

            if (selectedOptions.Contains(keys.BlockRefsInLayoutsCount) && resultsfromcad.BlockRefsInLayoutsCount.Any())
            {
                exportData.Add(keys.BlockRefsInLayoutsCount, resultsfromcad.BlockRefsInLayoutsCount);
            }

            // -----------------------------
            // Civil Object Styles
            // -----------------------------

            if (selectedOptions.Contains(keys.CivilStyles))
            {
                List<object> allStyles = new List<object>();

                allStyles.AddRange(resultsFromCivil.Alignments);
                allStyles.AddRange(resultsFromCivil.Corridors);
                allStyles.AddRange(resultsFromCivil.FeatureLines);
                allStyles.AddRange(resultsFromCivil.Pipes);
                allStyles.AddRange(resultsFromCivil.PressureNetworks);
                // Validamos
                if (allStyles.Any())
                {
                    exportData.Add(keys.CivilStyles, allStyles);
                }
            }

            // -----------------------------
            // Civil Objects
            // -----------------------------

            if (selectedOptions.Contains(keys.CivilObjects))
            {
                List<object> allObjects = new List<object>();

                allObjects.AddRange(resultsFromCivil.Assemblies);
                allObjects.AddRange(resultsFromCivil.Subassemblies);
                allObjects.AddRange(resultsFromCivil.Surfaces);
                allObjects.AddRange(resultsFromCivil.Sites);
                allObjects.AddRange(resultsFromCivil.Bodies);
                allObjects.AddRange(resultsFromCivil.Structures);
                // Validamos
                if (allObjects.Any())
                {
                    exportData.Add(keys.CivilObjects, allObjects);
                }
            }

            // return
            return exportData;
        }
    }
}
