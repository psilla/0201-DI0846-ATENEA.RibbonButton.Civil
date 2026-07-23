using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.Buttons;
using TYPSA.SharedLib.EndPoints;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_ButtonAteneaParamCheckExp
    {
        private static UiTexts GetUiTexts(bool isSpanish)
        {
            return new UiTexts
            {
                MsgCompleted = isSpanish
                    ? $"{nameof(RibbonCommands.ProcessAteneaParamCheckExp)} finalizado correctamente."
                    : $"{nameof(RibbonCommands.ProcessAteneaParamCheckExp)} completed successfully.",

                MsgTitle = isSpanish
                    ? "Proceso completado"
                    : "Process Complete"
            };
        }

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
                out DateTime startTime,
                customPathLabel: "Please, paste the folder containing the DWG files to analyze"
            );
            // Validamos
            if (!userData) return;

            // ---------------------------------
            // Detectar idioma 
            // ---------------------------------

            // Obtenemos info
            CadSessionInfo info = GetCivilSessionInfo();
            cls_00_CivilAteneaEndPoints ateneaEndpoints = new cls_00_CivilAteneaEndPoints();
            // Idioma Civil
            bool isSpanish =
                (info.CivilLanguage?.IndexOf("Spanish", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (info.CivilLanguage?.IndexOf("Español", StringComparison.OrdinalIgnoreCase) >= 0);

            // ---------------------------------
            // Texto UI según idioma
            // ---------------------------------

            UiTexts uiTexts = GetUiTexts(isSpanish);

            // -------------------------------
            // Ejecutamos
            // -------------------------------

            cls_00_MainAteneaParamCheckExp.MainAteneaParamCheckExp(
                selectedFiles.ToArray(), projectCode, startTime, info, ateneaEndpoints, uiTexts, isSpanish
            );
        }


    }
}
