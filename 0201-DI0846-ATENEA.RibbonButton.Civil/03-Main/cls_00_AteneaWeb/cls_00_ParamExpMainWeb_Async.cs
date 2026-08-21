using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.EndPoints;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;

namespace TYPSA.PS.RibbonButton.Civil
{
    public class ParamExpPreparedData
    {
        public string CivilVersion { get; set; }
        public string UserName { get; set; }
        public string CivilLanguage { get; set; }
        public string DateTimeNow { get; set; }
        public string AteneaVersion { get; set; }
        public Dictionary<string, object> JsonData { get; set; }
        public List<Dictionary<string, object>> CivilParamCheckFromSet { get; set; }
        public int CurrentSetStatus { get; set; }
    }

    internal class cls_00_ParamExpMainWeb_Async
    {
        private static void ShowNoParameterSetMessage(
            bool isSpanish
        )
        {
            MessageBox.Show(
                isSpanish
                    ? "No existe un Conjunto de Parámetros.\n\n" +
                      "Como no se ha creado ni validado ningún Conjunto de Parámetros, el sistema no puede determinar\n" +
                      "qué parámetros deben exportarse desde los modelos seleccionados."
                    : "There is no existing Parameter Set.\n\n" +
                      "Since no Parameter Set has been created or validated, the system cannot determine\n" +
                      "which parameters should have their values exported from the selected models.",
                isSpanish
                    ? "Conjunto de Parámetros No Disponible"
                    : "Parameter Set Not Available",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        private static bool ValidateJsonData(
            Dictionary<string, object> jsonData,
            string keyParamCheck,
            bool isSpanish
        )
        {
            // -------------------------------
            // Validar data
            // -------------------------------

            if (jsonData == null || jsonData.Count == 0)
            {
                MessageBox.Show(
                    isSpanish
                        ? "No se ha obtenido información del Set de parámetros."
                        : "No parameter Set information was retrieved.",
                    isSpanish ? "Set no disponible" : "Set unavailable",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning
                );

                return false;
            }

            // -------------------------------
            // Validar key
            // -------------------------------

            if (!jsonData.ContainsKey(keyParamCheck) || jsonData[keyParamCheck] == null)
            {
                MessageBox.Show(
                    isSpanish
                        ? $"El JSON no contiene la propiedad '{keyParamCheck}'."
                        : $"The JSON does not contain the property '{keyParamCheck}'.",
                    isSpanish ? "Información no encontrada" : "Information not found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning
                );

                return false;
            }

            return true;
        }

        public static async Task<ParamExpPreparedData> ParamExpMainWebAsync(
            string projectCode,
            CadSessionInfo info,
            cls_00_AteneaEndPointsCivil ateneaEndpoints,
            string selectedEndpoint,
            bool isSpanish,
            bool validateExistingSet
        )
        {
            // -------------------------------
            // Normalizar idioma
            // -------------------------------

            string softwareLanguage = isSpanish ? "Spanish" : "English";

            // -------------------------------
            // Obtener informacion
            // -------------------------------

            string keySoftwareVersion = cls_00_AteneaJson.CivilVersion;
            string keySoftwareLanguage = cls_00_AteneaJson.CivilLanguage;
            string keyParamCheck = cls_00_AteneaJson.CivilParamCheck;

            string strEndpointProjectDataUrl = ateneaEndpoints.EndpointProjectDataUrl;
            string strEndpointValidateSetUrl = ateneaEndpoints.EndpointValidateSetUrl;

            string strUserName = info.UserName;
            string strAteneaVersion = info.AteneaVersion;

            // -------------------------------
            // Validar Datos Proyecto
            // -------------------------------

            Dictionary<string, object> dictProjectDataToVal = GetProjectDataDictionary(projectCode, softwareLanguage);

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026

            bool isValid = await cls_00_ValidateProjectInfo.ValidateProjectDataAsync(
                strEndpointProjectDataUrl, dictProjectDataToVal, keySoftwareVersion, keySoftwareLanguage, isSpanish
            );
            // Validamos
            if (!isValid) return null;
#endif

            // -------------------------------
            // Validar Status Set de Parametros
            // -------------------------------

            Dictionary<string, object> dictSetToVal = GetSetStatusDictionary(projectCode);
#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026

            // Validamos
            int currentSetStatus = await cls_00_ValidateParamSetStatus.ValidateSetStatusAsync(
                dictSetToVal, strEndpointValidateSetUrl, isSpanish
            );
            // Validamos
            if (currentSetStatus == -1) return null;
#endif

            // -------------------------------
            // Validar existencia Set
            // -------------------------------

            if (validateExistingSet)
            {
                // -------------------------------
                // Definir estado Set
                // -------------------------------

                bool hasExistingSet = currentSetStatus == 2 || currentSetStatus == 3;
                // Validamos
                if (!hasExistingSet)
                {
                    // Mostramos
                    ShowNoParameterSetMessage(isSpanish);
                    // Finalizamos
                    return null;
                }
            }

            // -------------------------------
            // Cargar Set JSON desde API
            // -------------------------------

            Dictionary<string, object> jsonSetDataFromWeb = null;
#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026

            // try
            try
            {
                // Obtenemos el JSON del Set desde la API
                jsonSetDataFromWeb = await cls_00_LoadJsonFromApiPostAsync.LoadJsonFromApiPostAsync<Dictionary<string, object>>(
                    selectedEndpoint, projectCode, strUserName, strAteneaVersion, isSpanish
                );

                // -------------------------------
                // Validar data
                // -------------------------------

                if (!ValidateJsonData(jsonSetDataFromWeb, keyParamCheck, isSpanish)) return null;
            }
            // catch
            catch (Exception ex)
            {
                // Mensaje
                MessageBox.Show(
                    isSpanish
                        ? $"Error inesperado al cargar el JSON:\n{ex.Message}"
                        : $"Unexpected error while loading the JSON:\n{ex.Message}",
                    isSpanish
                        ? "Error inesperado"
                        : "Unexpected Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );

                return null;
            }
#endif

            // -------------------------------
            // Obtener Civil Param Check
            // -------------------------------

            List<Dictionary<string, object>> civilParamCheckFromSet = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                jsonSetDataFromWeb[keyParamCheck].ToString()
            ) ?? new List<Dictionary<string, object>>();

            // -------------------------------
            // Validar existencia Set
            // -------------------------------

            if (validateExistingSet)
            {
                // Validamos
                if (civilParamCheckFromSet.Count == 0)
                {
                    // Mostramos
                    ShowNoParameterSetMessage(isSpanish);
                    // Finalizamos
                    return null;
                }
            }

            // -------------------------------
            // Return
            // -------------------------------

            return new ParamExpPreparedData
            {
                CivilVersion = info.CivilVersion,
                UserName = info.UserName,
                CivilLanguage = info.CivilLanguage,
                DateTimeNow = info.DateTimeNow,
                AteneaVersion = info.AteneaVersion,
                JsonData = jsonSetDataFromWeb,
                CivilParamCheckFromSet = civilParamCheckFromSet,
                CurrentSetStatus = currentSetStatus
            };
        }

        


    }
}
