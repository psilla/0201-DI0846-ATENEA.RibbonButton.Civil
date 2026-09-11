using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Newtonsoft.Json;
using TYPSA.SharedLib.Autocad.GetDocument;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.GetPropertyData;
using TYPSA.SharedLib.Civil.JsonTools;
using TYPSA.SharedLib.Civil.SetDataFromJson;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.UserForms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using static TYPSA.PS.RibbonButton.Civil.cls_00_ParamImpMainWeb;
using static TYPSA.PS.RibbonButton.Civil.cls_00_PrepareParamWebDataAsync;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_00_MainAteneaParamDataImp
    {
        private static Dictionary<string, List<string>> GetPsetPropertiesAvailableInModel(
            List<EntityData> civilParamData,
            Dictionary<string, SelectedPsetData> selectedPsetData
        )
        {
            // -------------------------------
            // Crear diccionario
            // -------------------------------

            Dictionary<string, List<string>> psetPropertiesAvailable =
                new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            // -------------------------------
            // Iterar Psets seleccionados
            // -------------------------------

            foreach (KeyValuePair<string, SelectedPsetData> selectedPset in selectedPsetData)
            {
                string psetName = selectedPset.Key;

                HashSet<string> selectedProperties = new HashSet<string>(
                    selectedPset.Value.Properties.Keys,
                    StringComparer.OrdinalIgnoreCase
                );

                // -------------------------------
                // Obtener Properties existentes en modelo
                // -------------------------------

                List<string> existingProperties = civilParamData
                    .Where(d => d.PropertySetInfo != null)
                    .SelectMany(d => d.PropertySetInfo)
                    .Where(pset =>
                        pset != null &&
                        !string.IsNullOrWhiteSpace(pset.PsetName) &&
                        pset.PsetName.Equals(psetName, StringComparison.OrdinalIgnoreCase) &&
                        pset.Parameters != null
                    )
                    .SelectMany(pset => pset.Parameters)
                    .Where(p =>
                        p != null &&
                        !string.IsNullOrWhiteSpace(p.PropName) &&
                        selectedProperties.Contains(p.PropName)
                    )
                    .Select(p => p.PropName)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x)
                    .ToList();

                // Añadimos
                if (existingProperties.Count > 0)
                {
                    psetPropertiesAvailable[psetName] = existingProperties;
                }
            }

            // return
            return psetPropertiesAvailable;
        }

        private static void SkipFileFromJson(
            bool isSpanish,
            string messageSpanish,
            string messageEnglish,
            ref int processedFiles,
            int totalFiles,
            int percentage,
            ProgressBarControl progressBarForm,
            int durationMs = 1000
        )
        {
            // Mostramos
            new AutoCloseMessageForm(
                isSpanish ? messageSpanish : messageEnglish,
                durationMs
            ).ShowDialog();

            // Actualizar progreso
            processedFiles++;
            percentage = (int)((double)processedFiles / totalFiles * 100);
            progressBarForm.ProgressValue = percentage;
        }

        private static void SkipDiscardedFile(
            Document openedDoc,
            ref int processedFiles,
            int totalFiles,
            ProgressBarControl progressBarForm
        )
        {
            // Cerramos documento
            openedDoc?.CloseAndDiscard();
            // Actualizar progreso
            processedFiles++;
            int percentage = (int)((double)processedFiles / totalFiles * 100);
            progressBarForm.ProgressValue = percentage;
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

        public static void MainAteneaParamDataImp(
            string[] selectedFiles,
            string selectedFolderPath,
            string projectCode,
            Root dataByModelFromJson,
            ParamImpPreparedData dataFromAsyn,
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

                    StringBuilder sbModelParamCheckImpInfo = new StringBuilder();
                    bool modelHasUpdates = false;

                    string fileName = System.IO.Path.GetFileNameWithoutExtension(file);

                    Dictionary<string, TimeSpan> modelDurations = new Dictionary<string, TimeSpan>();

                    // -------------------------------
                    // Validar modelo en JSON
                    // -------------------------------

                    FileData matchingFile = dataByModelFromJson.DataByFileName.FirstOrDefault(
                        d => d.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase
                    ));
                    // Validamos
                    if (matchingFile == null)
                    {
                        // Obviamos
                        SkipFileFromJson(
                            isSpanish,
                            $"El archivo '{fileName}' no se encontró en el JSON y será omitido.",
                            $"File '{fileName}' was not found in the JSON and will be skipped.",
                            ref processedFiles, totalFiles, percentage, progressBarForm
                        );
                        // Obviamos
                        continue;
                    }

                    // -------------------------------
                    // Validar datos del modelo en JSON
                    // -------------------------------

                    List<EntityData> civilParamData = matchingFile.CivilParamData;
                    // Validamos
                    if (civilParamData == null || civilParamData.Count == 0)
                    {
                        // Obviamos
                        SkipFileFromJson(
                            isSpanish,
                            $"El archivo '{fileName}' no contiene datos en 'CivilParamData' y será omitido.",
                            $"File '{fileName}' does not contain data in 'CivilParamData' and will be skipped.",
                            ref processedFiles, totalFiles, percentage, progressBarForm
                        );
                        // Obviamos
                        continue;
                    }

                    // -------------------------------
                    // Obtener Psets y Properties disponibles en Modelo
                    // -------------------------------

                    Dictionary<string, List<string>> psetPropertiesAvailable = GetPsetPropertiesAvailableInModel(
                        civilParamData, selectedPsetData
                    );
                    // Validamos
                    if (psetPropertiesAvailable == null || psetPropertiesAvailable.Count == 0)
                    {
                        // Obviamos
                        SkipFileFromJson(
                            isSpanish,
                            $"El archivo '{fileName}' no contiene ninguno de los Property Sets o Properties seleccionados.",
                            $"File '{fileName}' does not contain any of the selected Property Sets or Properties.",
                            ref processedFiles, totalFiles, percentage, progressBarForm
                        );
                        // Obviamos
                        continue;
                    }

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
                                // Obviamos
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

                            // -------------------------------
                            // Bloquear el documento
                            // -------------------------------

                            bool skipFile = false;

                            using (openedDoc.LockDocument())
                            using (Transaction tr = openedDoc.TransactionManager.StartTransaction())
                            {
                                // try
                                try
                                {
                                    // -------------------------------
                                    // Procesar informacion
                                    // -------------------------------

                                    msg = cls_00_ProcessMessages.ShowProcessMessage(
                                        isSpanish, cls_00_ProcessMessages.CollectModelData, fileName
                                    );

                                    modelProcessStopwatch = Stopwatch.StartNew();

                                    FileData fileData = cls_00_ProcessDocFromJson.ProcessDocFromJson(
                                        openedDoc, db, psetPropertiesAvailable, tr, fileName, matchingFile
                                    );

                                    // Añadimos
                                    cls_00_ProcessMessages.AddProcessDuration(
                                        modelDurations, msg, modelProcessStopwatch
                                    );

                                    // -------------------------------
                                    // Validar proceso
                                    // -------------------------------

                                    if (fileData == null)
                                    {
                                        // Abortamos proceso
                                        tr.Abort();
                                        // Obviamos
                                        skipFile = true;
                                    }
                                    else
                                    {
                                        // Cerramos transaccion
                                        tr.Commit();
                                    }
                                }
                                // catch
                                catch (Exception ex)
                                {
                                    // Abortamos
                                    tr.Abort();
                                    // Mensaje
                                    MessageBox.Show(
                                        $"Error while processing document {file}:" +
                                        $"\n{ex.Message}" +
                                        $"\nDetails:\n{ex.StackTrace}",
                                        "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error
                                    );
                                    // Obviamos
                                    skipFile = true;
                                }
                            }

                            // -----------------------------
                            // Validar archivo procesado
                            // -----------------------------

                            if (skipFile)
                            {
                                // Obviamos
                                SkipDiscardedFile(
                                    openedDoc, ref processedFiles, totalFiles, progressBarForm
                                );
                                // Obviamos
                                continue;
                            }

                            // -----------------------------
                            // Cerrar y Guardar documento
                            // -----------------------------

                            msg = cls_00_ProcessMessages.ShowProcessMessage(
                                isSpanish, cls_00_ProcessMessages.CloseDocument, processedFiles + 1, totalFiles, fileName
                            );

                            Stopwatch closeDocumentStopwatch = Stopwatch.StartNew();

                            string filePathName = System.IO.Path.GetFullPath(file);
                            openedDoc.CloseAndSave(filePathName);

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
                    catch (Exception ex)
                    {
                        // Mensaje
                        MessageBox.Show(
                            $"EXCEPTION while opening document:" +
                            $"\n{ex.Message}\n{ex.StackTrace}"
                        );
                    }
                }

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msgIter, filesProcessingStopwatch);

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
                    selectedFolderPath, processDurations, projectCode, dataFromAsyn.UserNameBySso, totalFiles, processedFiles, isSpanish,
                    processDurationsByModel, sendDurationsByModel, includeModelDetails: true
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
