using System;
using Autodesk.AutoCAD.Runtime;
using System.Windows.Forms;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using System.Collections.Generic;
using TYPSA.SharedLib.Civil.Buttons;
using TYPSA.SharedLib.Autocad.Main;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_ButtonAteneaParamDataImp
    {
        // Método para importar Properties
        [CommandMethod(RibbonCommands.ButtonAteneaParamDataImp)]
        public static void PropImportBackFromJSON()
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
            cls_00_MainAteneaParamDataImp mainProcess = 
                new cls_00_MainAteneaParamDataImp();

            // Obtener el resultado del proceso
            ProcessResult processResult = mainProcess.MainAteneaParamDataImp(
                selectedFiles.ToArray(), projectCode
            );

            // Mostrar el resumen de los resultados al finalizar
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime;

            // Mensaje
            MessageBox.Show("Properties Importer process has completed successfully." +
                            "\nDuration: " + duration.ToString(@"hh\:mm\:ss") +
                            "\nStarted at: " + startTime.ToString("HH:mm:ss") +
                            "\nEnded at: " + endTime.ToString("HH:mm:ss") +
                            $"\n\n{processResult.ParametersAnalyzed} parameters have been imported in total." +
                            $"\n{processResult.TotalFilesProcessed} files were processed successfully.",
                            "Extraction Complete",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
        }



    }
}
