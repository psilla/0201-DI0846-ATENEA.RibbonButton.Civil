using Autodesk.AutoCAD.Runtime;
using TYPSA.SharedLib.Autocad;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Autocad.cls_00_CadInfoHelper;
using static TYPSA.SharedLib.Civil.cls_00_UiTexts;
using Exception = System.Exception;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TYPSA.ATENEA.RibbonButton.Civil
{
    public class cls_00_ButtonAteneaModelChecker
    {
        private static UiTexts GetUiTexts(bool isSpanish)
        {
            return new UiTexts
            {
                Title = isSpanish
                    ? "Seleccione los análisis a ejecutar:"
                    : "Select the analysis to be performed:",

                MsgNoOptions = isSpanish
                    ? "No se seleccionó ninguna opción. El proceso ha sido cancelado."
                    : "No options were selected. The process has been cancelled.",

                MsgCompleted = isSpanish
                    ? $"{nameof(AteneaRibbonCommands.ProcessAteneaModelChecker)} finalizado correctamente."
                    : $"{nameof(AteneaRibbonCommands.ProcessAteneaModelChecker)} completed successfully.",

                MsgTitle = isSpanish
                    ? "Proceso completado"
                    : "Process Complete"
            };
        }

        [CommandMethod(AteneaRibbonCommands.ButtonAteneaModelChecker)]
        public static void ButtonAteneaModelChecker()
        {
            // try
            try
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

                // ---------------------------------
                // Form Opciones
                // ---------------------------------

                List<string> selectedOptions = cls_00_InstaForm_CheckedListBox.CheckListBoxFormSearchOut(
                    uiTexts.Title, ModelCheckerKeys.GetAllOptionsAtenea(isSpanish).OrderBy(x => x).ToList(),
                    ModelCheckerKeys.GetDefaultSelectedOptionsAtenea(isSpanish)
                );
                // Validamos
                if (selectedOptions == null || selectedOptions.Count == 0)
                {
                    // Mensaje
                    MessageBox.Show(
                        uiTexts.MsgNoOptions, "Information",
                        MessageBoxButtons.OK, MessageBoxIcon.Information
                    );
                    // Finalizamos
                    return;
                }

                // -------------------------------
                // Ejecutamos
                // -------------------------------

                cls_00_MainAteneaModelChecker.MainAteneaModelChecker(
                    sessionInfo, selectedOptions, uiTexts, isSpanish
                );
            }
            // catch
            catch (Exception ex)
            {
                // Mensaje
                MessageBox.Show(
                    "An unexpected error occurred while executing Atenea Model Checker.\n\n" +
                    ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error
                );
            }

        }


    }
}
