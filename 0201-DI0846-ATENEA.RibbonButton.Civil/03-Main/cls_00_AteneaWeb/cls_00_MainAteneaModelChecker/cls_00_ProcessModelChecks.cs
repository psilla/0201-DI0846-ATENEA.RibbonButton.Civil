using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.Main;
using static TYPSA.SharedLib.Civil.Main.cls_00_GetDataCivilModelChecker;
using static TYPSA.SharedLib.Autocad.Main.cls_00_GetDataCadModelChecker;
using static TYPSA.SharedLib.Autocad.Main.cls_00_ProcessCommonModelChecks;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_ProcessModelChecks
    {
        public static void ProcessModelChecks(
            List<string> selectedOptions,
            ModelCheckerKeys keys,
            Transaction tr,
            Database db,
            BlockTable bt,
            string file,
            string fileName,
            List<WarningCheckLogResult> warningChecksLog,
            ModelCheckerResults resultsfromcad,
            ModelCheckerResultsCivil resultsFromCivil,
            List<Dictionary<string, object>> extractedData,
            bool isSpanish
        )
        {
            // -----------------------------
            // Procesar checks comunes
            // -----------------------------

            ProcessCommonCadChecks(
                selectedOptions, keys, tr, db, bt, fileName, warningChecksLog, resultsfromcad, extractedData,
                isSpanish, AteneaModelCheckerDefaults.ExpectedPaperTextFont, applyCleanCheckName: true
            );

            // -----------------------------
            // Procesar Property Sets
            // -----------------------------

            ProcessPropertySetsCount(
                selectedOptions, keys, tr, db, fileName, warningChecksLog, resultsFromCivil, extractedData
            );

            // -----------------------------
            // Procesar Coordinate System
            // -----------------------------

            ProcessCoordinateSystem(
                selectedOptions, keys, fileName, warningChecksLog, resultsFromCivil, extractedData
            );

            // -----------------------------
            // Procesar File Size
            // -----------------------------

            ProcessFileSize(
                selectedOptions, keys, file, fileName, warningChecksLog, resultsfromcad, extractedData
            );

            // -----------------------------
            // Procesar Blocks In Use
            // -----------------------------

            ProcessBlocksInUse(
                selectedOptions, keys, tr, db, fileName, warningChecksLog, resultsfromcad, extractedData
            );

            // -----------------------------
            // Procesar Entity Types
            // -----------------------------

            ProcessEntityTypesCount(
                selectedOptions, keys, tr, db, bt, fileName, warningChecksLog, resultsfromcad, extractedData
            );

            // -----------------------------
            // Procesar Purgeable Styles
            // -----------------------------

            ProcessPurgeableStyles(
                selectedOptions, keys, tr, db, fileName, warningChecksLog, resultsFromCivil, extractedData
            );

            // -----------------------------
            // Procesar Block Refs Layouts
            // -----------------------------

            ProcessBlockRefsInLayoutsCount(
                selectedOptions, keys, tr, db, bt, fileName, warningChecksLog, resultsfromcad, extractedData
            );

            // -----------------------------
            // Procesar Civil Styles
            // -----------------------------

            ProcessCivilStyles(
                selectedOptions, keys, tr, db, fileName, warningChecksLog, resultsFromCivil, extractedData
            );

            // -----------------------------
            // Procesar Civil Objects
            // -----------------------------

            ProcessCivilObjects(
                selectedOptions, keys, tr, db, fileName, warningChecksLog, resultsFromCivil, extractedData
            );
        }




    }
}