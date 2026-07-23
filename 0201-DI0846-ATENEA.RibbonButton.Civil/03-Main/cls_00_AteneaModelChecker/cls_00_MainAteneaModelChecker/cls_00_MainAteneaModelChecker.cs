using System;
using System.Collections.Generic;
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
using TYPSA.SharedLib.UserForms;
using TYPSA.SharedLib.Json;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using static TYPSA.SharedLib.Excel.cls_00_ExportModCheckToExcel_OpenXml;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TYPSA.PS.RibbonButton.Civil
{
    public class cls_00_MainAteneaModelChecker
    {
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

        public static class AteneaModelCheckerDefaults
        {
            // -----------------------------
            // Fonts
            // -----------------------------

            public const string ExpectedPaperTextFont = "ISOCPEUR";
        }

        public class WarningCheckLogResult
        {
            public string FileName { get; set; }
            public string CheckName { get; set; }
            public string Message { get; set; }
        }

        public static async Task MainAteneaModelChecker(
            string[] selectedFiles,
            string projectCode,
            List<string> selectedOptions,
            DateTime startTime,
            CadSessionInfo info,
            cls_00_CivilAteneaEndPoints ateneaEndpoints,
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
            // Normalizar idioma
            // -------------------------------

            string softwareLanguage = isSpanish ? "Spanish" : "English";

            // -------------------------------
            // Validar Datos Proyecto
            // -------------------------------

            Dictionary<string, object> dictProjectDataToVal = GetProjectDataDictionary(
                projectCode, softwareLanguage
            );
#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026
            // Validamos Datos de Proyecto
            if (!await cls_00_ValidateProjectInfo.ValidateProjectDataAsync(
                ateneaEndpoints.EndpointProjectDataUrl, dictProjectDataToVal, cls_00_AteneaJson.CivilVersion, cls_00_AteneaJson.CivilLanguage, isSpanish
            ))
            {
                return;
            }
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
            progressBarForm.ProgressValue = 10; // Valor inicial de progreso
            progressBarForm.Show();

            // try
            try
            {
                // -----------------------------
                // Iterar archivos
                // -----------------------------
              
                foreach (string file in selectedFiles)
                {
                    // -------------------------------
                    // Definir variables
                    // -------------------------------

                    List<Dictionary<string, object>> extractedData = new List<Dictionary<string, object>>();

                    // Obtener el nombre sin extensión
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(file);

                    // try
                    try
                    {
                        using (Document openedDoc = Application.DocumentManager.Open(file, false))
                        {
                            // -------------------------------
                            // Abrir documento
                            // -------------------------------

                            // Validamos
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

                            msg = isSpanish
                                ? $"Analizando documento {processedFiles + 1}/{totalFiles}: '{fileName}'"
                                : $"Analyzing document {processedFiles + 1}/{totalFiles}: '{fileName}'";
                            // Mensaje
                            new AutoCloseMessageForm(msg, 1000).ShowDialog();

                            // -------------------------------
                            // Obtener info
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
                                    // Obtener data
                                    // -------------------------------

                                    cls_00_GetDataModelChecker.ProcessProjectUnits(
                                        selectedOptions, keys, db, fileName, warningChecksLog, resultsfromcad, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessLayersInUse(
                                        selectedOptions, keys, tr, db, bt, fileName, warningChecksLog, resultsfromcad, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessLayerZero(
                                        selectedOptions, keys, tr, db, bt, fileName, warningChecksLog, resultsfromcad, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessVersion(
                                        selectedOptions, keys, db, fileName, warningChecksLog, resultsfromcad, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessXrefs(
                                        selectedOptions, keys, tr, db, bt, fileName, warningChecksLog, resultsfromcad, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessCoordinateSystem(
                                        selectedOptions, keys, fileName, warningChecksLog, resultsFromCivil, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessPaperTextFont(
                                        selectedOptions, keys, tr, db, fileName, warningChecksLog, resultsfromcad, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessFileSize(
                                        selectedOptions, keys, file, fileName, warningChecksLog, resultsfromcad, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessBlocksInUse(
                                        selectedOptions, keys, tr, db, fileName, warningChecksLog, resultsfromcad, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessEntityTypes(
                                        selectedOptions, keys, tr, db, fileName, warningChecksLog, resultsfromcad, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessPurgeableStyles(
                                        selectedOptions, keys, tr, db, fileName, warningChecksLog, resultsFromCivil, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessBlockRefsInLayouts(
                                        selectedOptions, keys, tr, db, bt, fileName, warningChecksLog, resultsfromcad, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessCivilStyles(
                                        selectedOptions, keys, tr, db, fileName, warningChecksLog, resultsFromCivil, extractedData
                                    );
                                    cls_00_GetDataModelChecker.ProcessCivilObjects(
                                        selectedOptions, keys, tr, db, fileName, warningChecksLog, resultsFromCivil, extractedData
                                    );

                                    // -----------------------------
                                    // Crear Estructura Json By File
                                    // -----------------------------

                                    Dictionary<string, object> fileDataByDoc = new Dictionary<string, object>
                                    {
                                        { cls_00_AteneaJson.FileName, fileName },
                                        { cls_00_AteneaJson.CivilElementData, extractedData }
                                    };

                                    // -----------------------------
                                    // Añadir Json
                                    // -----------------------------

                                    dataJsonByModel.Add(fileDataByDoc);

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

                            openedDoc.CloseAndDiscard();

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

                // -----------------------------
                // Cerrar progreso
                // -----------------------------

                progressBarForm.Close();

                // -----------------------------
                // Validar info json
                // -----------------------------

                msg = isSpanish
                    ? "Validando el archivo JSON con los datos extraídos..."
                    : "Validando the JSON file with the extracted data...";
                // Mensaje
                new AutoCloseMessageForm(msg, 1000).ShowDialog();

                if (!dataJsonByModel.Any())
                {
                    // Mensaje
                    MessageBox.Show(
                        isSpanish
                        ? "Ninguno de los archivos seleccionados devolvió datos válidos.\n" +
                            "El proceso será cancelado."
                        : "None of the selected files returned valid data.\n" +
                            "The process will be canceled.",
                        isSpanish ? "Sin Datos Válidos" : "No Valid Data",
                        MessageBoxButtons.OK, MessageBoxIcon.Information
                    );
                    // Finalizamos
                    return;
                }

                // -----------------------------
                // Exportar JSON por Modelo
                // -----------------------------

                msg = isSpanish
                    ? "Generando el archivo JSON con los datos extraídos..."
                    : "Generating the JSON file with the extracted data...";
                // Mensaje
                new AutoCloseMessageForm(msg, 1000).ShowDialog();

                Dictionary<string, object> dictDataByFileToJson = GetFinalJsonDictionary(
                    projectCode, softwareLanguage, dataJsonByModel
                );
                // Exportamos
                cls_00_SaveJson.TrySaveJson(
                    isSpanish, dictDataByFileToJson, projectCode, info.RootFolderName, info.JsonFileNameDataExtraction
                );

                // -----------------------------
                // Enviar JSON por Modelo
                // -----------------------------

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026
                // Enviamos 
                await cls_00_SendJsonByChunk_GenAnalysis.SendJsonByChunk_GenAnalysis(
                    isSpanish, dictDataByFileToJson, cls_00_AteneaJson.CivilVersion, cls_00_AteneaJson.CivilLanguage,
                    ateneaEndpoints.EndpointDeleteUrl, ateneaEndpoints.EndpointPostUrl, cls_00_AteneaJson.CivilElementData
                );
#endif

                // -----------------------------
                // Validar info
                // -----------------------------

                msg = isSpanish
                    ? "Validando la información recopilada de todos los documentos..."
                    : "Validating the information collected from all documents...";
                // Mensaje
                new AutoCloseMessageForm(msg, 1000).ShowDialog();

                bool hasData =
                    resultsfromcad.ProjectUnits.Any() || resultsfromcad.LayersInUse.Any() || resultsfromcad.LayerZero.Any() ||
                    resultsfromcad.Version.Any() || resultsfromcad.Xrefs.Any() || resultsfromcad.PaperTextFont.Any() ||
                    resultsfromcad.FileSize.Any() || resultsfromcad.BlocksInUse.Any() || resultsfromcad.BlockRefsInLayouts.Any() ||
                    resultsfromcad.EntityTypes.Any() || resultsFromCivil.CoordSystem.Any() || resultsFromCivil.PurgeableStyles.Any() ||
                    resultsFromCivil.HasCivilStyles || resultsFromCivil.HasCivilObjects;
                // Validamos
                if (hasData || warningChecksLog.Any())
                {
                    // ---------------------------------
                    // Preparar datos exportacion
                    // ---------------------------------

                    Dictionary<string, object> exportData = cls_00_GetExportDataModelChecker.GetExportData(
                        keys, selectedOptions, resultsfromcad, resultsFromCivil
                    );

                    // ---------------------------------
                    // Validar
                    // ---------------------------------

                    if (warningChecksLog.Any())
                    {
                        exportData.Add("Warning Selected Checks Log", warningChecksLog);
                    }

                    // ---------------------------------
                    // Exportar report
                    // ---------------------------------

                    msg = isSpanish
                        ? "Generando los informes finales en Excel y HTML..."
                        : "Generating the final Excel and HTML reports...";
                    // Mensaje
                    new AutoCloseMessageForm(msg, 1000).ShowDialog();

                    // Excel
                    ExportDataToExcel(exportData);
                    // Html
                    cls_00_ExportAteneaCheckToHtml.ExportToHtml(
                        exportData, warningChecksLog, projectCode, totalFiles, processedFiles
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
