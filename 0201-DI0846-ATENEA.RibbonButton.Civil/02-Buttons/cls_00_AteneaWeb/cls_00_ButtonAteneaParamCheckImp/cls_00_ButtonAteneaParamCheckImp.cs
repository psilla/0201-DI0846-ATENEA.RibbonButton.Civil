using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.Buttons;
using TYPSA.SharedLib.EndPoints;
using static TYPSA.PS.RibbonButton.Civil.cls_00_ParamImpMainWeb_Async;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_ButtonAteneaParamCheckImp
    {
        private static UiTexts GetUiTexts(bool isSpanish)
        {
            return new UiTexts
            {
                MsgCompleted = isSpanish
                    ? $"{nameof(RibbonCommands.ProcessAteneaParamCheckImp)} finalizado correctamente."
                    : $"{nameof(RibbonCommands.ProcessAteneaParamCheckImp)} completed successfully.",

                MsgTitle = isSpanish
                    ? "Proceso completado"
                    : "Process Complete"
            };
        }

        [CommandMethod(RibbonCommands.ButtonAteneaParamCheckImp)]
        public static void ButtonAteneaParamDataImp()
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

            // ---------------------------------
            // Llamar fase asyn
            // ---------------------------------

            ParamImpPreparedData dataFromAsyn = Task.Run(() => ParamImpMainWeb_Async(
                projectCode, info, ateneaEndpoints, isSpanish)).GetAwaiter().GetResult();
            // Validamos
            if (dataFromAsyn == null) return;

            // -------------------------------
            // Obtener informacion asyn
            // -------------------------------

            Dictionary<string, object> jsonSetDataFromWeb = dataFromAsyn.JsonData;

            // -------------------------------
            // Ejecutamos
            // -------------------------------

            cls_00_MainAteneaParamCheckImp.MainAteneaParamCheckImp(
                selectedFiles.ToArray(), projectCode, startTime, info, ateneaEndpoints, 
                uiTexts, jsonSetDataFromWeb, isSpanish
            );

            // -------------------------------
            // Resumen
            // -------------------------------

            DateTime endTime = DateTime.Now;
            TimeSpan duration = endTime - startTime;

            // Mensaje
            MessageBox.Show(
                uiTexts.MsgCompleted +
                "\nDuration: " + duration.ToString(@"hh\:mm\:ss") +
                "\nStarted at: " + startTime.ToString("HH:mm:ss") +
                "\nEnded at: " + endTime.ToString("HH:mm:ss"),
                uiTexts.Title, MessageBoxButtons.OK, MessageBoxIcon.Information
            );

        }
    }
}
