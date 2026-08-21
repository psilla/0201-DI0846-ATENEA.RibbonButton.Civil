using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.Buttons;
using TYPSA.SharedLib.Civil.JsonTools;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.UserForms;
using static TYPSA.PS.RibbonButton.Civil.cls_00_ParamImpMainWeb_Async;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_ButtonAteneaParamDataImp
    {
        private static bool ValidateJsonData(
            Root dataByModelFromJson,
            bool isSpanish
        )
        {
            // Validamos
            if (dataByModelFromJson == null)
            {
                MessageBox.Show(
                    isSpanish
                        ? "No se ha obtenido información de los modelos."
                        : "No model information was retrieved.",
                    isSpanish ? "Información no disponible" : "Information unavailable",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning
                );

                return false;
            }

            return true;
        }

        private static bool ValidateDataByFileName(
            Root dataByModelFromJson,
            bool isSpanish
        )
        {
            // Validamos
            if (dataByModelFromJson.DataByFileName == null || dataByModelFromJson.DataByFileName.Count == 0)
            {
                MessageBox.Show(
                    isSpanish
                        ? "El JSON no contiene información de modelos en 'DataByFileName'."
                        : "The JSON does not contain model information in 'DataByFileName'.",
                    isSpanish ? "Información no encontrada" : "Information not found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning
                );

                return false;
            }

            return true;
        }

        private static UiTexts GetUiTexts(bool isSpanish)
        {
            return new UiTexts
            {
                MsgCompleted = isSpanish
                    ? $"{nameof(RibbonCommands.ButtonAteneaParamDataImp)} finalizado correctamente."
                    : $"{nameof(RibbonCommands.ButtonAteneaParamDataImp)} completed successfully.",

                MsgTitle = isSpanish
                    ? "Proceso completado"
                    : "Process Complete"
            };
        }

        [CommandMethod(RibbonCommands.ButtonAteneaParamDataImp)]
        public static void PropImportBackFromJSON()
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
            // Texto UI según idioma
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

            string strUserName = dataFromAsyn.UserName;
            string strAteneaVersion = dataFromAsyn.AteneaVersion;
            Dictionary<string, object> jsonSetDataFromWeb = dataFromAsyn.JsonData;

            // -------------------------------
            // Cargar JSON Global desde API
            // -------------------------------

            Root dataByModelFromJson;
            // try
            try
            {
                // Obtenemos el objeto
                dataByModelFromJson = Task.Run(() => cls_00_LoadJsonFromApiPostAsync.LoadJsonFromApiPostAsync<Root>(
                    ateneaEndpoints.EndpointGetDataByModelUrl, projectCode, strUserName, strAteneaVersion, isSpanish
                )).GetAwaiter().GetResult();

                // -------------------------------
                // Validar data
                // -------------------------------

                if (!ValidateJsonData(dataByModelFromJson, isSpanish)) return;

                // -------------------------------
                // Validar key
                // -------------------------------

                if (!ValidateDataByFileName(dataByModelFromJson, isSpanish)) return;
            }
            // catch
            catch (System.Exception ex)
            {
                // Mensaje
                MessageBox.Show(
                    $"Unexpected error while loading the JSON:\n{ex.Message}", "Unexpected Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                // Finalizamos
                return;
            }

            // -------------------------------
            // Ejecutamos
            // -------------------------------

            cls_00_MainAteneaParamDataImp.MainAteneaParamDataImp(
                selectedFiles.ToArray(), projectCode, startTime, info, ateneaEndpoints,
                uiTexts, dataByModelFromJson, jsonSetDataFromWeb, isSpanish
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
