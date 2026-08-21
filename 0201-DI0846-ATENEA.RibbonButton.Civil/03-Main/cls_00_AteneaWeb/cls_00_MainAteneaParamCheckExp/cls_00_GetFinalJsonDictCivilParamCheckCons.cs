using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using TYPSA.SharedLib.EndPoints;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{

    internal class cls_00_GetFinalJsonDictCivilParamCheckCons
    {
        private static string NormalizeValue(object value)
        {
            if (value == null) return "";

            return JsonConvert.SerializeObject(value);
        }

        private static bool AreAllValuesEqual(List<object> values)
        {
            if (values == null || values.Count == 0) return false;

            string firstValue = NormalizeValue(values[0]);

            foreach (object value in values)
            {
                if (NormalizeValue(value) != firstValue)
                {
                    return false;
                }
            }

            return true;
        }

        private static object GetCommonValueOrReview(
            List<Dictionary<string, object>> items,
            string key,
            string reviewValue
        )
        {
            List<object> values = new List<object>();

            foreach (Dictionary<string, object> item in items)
            {
                if (!item.ContainsKey(key))
                {
                    values.Add(reviewValue);
                    continue;
                }

                values.Add(item[key]);
            }

            if (values.Count == 0) return reviewValue;

            return AreAllValuesEqual(values)
                ? values.FirstOrDefault()
                : reviewValue;
        }

        private static List<Dictionary<string, object>> ConsolidateCategories(
            List<Dictionary<string, object>> psets,
            string reviewValue
        )
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
            HashSet<string> allCategoryNames = new HashSet<string>();

            // -------------------------------
            // Obtener info
            // -------------------------------

            string keyCategories = cls_00_AteneaJson.Categories;
            string keyCategoryName = cls_00_AteneaJson.CategoryName;
            string keyCategoryValue = cls_00_AteneaJson.CategoryValue;

            foreach (Dictionary<string, object> pset in psets)
            {
                if (!pset.ContainsKey(keyCategories)) continue;

                List<Dictionary<string, object>> categories =
                    pset[keyCategories] as List<Dictionary<string, object>>;

                if (categories == null) continue;

                foreach (Dictionary<string, object> cat in categories)
                {
                    if (!cat.ContainsKey(keyCategoryName) || cat[keyCategoryName] == null) continue;

                    allCategoryNames.Add(cat[keyCategoryName].ToString());
                }
            }

            foreach (string categoryName in allCategoryNames.OrderBy(x => x))
            {
                List<object> values = new List<object>();

                foreach (Dictionary<string, object> pset in psets)
                {
                    if (!pset.ContainsKey(keyCategories)) continue;

                    List<Dictionary<string, object>> categories =
                        pset[keyCategories] as List<Dictionary<string, object>>;

                    if (categories == null) continue;

                    Dictionary<string, object> cat = categories
                        .FirstOrDefault(x =>
                            x.ContainsKey(keyCategoryName)
                            && x[keyCategoryName] != null
                            && x[keyCategoryName].ToString() == categoryName
                        );

                    if (cat != null && cat.ContainsKey(keyCategoryValue))
                    {
                        values.Add(cat[keyCategoryValue]);
                    }
                    else
                    {
                        values.Add(reviewValue);
                    }
                }

                object finalValue = AreAllValuesEqual(values)
                    ? values.FirstOrDefault()
                    : reviewValue;

                result.Add(new Dictionary<string, object>
                {
                    { keyCategoryName, categoryName },
                    { keyCategoryValue, finalValue }
                });
            }

            return result;
        }

        private static List<Dictionary<string, object>> ConsolidateCategories(
            List<Dictionary<string, object>> psets
        )
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
            HashSet<string> allCategoryNames = new HashSet<string>();

            // -------------------------------
            // Obtener info
            // -------------------------------

            string keyCategories = cls_00_AteneaJson.Categories;
            string keyCategoryName = cls_00_AteneaJson.CategoryName;
            string keyCategoryValue = cls_00_AteneaJson.CategoryValue;

            foreach (Dictionary<string, object> pset in psets)
            {
                if (!pset.ContainsKey(keyCategories)) continue;

                List<Dictionary<string, object>> categories =
                    pset[keyCategories] as List<Dictionary<string, object>>;

                if (categories == null) continue;

                foreach (Dictionary<string, object> cat in categories)
                {
                    if (!cat.ContainsKey(keyCategoryName) || cat[keyCategoryName] == null) continue;

                    allCategoryNames.Add(cat[keyCategoryName].ToString());
                }
            }

            foreach (string categoryName in allCategoryNames.OrderBy(x => x))
            {
                bool allTrue = true;

                foreach (Dictionary<string, object> pset in psets)
                {
                    if (!pset.ContainsKey(keyCategories))
                    {
                        allTrue = false;
                        break;
                    }

                    List<Dictionary<string, object>> categories =
                        pset[keyCategories] as List<Dictionary<string, object>>;

                    if (categories == null)
                    {
                        allTrue = false;
                        break;
                    }

                    Dictionary<string, object> cat = categories
                        .FirstOrDefault(x =>
                            x.ContainsKey(keyCategoryName)
                            && x[keyCategoryName] != null
                            && x[keyCategoryName].ToString() == categoryName
                        );

                    if (
                        cat == null ||
                        !cat.ContainsKey(keyCategoryValue) ||
                        !(cat[keyCategoryValue] is bool) ||
                        !(bool)cat[keyCategoryValue]
                    )
                    {
                        allTrue = false;
                        break;
                    }
                }

                if (!allTrue) continue;

                result.Add(new Dictionary<string, object>
                {
                    { keyCategoryName, categoryName },
                    { keyCategoryValue, true }
                });
            }

            return result;
        }

        private static List<Dictionary<string, object>> ConsolidatePsetData(
            List<Dictionary<string, object>> psets,
            string reviewValue
        )
        {
            List<Dictionary<string, object>> result = new List<Dictionary<string, object>>();
            HashSet<string> allPropNames = new HashSet<string>();

            // -------------------------------
            // Obtener info
            // -------------------------------

            string keyPropName = cls_00_AteneaJson.PropName;
            string keyPsetData = cls_00_AteneaJson.PsetData;
            string keyDataType = cls_00_AteneaJson.DataType;
            string keyPropDefaultValue = cls_00_AteneaJson.PropDefaultValue;
            string keyPropDescription = cls_00_AteneaJson.PropDescription;

            foreach (Dictionary<string, object> pset in psets)
            {
                if (!pset.ContainsKey(keyPsetData)) continue;

                List<Dictionary<string, object>> psetData =
                    pset[keyPsetData] as List<Dictionary<string, object>>;

                if (psetData == null) continue;

                foreach (Dictionary<string, object> prop in psetData)
                {
                    if (!prop.ContainsKey(keyPropName) || prop[keyPropName] == null) continue;

                    allPropNames.Add(prop[keyPropName].ToString());
                }
            }

            foreach (string propName in allPropNames.OrderBy(x => x))
            {
                List<Dictionary<string, object>> propsByName =
                    new List<Dictionary<string, object>>();

                foreach (Dictionary<string, object> pset in psets)
                {
                    if (!pset.ContainsKey(keyPsetData)) continue;

                    List<Dictionary<string, object>> psetData =
                        pset[keyPsetData] as List<Dictionary<string, object>>;

                    if (psetData == null) continue;

                    Dictionary<string, object> prop = psetData
                        .FirstOrDefault(x =>
                            x.ContainsKey(keyPropName)
                            && x[keyPropName] != null
                            && x[keyPropName].ToString() == propName
                        );

                    if (prop != null)
                    {
                        propsByName.Add(prop);
                    }
                }

                Dictionary<string, object> consolidatedProp =
                    new Dictionary<string, object>
                    {
                        { keyPropName, propName },
                        { keyDataType, GetCommonValueOrReview(propsByName, keyDataType, reviewValue) },
                        { keyPropDefaultValue, GetCommonValueOrReview(propsByName, keyPropDefaultValue, reviewValue) },
                        { keyPropDescription, GetCommonValueOrReview(propsByName, keyPropDescription, reviewValue) }
                    };

                result.Add(consolidatedProp);
            }

            return result;
        }

        public static Dictionary<string, object> GetFinalJsonPsetSet(
            string projectCode,
            string civilLanguage,
            List<Dictionary<string, object>> dataJsonByModel
        )
        {
            string reviewValue = "revisar";

            // -------------------------------
            // Obtener info
            // -------------------------------

            string keyParamCheck = cls_00_AteneaJson.CivilParamCheck;
            string keyPsetName = cls_00_AteneaJson.PsetName;
            string keyPsetData = cls_00_AteneaJson.PsetData;
            string keyCategories = cls_00_AteneaJson.Categories;

            // -----------------------------
            // Agrupar Psets por nombre
            // -----------------------------

            Dictionary<string, List<Dictionary<string, object>>> psetsByName =
                new Dictionary<string, List<Dictionary<string, object>>>();

            foreach (Dictionary<string, object> fileData in dataJsonByModel)
            {
                if (!fileData.ContainsKey(keyParamCheck)) continue;

                List<Dictionary<string, object>> psets =
                    fileData[keyParamCheck] as List<Dictionary<string, object>>;

                if (psets == null) continue;

                foreach (Dictionary<string, object> pset in psets)
                {
                    if (!pset.ContainsKey(keyPsetName) || pset[keyPsetName] == null) continue;

                    string psetName = pset[keyPsetName].ToString();

                    if (!psetsByName.ContainsKey(psetName))
                    {
                        psetsByName[psetName] = new List<Dictionary<string, object>>();
                    }

                    psetsByName[psetName].Add(pset);
                }
            }

            // -----------------------------
            // Crear CivilParamCheck consolidado
            // -----------------------------

            List<Dictionary<string, object>> civilParamCheck =
                new List<Dictionary<string, object>>();

            foreach (KeyValuePair<string, List<Dictionary<string, object>>> kvp in psetsByName)
            {
                string psetName = kvp.Key;
                List<Dictionary<string, object>> psets = kvp.Value;

                // -----------------------------
                // Consolidar categorías
                // -----------------------------

                List<Dictionary<string, object>> consolidatedCategories = ConsolidateCategories(psets);

                // -----------------------------
                // Consolidar propiedades
                // -----------------------------

                List<Dictionary<string, object>> consolidatedPsetData = ConsolidatePsetData(psets, reviewValue);

                // -----------------------------
                // Crear objeto PSet consolidado
                // -----------------------------

                Dictionary<string, object> consolidatedPset =
                    new Dictionary<string, object>
                    {
                        { keyPsetName, psetName },
                        { keyCategories, consolidatedCategories },
                        { keyPsetData, consolidatedPsetData }
                    };

                civilParamCheck.Add(consolidatedPset);
            }

            // -----------------------------
            // Crear JSON final
            // -----------------------------

            Dictionary<string, object> baseDict = GetProjectDataDictionary(projectCode, civilLanguage);

            // Añadimos info
            baseDict.Add(keyParamCheck,civilParamCheck);

            // return
            return baseDict;
        }

        public static Dictionary<string, object> GetFinalJsonPsetSet(
            string projectCode,
            string civilLanguage,
            List<Dictionary<string, object>> dataJsonByModelFiltered,
            List<Dictionary<string, object>> civilParamCheckFromSet,
            int currentSetStatus,
            out int updatedSetStatus
        )
        {
            string reviewValue = "revisar";

            // -------------------------------
            // Obtener info
            // -------------------------------

            string keyParamCheck = cls_00_AteneaJson.CivilParamCheck;
            string keyPsetName = cls_00_AteneaJson.PsetName;
            string keyPsetData = cls_00_AteneaJson.PsetData;
            string keyCategories = cls_00_AteneaJson.Categories;

            // -----------------------------
            // Partir del set existente
            // -----------------------------

            List<Dictionary<string, object>> civilParamCheck = new List<Dictionary<string, object>>();
            // Validamos
            if (civilParamCheckFromSet != null)
            {
                civilParamCheck.AddRange(civilParamCheckFromSet);
            }

            // -----------------------------
            // Validar datos filtrados
            // -----------------------------

            if (dataJsonByModelFiltered.Count == 0)
            {
                // Actualizamos status
                updatedSetStatus = 3;

                // Creamos JSON final con el Set existente
                Dictionary<string, object> existingSetDict = GetParamSetDictionary(
                    projectCode, civilLanguage, updatedSetStatus
                );
                // Añadimos
                existingSetDict.Add(keyParamCheck, civilParamCheck);

                // return
                return existingSetDict;
            }

            // -------------------------------
            // Obtener Psets del Set Web
            // -------------------------------

            HashSet<string> psetNamesFromSet = cls_00_ParamImpMainWeb_Async.GetPsetNames(
                civilParamCheck, keyPsetName
            );

            // -----------------------------
            // Agrupar nuevos Psets por nombre
            // -----------------------------

            Dictionary<string, List<Dictionary<string, object>>> psetsByName =
                new Dictionary<string, List<Dictionary<string, object>>>();

            foreach (Dictionary<string, object> fileData in dataJsonByModelFiltered)
            {
                if (!fileData.ContainsKey(keyParamCheck)) continue;

                List<Dictionary<string, object>> psets =
                    fileData[keyParamCheck] as List<Dictionary<string, object>>;

                if (psets == null) continue;

                foreach (Dictionary<string, object> pset in psets)
                {
                    if (!pset.ContainsKey(keyPsetName) || pset[keyPsetName] == null) continue;

                    string psetName = pset[keyPsetName].ToString();

                    // Si ya existe en el set original, no lo añadimos otra vez
                    if (psetNamesFromSet.Contains(psetName))
                    {
                        continue;
                    }

                    if (!psetsByName.ContainsKey(psetName))
                    {
                        psetsByName[psetName] = new List<Dictionary<string, object>>();
                    }

                    psetsByName[psetName].Add(pset);
                }
            }

            // -----------------------------
            // Consolidar y añadir solo nuevos Psets
            // -----------------------------

            foreach (KeyValuePair<string, List<Dictionary<string, object>>> kvp in psetsByName)
            {
                string psetName = kvp.Key;
                List<Dictionary<string, object>> psets = kvp.Value;

                List<Dictionary<string, object>> consolidatedCategories = ConsolidateCategories(psets);

                List<Dictionary<string, object>> consolidatedPsetData = ConsolidatePsetData(psets, reviewValue);

                Dictionary<string, object> consolidatedPset = new Dictionary<string, object>
                    {
                        { keyPsetName, psetName },
                        { keyCategories, consolidatedCategories },
                        { keyPsetData, consolidatedPsetData }
                    };

                civilParamCheck.Add(consolidatedPset);
            }

            // -----------------------------
            // Crear JSON final
            // -----------------------------

            // Actualizamos status
            updatedSetStatus = 2;

            Dictionary<string, object> baseDict = GetParamSetDictionary(
                projectCode, civilLanguage, updatedSetStatus
            );

            baseDict.Add(keyParamCheck, civilParamCheck);

            // return
            return baseDict;
        }
    }
}
