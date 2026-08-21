using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using TYPSA.SharedLib.Civil.SetDataFromJson;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_ParamImpMainWeb_Async
    {
        private static void ShowPropertySetsNotFoundMessage(bool isSpanish)
        {
            MessageBox.Show(
                isSpanish
                    ? "El Set Web no contiene Property Sets disponibles."
                    : "The Web Set does not contain any available Property Sets.",
                isSpanish
                    ? "Property Sets no encontrados"
                    : "Property Sets not found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        private static void ShowSelectedPropertySetsDataNotFoundMessage(bool isSpanish)
        {
            MessageBox.Show(
                isSpanish
                    ? "No se han encontrado datos para los Property Sets seleccionados."
                    : "No data was found for the selected Property Sets.",
                isSpanish
                    ? "Property Sets no encontrados"
                    : "Property Sets not found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }

        private static void ShowPropertiesNotFoundMessage(bool isSpanish)
        {
            MessageBox.Show(
                isSpanish
                    ? "Los Property Sets seleccionados no contienen propiedades disponibles."
                    : "The selected Property Sets do not contain any available properties.",
                isSpanish
                    ? "Propiedades no encontradas"
                    : "Properties not found",
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

        private static void ShowNoParameterSetMessage(
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


        public class ParamImpPreparedData
        {
            public string CivilVersion { get; set; }
            public string UserName { get; set; }
            public string CivilLanguage { get; set; }
            public string DateTimeNow { get; set; }
            public string AteneaVersion { get; set; }
            public Dictionary<string, object> JsonData { get; set; }
            public int CurrentSetStatus { get; set; }
        }

        public static ParamImpPreparedData ParamImpMainWeb_Async(
            string projectCode,
            CadSessionInfo info,
            cls_00_AteneaEndPointsCivil ateneaEndpoints,
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

            string keySoftwareVersion = cls_00_AteneaJson.CivilVersion;
            string keySoftwareLanguage = cls_00_AteneaJson.CivilLanguage;
            string keyParamCheck = cls_00_AteneaJson.CivilParamCheck;
            string strEndpointProjectDataUrl = ateneaEndpoints.EndpointProjectDataUrl;
            string strEndpointValidateSetUrl = ateneaEndpoints.EndpointValidateSetUrl;
            string strEndpointGetSetUrl = ateneaEndpoints.EndpointGetSetUrl;
            string strUserName = info.UserName;
            string strAteneaVersion = info.AteneaVersion;

            // -------------------------------
            // Validar Datos Proyecto
            // -------------------------------

            Dictionary<string, object> dictProjectDataToVal = GetProjectDataDictionary(projectCode, softwareLanguage);
#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026
            // Validamos Datos de Proyecto
            bool isValid = cls_00_ValidateProjectInfo.ValidateProjectDataAsync(
                strEndpointProjectDataUrl, dictProjectDataToVal, keySoftwareVersion, keySoftwareLanguage, isSpanish
            ).GetAwaiter().GetResult();
            // Validamos
            if (!isValid) return null;
#endif

            // -------------------------------
            // Validar Status Set de Parametros
            // -------------------------------

            Dictionary<string, object> dictSetToVal = GetSetStatusDictionary(projectCode);
#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026
            // Validamos 
            int currentSetStatus = cls_00_ValidateParamSetStatus.ValidateSetStatusAsync(
                dictSetToVal, strEndpointValidateSetUrl, isSpanish
            ).GetAwaiter().GetResult();
            // Validamos
            if (currentSetStatus == -1) return null;
#endif

            // -------------------------------
            // Validar Status Set de Parametros
            // -------------------------------

            if (currentSetStatus != 3)
            {
                ShowNoParameterSetMessage(isSpanish);
                return null;
            }

            // -------------------------------
            // Cargar Set JSON desde API
            // -------------------------------

            Dictionary<string, object> jsonSetDataFromWeb = null;
#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026
            // try
            try
            {
                // Obtenemos el objeto desde el JSON
                jsonSetDataFromWeb = cls_00_LoadJsonFromApiPostAsync.LoadJsonFromApiPostAsync<Dictionary<string, object>>(
                    strEndpointGetSetUrl, projectCode, strUserName, strAteneaVersion, isSpanish
                ).GetAwaiter().GetResult();

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
                    isSpanish ? "Error inesperado" : "Unexpected Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error
                );
                // Finalizamos
                return null;
            }

            // return
            return new ParamImpPreparedData
            {
                CivilVersion = info.CivilVersion,
                UserName = info.UserName,
                CivilLanguage = info.CivilLanguage,
                DateTimeNow = info.DateTimeNow,
                AteneaVersion = info.AteneaVersion,
                JsonData = jsonSetDataFromWeb,
                CurrentSetStatus = currentSetStatus
            };
        }
#endif

        private static List<string> GetSelectedPsets(
            List<string> existingPsetNamesInSet,
            bool isSpanish
        )
        {
            // -------------------------------
            // Obtener informacion
            // -------------------------------

            string formTitle = isSpanish
                ? "Selecciona los Property Sets que quieres analizar en la web"
                : "Select the Property Sets you want to analyze on the web";
            // return
            return cls_00_InstaForm_CheckedListBox.CheckListBoxFormSearchOut(
                formTitle, existingPsetNamesInSet, existingPsetNamesInSet
            );
        }

        private static List<string> GetSelectedProperties(
            List<string> existingPropertyNames,
            bool isSpanish
        )
        {
            // -------------------------------
            // Obtener informacion
            // -------------------------------

            string formTitle = isSpanish
                ? "Selecciona las Properties que quieres analizar en la web"
                : "Select the Properties you want to analyze on the web";
            // return
            return cls_00_InstaForm_CheckedListBox.CheckListBoxFormSearchOut(
                formTitle, existingPropertyNames, existingPropertyNames
            );
        }

        private static List<Dictionary<string, object>> GetFilteredPsetsFromJson(
            List<Dictionary<string, object>> civilParamCheckFromSet,
            List<string> selectedPsets
        )
        {
            // -------------------------------
            // Obtener informacion
            // -------------------------------

            string keyPsetName = cls_00_AteneaJson.PsetName;

            // Creamos set para filtrar
            HashSet<string> selectedPsetsSet = new HashSet<string>(
                selectedPsets, StringComparer.OrdinalIgnoreCase
            );

            // Filtramos
            List<Dictionary<string, object>> filteredPsets = civilParamCheckFromSet
                .Where(x =>
                    x.ContainsKey(keyPsetName) && x[keyPsetName] != null &&
                    selectedPsetsSet.Contains(x[keyPsetName].ToString())
                ).ToList();

            // return
            return filteredPsets;
        }

        private static List<string> GetPropertyNamesFromFilteredPsets(
            List<Dictionary<string, object>> filteredPsets
        )
        {
            // -------------------------------
            // Definir keys
            // -------------------------------

            string keyPsetData = cls_00_AteneaJson.PsetData;
            string keyPropName = cls_00_AteneaJson.PropName;

            // -------------------------------
            // Obtener Properties
            // -------------------------------

            List<string> propertyNames = new List<string>();
            // Iteramos
            foreach (Dictionary<string, object> pset in filteredPsets)
            {
                // Validamos
                if (!pset.ContainsKey(keyPsetData) || pset[keyPsetData] == null) continue;

                // Obtenemos datos
                List<Dictionary<string, object>> pSetDataFromJson =
                    JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(pset[keyPsetData].ToString());
                // Validamos
                if (pSetDataFromJson == null || pSetDataFromJson.Count == 0) continue;

                // Obtenemos nombres
                foreach (Dictionary<string, object> prop in pSetDataFromJson)
                {
                    // Validamos
                    if (!prop.ContainsKey(keyPropName) || prop[keyPropName] == null) continue;
                    // Añadimos
                    propertyNames.Add(prop[keyPropName].ToString());
                }
            }

            // -------------------------------
            // Eliminar duplicados y ordenar
            // -------------------------------

            propertyNames = propertyNames.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

            // return
            return propertyNames;
        }

        private static List<string> GetEntitiesToApplyFromJson(
            List<Dictionary<string, object>> categories
        )
        {
            List<string> entitiesToApply = new List<string>();
            // Iteramos categorías
            foreach (Dictionary<string, object> category in categories)
            {
                // Validamos nombre
                if (!category.ContainsKey(cls_00_AteneaJson.CategoryName) || category[cls_00_AteneaJson.CategoryName] == null)
                {
                    continue;
                }

                // Validamos valor
                if (!category.ContainsKey(cls_00_AteneaJson.CategoryValue) || category[cls_00_AteneaJson.CategoryValue] == null)
                {
                    continue;
                }

                // Obtenemos valor bool
                bool categoryValue = Convert.ToBoolean(category[cls_00_AteneaJson.CategoryValue]);

                // Si esta activo
                if (categoryValue)
                {
                    // Añadimos
                    entitiesToApply.Add(category[cls_00_AteneaJson.CategoryName].ToString());
                }
            }

            // return
            return entitiesToApply;
        }

        private static Dictionary<string, SelectedPsetData> GetSelectedPsetData(
            List<Dictionary<string, object>> filteredPsets,
            List<string> selectedProperties,
            Dictionary<string, Autodesk.Aec.PropertyData.DataType> dataTypeDictFiltered
        )
        {
            // -------------------------------
            // Definir keys
            // -------------------------------

            string keyPsetName = cls_00_AteneaJson.PsetName;
            string keyPsetData = cls_00_AteneaJson.PsetData;
            string keyCategories = cls_00_AteneaJson.Categories;

            // -------------------------------
            // Crear set Properties seleccionadas
            // -------------------------------

            HashSet<string> selectedPropertiesSet = new HashSet<string>(
                selectedProperties, StringComparer.OrdinalIgnoreCase
            );

            // -------------------------------
            // Crear diccionario
            // -------------------------------

            Dictionary<string, SelectedPsetData> selectedPsetData =
                new Dictionary<string, SelectedPsetData>(StringComparer.OrdinalIgnoreCase);

            // Iteramos
            foreach (Dictionary<string, object> pset in filteredPsets)
            {
                // -------------------------------
                // Validar informacion
                // -------------------------------

                if (!pset.ContainsKey(keyPsetName) || pset[keyPsetName] == null) continue;
                if (!pset.ContainsKey(keyPsetData) || pset[keyPsetData] == null) continue;
                if (!pset.ContainsKey(keyCategories) || pset[keyCategories] == null) continue;

                // -------------------------------
                // Obtener nombre
                // -------------------------------

                string psetName = pset[keyPsetName].ToString();

                // -------------------------------
                // Obtener Properties
                // -------------------------------

                List<Dictionary<string, object>> pSetDataFromJson = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                    pset[keyPsetData].ToString()
                );
                // Validamos
                if (pSetDataFromJson == null || pSetDataFromJson.Count == 0) continue;

                Dictionary<string, Autodesk.Aec.PropertyData.DataType> properties = cls_00_GetDataParamCheckImp.GetPropFromJson(
                    pSetDataFromJson, dataTypeDictFiltered
                );

                // Filtramos Properties seleccionadas
                properties = properties.Where(x => selectedPropertiesSet.Contains(x.Key))
                    .ToDictionary(
                        x => x.Key,
                        x => x.Value,
                        StringComparer.OrdinalIgnoreCase
                    );
                // Validamos
                if (properties.Count == 0) continue;

                // -------------------------------
                // Obtener Categories
                // -------------------------------

                List<Dictionary<string, object>> categoriesFromJson = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                    pset[keyCategories].ToString()
                );
                // Validamos
                if (categoriesFromJson == null || categoriesFromJson.Count == 0) continue;

                List<string> categories = GetEntitiesToApplyFromJson(categoriesFromJson);
                // Validamos
                if (categories == null || categories.Count == 0) continue;

                // -------------------------------
                // Añadir informacion
                // -------------------------------

                selectedPsetData[psetName] = new SelectedPsetData
                {
                    Categories = categories.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList(),
                    Properties = properties
                };
            }

            // return
            return selectedPsetData;
        }

        public static HashSet<string> GetPsetNames(
            List<Dictionary<string, object>> psets,
            string keyPsetName
        )
        {
            return new HashSet<string>(
                psets
                    .Where(x =>
                        x.ContainsKey(keyPsetName)
                        && x[keyPsetName] != null
                        && !string.IsNullOrWhiteSpace(x[keyPsetName].ToString())
                    )
                    .Select(x => x[keyPsetName].ToString()),
                StringComparer.OrdinalIgnoreCase
            );
        }

        public static bool TryGetFiltPsetsDataFromUserSelection(
            List<Dictionary<string, object>> civilParamCheckFromSet,
            string keyPsetName,
            Dictionary<string, Autodesk.Aec.PropertyData.DataType> dataTypeDictFiltered,
            bool isSpanish,
            out Dictionary<string, SelectedPsetData> selectedPsetData
        )
        {
            selectedPsetData = null;

            // -------------------------------
            // Obtener Property Sets del Set Web
            // -------------------------------

            List<string> existingPsetNamesInSet = GetPsetNames(
                civilParamCheckFromSet, keyPsetName
            ).OrderBy(x => x).ToList();
            // Validamos
            if (existingPsetNamesInSet.Count == 0)
            {
                ShowPropertySetsNotFoundMessage(isSpanish);
                return false;
            }

            // -------------------------------
            // Seleccion personalizada pSets del JSON
            // -------------------------------

            List<string> selectedPsets = GetSelectedPsets(existingPsetNamesInSet, isSpanish);
            // Validamos
            if (selectedPsets == null || selectedPsets.Count == 0) return false;

            // -------------------------------
            // Filtrar Property Sets seleccionados
            // -------------------------------

            List<Dictionary<string, object>> filteredPsets = GetFilteredPsetsFromJson(
                civilParamCheckFromSet, selectedPsets
            );
            // Validamos
            if (filteredPsets == null || filteredPsets.Count == 0)
            {
                ShowSelectedPropertySetsDataNotFoundMessage(isSpanish);
                return false;
            }

            // -------------------------------
            // Obtener Properties
            // -------------------------------

            List<string> propertyNamesFromSelectedPsets = GetPropertyNamesFromFilteredPsets(filteredPsets);
            // Validamos
            if (propertyNamesFromSelectedPsets == null || propertyNamesFromSelectedPsets.Count == 0)
            {
                ShowPropertiesNotFoundMessage(isSpanish);
                return false;
            }

            // -------------------------------
            // Seleccion personalizada Properties
            // -------------------------------

            List<string> selectedProperties = GetSelectedProperties(propertyNamesFromSelectedPsets, isSpanish);
            // Validamos
            if (selectedProperties == null || selectedProperties.Count == 0) return false;

            // -------------------------------
            // Relacionar Psets con Properties seleccionadas
            // -------------------------------

            selectedPsetData = GetSelectedPsetData(filteredPsets, selectedProperties, dataTypeDictFiltered);
            // Validamos
            if (selectedPsetData == null || selectedPsetData.Count == 0)
            {
                ShowPropertiesNotFoundMessage(isSpanish);
                return false;
            }

            // return
            return true;
        }


    }

}
