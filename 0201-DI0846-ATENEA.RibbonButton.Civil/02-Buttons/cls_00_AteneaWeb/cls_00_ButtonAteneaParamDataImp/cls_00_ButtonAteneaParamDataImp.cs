using Autodesk.AutoCAD.Runtime;
using TYPSA.SharedLib.Civil;
using TYPSA.SharedLib.EndPoints;
using static TYPSA.SharedLib.Autocad.cls_00_CadInfoHelper;
using static TYPSA.SharedLib.Civil.cls_00_UiTexts;
using System.Windows.Forms;
using System;
using System.Threading.Tasks;

namespace TYPSA.ATENEA.RibbonButton.Civil
{
    public class cls_00_ButtonAteneaParamDataImp
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
                    ? $"{nameof(AteneaRibbonCommands.ButtonAteneaParamDataImp)} finalizado correctamente."
                    : $"{nameof(AteneaRibbonCommands.ButtonAteneaParamDataImp)} completed successfully.",

                MsgTitle = isSpanish
                    ? "Proceso completado"
                    : "Process Complete"
            };
        }

        [CommandMethod(AteneaRibbonCommands.ButtonAteneaParamDataImp)]
        public static void ButtonAteneaParamDataImp()
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
            // Texto UI según idioma
            // ---------------------------------

            UiTexts uiTexts = GetUiTexts(isSpanish);

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026 || CIVIL2027

            // ---------------------------------
            // Preparar informacion Web
            // ---------------------------------

            ParamImpPreparedDataCivil dataFromAsyn = Task.Run(() =>
                cls_00_PrepareParamWebDataAsyncCivil.ParamImpMainWeb_Async(sessionInfo, isSpanish)).GetAwaiter().GetResult();
            // Validamos
            if (dataFromAsyn == null) return;

#endif

            // -------------------------------
            // Cargar JSON Global desde API
            // -------------------------------

            cls_00_AteneaEndPointsCivil ateneaEndpoints = new cls_00_AteneaEndPointsCivil();
            Root dataByModelFromJson;
            // try
            try
            {
                // Obtenemos el objeto
                dataByModelFromJson = Task.Run(() => cls_00_PostParamSet.PostSetAsync<Root>(
                    sessionInfo, isSpanish, ateneaEndpoints.EndpointGetDataByModelUrl, dataFromAsyn.UserNameBySso
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
                sessionInfo, dataByModelFromJson, dataFromAsyn, isSpanish
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
