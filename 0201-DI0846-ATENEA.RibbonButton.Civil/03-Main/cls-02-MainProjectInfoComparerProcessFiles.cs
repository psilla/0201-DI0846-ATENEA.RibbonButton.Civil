using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using TYPSA.PS.RibbonButton.Civil.Source.Class.ProjectInfo;
using TYPSA.PS.RibbonButton.Civil.Source.Class.ExcelData;
using TYPSA.PS.RibbonButton.Civil._05_Source.Class.ExcelData;
using TYPSA.SharedLib.Autocad.Metrics;
using TYPSA.SharedLib.UserForms;
using TYPSA.SharedLib.Civil.Main;
using TYPSA.SharedLib.Autocad.Main;


namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_02_MainProjectInfoComparerProcessFiles
    {
        public ProcessResult ProcessFiles(string[] selectedFiles, string projectCode)
        {
            // Variables para recopilar métricas
            int totalSelectedFiles = selectedFiles.Length;
            int filesSelectedProcessed = 0;
            int percentage = 0;
            HashSet<string> uniqueLayers = new HashSet<string>();
            int recountProjectIfoExporter = 0;
            string sheetName = "C3D-ProjectInfo";

            // Instanciar ExcelManager y preparar Excel
            cls_00_ExcelManager excelManager = new cls_00_ExcelManager();
            var (excelPath, filesToProcess) = excelManager.PrepareExcel(selectedFiles, sheetName);

            if (excelPath == null || filesToProcess == null || filesToProcess.Count == 0)
            {
                return new ProcessResult
                {
                    TotalFilesProcessed = 0,
                    ParametersAnalyzed = 0
                };
            }

            cls_00_ExcelData excelData = new cls_00_ExcelData();

            // Crear el formulario de la barra de progreso
            using (ProgressBarControl progressBarForm = new ProgressBarControl())
            {
                progressBarForm.Show();

                // Iterar sobre los archivos seleccionados
                foreach (string file in filesToProcess)
                {
                    // Abrimos documento
                    using (Document openedDoc = Application.DocumentManager.Open(file))
                    {
                        // Validamos
                        if (openedDoc == null)
                        {
                            // Mensaje
                            new AutoCloseMessageForm(
                                $"Error opening document:\n{file}"
                            ).ShowDialog();
                            // Obviamos
                            continue;
                        }

                        // Obtenemos el nombre
                        string fileName = Path.GetFileName(file);

                        // Procesamos info
                        cls_02_ProjectInfoComparer projectInfo = new cls_02_ProjectInfoComparer();
                        projectInfo.GetProjectInfo(openedDoc, projectCode, excelPath);

                        // Cerramos documento
                        openedDoc.CloseAndDiscard();

                        // Actualizar la barra de progreso
                        filesSelectedProcessed++;
                        percentage = (int)((double)filesSelectedProcessed / totalSelectedFiles * 100);
                        progressBarForm.ProgressValue = percentage;
                    }
                }
            }

            // Obtener métricas finales
            int headerCount = excelData.GetExcelHeadersCount(excelPath, sheetName);
            recountProjectIfoExporter = filesSelectedProcessed * headerCount;

            // Enviar métricas
            SendMetrics(totalSelectedFiles, recountProjectIfoExporter, projectCode);

            // Devolver los resultados del proceso
            return new ProcessResult
            {
                TotalFilesProcessed = filesSelectedProcessed,
                ParametersAnalyzed = recountProjectIfoExporter
            };
        }

        private void SendMetrics(int totalFiles, int parametersExported, string projectCode)
        {
            string accionId = "67a9f794b10174ef740fd380";
            string processIdOpeningDWGFile = "669669da1d83111125968025";
            string processIdPropertySetExporter = "669669da1d83111125968027";
            string emailUser = Environment.UserName;

            var executed_process = new[]
            {
            new { proceso = processIdOpeningDWGFile, recuento = totalFiles },
            new { proceso = processIdPropertySetExporter, recuento = parametersExported }
        };

            var additionalData = new
            {
                ScriptName = "0381-HY9678-TYPSA.PS.RibbonButton.Civil.csproj",
                FilesProcessed = totalFiles,
                ProjectCode = projectCode,
                ExecutionStatus = 1,
                Version = "V.00.01"
            };

            cls_00_MetricsSender.SendMetricsAsync(emailUser, accionId, executed_process, additionalData);
        }
    }

            
}


