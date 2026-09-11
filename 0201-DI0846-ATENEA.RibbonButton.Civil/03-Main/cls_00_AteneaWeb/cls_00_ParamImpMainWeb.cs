using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;
using TYPSA.PS.RibbonButton.Civil.Source.Class.Main;
using TYPSA.SharedLib.Civil.SetDataFromJson;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.UserForms;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_ParamImpMainWeb
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
                formTitle, existingPsetNamesInSet
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
                formTitle, existingPropertyNames
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
