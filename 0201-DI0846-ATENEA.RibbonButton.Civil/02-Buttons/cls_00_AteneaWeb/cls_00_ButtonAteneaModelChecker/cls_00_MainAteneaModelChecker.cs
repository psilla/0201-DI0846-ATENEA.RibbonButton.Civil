using System.Diagnostics;
using TYPSA.SharedLib.Autocad;
using TYPSA.SharedLib.Civil;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Autocad.cls_00_CadInfoHelper;
using static TYPSA.SharedLib.Excel.cls_00_ExportModCheckToExcel_OpenXml;
using static TYPSA.SharedLib.Civil.cls_00_UiTexts;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TYPSA.ATENEA.RibbonButton.Civil
{
    public class cls_00_MainAteneaModelChecker
    {
        public static async Task MainAteneaModelChecker(
            AteneaSessionInfo sessionInfo,
            List<string> selectedOptions,
            UiTexts uiTexts,
            bool isSpanish
        )
        {
            // ---------------------------------
            // Obtener informacion sesion
            // ---------------------------------

            CadSessionInfo infoCad = GetCivilSessionInfo(sessionInfo);
            cls_00_AteneaEndPointsCivil ateneaEndpoints = new cls_00_AteneaEndPointsCivil();

            // -------------------------------
            // Definir variables
            // -------------------------------

            List<Dictionary<string, object>> dataJsonByModel = new List<Dictionary<string, object>>();
            List<WarningCheckLogResult> warningChecksLog = new List<WarningCheckLogResult>();
            ModelCheckerResults resultsfromcad = new ModelCheckerResults();
            ModelCheckerResultsCivil resultsFromCivil = new ModelCheckerResultsCivil();
            ModelCheckerKeys keys = new ModelCheckerKeys(isSpanish);
            string msg = string.Empty;

            // -------------------------------
            // Tiempos de ejecución
            // -------------------------------

            // Global
            Dictionary<string, TimeSpan> processDurations = new Dictionary<string, TimeSpan>();
            // Modelo
            Dictionary<string, Dictionary<string, TimeSpan>> processDurationsByModel =
                new Dictionary<string, Dictionary<string, TimeSpan>>();

            Stopwatch totalStopwatch = Stopwatch.StartNew();

            // -------------------------------
            // Normalizar idioma
            // -------------------------------

            string softwareLanguage = isSpanish ? "Spanish" : "English";

            // -------------------------------
            // Reiniciamos cronometro global
            // -------------------------------

            sessionInfo.StartTime = DateTime.Now;

            // -------------------------------
            // Validar Datos Proyecto SSO
            // -------------------------------

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026 || CIVIL2027

            if (!await cls_00_PostProjectInfo.ValidateProjectDataAsyncSSO(
                ateneaEndpoints.EndpointProjectDataUrl, cls_00_AteneaJson.CivilVersion, cls_00_AteneaJson.CivilLanguage,
                sessionInfo, isSpanish, cls_00_AteneaSession.AccessToken
            )) return;

#endif

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

                cls_00_ProcessFilesModelChecker.ProcessSelectedModels(
                    selectedFiles.ToArray(), selectedOptions, keys, isSpanish, warningChecksLog, 
                    resultsfromcad, resultsFromCivil, dataJsonByModel, processDurations,
                    processDurationsByModel, progressBarForm, ref processedFiles
                );

                // -----------------------------
                // Cerrar progreso
                // -----------------------------

                progressBarForm.Close();

                // -----------------------------
                // Preparar informacion modelos
                // -----------------------------

                Dictionary<string, object> dictDataByFileToJson = cls_00_AteneaSessionInfo.GetFinalJsonDictionary(
                    cls_00_AteneaSession.Email, cls_00_AteneaJson.CivilVersion, cls_00_AteneaJson.CivilLanguage,
                    dataJsonByModel, sessionInfo
                );

                PreparedModelDataResult preparedModelData = cls_00_PrepareDataModelCheck.PrepareModelData(
                    sessionInfo, infoCad.RootFolderName, infoCad.JsonFileNameDataExtraction, dictDataByFileToJson,
                    selectedFolderPath, dataJsonByModel, processDurations, isSpanish
                );
                // Validamos
                if (!preparedModelData.Success) return;

                // -----------------------------
                // Obtener informacion preparada
                // -----------------------------

                List<Dictionary<string, object>> dictDataByFileToJsonToList = preparedModelData.DataByFileToJsonToList;

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026 || CIVIL2027

                // -----------------------------
                // Gestionar datos existentes y envío
                // -----------------------------

                bool uploaded = await cls_00_HandleData.HandleData(
                    sessionInfo, cls_00_AteneaSession.AccessToken, isSpanish, dictDataByFileToJson, dictDataByFileToJsonToList,
                    ateneaEndpoints.EndpointElementDataUrl, cls_00_AteneaJson.CivilElementData, processDurations
                );
                // Validamos
                if (!uploaded) return;
#endif

                // -----------------------------
                // Preparar datos para informes
                // -----------------------------

                Dictionary<string, object> exportData = cls_00_GetReportData.GetReportData(
                    keys, selectedOptions, resultsfromcad, resultsFromCivil,
                    warningChecksLog, processDurations, isSpanish
                );
                // Validamos
                if (exportData == null) return;

                // -----------------------------
                // Generar informes finales
                // -----------------------------

                Stopwatch processStopwatch = Stopwatch.StartNew();

                msg = cls_00_ProcessMessages.ShowProcessMessage(
                    isSpanish, cls_00_ProcessMessages.GenerateFinalReports
                );

                // Excel
                ExportDataToExcel(exportData);

                // Html
                cls_00_ExportAteneaCheckToHtml.ExportToHtml(
                    selectedFolderPath, exportData, warningChecksLog, sessionInfo.ProjectCode, totalFiles, processedFiles
                );

                cls_00_ProcessMessages.AddProcessDuration(
                    processDurations, msg, processStopwatch
                );

                // -------------------------------
                // Tiempo total
                // -------------------------------

                totalStopwatch.Stop();

                processDurations["Total"] = totalStopwatch.Elapsed;

                // -------------------------------
                // Summary
                // -------------------------------

                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - sessionInfo.StartTime;

                // Mensaje
                MessageBox.Show(
                    uiTexts.MsgCompleted +
                    "\nDuration: " + duration.ToString(@"hh\:mm\:ss") +
                    "\nStarted at: " + sessionInfo.StartTime.ToString("HH:mm:ss") +
                    "\nEnded at: " + endTime.ToString("HH:mm:ss"),
                    uiTexts.Title, MessageBoxButtons.OK, MessageBoxIcon.Information
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
