using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Aec.PropertyData.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Newtonsoft.Json;
using TYPSA.SharedLib.Autocad.GetDocument;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.Excel;
using TYPSA.SharedLib.Json;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_00_MainAteneaParamCheckExp
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
            string[] selectedFiles,
            string projectCode,
            DateTime startTime,
            CadSessionInfo info,
            cls_00_AteneaEndPointsCivil ateneaEndpoints,
            UiTexts uiTexts,
            bool isSpanish
        )
        {
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
            // Obtener informacion
            // -------------------------------

            string keySoftwareVersion = cls_00_AteneaJson.CivilVersion;
            string keySoftwareLanguage = cls_00_AteneaJson.CivilLanguage;
            string keyDataByFileName = cls_00_AteneaJson.DataByFileName;
            string keyParamCheck = cls_00_AteneaJson.CivilParamCheck;
            string keyFileName = cls_00_AteneaJson.FileName;
            string keyPsetName = cls_00_AteneaJson.PsetName;
            string keyPropName = cls_00_AteneaJson.PropName;
            string keyDataType = cls_00_AteneaJson.DataType;
            string keyPropDefaultValue = cls_00_AteneaJson.PropDefaultValue;
            string keyPropDescription = cls_00_AteneaJson.PropDescription;
            string keyPropIsAutomatic = cls_00_AteneaJson.PropIsAutomatic;
            string keyPropIsVisible = cls_00_AteneaJson.PropIsVisible;
            string keyPropIsReadOnly = cls_00_AteneaJson.PropIsReadOnly;

            string strEndpointGetSetUrl = ateneaEndpoints.EndpointGetSetUrl;
            string strEndpointSetUrl = ateneaEndpoints.EndpointSetUrl;
            string strEndpointDataByFileUrl = ateneaEndpoints.EndpointDataByFileUrl;

            string strUserName = info.UserName;
            string strRootFolderName = info.RootFolderName;
            string strJsonFileNameParamCheckExp = info.JsonFileNameParamCheckExp;
            string strJsonFileNameParamCheckSet = info.JsonFileNameParamCheckSet;

            // -------------------------------
            // Reiniciamos cronometro global
            // -------------------------------

            startTime = DateTime.Now;

            // -------------------------------
            // Preparar informacion Web
            // -------------------------------

            ParamExpPreparedData preparedData = await cls_00_ParamExpMainWeb_Async.ParamExpMainWebAsync(
                projectCode, info, ateneaEndpoints, strEndpointGetSetUrl, isSpanish, validateExistingSet: false
            );
            // Validamos
            if (preparedData == null) return;

            // -------------------------------
            // Obtener informacion preparada
            // -------------------------------

            int currentSetStatus = preparedData.CurrentSetStatus;
            List<Dictionary<string, object>> civilParamCheckFromSet = preparedData.CivilParamCheckFromSet;

            // -----------------------------
            // Ordenar listado de categorias 
            // -----------------------------

            StringCollection allCategories = cls_00_GetAllObjectCat.GetAllObjectCatForPsetsByVersion();

            bool exportToJson = false;
            if (exportToJson)
                // Exportamos a JSON 
                ExportCategoriesToJson(allCategories);

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

                            DictionaryPropertySetDefinitions dictPropSetDef = new DictionaryPropertySetDefinitions(db);

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

                                    // -------------------------------
                                    // Obtener Property Sets del documento
                                    // -------------------------------

                                    extractedData = cls_00_ProcessAteneaParamCheckExp.ProcessAteneaParamCheckExp(
                                        tr, dictPropSetDef, allCategories
                                    );

                                    // Añadimos
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
                                        { keyParamCheck, extractedData }
                                    };

                                    // Añadimos
                                    dataJsonByModel.Add(fileDataByDoc);

                                    // Añadimos
                                    cls_00_ProcessMessages.AddProcessDuration(modelDurations, msg, modelProcessStopwatch);

                                    // -----------------------------
                                    // Cerrar transaccion
                                    // -----------------------------

                                    tr.Commit();
                                }
                                // catch
                                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                                {
                                    // Mensaje
                                    string inner = ex.InnerException != null
                                        ? $"\n\nInner:\n{ex.InnerException.Message}"
                                        : "";
                                    new AutoCloseMessageForm(
                                        $"Error while processing '{file}':\n\n" +
                                        $"{ex.Message}\n\n" +
                                        $"Stack:\n{ex.StackTrace}" +
                                        inner, 1500
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

                            // Añadimos
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
                // Filtrar informacion Global sin Categorias
                // -----------------------------

                List<Dictionary<string, object>> dataJsonByModelNoCategories = 
                    cls_00_FilterJsonBySelectedPsets.RemoveCategoriesFromJson(dataJsonByModel);

                // -----------------------------
                // Exportar JSON Global
                // -----------------------------

                processStopwatch = Stopwatch.StartNew();

                msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.GenerateJson);

                Dictionary<string, object> dictDataByFileToJson = GetFinalJsonDictionary(
                    projectCode, softwareLanguage, dataJsonByModelNoCategories
                );
                // Exportamos
                cls_00_SaveJson.TrySaveJson(
                    isSpanish, dictDataByFileToJson, projectCode, strRootFolderName, strJsonFileNameParamCheckExp
                );

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

                // -----------------------------
                // Filtrar Psets seleccionados para el Set
                // -----------------------------

                List<Dictionary<string, object>> dataJsonByModelFiltered = cls_00_FilterJsonBySelectedPsets.FilterJsonBySelectedPsets(
                    dataJsonByModel, civilParamCheckFromSet, currentSetStatus, isSpanish, out bool addPsetsToSet
                );
                // Validamos
                if (dataJsonByModelFiltered == null) return;

                // -----------------------------
                // Variables del Set
                // -----------------------------

                int updatedSetStatus = currentSetStatus;
                Dictionary<string, object> dictDataSetToJson = null;

                // -----------------------------
                // Actualizar status Set
                // -----------------------------

                if (addPsetsToSet)
                {
                    msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.UpdateParameterSetStatus);

                    processStopwatch = Stopwatch.StartNew();

                    dictDataSetToJson = cls_00_GetFinalJsonDictCivilParamCheckCons.GetFinalJsonPsetSet(
                        projectCode, softwareLanguage, dataJsonByModelFiltered, civilParamCheckFromSet,
                        currentSetStatus, out updatedSetStatus
                    );

                    // Añadimos
                    cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

                    // -----------------------------
                    // Exportar JSON Set 
                    // -----------------------------

                    msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.GenerateParameterSetJson);

                    processStopwatch = Stopwatch.StartNew();

                    cls_00_SaveJson.TrySaveJson(
                        isSpanish, dictDataSetToJson, projectCode, strRootFolderName, strJsonFileNameParamCheckSet
                    );

                    // Añadimos
                    cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);
                }

                // -----------------------------
                // Exportar Excel report
                // -----------------------------

                bool exportToExcel = false;
                // Exportamos
                if (exportToExcel)
                {
                    msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.GenerateExcel);

                    processStopwatch = Stopwatch.StartNew();

                    List<string> headers = new List<string>
                    {
                        keyFileName, keyPsetName, keyPropName, keyDataType, keyPropDefaultValue,
                        keyPropDescription, keyPropIsAutomatic, keyPropIsVisible, keyPropIsReadOnly
                    };
                    // Exportamos
                    cls_00_ExportFilteredParamCheckToExcel.ExportFilteredParamCheckToExcel(dataJsonByModel, headers);

                    // Añadimos
                    cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);
                }
                
