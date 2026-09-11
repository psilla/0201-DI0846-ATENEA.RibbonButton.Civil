using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.UserForms;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_00_FilterJsonBySelectedPsets
    {
        private static string GetPsetSelectionFormTitle(
            bool isSpanish
        )
        {
            return isSpanish
                ? "Selecciona los Property Sets del archivo de configuración que deseas añadir al Set de propiedades definido en la web.\n\n" +
                  "Los Property Sets que ya estén incluidos en el Set de propiedades web no aparecerán en este listado."
                : "Select the Property Sets from the configuration file that you want to add to the Property Set defined on the web.\n\n" +
                  "Property Sets that are already included in the web Property Set will not appear in this list.";
        }

        private static void ShowAllPsetsAlreadyConfiguredMessage(bool isSpanish)
        {
            ShowStringBuilder.ShowInfo(
                isSpanish
                    ? "Información Property Sets:"
                    : "Property Sets information:",
                isSpanish
                    ? "Todos los Property Sets detectados en los modelos ya están configurados en el conjunto web."
                    : "All Property Sets detected in the models are already configured in the web configuration set."
            );
        }

        private static void AppendModelWithoutSelectedPsetsWarning(
            StringBuilder sb,
            string fileName,
            bool isSpanish
        )
        {
            sb.AppendLine(
                isSpanish
                    ? $"• El modelo '{fileName}' no contiene ninguno de los Property Sets seleccionados y no será exportado."
                    : $"• Model '{fileName}' does not contain any of the selected Property Sets and will not be exported."
            );
        }

        private static void AppendMissingPsetDataFieldWarning(
            StringBuilder sb,
            string fileName,
            string psetName,
            bool isSpanish
        )
        {
            sb.AppendLine(
                isSpanish
                    ? $"• El modelo '{fileName}' contiene el Property Set '{psetName}', pero no contiene el campo de datos y no será exportado."
                    : $"• Model '{fileName}' contains Property Set '{psetName}', but it does not contain the data field and will not be exported."
            );
        }

        private static void AppendEmptyPsetWarning(
            StringBuilder sb,
            string fileName,
            string psetName,
            bool isSpanish
        )
        {
            sb.AppendLine(
                isSpanish
                    ? $"• El modelo '{fileName}' contiene el Property Set '{psetName}', pero no tiene propiedades definidas y no será exportado."
                    : $"• Model '{fileName}' contains Property Set '{psetName}', but it has no defined properties and will not be exported."
            );
        }

        private static void AppendModelWithoutValidPsetsWarning(
            StringBuilder sb,
            string fileName,
            bool isSpanish
        )
        {
            sb.AppendLine(
                isSpanish
                    ? $"• El modelo '{fileName}' no contiene ningún Property Set válido para exportar."
                    : $"• Model '{fileName}' does not contain any valid Property Sets to export."
            );
        }

        private static void ShowPsetWarnings(
            StringBuilder sbWarnings,
            bool isSpanish
        )
        {
            // Validamos
            if (sbWarnings.Length == 0) return;

            string title = isSpanish
                ? "⚠ Incidencias detectadas en Property Sets:"
                : "⚠ Property Sets issues found:";

            ShowStringBuilder.ShowInfo(
                title,
                sbWarnings.ToString()
            );
        }

        private static HashSet<string> GetAllPsetNamesFromJson(
            List<Dictionary<string, object>> dataJsonByModel,
            string keyParamCheck,
            string keyPsetName
        )
        {
            // Inicializamos
            HashSet<string> allPsetNamesFromJson = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase
            );

            // -----------------------------
            // Obtener todos los Psets
            // -----------------------------

            // Iteramos archivos
            foreach (Dictionary<string, object> fileData in dataJsonByModel)
            {
                // Validamos
                if (!fileData.ContainsKey(keyParamCheck)) continue;

                // Obtenemos lista de Psets
                List<Dictionary<string, object>> psets =
                    fileData[keyParamCheck] as List<Dictionary<string, object>>;
                // Validamos
                if (psets == null) continue;

                // Iteramos
                foreach (Dictionary<string, object> pset in psets)
                {
                    // Validamos
                    if (pset.ContainsKey(keyPsetName) && pset[keyPsetName] != null
                    )
                    {
                        // Almacenamos
                        allPsetNamesFromJson.Add(pset[keyPsetName].ToString());
                    }
                }
            }

            // return
            return allPsetNamesFromJson;
        }

        private static List<Dictionary<string, object>> FilterDataBySelectedPsets(
            List<Dictionary<string, object>> dataJsonByModel,
            HashSet<string> selectedPsetsSet,
            StringBuilder sbWarnings,
            string keyFileName,
            string keyParamCheck,
            string keyPsetName,
            string keyPsetData,
            bool isSpanish
        )
        {
            // Inicializamos
            List<Dictionary<string, object>> filteredData = new List<Dictionary<string, object>>();

            // -----------------------------
            // Iteramos archivos
            // -----------------------------

            foreach (Dictionary<string, object> fileData in dataJsonByModel)
            {
                // Validamos
                if (!fileData.ContainsKey(keyParamCheck)) continue;

                // Obtenemos nombre archivo
                string fileName = fileData.ContainsKey(keyFileName) && fileData[keyFileName] != null
                    ? fileData[keyFileName].ToString()
                    : "Unknown";

                // Obtenemos lista original
                List<Dictionary<string, object>> psets =
                    fileData[keyParamCheck] as List<Dictionary<string, object>>;
                // Validamos
                if (psets == null) continue;

                // Filtramos Psets
                List<Dictionary<string, object>> filteredPsets = psets
                    .Where(x =>
                        x.ContainsKey(keyPsetName)
                        && x[keyPsetName] != null
                        && selectedPsetsSet.Contains(x[keyPsetName].ToString())
                    )
                    .ToList();

                // Si el modelo no tiene ninguno de los Psets seleccionados, no se exporta
                if (filteredPsets.Count == 0)
                {
                    // Mostramos
                    AppendModelWithoutSelectedPsetsWarning(
                        sbWarnings, fileName, isSpanish
                    );
                    // Obviamos
                    continue;
                }

                // -----------------------------
                // Eliminar Property Sets sin datos
                // -----------------------------

                List<Dictionary<string, object>> validPsets = new List<Dictionary<string, object>>();
                // Iteramos
                foreach (Dictionary<string, object> pset in filteredPsets)
                {
                    string psetName = pset.ContainsKey(keyPsetName) && pset[keyPsetName] != null
                        ? pset[keyPsetName].ToString()
                        : "Unknown";
                    // Validamos
                    if (!pset.ContainsKey(keyPsetData))
                    {
                        // Mostramos
                        AppendMissingPsetDataFieldWarning(
                            sbWarnings, fileName, psetName, isSpanish
                        );
                        // Obviamos
                        continue;
                    }

                    // Obtenemos datos del Property Set
                    List<Dictionary<string, object>> psetData =
                        pset[keyPsetData] as List<Dictionary<string, object>>;
                    // Validamos data
                    if (psetData == null || psetData.Count == 0)
                    {
                        // Mostramos
                        AppendEmptyPsetWarning(
                            sbWarnings, fileName, psetName, isSpanish
                        );
                        // Obviamos
                        continue;
                    }

                    // Añadimos
                    validPsets.Add(pset);
                }

                // Si después de limpiar no queda ningún Pset válido, no se exporta el modelo
                if (validPsets.Count == 0)
                {
                    // Mostramos
                    AppendModelWithoutValidPsetsWarning(
                        sbWarnings, fileName, isSpanish
                    );
                    // Obviamos
                    continue;
                }

                // Creamos nuevo objeto archivo
                Dictionary<string, object> filteredFileData = new Dictionary<string, object>
                    {
                        { keyFileName, fileName  },
                        { keyParamCheck, validPsets }
                    };

                // Añadimos
                filteredData.Add(filteredFileData);
            }

            // Finalizamos
            return filteredData;
        }

        private static bool? AskIfAddMorePsets(
            IEnumerable<string> existingPsetNames,
            bool isSpanish
        )
        {
            StringBuilder sb = new StringBuilder();

            if (existingPsetNames.Any())
            {
                sb.AppendLine(
                    isSpanish
                        ? "El Set Web ya contiene los siguientes Property Sets:"
                        : "The Web Set already contains the following Property Sets:"
                );

                sb.AppendLine();

                foreach (string pset in existingPsetNames.OrderBy(x => x))
                {
                    sb.AppendLine($"• {pset}");
                }

                sb.AppendLine();
            }

            sb.AppendLine(
                isSpanish
                    ? "¿Deseas añadir más Property Sets al Set?"
                    : "Do you want to add more Property Sets to the Set?"
            );

            sb.AppendLine();

            sb.Append(
                isSpanish
                    ? "True: Añadir nuevos Property Sets.\n" +
                      "False: Mantener únicamente los Property Sets actuales."
                    : "True: Add new Property Sets.\n" +
                      "False: Keep only the current Property Sets."
            );

            object result = cls_00_InstaForm_ComboBox.ComboBoxFormOut(
                sb.ToString(), defaultValue: true
            );
            // Validamos
            if (result is bool value) return value;

            // Finalizamos
            return null;
        }

        public static List<Dictionary<string, object>> RemoveCategoriesFromJson(
            List<Dictionary<string, object>> dataJsonByModel
        )
        {
            // -----------------------------
            // Resultado
            // -----------------------------

            List<Dictionary<string, object>> result =
                new List<Dictionary<string, object>>();

            // -----------------------------
            // Obtener info
            // -----------------------------

            string keyParamCheck = cls_00_AteneaJson.CivilParamCheck;
            string keyFileName = cls_00_AteneaJson.FileName;
            string keyCategories = cls_00_AteneaJson.Categories;

            // -----------------------------
            // Iterar archivos
            // -----------------------------

            foreach (Dictionary<string, object> fileData in dataJsonByModel)
            {
                Dictionary<string, object> newFileData =
                    new Dictionary<string, object>();

                // -----------------------------
                // Copiar nombre
                // -----------------------------

                if (fileData.TryGetValue(keyFileName, out object fileName))
                {
                    newFileData.Add(keyFileName, fileName);
                }

                // -----------------------------
                // Obtener Property Sets
                // -----------------------------

                if (!fileData.TryGetValue(keyParamCheck, out object paramCheckObj))
                {
                    result.Add(newFileData);
                    continue;
                }

                List<Dictionary<string, object>> psets =
                    paramCheckObj as List<Dictionary<string, object>>;

                List<Dictionary<string, object>> newPsets =
                    new List<Dictionary<string, object>>();

                // -----------------------------
                // Copiar Property Sets
                // -----------------------------

                if (psets != null)
                {
                    foreach (Dictionary<string, object> pset in psets)
                    {
                        Dictionary<string, object> newPset = new Dictionary<string, object>();

                        foreach (KeyValuePair<string, object> kv in pset)
                        {
                            // Excluir Categories
                            if (kv.Key.Equals(keyCategories, StringComparison.OrdinalIgnoreCase)) continue;
                            
                            newPset.Add(kv.Key, kv.Value);
                        }

                        newPsets.Add(newPset);
                    }
                }

                // -----------------------------
                // Añadir Property Sets
                // -----------------------------

                newFileData.Add(keyParamCheck, newPsets);

                // -----------------------------
                // Almacenar
                // -----------------------------

                result.Add(newFileData);
            }

            // return
            return result;
        }

        public static List<Dictionary<string, object>> FilterJsonBySelectedPsets(
            List<Dictionary<string, object>> dataJsonByModel,
            List<Dictionary<string, object>> civilParamCheckFromSet,
            int currentSetStatus,
            bool isSpanish,
            out bool addPsetsToSet
        )
        {
            // Por defecto, no se actualizará el Set
            addPsetsToSet = false;

            // -----------------------------
            // Inicializar log
            // -----------------------------

            StringBuilder sbWarnings = new StringBuilder();

            // -------------------------------
            // Obtener info
            // -------------------------------

            string keyParamCheck = cls_00_AteneaJson.CivilParamCheck;
            string keyFileName = cls_00_AteneaJson.FileName;
            string keyPsetName = cls_00_AteneaJson.PsetName;
            string keyPsetData = cls_00_AteneaJson.PsetData;

            // -----------------------------
            // Obtener Psets de los modelos iterados
            // -----------------------------

            HashSet<string> allPsetNamesFromJson = GetAllPsetNamesFromJson(
                dataJsonByModel, keyParamCheck, keyPsetName
            );
            // Validamos
            if (allPsetNamesFromJson.Count == 0) return dataJsonByModel;
           
            // -----------------------------
            // Filtrar Psets a incorporar
            // -----------------------------

            List<string> selectablePsetNames;
            // Validamos si existe Set Web
            if (currentSetStatus == 1)
            {
                // No existe Set, todos son seleccionables
                selectablePsetNames = allPsetNamesFromJson.OrderBy(x => x).ToList();
                // Definimos
                addPsetsToSet = true;
            }
            else
            {
                // -------------------------------
                // Obtener Psets del Set Web
                // -------------------------------

                List<string> existingPsetNamesInSet = cls_00_ParamImpMainWeb.GetPsetNames(
                    civilParamCheckFromSet, keyPsetName)?.OrderBy(x => x).ToList()?? new List<string>();

                // -------------------------------
                // Posibilidad de añadir mas Psets
                // -------------------------------

                bool? addMorePsets = AskIfAddMorePsets(existingPsetNamesInSet, isSpanish);
                // Validamos
                if (addMorePsets == null) return null;

                // El usuario no quiere añadir
                if (!addMorePsets.Value)
                {
                    // Definimos
                    addPsetsToSet = false;
                    // Finalizamos
                    return new List<Dictionary<string, object>>();
                }

                // Definimos
                addPsetsToSet = true;

                // -------------------------------
                // Obtener valores unicos
                // -------------------------------

                HashSet<string> existingPsetNamesSet = new HashSet<string>(
                    existingPsetNamesInSet ?? new List<string>(), StringComparer.OrdinalIgnoreCase
                );

                // -----------------------------
                // Obtener Psets disponibles
                // -----------------------------

                selectablePsetNames = allPsetNamesFromJson
                    .Where(x => !existingPsetNamesSet.Contains(x)).OrderBy(x => x).ToList();
                // Validamos
                if (selectablePsetNames.Count == 0)
                {
                    // Mostramos
                    ShowAllPsetsAlreadyConfiguredMessage(isSpanish);
                    // Definimos
                    addPsetsToSet = false;
                    // Finalizamos
                    return new List<Dictionary<string, object>>();
                }
            }

            // -----------------------------
            // Form
            // -----------------------------

            string formTitle = GetPsetSelectionFormTitle(isSpanish);
            // Form
            List<string> selectedPsets = cls_00_InstaForm_CheckedListBox.CheckListBoxFormSearchOut(
                formTitle, selectablePsetNames
            );
            // Validamos cancelacion
            if (selectedPsets == null) return null;
           
            // Validamos que haya seleccion
            if (selectedPsets.Count == 0)
            {
                // Definimos
                addPsetsToSet = false;
                // Finalizamos
                return new List<Dictionary<string, object>>();
            }

            // HashSet 
            HashSet<string> selectedPsetsSet = new HashSet<string>(
                selectedPsets, StringComparer.OrdinalIgnoreCase
            );

            // -----------------------------
            // Filtrar datos
            // -----------------------------

            List<Dictionary<string, object>> filteredData = FilterDataBySelectedPsets(
                dataJsonByModel, selectedPsetsSet, sbWarnings, keyFileName, keyParamCheck,
                keyPsetName, keyPsetData, isSpanish
            );

            // -----------------------------
            // Mostrar log
            // -----------------------------

            ShowPsetWarnings(sbWarnings, isSpanish);
           
            // Finalizamos
            return filteredData;
        }



    }
}
