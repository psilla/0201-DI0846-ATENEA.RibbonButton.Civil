using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.Buttons;
using TYPSA.SharedLib.Civil.Main;
using TYPSA.SharedLib.EndPoints;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_ButtonAteneaParamDataExp
    {
        private static UiTexts GetUiTexts(bool isSpanish)
        {
            return new UiTexts
            {
                MsgCompleted = isSpanish
                    ? $"{nameof(RibbonCommands.ProcessAteneaParamDataExp)} finalizado correctamente."
                    : $"{nameof(RibbonCommands.ProcessAteneaParamDataExp)} completed successfully.",

                MsgTitle = isSpanish
                    ? "Proceso completado"
                    : "Process Complete"
            };
        }

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
                out DateTime startTime,
                customPathLabel: "Please, paste the folder containing the DWG files to analyze"
            );
            // Validamos
            if (!userData) return;

            // ---------------------------------
            // Obtener informacion
            // ---------------------------------

            CadSessionInfo info = GetCivilSessionInfo();
            cls_00_AteneaEndPointsCivil ateneaEndpoints = new cls_00_AteneaEndPointsCivil();

            // ---------------------------------
            // Detectar idioma 
            // ---------------------------------

            bool isSpanish =
                (info.CivilLanguage?.IndexOf("Spanish", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (info.CivilLanguage?.IndexOf("Español", StringComparison.OrdinalIgnoreCase) >= 0);

            // ---------------------------------
            // Texto UI segun idioma
            // ---------------------------------

            UiTexts uiTexts = GetUiTexts(isSpanish);

            // -------------------------------
            // Ejecutamos
            // -------------------------------

            cls_00_MainAteneaParamDataExp.MainAteneaParamDataExp(
                selectedFiles.ToArray(), projectCode, startTime, info, ateneaEndpoints, uiTexts, isSpanish
            );
        }




    }
}
