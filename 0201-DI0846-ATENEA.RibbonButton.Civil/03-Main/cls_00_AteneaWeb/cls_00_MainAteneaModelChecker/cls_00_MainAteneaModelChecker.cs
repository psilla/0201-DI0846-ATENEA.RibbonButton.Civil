using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using TYPSA.SharedLib.Autocad.GetDocument;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.Main;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.Json;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using static TYPSA.SharedLib.Excel.cls_00_ExportModCheckToExcel_OpenXml;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TYPSA.PS.RibbonButton.Civil
{
    public class cls_00_MainAteneaModelChecker
    {
        private static bool ValidateRetrievedModelData(
            List<Dictionary<string, object>> dataJsonByModel,
            bool isSpanish
        )
        {
            // Validamos
            if (dataJsonByModel != null && dataJsonByModel.Any())
            {
                return true;
            }

            MessageBox.Show(
                isSpanish
                    ? "No se recuperó información de ninguno de los modelos seleccionados.\n" +
                      "No se generará ningún archivo JSON."
                    : "No data was retrieved from any of the selected models.\n" +
                      "No JSON file will be generated.",
                isSpanish ? "Advertencia" : "Warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return false;
        }

        private static void SkipNullFile(
            bool isSpanish,
            string fileName,
            ref int processedFiles,
            int totalFiles,
            int percentage,
            ProgressBarControl progressBarForm,
            int durationMs = 1000
        )
        {
            // Mostramos
            new AutoCloseMessageForm(
                isSpanish
                    ? $"El documento '{fileName}' no se pudo abrir."
                    : $"The document '{fileName}' could not be opened.",
                durationMs
            ).ShowDialog();
            // Actualizar progreso
            processedFiles++;
            percentage = (int)((double)processedFiles / totalFiles * 100);
            progressBarForm.ProgressValue = percentage;
        }

        public static async Task MainAteneaModelChecker(
            string[] selectedFiles,
            string selectedFolderPath,
            string projectCode,
            List<string> selectedOptions,
            DateTime startTime,
            CadSessionInfo infoCad,
            UiTexts uiTexts,
            bool isSpanish
        )
        {
            // -------------------------------
            // Definir variables
            // -------------------------------

            List<Dictionary<string, object>> dataJsonByModel = new List<Dictionary<string, object>>();
            List<WarningCheckLogResult> warningChecksLog = new List<WarningCheckLogResult>();
            List<List<string>> dataToExcelGlobal = new List<List<string>>();
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
            // Obtener informacion
            // -------------------------------

            string keyDataByFileName = cls_00_AteneaJson.DataByFileName;
            string keyElementData = cls_00_AteneaJson.CivilElementData;
            string keyFileName = cls_00_AteneaJson.FileName;

            cls_00_AteneaEndPointsCivil ateneaEndpoints = new cls_00_AteneaEndPointsCivil();
            string strEndpointProjectDataUrl = ateneaEndpoints.EndpointProjectDataUrl;
            string strEndpointElementDataUrl = ateneaEndpoints.EndpointElementDataUrl;

            string strAccessToken = cls_00_AteneaSession.AccessToken;
            string strEmail = cls_00_AteneaSession.Email;

            // -------------------------------
            // Reiniciamos cronometro global
            // -------------------------------

            startTime = DateTime.Now;

            // -------------------------------
            // Validar Datos Proyecto SSO
            // -------------------------------

            Dictionary<string, object> dictProjectDataToVal = GetProjectDataDictionarySSO(
                projectCode, softwareLanguage, infoCad
            );
#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026
            // Validamos Datos de Proyecto
            if (!await cls_00_PostProjectInfo.ValidateProjectDataAsyncSSO(
                strEndpointProjectDataUrl, dictProjectDataToVal, isSpanish, strAccessToken
            )) return;
#endif

            // -------------------------------
            // Crear progreso
            // -------------------------------

            ProgressBarControl progressBarForm = new ProgressBarControl();
            // Iniciamos variables
            int totalFiles = selectedFiles.Length;
            int processedFiles = 0;
            int percentage = 0;

            // Mostramos
            progressBarForm.ProgressValue = 10; // Valor inicial 
            progressBarForm.Show();

            // try
            try
            {
                // -----------------------------
                // Iniciar cronometro modelos
                // -----------------------------

                string msgIter = cls_00_ProcessMessages.ShowProcessMessage(
                    isSpanish, cls_00_ProcessMessages.StartDocumentProcessing
                );

                Stopwatch filesProcessingStopwatch = Stopwatch.StartNew();

                // -----------------------------
                // Iterar archivos
                // -----------------------------

                foreach (string file in selectedFiles)
                {
                    // -------------------------------
                    // Definir variables
                    // -------------------------------

                    List<Dictionary<string, object>> extractedData = new List<Dictionary<string, object>>();

                    string fileName = System.IO.Path.GetFileNameWithoutExtension(file);

                    Dictionary<string, TimeSpan> modelDurations = new Dictionary<string, TimeSpan>();

                    // try
                    try
                    {
                        // -------------------------------
                        // Abrir documento
                        // -------------------------------

                        msg = cls_00_ProcessMessages.ShowProcessMessage(
                            isSpanish, cls_00_ProcessMessages.OpenDocument, processedFiles + 1, totalFiles, fileName
                        );

                        Stopwatch modelProcessStopwatch = Stopwatch.StartNew();

                        using (Document openedDoc = Application.DocumentManager.Open(file, false))
                        {
                            cls_00_ProcessMessages.AddProcessDuration(
                                modelDurations, msg, modelProcessStopwatch
                            );

                            // -------------------------------
                            // Validar documento
                            // -------------------------------

                            if (openedDoc == null)
                            {
                                // Obviamos
                                SkipNullFile(
                                    isSpanish, fileName, ref processedFiles, totalFiles, percentage, progressBarForm
                                );
                                continue;
                            }

                            // -------------------------------
                            // Procesar documento
                            // -------------------------------

                            msg = cls_00_ProcessMessages.ShowProcessMessage(
                                isSpanish, cls_00_ProcessMessages.AnalyzeDocument, processedFiles + 1, totalFiles, fileName
                            );

                            // -------------------------------
                            // Obtener informacion
                            // -------------------------------

                            Database db = openedDoc.Database;
                            Editor ed = cls_00_DocumentInfo.GetEditor(openedDoc);

                            // -------------------------------
                            // Bloquear el documento
                            // -------------------------------

                            using (openedDoc.LockDocument())
                            using (Transaction tr = openedDoc.TransactionManager.StartTransaction())
                            {
                                // try
                                try
                                {
                                    // Obtener BlockTable
                                    BlockTable bt = cls_00_DocumentInfo.GetBlockTableForRead(tr, db);

                                    // -------------------------------
                                    // Procesar informacion
                                    // -------------------------------

                                    msg = cls_00_ProcessMessages.ShowProcessMessage(
                                        isSpanish, cls_00_ProcessMessages.CollectModelData, fileName
                                    );

                                    modelProcessStopwatch = Stopwatch.StartNew();

                                    cls_00_ProcessModelChecks.ProcessModelChecks(
                                        selectedOptions, keys, tr, db, bt, file, fileName, warningChecksLog,
                                        resultsfromcad, resultsFromCivil, extractedData, isSpanish
                                    );

                                    cls_00_ProcessMessages.AddProcessDuration(modelDurations, msg, modelProcessStopwatch);

                                    // -------------------------------
                                    // Construir y almacenar informacion
                                    // -------------------------------

                                    msg = cls_00_ProcessMessages.ShowProcessMessage(
                                        isSpanish, cls_00_ProcessMessages.TransformModelData, fileName
                                    );

                                    modelProcessStopwatch = Stopwatch.StartNew();

                                    Dictionary<string, object> fileDataByDoc = new Dictionary<string, object>
                                    {
                                        { keyFileName, fileName },
                                        { keyElementData, extractedData }
                                    };

                                    // Añadimos
                                    dataJsonByModel.Add(fileDataByDoc);

                                    cls_00_ProcessMessages.AddProcessDuration(modelDurations, msg, modelProcessStopwatch);

                                    // -----------------------------
                                    // Cerrar transaccion
                                    // -----------------------------

                                    tr.Commit();
                                }
                                // catch
                                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                                {
                                    warningChecksLog.Add(new WarningCheckLogResult
                                    {
                                        FileName = fileName,
                                        CheckName = "Processing Error",
                                        Message = ex.Message
                                    });

                                    // Mensaje
                                    new AutoCloseMessageForm(
                                        $"Error while processing '{file}':\n{ex.Message}"
                                    ).ShowDialog();
                                }
                            }

                            // -----------------------------
                            // Cerrar documento
                            // -----------------------------

                            msg = cls_00_ProcessMessages.ShowProcessMessage(
                                isSpanish, cls_00_ProcessMessages.CloseDocument, processedFiles + 1, totalFiles, fileName
                            );

                            Stopwatch closeDocumentStopwatch = Stopwatch.StartNew();

                            openedDoc.CloseAndDiscard();

                            cls_00_ProcessMessages.AddProcessDuration(modelDurations, msg, closeDocumentStopwatch);

                            // -------------------------------
                            // Tiempo total de procesos registrados
                            // -------------------------------

                            if (modelDurations.Any())
                            {
                                TimeSpan modelTotalDuration = TimeSpan.FromTicks(
                                    modelDurations.Values.Sum(x => x.Ticks)
                                );

                                modelDurations["Total"] = modelTotalDuration;

                                // Guardamos los tiempos del modelo
                                processDurationsByModel[fileName] = modelDurations;
                            }

                            // -----------------------------
                            // Actualizar progreso
                            // -----------------------------

                            processedFiles++;
                            percentage = (int)((double)processedFiles / totalFiles * 100);
                            progressBarForm.ProgressValue = percentage;
                        }
                    }
                    // catch
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        warningChecksLog.Add(new WarningCheckLogResult
                        {
                            FileName = fileName,
                            CheckName = "Fatal File Error",
                            Message = ex.Message
                        });

                        // Mensaje
                        MessageBox.Show(
                            $"EXCEPTION:\n{ex.Message}\n{ex.StackTrace}"
                        );
                    }
                }

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msgIter, filesProcessingStopwatch);

                // -----------------------------
                // Cerrar progreso
                // -----------------------------

                progressBarForm.Close();

                // -----------------------------
                // Validar informacion Global
                // -----------------------------

                Stopwatch processStopwatch = Stopwatch.StartNew();

                msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.ValidateJson);

                // Validamos
                if (!ValidateRetrievedModelData(dataJsonByModel, isSpanish)) return;

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

                // -----------------------------
                // Exportar JSON Global
                // -----------------------------

                processStopwatch = Stopwatch.StartNew();

                msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.GenerateJson);

                Dictionary<string, object> dictDataByFileToJson = GetFinalJsonDictionary(
                    projectCode, softwareLanguage, strEmail, dataJsonByModel, infoCad
                );
                // Exportamos
                cls_00_SaveJson.TrySaveJson(
                    isSpanish, dictDataByFileToJson, projectCode, infoCad.RootFolderName, infoCad.JsonFileNameDataExtraction,
                    selectedFolderPath
                );

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026

                // -----------------------------
                // Preparar JSON Global para el envío
                // -----------------------------

                msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.PrepareJsonForUpload);

                processStopwatch = Stopwatch.StartNew();

                List<Dictionary<string, object>> dictDataByFileToJsonToList = cls_00_GetDataListFromJson.ConvertJsonToDictionaryList(
                    dictDataByFileToJson, keyDataByFileName, isSpanish
                );
                // Validamos
                if (dictDataByFileToJsonToList == null || !dictDataByFileToJsonToList.Any()) return;

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

                // -----------------------------
                // Consultar datos existentes
                // -----------------------------

                ExistingDataResult existingData = await cls_00_GetExistingData.GetExistingDataByFileAsync(
                    strAccessToken, isSpanish, dictDataByFileToJson, strEndpointElementDataUrl
                );
                // Validamos consulta
                if (!existingData.Success) return;

                // -----------------------------
                // Configuracion debug
                // -----------------------------

                bool existingDataShow = false;

                // -----------------------------
                // Mostrar datos existentes
                // -----------------------------

                cls_00_GetExistingData.ShowExistingData(existingData, existingDataShow);

                // -----------------------------
                // Validar si existen datos
                // -----------------------------

                if (existingData.HasData)
                {
                    // -----------------------------
                    // Obtener informacion a borrar
                    // -----------------------------

                    List<Dictionary<string, object>> dataByFileName = cls_00_GetModelData.GetDeleteExistingElementData(
                        dictDataByFileToJsonToList, existingData.Json, keyElementData
                    );
                    // Validamos
                    if (dataByFileName != null && dataByFileName.Count > 0)
                    {
                        // -----------------------------
                        // Borrar datos previos
                        // -----------------------------

                        string msgDeletePreviousData = cls_00_ProcessMessages.ShowProcessMessage(
                            isSpanish, cls_00_ProcessMessages.DeletePreviousModelData
                        );

                        Stopwatch deletePreviousDataStopwatch = Stopwatch.StartNew();

                        try
                        {
                            bool deleted = await cls_00_DeleteData.DeleteDataByEndpoint(
                                strAccessToken, isSpanish, dictDataByFileToJson, dataByFileName, strEndpointElementDataUrl
                            );
                            // Validamos
                            if (!deleted) return;
                        }
                        finally
                        {
                            cls_00_ProcessMessages.AddProcessDuration(
                                processDurations, msgDeletePreviousData, deletePreviousDataStopwatch
                            );
                        }
                    }
                }

                // -----------------------------
                // Enviar JSON global
                // -----------------------------

                processStopwatch = Stopwatch.StartNew();

                msg = cls_00_ProcessMessages.ShowProcessMessage(
                    isSpanish, cls_00_ProcessMessages.SendGlobalJsonByModelToServer
                );

                bool uploaded = await cls_00_SendJsonByChunk.SendJsonByChunk(
                    strAccessToken, isSpanish, dictDataByFileToJsonToList, dictDataByFileToJson,
                    strEndpointElementDataUrl, keyElementData
                );

                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);
                // Validamos
                if (!uploaded) return;
