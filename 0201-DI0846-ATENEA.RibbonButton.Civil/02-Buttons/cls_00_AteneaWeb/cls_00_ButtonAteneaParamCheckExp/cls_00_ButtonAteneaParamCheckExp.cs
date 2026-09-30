using Autodesk.AutoCAD.Runtime;
using TYPSA.SharedLib.EndPoints;
using static TYPSA.SharedLib.Autocad.cls_00_CadInfoHelper;
using static TYPSA.SharedLib.Civil.cls_00_UiTexts;
using System;

namespace TYPSA.ATENEA.RibbonButton.Civil
{
    public class cls_00_ButtonAteneaParamCheckExp
    {
        private static UiTexts GetUiTexts(bool isSpanish)
        {
            return new UiTexts
            {
                MsgCompleted = isSpanish
                    ? $"{nameof(AteneaRibbonCommands.ProcessAteneaParamCheckExp)} finalizado correctamente."
                    : $"{nameof(AteneaRibbonCommands.ProcessAteneaParamCheckExp)} completed successfully.",

                MsgTitle = isSpanish
                    ? "Proceso completado"
                    : "Process Complete"
            };
        }

        [CommandMethod(AteneaRibbonCommands.ButtonAteneaParamCheckExp)]
        public static void ButtonAteneaParamCheckExp()
        {
            // ---------------------------------
            // Obtener datos de usuario
            // ---------------------------------

            bool userData = TYPSA.SharedLib.Autocad.cls_00_GetUserData.GetUserData(
                out string projectCode, out DateTime startTime
            );
            // Validamos
            if (!userData) return;

            // ---------------------------------
            // Obtener información sesión ATENEA
            // ---------------------------------

            AteneaSessionInfo sessionInfo = GetAteneaCivilSessionInfo(
                projectCode, startTime
            );

            // ---------------------------------
            // Detectar idioma
            // ---------------------------------

            bool isSpanish =
                (sessionInfo.SoftwareLanguage?.IndexOf(
                    "Spanish", StringComparison.OrdinalIgnoreCase
                ) >= 0) ||
                (sessionInfo.SoftwareLanguage?.IndexOf(
                    "Español", StringComparison.OrdinalIgnoreCase
                ) >= 0);

            // ---------------------------------
            // Texto UI segun idioma
            // ---------------------------------

            UiTexts uiTexts = GetUiTexts(isSpanish);

            // -------------------------------
            // Ejecutamos
            // -------------------------------

            cls_00_MainAteneaParamCheckExp.MainAteneaParamCheckExp(
                sessionInfo, uiTexts, isSpanish
            );
        }


    }
}
