using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.Buttons;
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
        public static void ButtonAteneaParamCheckExp()
        {
            // ---------------------------------
            // Obtener datos de usuario
            // ---------------------------------

            bool userData = cls_00_GetUserData.GetUserData(
                out string projectCode, out List<string> selectedFiles, out string selectedFolderPath, out DateTime startTime,
                customPathLabel: "Please, paste the folder containing the DWG files to analyze"
            );
            // Validamos
            if (!userData) return;

            // ---------------------------------
            // Obtener informacion
            // ---------------------------------

            CadSessionInfo infoCad = GetCivilSessionInfo();

            // ---------------------------------
            // Detectar idioma 
            // ---------------------------------

            bool isSpanish =
                (infoCad.CivilLanguage?.IndexOf("Spanish", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (infoCad.CivilLanguage?.IndexOf("Español", StringComparison.OrdinalIgnoreCase) >= 0);

            // ---------------------------------
            // Texto UI segun idioma
            // ---------------------------------

            UiTexts uiTexts = GetUiTexts(isSpanish);

            // -------------------------------
            // Ejecutamos
            // -------------------------------

            cls_00_MainAteneaParamCheckExp.MainAteneaParamCheckExp(
                selectedFiles.ToArray(), selectedFolderPath, projectCode, 
                startTime, infoCad, uiTexts, isSpanish
            );
        }


    }
}
