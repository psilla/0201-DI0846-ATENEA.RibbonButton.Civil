using System.Diagnostics;
using TYPSA.SharedLib.Civil;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Civil.cls_00_ParamImpMainWeb;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;

namespace TYPSA.ATENEA.RibbonButton.Civil
{
    internal class cls_00_MainAteneaParamDataImp
    {
        public static void MainAteneaParamDataImp(
            AteneaSessionInfo sessionInfo,
            Root dataByModelFromJson,
            ParamImpPreparedDataCivil dataFromAsyn,
            bool isSpanish
        )
        {
            // -------------------------------
            // Definir variables
            // -------------------------------

            List<Dictionary<string, object>> dataJsonByModel = new List<Dictionary<string, object>>();

            Dictionary<string, Autodesk.Aec.PropertyData.DataType> dataTypeDict = cls_00_GetDictDataType.GetDataTypeDictionary();

            List<string> allowedDataTypes = new List<string> { "Text", "Integer", "Real", "List", "TrueFalse" };

            Dictionary<string, Autodesk.Aec.PropertyData.DataType> dataTypeDictFiltered = dataTypeDict.Where(
                x => allowedDataTypes.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value
            );

            string msg = string.Empty;

            // -------------------------------
            // Tiempos de ejecución
            // -------------------------------

            // Global
            Dictionary<string, TimeSpan> processDurations = new Dictionary<string, TimeSpan>();
            // Modelo
            Dictionary<string, Dictionary<string, TimeSpan>> processDurationsByModel = new Dictionary<string, Dictionary<string, TimeSpan>>();
            // Envíos por Modelo
            Dictionary<string, Dictionary<string, TimeSpan>> sendDurationsByModel = new Dictionary<string, Dictionary<string, TimeSpan>>();

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            // -------------------------------
            // Obtener informacion
            // -------------------------------

            string keyPsetName = cls_00_AteneaJson.PsetName;

            // -------------------------------
            // Obtener Opciones importacion
            // -------------------------------

            msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.ConfigureImportOptions);

            Stopwatch processStopwatch = Stopwatch.StartNew();

            // -------------------------------
            // Property Sets
            // -------------------------------

            if (!TryGetFiltPsetsDataFromUserSelection(
                dataFromAsyn.CivilParamCheckFromSet, keyPsetName, dataTypeDictFiltered, isSpanish,
                out Dictionary<string, SelectedPsetData> selectedPsetData
            )) return;

            // Añadimos
            cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

            // -------------------------------
            // Obtener archivos
            // -------------------------------

            bool filesResult = TYPSA.SharedLib.Autocad.cls_00_GetUserData.GetSelectedFiles(
                sessionInfo.ProjectCode, out List<string> selectedFiles, out string selectedFolderPath,
                customPathLabel: "Please, paste the folder containing the DWG files to analyze"
            );

            // -------------------------------
            // Crear progreso
            // -------------------------------

            ProgressBarControl progressBarForm = new ProgressBarControl();
            // Iniciamos variables
            int totalFiles = selectedFiles.ToArray().Length;
            int processedFiles = 0;

            // Mostramos
            progressBarForm.ProgressValue = 10; // Valor inicial 
            progressBarForm.Show();

            // try
            try
            {
                // -----------------------------
                // Procesar modelos seleccionados
                // -----------------------------

                cls_00_ProcessFilesParamDataImp.ProcessSelectedModels(
                    selectedFiles.ToArray(), dataByModelFromJson, selectedPsetData, processDurations, 
                    processDurationsByModel, progressBarForm, isSpanish, ref processedFiles
                );

                // -----------------------------
                // Cerrar progreso
                // -----------------------------

                progressBarForm.Close();

                // -------------------------------
                // Tiempo total
                // -------------------------------

                totalStopwatch.Stop();

                processDurations["Total"] = totalStopwatch.Elapsed;

                // -------------------------------
                // Exportar informe de tiempos
                // -------------------------------

                cls_00_ExportProcessTimesToHtml.ExportProcessTimesToHtml(
                    sessionInfo, selectedFolderPath, processDurations, dataFromAsyn.UserNameBySso, totalFiles, 
                    processedFiles, isSpanish, processDurationsByModel, sendDurationsByModel, includeModelDetails: true
                );
            }
            // catch
            catch (System.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }






    }
}