#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026

                // -----------------------------
                // Preparar JSON Global para el envío
                // -----------------------------

                msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.PrepareJsonForUpload);

                processStopwatch = Stopwatch.StartNew();

                List<Dictionary<string, object>> dictDataByFileToJsonToList = cls_00_GetDataListFromJson.ConvertJsonToDictionaryList(
                    dictDataByFileToJson, keyDataByFileName, isSpanish
                );

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

                // Validamos
                if (dictDataByFileToJsonToList == null || !dictDataByFileToJsonToList.Any()) return;

                // -----------------------------
                // Enviar JSON Global
                // -----------------------------

                processStopwatch = Stopwatch.StartNew();

                msg = cls_00_ProcessMessages.ShowProcessMessage(
                    isSpanish, cls_00_ProcessMessages.SendGlobalJsonByModelToServer
                );

                bool uploaded = await cls_00_SendJsonByChunk.SendJsonByChunk(
                    isSpanish, dictDataByFileToJsonToList, dictDataByFileToJson, keySoftwareVersion, keySoftwareLanguage,
                    strEndpointDataByFileUrl, keyParamCheck
                );

                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);
                // Validamos
                if (!uploaded) return;

                // -----------------------------
                // Enviar JSON Set
                // -----------------------------

                if (addPsetsToSet && updatedSetStatus != 3 && dictDataSetToJson != null)
                {
                    msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.SendParameterSetToServer);

                    processStopwatch = Stopwatch.StartNew();

                    await cls_00_SendJsonByChunk.SendJsonByChunk_ParamCheckSet(
                        isSpanish, dictDataSetToJson, currentSetStatus, strEndpointSetUrl,
                        keySoftwareVersion, keySoftwareLanguage, keyParamCheck
                    );

                    // Añadimos
                    cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);
                }
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
                    processDurations, projectCode, strUserName, totalFiles, processedFiles, isSpanish,
                    processDurationsByModel, sendDurationsByModel, includeModelDetails: true
                );

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

            // Por defecto
            return;
            
        }

    }
}

