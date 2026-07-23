using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using TYPSA.SharedLib.Civil.Main;
using TYPSA.SharedLib.Civil.Buttons;
using TYPSA.SharedLib.Autocad.Main;

namespace TYPSA.PS.RibbonButton.Civil.Buttons
{
    internal class cls_02_ButtonProjectInfoComparer
    {
        [CommandMethod(RibbonCommands.ProjectInfoComparer)]
        public void ProjectInfoComparer()
        {
            // Obtener datos de usuario
            bool userData = cls_00_GetUserData.GetUserData(
                out string projectCode,
                out List<string> selectedFiles,
                out string selectedFolderPath,
                out DateTime startTime
            );
            // Validamos
            if (!userData) return;

            // Procesar los archivos seleccionados
            cls_02_MainProjectInfoComparerProcessFiles mainProcess = 
                new cls_02_MainProjectInfoComparerProcessFiles();

            // Obtener el resultado del proceso
            ProcessResult processResult = 
                mainProcess.ProcessFiles(selectedFiles.ToArray(), projectCode);

            // Mostrar el resumen de los resultados al finalizar
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime;

            // Mensaje
            MessageBox.Show(
                "Data extraction process has completed successfully." +
                "\nDuration: " + duration.ToString(@"hh\:mm\:ss") +
                "\nStarted at: " + startTime.ToString("HH:mm:ss") +
                "\nEnded at: " + endTime.ToString("HH:mm:ss") +
                $"\n\n{processResult.ParametersAnalyzed} parameters have been exported in total." +
                $"\n\n{processResult.TotalFilesProcessed} files were processed successfully.",
                "Extraction Complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information
            );
                       
            
        }




    }
}