#endif

                // -----------------------------
                // Validar informacion
                // -----------------------------

                processStopwatch = Stopwatch.StartNew();

                msg = cls_00_ProcessMessages.ShowProcessMessage(
                    isSpanish, cls_00_ProcessMessages.ValidateCollectedData
                );

                bool hasData =
                    resultsfromcad.ByLayer.Any() ||
                    resultsfromcad.Audit.Any() ||
                    resultsfromcad.PurgeableItemsCount.Any() ||
                    resultsfromcad.ProjectUnits.Any() ||
                    resultsfromcad.LayersInUse.Any() ||
                    resultsfromcad.LayerZero.Any() ||
                    resultsfromcad.Version.Any() ||
                    resultsfromcad.Xrefs.Any() ||
                    resultsfromcad.PaperTextFont.Any() ||
                    resultsfromcad.FileSize.Any() ||
                    resultsfromcad.BlockRefsInLayoutsCount.Any() ||
                    resultsfromcad.BlocksInUse.Any() ||
                    resultsfromcad.EntityTypesCount.Any() ||
                    resultsFromCivil.CoordSystem.Any() ||
                    resultsFromCivil.PurgeableStyles.Any() ||
                    resultsFromCivil.HasCivilStyles ||
                    resultsFromCivil.HasCivilObjects ||
                    resultsFromCivil.PropertySetsCount.Any();

                cls_00_ProcessMessages.AddProcessDuration(
                    processDurations, msg, processStopwatch
                );

                // Validamos
                if (hasData || warningChecksLog.Any())
                {
                    // ---------------------------------
                    // Preparar datos exportacion
                    // ---------------------------------

                    Dictionary<string, object> exportData = cls_00_GetExportData.GetExportData(
                        keys, selectedOptions, resultsfromcad, resultsFromCivil
                    );

                    // ---------------------------------
                    // Validar
                    // ---------------------------------

                    if (warningChecksLog.Any())
                    {
                        exportData.Add("Warning Selected Checks Log", warningChecksLog);
                    }

                    // -----------------------------
                    // Generar informes finales
                    // -----------------------------

                    processStopwatch = Stopwatch.StartNew();

                    msg = cls_00_ProcessMessages.ShowProcessMessage(
                        isSpanish, cls_00_ProcessMessages.GenerateFinalReports
                    );

                    // Excel
                    ExportDataToExcel(exportData);

                    // Html
                    cls_00_ExportAteneaCheckToHtml.ExportToHtml(
                        selectedFolderPath, exportData, warningChecksLog, projectCode, totalFiles, processedFiles
                    );

                    cls_00_ProcessMessages.AddProcessDuration(
                        processDurations, msg, processStopwatch
                    );
                }
                else
                {
                    // Mensaje
                    MessageBox.Show(
                        "No data found for the selected checks.", "Atenea Model Checker",
                        MessageBoxButtons.OK, MessageBoxIcon.Information
                    );
                }

                // -------------------------------
                // Tiempo total
                // -------------------------------

                totalStopwatch.Stop();

                processDurations["Total"] = totalStopwatch.Elapsed;

                // -------------------------------
                // Summary
                // -------------------------------

                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - startTime;

                // Mensaje
                MessageBox.Show(
                    uiTexts.MsgCompleted +
                    "\nDuration: " + duration.ToString(@"hh\:mm\:ss") +
                    "\nStarted at: " + startTime.ToString("HH:mm:ss") +
                    "\nEnded at: " + endTime.ToString("HH:mm:ss"),
                    uiTexts.Title, MessageBoxButtons.OK, MessageBoxIcon.Information
                );
            }
            // catch
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
        }

    }
}
