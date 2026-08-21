using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.Buttons;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using Exception = System.Exception;

namespace TYPSA.PS.RibbonButton.Civil
{
    public class UiTexts
    {
        public string Title { get; set; }
        public string MsgNoOptions { get; set; }
        public string MsgCompleted { get; set; }
        public string MsgTitle { get; set; }
    }

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
                    ? $"{nameof(RibbonCommands.ProcessAteneaModelChecker)} finalizado correctamente."
                    : $"{nameof(RibbonCommands.ProcessAteneaModelChecker)} completed successfully.",

                MsgTitle = isSpanish
                    ? "Proceso completado"
                    : "Process Complete"
            };
        }

        [CommandMethod(RibbonCommands.ButtonAteneaModelChecker)]
        public static void ButtonAteneaModelChecker()
        {
            // try
            try
            {
                // ---------------------------------
                // Obtener datos de usuario
                // ---------------------------------

                bool userData = cls_00_GetUserData.GetUserData(
                    out string projectCode, out List<string> selectedFiles,
                    out string selectedFolderPath, out DateTime startTime,
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

                // ---------------------------------
                // Form Opciones
                // ---------------------------------

                List<string> selectedOptions = cls_00_InstaForm_CheckedListBox.CheckListBoxFormSearchOut(
                    uiTexts.Title, ModelCheckerKeys.GetAllOptionsAteneaCivilCustom(isSpanish).OrderBy(x => x).ToList(),
                    ModelCheckerKeys.GetDefaultSelectedOptionsAteneaCivilCustom(isSpanish)
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
                    selectedFiles.ToArray(), projectCode, selectedOptions, 
                    startTime, info, ateneaEndpoints, uiTexts, isSpanish
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
