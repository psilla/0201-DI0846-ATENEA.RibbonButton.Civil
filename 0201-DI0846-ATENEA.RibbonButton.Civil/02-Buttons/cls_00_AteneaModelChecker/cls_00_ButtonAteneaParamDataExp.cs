using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using TYPSA.SharedLib.Civil.Buttons;
using TYPSA.SharedLib.Civil.Main;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CivilInfoHelper;
using TYPSA.SharedLib.Autocad.Main;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_ButtonAteneaParamDataExp
    {
        // Método para exportar Properties
        [CommandMethod(RibbonCommands.ButtonAteneaParamDataExp)]
        public static void PropExportBackToJSON()
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

            // -------------------------------
            // Ejecutamos
            // -------------------------------

            cls_00_MainAteneaParamDataExp mainProcess = new cls_00_MainAteneaParamDataExp();
            // Procesar archivos
            mainProcess.MainAteneaParamDataExp(
                selectedFiles.ToArray(), projectCode, isSpanish, info
            );

            // -------------------------------
            // Summary
            // -------------------------------

            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime;

            // Mensaje
            MessageBox.Show(
                "Properties Exporter process has completed successfully." +
                "\nDuration: " + duration.ToString(@"hh\:mm\:ss") +
                "\nStarted at: " + startTime.ToString("HH:mm:ss") +
                "\nEnded at: " + endTime.ToString("HH:mm:ss"),
                "Extraction Complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information
            );
        }




    }
}
