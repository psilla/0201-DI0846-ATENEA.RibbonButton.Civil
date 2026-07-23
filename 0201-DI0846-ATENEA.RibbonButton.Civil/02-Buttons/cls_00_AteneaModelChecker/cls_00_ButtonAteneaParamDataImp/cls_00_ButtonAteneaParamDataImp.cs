using System;
using Autodesk.AutoCAD.Runtime;
using System.Windows.Forms;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using System.Collections.Generic;
using TYPSA.SharedLib.Civil.Buttons;
using TYPSA.SharedLib.Autocad.Main;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;

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

            // ---------------------------------
            // Detectar idioma Civil
            // ---------------------------------

            // Obtenemos info
            CadSessionInfo info = GetCivilSessionInfo();
            // Idioma Civil
            bool isSpanish =
                (info.CivilLanguage?.IndexOf("Spanish", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (info.CivilLanguage?.IndexOf("Español", StringComparison.OrdinalIgnoreCase) >= 0);

            // Texto UI según idioma
            string msgCompleted = isSpanish
                ? $"{nameof(RibbonCommands.ProcessAteneaParamDataImp)} finalizado correctamente."
                : $"{nameof(RibbonCommands.ProcessAteneaParamDataImp)} completed successfully.";

            string msgTitle = isSpanish
                ? "Proceso completado"
                : "Process Complete";

            // -------------------------------
            // Ejecutamos
            // -------------------------------

            // Procesar los archivos seleccionados
            cls_00_MainAteneaParamDataImp mainProcess = new cls_00_MainAteneaParamDataImp();

            // Obtener el resultado del proceso
            ProcessResult processResult = mainProcess.MainAteneaParamDataImp(
                selectedFiles.ToArray(), projectCode
            );

            // -------------------------------
            // Summary
            // -------------------------------

            // Mostrar el resumen de los resultados al finalizar
            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime;

            // Mensaje
            MessageBox.Show(
                msgCompleted +
                "\nDuration: " + duration.ToString(@"hh\:mm\:ss") +
                "\nStarted at: " + startTime.ToString("HH:mm:ss") +
                "\nEnded at: " + endTime.ToString("HH:mm:ss"),
                msgTitle, MessageBoxButtons.OK, MessageBoxIcon.Information
            );       
        }



    }
}
