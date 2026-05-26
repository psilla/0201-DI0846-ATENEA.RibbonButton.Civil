using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using TYPSA.SharedLib.Civil.Buttons;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CivilInfoHelper;
using TYPSA.SharedLib.Autocad.Main;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_ButtonAteneaParamCheckExp
    {
        [CommandMethod(RibbonCommands.ButtonAteneaParamCheckExp)]
        public static void ButtonAteneaParamDataExp()
        {
            // ---------------------------------
            // Obtener datos de usuario
            // ---------------------------------

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
            CivilSessionInfo info = GetCivilSessionInfo();
            // Idioma Civil
            bool isSpanish = info.CivilLanguage?.Equals(
                "Spanish", StringComparison.OrdinalIgnoreCase
            ) == true;

            // Texto UI según idioma
            string msgCompleted = isSpanish
                ? "Atenea Model Checker finalizado correctamente."
                : "Atenea Model Checker completed successfully.";

            string msgTitle = isSpanish
                ? "Proceso completado"
                : "Process Complete";

            // -------------------------------
            // Ejecutamos
            // -------------------------------

            cls_00_MainAteneaParamCheckExp mainProcess = new cls_00_MainAteneaParamCheckExp();
            // Procesar archivos
            ProcessResult processResult = mainProcess.MainAteneaParamCheckExp(
                selectedFiles.ToArray(), projectCode, isSpanish, info
            );

            // -------------------------------
            // Summary
            // -------------------------------

            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime;

            // Mensaje
            MessageBox.Show(
                msgCompleted +
                "\nDuration: " + duration.ToString(@"hh\:mm\:ss") +
                "\nStarted at: " + startTime.ToString("HH:mm:ss") +
                "\nEnded at: " + endTime.ToString("HH:mm:ss"),
                msgTitle,
                MessageBoxButtons.OK, MessageBoxIcon.Information
            );
        }
    }
}
