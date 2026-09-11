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
    public class ParamWebPreparedData
    {
        //public string AccessToken { get; set; }
        public Dictionary<string, object> JsonData { get; set; }
        public int CurrentSetStatus { get; set; }
    }

    public class ParamPreparedData
    {
        public string CivilVersion { get; set; }
        public string UserNameBySso { get; set; }
        public string CivilLanguage { get; set; }
        public string DateTimeNow { get; set; }
        public string AteneaVersion { get; set; }
        public Dictionary<string, object> JsonData { get; set; }
        public List<Dictionary<string, object>> CivilParamCheckFromSet { get; set; }
        public int CurrentSetStatus { get; set; }
    }

    public class ParamExpPreparedData : ParamPreparedData
    {
    }

    public class ParamImpPreparedData : ParamPreparedData
    {
    }

    internal class cls_00_PrepareParamWebDataAsync
    {
        private static void ShowNoParameterSetMessageExp(
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
                MessageBoxButtons.OK, MessageBoxIcon.Warning
            );
        }

        private static void ShowNoParameterSetMessageImp(
            bool isSpanish
        )
        {
            // Mensaje
            MessageBox.Show(
                isSpanish
                ? "No hay un Set de Parámetros validado disponible.\n" +
                  "No se procederá a la importación hasta que el set sea validado nuevamente por el responsable de proyecto."
                : "There is no validated Parameter Set available.\n" +
                  "The import will not proceed until the set has been revalidated by the Project Manager.",
                isSpanish
                ? "Set de Parámetros No Validado"
                : "Parameter Set Not Validated",
                MessageBoxButtons.OK, MessageBoxIcon.Warning
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

        private static async Task<ParamWebPreparedData> PrepareParamWebDataAsync(
            string projectCode,
            CadSessionInfo infoCad,
            cls_00_AteneaEndPointsCivil ateneaEndpoints,
            string selectedEndpoint,
            bool isSpanish
        )
        {
            // -------------------------------
            // Normalizar idioma
            // -------------------------------

            string softwareLanguage = isSpanish ? "Spanish" : "English";

            // -------------------------------
            // Obtener informacion
            // -------------------------------

            string keyParamCheck = cls_00_AteneaJson.CivilParamCheck;
            string strEndpointProjectDataUrl = ateneaEndpoints.EndpointProjectDataUrl;
            string strEndpointValidateSetUrl = ateneaEndpoints.EndpointValidateSetUrl;
            string strAteneaVersion = infoCad.AteneaVersion;

            string strAccessToken = cls_00_AteneaSession.AccessToken;
            string strEmail = cls_00_AteneaSession.Email;

            // -------------------------------
            // Validar Datos Proyecto
            // -------------------------------

            Dictionary<string, object> dictProjectDataToVal = GetProjectDataDictionarySSO(
                projectCode, softwareLanguage, infoCad
            );

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026

            bool isValid = await cls_00_PostProjectInfo.ValidateProjectDataAsyncSSO(
                strEndpointProjectDataUrl, dictProjectDataToVal, isSpanish, strAccessToken
            );
            // Validamos
            if (!isValid) return null;
#endif

            // -------------------------------
            // Validar Status Set de Parametros
            // -------------------------------

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026

            int currentSetStatus = await cls_00_PostParamSetStatus.PostSetStatusAsync(
                projectCode, strAteneaVersion, strEndpointValidateSetUrl, isSpanish, strAccessToken
            );
            // Validamos
            if (currentSetStatus == -1) return null;
#endif

            // -------------------------------
            // Cargar Set JSON desde API
            // -------------------------------

            Dictionary<string, object> jsonSetDataFromWeb = null;

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026

            // try
            try
            {
                jsonSetDataFromWeb = await cls_00_PostParamSet.PostSetAsync<Dictionary<string, object>>(
                    selectedEndpoint, projectCode, strAteneaVersion, isSpanish, strAccessToken
                );

                // -------------------------------
                // Validar data
                // -------------------------------

                if (!ValidateJsonData(jsonSetDataFromWeb, keyParamCheck, isSpanish)) return null;
            }
            // catch
            catch (Exception ex)
            {
                MessageBox.Show(
                    isSpanish
                        ? $"Error inesperado al cargar el JSON:\n{ex.Message}"
                        : $"Unexpected error while loading the JSON:\n{ex.Message}",
                    isSpanish
                        ? "Error inesperado"
                        : "Unexpected Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                return null;
            }
#endif
            // -------------------------------
            // Return
            // -------------------------------

            return new ParamWebPreparedData
            {
                //AccessToken = strAccessToken,
                JsonData = jsonSetDataFromWeb,
                CurrentSetStatus = currentSetStatus
            };
        }

        public static async Task<ParamExpPreparedData> ParamExpMainWebAsync(
            string projectCode,
            CadSessionInfo infoCad,
            cls_00_AteneaEndPointsCivil ateneaEndpoints,
            string selectedEndpoint,
            bool isSpanish,
            bool validateExistingSet
        )
        {
            // -------------------------------
            // Obtener datos comunes
            // -------------------------------

            ParamWebPreparedData preparedData = await PrepareParamWebDataAsync(
                projectCode, infoCad, ateneaEndpoints, selectedEndpoint, isSpanish
            );
            // Validamos
            if (preparedData == null) return null;

            // -------------------------------
            // Validar existencia Set
            // -------------------------------

            if (validateExistingSet)
            {
                // -------------------------------
                // Validar Status Set de Parametros
                // -------------------------------

                if (preparedData.CurrentSetStatus != 2 && preparedData.CurrentSetStatus != 3)
                {
                    ShowNoParameterSetMessageExp(isSpanish);
                    return null;
                }
            }

            // -------------------------------
            // Obtener Civil Param Check
            // -------------------------------

            List<Dictionary<string, object>> civilParamCheckFromSet = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                preparedData.JsonData[cls_00_AteneaJson.CivilParamCheck].ToString()
            ) ?? new List<Dictionary<string, object>>();

            // -------------------------------
            // Validar existencia Set
            // -------------------------------

            if (validateExistingSet && civilParamCheckFromSet.Count == 0)
            {
                ShowNoParameterSetMessageExp(isSpanish);
                return null;
            }

            // -------------------------------
            // Return
            // -------------------------------

            return new ParamExpPreparedData
            {
                CivilVersion = infoCad.CivilVersion,
                //UserNameBySso = preparedData.AccessToken,
                CivilLanguage = infoCad.CivilLanguage,
                DateTimeNow = infoCad.DateTimeNow,
                AteneaVersion = infoCad.AteneaVersion,
                JsonData = preparedData.JsonData,
                CivilParamCheckFromSet = civilParamCheckFromSet,
                CurrentSetStatus = preparedData.CurrentSetStatus
            };
        }

        public static async Task<ParamImpPreparedData> ParamImpMainWeb_Async(
            string projectCode,
            CadSessionInfo infoCad,
            cls_00_AteneaEndPointsCivil ateneaEndpoints,
            bool isSpanish
        )
        {
            // -------------------------------
            // Obtener datos comunes
            // -------------------------------

            ParamWebPreparedData preparedData = await PrepareParamWebDataAsync(
                projectCode, infoCad, ateneaEndpoints, ateneaEndpoints.EndpointGetSetUrl, isSpanish
            );
            // Validamos
            if (preparedData == null) return null;

            // -------------------------------
            // Validar Status Set de Parametros
            // -------------------------------

            if (preparedData.CurrentSetStatus != 3)
            {
                ShowNoParameterSetMessageImp(isSpanish);
                return null;
            }

            // -------------------------------
            // Obtener Civil Param Check
            // -------------------------------

            List<Dictionary<string, object>> civilParamCheckFromSet = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                preparedData.JsonData[cls_00_AteneaJson.CivilParamCheck].ToString()
            ) ?? new List<Dictionary<string, object>>();

            // -------------------------------
            // Validar existencia Set
            // -------------------------------

            if (civilParamCheckFromSet.Count == 0)
            {
                ShowNoParameterSetMessageImp(isSpanish);
                return null;
            }

            // -------------------------------
            // Return
            // -------------------------------

            return new ParamImpPreparedData
            {
                CivilVersion = infoCad.CivilVersion,
                //UserNameBySso = preparedData.AccessToken,
                CivilLanguage = infoCad.CivilLanguage,
                DateTimeNow = infoCad.DateTimeNow,
                AteneaVersion = infoCad.AteneaVersion,
                JsonData = preparedData.JsonData,
                CivilParamCheckFromSet = civilParamCheckFromSet,
                CurrentSetStatus = preparedData.CurrentSetStatus
            };
        }

        


    }
}
