using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using TYPSA.SharedLib.Autocad;
using TYPSA.SharedLib.Civil;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.Excel;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Civil.cls_00_UiTexts;
using static TYPSA.SharedLib.Autocad.cls_00_CadInfoHelper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;

namespace TYPSA.ATENEA.RibbonButton.Civil
{
    internal class cls_00_MainAteneaParamCheckExp
    {
        private static void ExportCategoriesToJson(StringCollection categories)
        {
            // Convertimos a lista
            List<string> catToExport = categories.Cast<string>()
                .OrderBy(x => x).ToList();

            // Serializamos
            string json = JsonConvert.SerializeObject(catToExport, Formatting.Indented);

            // Guardamos en un archivo temporal
            string jsonPath = Path.Combine(
                Path.GetTempPath(), $"Civil3D_Categories_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            );

            File.WriteAllText(jsonPath, json);

            // Abrimos el JSON
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = jsonPath,
                    UseShellExecute = true
                });
            }
            catch
            {
                ShowStringBuilder.ShowInfo(
                    "JSON exportado", $"Archivo generado correctamente:\n{jsonPath}"
                );
            }
        }

        public static async Task MainAteneaParamCheckExp(
            AteneaSessionInfo sessionInfo,
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
            // Normalizar idioma
            // -------------------------------

            string softwareLanguage = isSpanish ? "Spanish" : "English";

            // -------------------------------
            // Reiniciamos cronometro global
            // -------------------------------

            sessionInfo.StartTime = DateTime.Now;

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026 || CIVIL2027

            // -------------------------------
            // Preparar informacion Web
            // -------------------------------

            ParamExpPreparedDataCivil preparedDataExp = await cls_00_PrepareParamWebDataAsyncCivil.ParamExpMainWebAsync(
                sessionInfo, ateneaEndpoints, ateneaEndpoints.EndpointGetSetUrl,
                isSpanish, validateExistingSet: false
            );
            // Validamos
            if (preparedDataExp == null) return;

#endif

            // -------------------------------
            // Obtener informacion preparada
            // -------------------------------

            int currentSetStatus = preparedDataExp.CurrentSetStatus;
            List<Dictionary<string, object>> civilParamCheckFromSet = preparedDataExp.CivilParamCheckFromSet;

            // -----------------------------
            // Ordenar listado de categorias 
            // -----------------------------

            StringCollection allCategories = cls_00_GetAllObjectCat.GetAllObjectCatForPsetsByVersion();

            bool exportToJson = false;
            if (exportToJson)
                // Exportamos a JSON 
                ExportCategoriesToJson(allCategories);

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

                cls_00_ProcessFilesParamCheckExp.ProcessSelectedModels(
                    selectedFiles.ToArray(), allCategories, dataJsonByModel, processDurations, 
                    processDurationsByModel, progressBarForm, isSpanish, ref processedFiles
                );

                // -----------------------------
                // Cerrar progreso
                // -----------------------------

                progressBarForm.Close();

                // -----------------------------
                // Preparar informacion 
                // -----------------------------

                PreparedParamCheckDataResult preparedData = cls_00_PrepareParamCheckData.PrepareParamCheckData(
                    sessionInfo, cls_00_AteneaSession.Email, selectedFolderPath, infoCad.RootFolderName, 
                    infoCad.JsonFileNameParamCheckExp, infoCad.JsonFileNameParamCheckSet, cls_00_AteneaJson.DataByFileName,
                    dataJsonByModel, civilParamCheckFromSet, currentSetStatus, processDurations, isSpanish
                );
                // Validamos
                if (!preparedData.Success) return;

                // -----------------------------
                // Obtener informacion preparada
                // -----------------------------

                Dictionary<string, object> dictDataByFileToJson = preparedData.DataByFileToJson;
                List<Dictionary<string, object>> dictDataByFileToJsonToList = preparedData.DataByFileToJsonToList;
                bool addPsetsToSet = preparedData.AddPsetsToSet;
                int updatedSetStatus = preparedData.UpdatedSetStatus;
                Dictionary<string, object> dictDataSetToJson = preparedData.DataSetToJson;

                // -----------------------------
                // Exportar Excel report
                // -----------------------------

                bool exportToExcel = false;
                // Exportamos
                if (exportToExcel)
                {
                    msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.GenerateExcel);

                    Stopwatch processStopwatch = Stopwatch.StartNew();

                    List<string> headers = new List<string>
                    {
                        cls_00_AteneaJson.FileName, cls_00_AteneaJson.PsetName, cls_00_AteneaJson.PropName, cls_00_AteneaJson.DataType, cls_00_AteneaJson.PropDefaultValue,
                        cls_00_AteneaJson.PropDescription, cls_00_AteneaJson.PropIsAutomatic, cls_00_AteneaJson.PropIsVisible, cls_00_AteneaJson.PropIsReadOnly
                    };
                    // Exportamos
                    cls_00_ExportFilteredParamCheckToExcel.ExportFilteredParamCheckToExcel(dataJsonByModel, headers);

                    // Añadimos
                    cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);
                }

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026 || CIVIL2027

                // -----------------------------
                // Gestionar datos existentes y envío
                // -----------------------------

                bool uploaded = await cls_00_HandleData.HandleData(
                    sessionInfo, cls_00_AteneaSession.AccessToken, isSpanish, dictDataByFileToJson, dictDataByFileToJsonToList, 
                    ateneaEndpoints.EndpointDataByFileUrl, cls_00_AteneaJson.CivilParamCheck, addPsetsToSet, updatedSetStatus, 
                    dictDataSetToJson, currentSetStatus, ateneaEndpoints.EndpointSetUrl, processDurations
                );
                // Validamos
                if (!uploaded) return;
#endif

                // -------------------------------
                // Tiempo total
                // -------------------------------

                totalStopwatch.Stop();

                processDurations["Total"] = totalStopwatch.Elapsed;

                // -------------------------------
                // Exportar informe de tiempos
                // -------------------------------

                cls_00_ExportProcessTimesToHtml.ExportProcessTimesToHtml(
                    sessionInfo, selectedFolderPath, processDurations, preparedDataExp.UserNameBySso, totalFiles, processedFiles, isSpanish,
                    processDurationsByModel, sendDurationsByModel, includeModelDetails: true
                );

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

            // Por defecto
            return;

        }

    }
}

