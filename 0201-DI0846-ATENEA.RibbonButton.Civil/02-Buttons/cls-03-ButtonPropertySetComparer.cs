using Autodesk.AutoCAD.Runtime;
using System;
using System.Windows.Forms;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Collections.Generic;
using TYPSA.SharedLib.Civil.Main;
using TYPSA.SharedLib.Civil.Buttons;

namespace TYPSA.PS.RibbonButton.Civil.Buttons
{
    internal class cls_03_ButtonPropertySetComparer
    {
        [CommandMethod("PropertySetComparer")]
        public void PropertySetComparer()
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
            cls_03_MainPropertySetComparerProcessFiles mainProcess = 
                new cls_03_MainPropertySetComparerProcessFiles();

            // Obtener el resultado del proceso
            ProcessResult processResult =  mainProcess.ProcessFiles(
                selectedFiles.ToArray(), projectCode
            );

            // Mostrar el resumen de los resultados al finalizar
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime;

            // Mensaje
            MessageBox.Show("Data extraction process has completed successfully." +
                            "\nDuration: " + duration.ToString(@"hh\:mm\:ss") +
                            "\nStarted at: " + startTime.ToString("HH:mm:ss") +
                            "\nEnded at: " + endTime.ToString("HH:mm:ss") +
                            $"\n\n{processResult.ParametersAnalyzed} parameters have been exported in total." +
                            $"\n{processResult.TotalFilesProcessed} files were processed successfully.",
                            "Extraction Complete",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
        }





    }
}

