using Autodesk.AutoCAD.Runtime;
using TYPSA.SharedLib.EndPoints;
using static TYPSA.SharedLib.Autocad.cls_00_CadInfoHelper;
using static TYPSA.SharedLib.Civil.cls_00_UiTexts;
using System.Windows.Forms;
using System;
using System.Threading.Tasks;

namespace TYPSA.ATENEA.RibbonButton.Civil
{
    public class cls_00_ButtonAteneaParamCheckImp
    {
        private static UiTexts GetUiTexts(bool isSpanish)
        {
            return new UiTexts
            {
                MsgCompleted = isSpanish
                    ? $"{nameof(AteneaRibbonCommands.ProcessAteneaParamCheckImp)} finalizado correctamente."
                    : $"{nameof(AteneaRibbonCommands.ProcessAteneaParamCheckImp)} completed successfully.",

                MsgTitle = isSpanish
                    ? "Proceso completado"
                    : "Process Complete"
            };
        }

        [CommandMethod(AteneaRibbonCommands.ButtonAteneaParamCheckImp)]
        public static void ButtonAteneaParamCheckImp()
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
            // Ejecutamos
            // -------------------------------

            cls_00_MainAteneaParamCheckImp.MainAteneaParamCheckImp(
                sessionInfo, uiTexts, dataFromAsyn, isSpanish
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
