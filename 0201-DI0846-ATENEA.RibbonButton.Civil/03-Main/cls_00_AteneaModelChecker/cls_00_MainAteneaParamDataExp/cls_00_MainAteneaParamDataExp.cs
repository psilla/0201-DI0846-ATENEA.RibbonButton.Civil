using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Ribbon;
using Newtonsoft.Json;
using TYPSA.MC.RibbonButton.Civil.ExportJSON;
using TYPSA.SharedLib.Autocad.DbObjectsByType;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.DictInfoByObject;
using TYPSA.SharedLib.Civil.ExportToExcel;
using TYPSA.SharedLib.Civil.Main;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.Excel;
using TYPSA.SharedLib.Json;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_00_MainAteneaParamDataExp
    {
        private static void ShowAvailableItems(
            List<string> availableItems,
            string itemNameSpanish,
            string itemNameEnglish,
            bool isSpanish
        )
        {
            // -------------------------------
            // Preparar información
            // -------------------------------

            StringBuilder sb = new StringBuilder();

            sb.AppendLine(
                isSpanish
                    ? $"{itemNameSpanish} disponibles:"
                    : $"Available {itemNameEnglish}:"
            );

            sb.AppendLine();

            foreach (string item in availableItems)
            {
                sb.AppendLine($"• {item}");
            }

            // -------------------------------
            // Mostrar
            // -------------------------------

            ShowStringBuilder.ShowInfo(
                isSpanish
                    ? $"{itemNameSpanish} disponibles"
                    : $"Available {itemNameEnglish}",
                sb.ToString()
            );
        }

        private static List<string> ResolveSelection(
            string selectionMode,
            List<string> availableItems,
            string formTitle,
            string itemNameSingularSpanish,
            string itemNameSingularEnglish,
            bool isSpanish
        )
        {
            // Todos
            if (selectionMode == SelectionModes.All)
            {
                return new List<string>(availableItems);
            }

            // Selección que se utilizará como Default
            if (selectionMode == SelectionModes.Default)
            {
                List<string> selectedItems = cls_00_InstaForm_CheckedListBox.CheckListBoxFormSearchOut(
                    formTitle, availableItems
                );
                // Validamos
                if (selectedItems == null) return null;

                // Selección vacía
                if (selectedItems.Count == 0)
                {
                    MessageBox.Show(
                        isSpanish
                            ? itemNameSingularSpanish == "propiedad"
                                ? "Debe seleccionar al menos una propiedad."
                                : $"Debe seleccionar al menos un {itemNameSingularSpanish}."
                            : $"You must select at least one {itemNameSingularEnglish}.",
                        isSpanish
                            ? "Selección vacía"
                            : "Empty selection",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );

                    return null;
                }

                return selectedItems;
            }

            // Modo invalido
            return null;
        }

        private static List<string> GetPropertyNamesFromSelectedPsets(
            List<Dictionary<string, object>> civilParamCheck,
            List<string> selectedPsets,
            string keyPsetName,
            string keyPsetData,
            string keyPropName
        )
        {
            // Resultado
            HashSet<string> propertyNames =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Validamos
            if (
                civilParamCheck == null ||
                selectedPsets == null ||
                selectedPsets.Count == 0
            )
            {
                return propertyNames.ToList();
            }

            // Psets seleccionados
            HashSet<string> selectedPsetNames = new HashSet<string>(
                selectedPsets,
                StringComparer.OrdinalIgnoreCase
            );

            // -------------------------------
            // Iterar Property Sets
            // -------------------------------

            foreach (Dictionary<string, object> psetData in civilParamCheck)
            {
                // Validamos nombre del Pset
                if (
                    !psetData.TryGetValue(keyPsetName, out object psetNameValue) ||
                    psetNameValue == null
                )
                {
                    continue;
                }

                string psetName = psetNameValue.ToString();

                // Validamos que haya sido seleccionado
                if (!selectedPsetNames.Contains(psetName))
                    continue;

                // Validamos datos del Pset
                if (
                    !psetData.TryGetValue(keyPsetData, out object psetDataValue) ||
                    psetDataValue == null
                )
                {
                    continue;
                }

                // Deserializamos las propiedades
                List<Dictionary<string, object>> properties =
                    JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                        psetDataValue.ToString()
                    ) ?? new List<Dictionary<string, object>>();

                // -------------------------------
                // Iterar propiedades
                // -------------------------------

                foreach (Dictionary<string, object> propertyData in properties)
                {
                    // Validamos nombre
                    if (
                        !propertyData.TryGetValue(keyPropName, out object propNameValue) ||
                        propNameValue == null
                    )
                    {
                        continue;
                    }

                    string propertyName = propNameValue.ToString();

                    // Validamos
                    if (!string.IsNullOrWhiteSpace(propertyName))
                        propertyNames.Add(propertyName);
                }
            }

            // Ordenamos
            return propertyNames
                .OrderBy(x => x)
                .ToList();
        }

        private static string GetPsetSelectionFormTitle(
            bool isSpanish
        )
        {
            return isSpanish
                ? "Selecciona los Property Sets que contienen las propiedades de las que deseas extraer los valores en los elementos."
                : "Select the Property Sets that contain the properties whose values you want to extract from the elements.";
        }

        private static string GetPropertySelectionFormTitle(
            bool isSpanish
        )
        {
            return isSpanish
                ? "Selecciona las propiedades cuyos valores deseas extraer de los elementos pertenecientes a los Property Sets seleccionados."
                : "Select the properties whose values you want to extract from the elements belonging to the selected Property Sets.";
        }

        private static bool ValidateJsonData(
            Dictionary<string, object> jsonData,
            bool isSpanish
        )
        {
            // Validamos
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

            return true;
        }

        private static bool ValidateKeyParamCheck(
            Dictionary<string, object> jsonData,
            string keyParamCheck,
            bool isSpanish
        )
        {
            // Validamos
            if (
                !jsonData.ContainsKey(keyParamCheck) || jsonData[keyParamCheck] == null
            )
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

        private static void SkipNullFile(
            bool isSpanish,
            string fileName,
            ref int processedFiles,
            int totalFiles,
            int percentage,
            ProgressBarControl progressBarForm,
            int durationMs = 1000
        )
        {
            // Mostramos
            new AutoCloseMessageForm(
                isSpanish
                    ? $"El documento '{fileName}' no se pudo abrir."
                    : $"The document '{fileName}' could not be opened.",
                durationMs
            ).ShowDialog();
            // Actualizar progreso
            processedFiles++;
            percentage = (int)((double)processedFiles / totalFiles * 100);
            progressBarForm.ProgressValue = percentage;
        }

        private static void SkipNullFileData(
            Document openedDoc,
            ref int processedFiles,
            int totalFiles,
            int percentage,
            ProgressBarControl progressBarForm
        )
        {
            // Cerramos documento
            openedDoc.CloseAndDiscard();

            // Actualizar progreso
            processedFiles++;
            percentage = (int)((double)processedFiles / totalFiles * 100);
            progressBarForm.ProgressValue = percentage;
        }

        private static Dictionary<string, string> ShowSelectionModesForm(bool isSpanish)
        {
            // -----------------------------
            // Textos del formulario
            // -----------------------------

            string formMessage = isSpanish
                ? "Seleccione el modo de filtrado para cada campo.\n\n" +
                  "• All: se analizarán todos los elementos disponibles.\n" +
                  "• Manual: podrá seleccionar manualmente los elementos a analizar por modelo.\n" +
                  "• Default: podrá seleccionar los elementos por defecto que se utilizarán en cada iteración."
                : "Select the filtering mode for each field.\n\n" +
                  "• All: all available elements will be analyzed.\n" +
                  "• Manual: you will be able to manually select the elements to analyze for each model.\n" +
                  "• Default: you will be able to select the default elements that will be used in each iteration.";

            string formTitle = isSpanish
                ? "Modos de Selección"
                : "Selection Modes";

            // -----------------------------
            // Opciones
            // -----------------------------

            List<string> standardOptions = new List<string>
            {
                SelectionModes.All,
                SelectionModes.Manual
            };

            List<string> optionsWithDefault = new List<string>
            {
                SelectionModes.All,
                SelectionModes.Default
            };

            // -----------------------------
            // Campos
            // -----------------------------

            List<(string propiedad, List<string> options, string valorDefecto)> fields = 
                new List<(string propiedad, List<string> options, string valorDefecto)>
            {
                (
                    isSpanish ? "Selección de entidades" : "Entities selection",
                    new List<string>(standardOptions),
                    SelectionModes.All
                ),
                (
                    isSpanish ? "Selección de capas" : "Layers selection",
                    new List<string>(standardOptions),
                    SelectionModes.All
                ),
                (
                    isSpanish ? "Selección de Property Sets" : "Property Sets selection",
                    new List<string>(optionsWithDefault),
                    SelectionModes.Default
                ),
                (
                    isSpanish ? "Selección de propiedades" : "Properties selection",
                    new List<string>(optionsWithDefault),
                    SelectionModes.Default
                )
            };

            // -----------------------------
            // Form
            // -----------------------------

            return cls_00_InstaForm_ComboBox.ComboBoxFormOut_NextToLabel(
                formMessage, fields, formTitle: formTitle
            );
        }

        public static async Task MainAteneaParamDataExp(
            string[] selectedFiles, 
            string projectCode,
            DateTime startTime,
            CadSessionInfo info,
            cls_00_CivilAteneaEndPoints ateneaEndpoints,
            UiTexts uiTexts,
            bool isSpanish
        )
        {
            // -------------------------------
            // Definir variables
            // -------------------------------

            // Diccionario principal para agrupar los resultados de todos los archivos
            Dictionary<string, Dictionary<string, Dictionary<string, object>>> globalResult =
                new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
            // Creamos una lista vacia para almacenar diccionario por modelo
            List<Dictionary<string, Dictionary<string, Dictionary<string, object>>>> dataTot =
                new List<Dictionary<string, Dictionary<string, Dictionary<string, object>>>>();
            string msg = string.Empty;

            // -------------------------------
            // Normalizamos idioma
            // -------------------------------

            string softwareLanguage = isSpanish ? "Spanish" : "English";

            // -------------------------------
            // Obtener info
            // -------------------------------

            string keySoftwareVersion = cls_00_AteneaJson.CivilVersion;
            string keySoftwareLanguage = cls_00_AteneaJson.CivilLanguage;
            string keyParamCheck = cls_00_AteneaJson.CivilParamCheck;
            string keyPsetName = cls_00_AteneaJson.PsetName;
            string keyParamData = cls_00_AteneaJson.CivilParamData;

            string keyDataByFileName = cls_00_AteneaJson.DataByFileName;
            string keyFileName = cls_00_AteneaJson.FileName;

            // -------------------------------
            // Crear progreso
            // -------------------------------

            ProgressBarControl progressBarForm = new ProgressBarControl();
            // Iniciamos variables
            int totalFiles = selectedFiles.Length;
            int processedFiles = 0;
            int percentage = 0;

            // Mostramos
            progressBarForm.ProgressValue = 10; // Valor inicial de progreso
            progressBarForm.Show();

            // -------------------------------
            // Validar Datos Proyecto
            // -------------------------------

            msg = isSpanish
                ? "Validando la información del proyecto con el servidor."
                : "Validating project information with the server.";
            // Mostramos 
            new AutoCloseMessageForm(msg, 1000).ShowDialog();

            Dictionary<string, object> dictProjectDataToVal = GetProjectDataDictionary(projectCode, softwareLanguage);
#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026
            // Validamos Datos de Proyecto
            if (!await cls_00_ValidateProjectInfo.ValidateProjectDataAsync(
                ateneaEndpoints.EndpointProjectDataUrl, dictProjectDataToVal, keySoftwareVersion, keySoftwareLanguage, isSpanish
            ))
            {
                return;
            }
#endif

            // -------------------------------
            // Validar Status Set de Parametros
            // -------------------------------

            msg = isSpanish
                ? "Validando el estado del conjunto de parámetros en el servidor."
                : "Validating parameter set status on the server.";
            // Mostramos
            new AutoCloseMessageForm(msg, 1000).ShowDialog();

            Dictionary<string, object> dictSetToVal = GetSetStatusDictionary(projectCode);
#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026
            // Validamos
            int currentSetStatus = await cls_00_ValidateParamSetStatus.ValidateSetStatusAsync(
                dictSetToVal, ateneaEndpoints.EndpointValidateSetUrl, isSpanish
            );
#else

            int currentSetStatus = 1;

#endif
            // Validamos
            if (currentSetStatus == -1) return;

            // -------------------------------
            // Definimos estado existente Set
            // -------------------------------

            bool hasExistingSet = currentSetStatus == 2 || currentSetStatus == 3;

            // -------------------------------
            // Validar Status Set de Parametros
            // -------------------------------

            if (!hasExistingSet)
            {
                // Mensaje
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
                // Finalizamos
                return;
            }

            // -------------------------------
            // Cargar Set JSON desde API
            // -------------------------------

            msg = isSpanish
                ? "Cargando la configuración del conjunto de parámetros desde el servidor."
                : "Loading parameter set configuration from the server.";
            // Mostramos 
            new AutoCloseMessageForm(msg, 1000).ShowDialog();

            Dictionary<string, object> jsonSetDataFromWeb = new Dictionary<string, object>();
#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026
            // Validamos
            if (hasExistingSet)
            {
                // try
                try
                {
                    // Obtenemos el JSON del Set desde la API
                    jsonSetDataFromWeb = await cls_00_LoadJsonFromApiPostAsync.LoadJsonFromApiPostAsync<Dictionary<string, object>>(
                        ateneaEndpoints.EndpointGetSetUrl, projectCode, info.UserName, info.AteneaVersion, isSpanish
                    );
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
                    return;
                }
            }
#endif

            // -------------------------------
            // Obtener Civil Param Check
            // -------------------------------

            List<Dictionary<string, object>> civilParamCheckFromSet = new List<Dictionary<string, object>>();
            // Validamos
            if (hasExistingSet)
            {
                // Validamos el JSON
                if (!ValidateJsonData(jsonSetDataFromWeb, isSpanish)) return;

                // Validamos la clave
                if (!ValidateKeyParamCheck(jsonSetDataFromWeb, keyParamCheck, isSpanish)) return;

                // Obtenemos los datos
                civilParamCheckFromSet = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                    jsonSetDataFromWeb[keyParamCheck].ToString()
                ) ?? new List<Dictionary<string, object>>();
            }

            // -----------------------------
            // Mostrar formulario de seleccion
            // -----------------------------

            Dictionary<string, string> selectionResult = ShowSelectionModesForm(isSpanish);
            // Validamos
            if (selectionResult == null) return;

            // -----------------------------
            // Mapeo de la seleccion
            // -----------------------------

            string keyEntities = isSpanish ? "Selección de entidades" : "Entities selection";
            string keyLayers = isSpanish ? "Selección de capas" : "Layers selection";
            string keyPsets = isSpanish ? "Selección de Property Sets" : "Property Sets selection";
            string keyProps = isSpanish ? "Selección de propiedades" : "Properties selection";

            string entitiesModeByUser = selectionResult[keyEntities];
            string layersModeByUser = selectionResult[keyLayers];
            string psetsModeByUser = selectionResult[keyPsets];
            string propsModeByUser = selectionResult[keyProps];

            // -------------------------------
            // Obtener Psets del Set Web
            // -------------------------------

            List<string> existingPsetNamesInSet = cls_00_MainAteneaParamCheckExp.GetPsetNames(
                civilParamCheckFromSet, keyPsetName
            ).OrderBy(x => x).ToList();
            // Validamos
            if (existingPsetNamesInSet.Count == 0)
            {
                // Mensaje
                MessageBox.Show(
                    isSpanish
                        ? "El Set Web no contiene Property Sets disponibles."
                        : "The Web Set does not contain any available Property Sets.",
                    isSpanish
                        ? "Property Sets no encontrados"
                        : "Property Sets not found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning
                );
                // Finalizamos
                return;
            }

            // -------------------------------
            // Mostrar Property Sets disponibles en Set
            // -------------------------------

            bool showInfo = false;
            if (showInfo)
                ShowAvailableItems(
                    existingPsetNamesInSet, "Property Sets", "Property Sets", isSpanish
                );

            // -------------------------------
            // Resolver seleccion de Psets
            // -------------------------------

            List<string> selectedPsets = ResolveSelection(
                psetsModeByUser, existingPsetNamesInSet, GetPsetSelectionFormTitle(isSpanish),
                "Property Set", "Property Set", isSpanish
            );
            // Validamos
            if (selectedPsets == null) return;

            // -------------------------------
            // Obtener propiedades de los Psets seleccionados
            // -------------------------------

            List<string> propertyNamesFromSelectedPsets = GetPropertyNamesFromSelectedPsets(
                civilParamCheckFromSet, selectedPsets, keyPsetName, cls_00_AteneaJson.PsetData, cls_00_AteneaJson.PropName
            );
            // Validamos
            if (propertyNamesFromSelectedPsets.Count == 0)
            {
                // Mostramos
                MessageBox.Show(
                    isSpanish
                        ? "Los Property Sets seleccionados no contienen propiedades."
                        : "The selected Property Sets do not contain any properties.",
                    isSpanish
                        ? "Propiedades no encontradas"
                        : "Properties not found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning
                );
                // Finalizamos
                return;
            }

            // -------------------------------
            // Resolver seleccion de propiedades
            // -------------------------------

            List<string> selectedProperties = ResolveSelection(
                propsModeByUser, propertyNamesFromSelectedPsets, GetPropertySelectionFormTitle(isSpanish),
                "propiedad", "property", isSpanish
            );
            // Validamos
            if (selectedProperties == null) return;

            // try
            try
            {
                // -----------------------------
                // Iterar archivos
                // -----------------------------

                msg = isSpanish
                    ? "Iniciando el procesamiento de los documentos seleccionados."
                    : "Starting the processing of the selected documents.";
                // Mostramos 
                new AutoCloseMessageForm(msg, 1000).ShowDialog();

                foreach (string file in selectedFiles)
                {
                    // try
                    try
                    {
                        // -------------------------------
                        // Definir variables
                        // -------------------------------

                        // Obtener el nombre sin extensión
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(file);

                        using (Document openedDoc = Application.DocumentManager.Open(file, false))
                        {
                            // -------------------------------
                            // Abrir documento
                            // -------------------------------

                            if (openedDoc == null)
                            {
                                // Obviamos
                                SkipNullFile(
                                    isSpanish, fileName, ref processedFiles, totalFiles, percentage, progressBarForm
                                );
                                // Obviamos
                                continue;
                            }

                            // -------------------------------
                            // Procesar documento
                            // -------------------------------

                            msg = isSpanish
                                ? $"Analizando documento {processedFiles + 1}/{totalFiles}: '{fileName}'"
                                : $"Analyzing document {processedFiles + 1}/{totalFiles}: '{fileName}'";
                            // Mensaje
                            new AutoCloseMessageForm(msg, 1000).ShowDialog();

                            // -----------------------------
                            // Listas según modo seleccionado
                            // -----------------------------

                            List<string> defaultPsetsToUse = psetsModeByUser == SelectionModes.Default
                                ? selectedPsets
                                : null;

                            List<string> defaultPropsToUse = propsModeByUser == SelectionModes.Default
                                ? selectedProperties
                                : null;

                            // -----------------------------
                            // Diccionario Elementos
                            // -----------------------------

                            Dictionary<string, Dictionary<string, Dictionary<string, object>>> fileData =
                                cls_00_DictInfoByObjectPsetProp.dicc_InfoDiccByObjectByFileCustomPsetProp(
                                    openedDoc,
                                    entitiesSelectionMode: entitiesModeByUser,
                                    layerSelectionMode: layersModeByUser,
                                    psetSelectionMode: psetsModeByUser, defaultPsets: defaultPsetsToUse,
                                    propSelectionMode: propsModeByUser, defaultProps: defaultPropsToUse
                                );
                            // Validamos
                            if (fileData == null)
                            {
                                // Cerramos documento
                                SkipNullFileData(
                                    openedDoc, ref processedFiles, totalFiles, percentage, progressBarForm
                                );
                                // Obviamos
                                continue;
                            }

                            // -----------------------------
                            // Añadir info
                            // -----------------------------

                            dataTot.Add(fileData);

                            // -----------------------------
                            // Cerrar documento
                            // -----------------------------

                            openedDoc.CloseAndDiscard();

                            // -----------------------------
                            // Actualizar progreso
                            // -----------------------------

                            processedFiles++;
                            percentage = (int)((double)processedFiles / totalFiles * 100);
                            progressBarForm.ProgressValue = percentage;
                        }
                    }
                    // catch
                    catch (Autodesk.AutoCAD.Runtime.Exception ex)
                    {
                        // Mensaje
                        MessageBox.Show(
                            $"EXCEPTION:\n{ex.Message}\n{ex.StackTrace}"
                        );
                    }
                }

                // -----------------------------
                // Cerrar progreso
                // -----------------------------

                progressBarForm.Close();

                // -----------------------------
                // Validar info global
                // -----------------------------

                msg = isSpanish
                    ? "Validando el archivo JSON con los datos extraídos..."
                    : "Validando the JSON file with the extracted data...";
                // Mensaje
                new AutoCloseMessageForm(msg, 1000).ShowDialog();

                if (dataTot == null || dataTot.Count == 0)
                {
                    // Mensaje
                    MessageBox.Show(
                        "No valid data was collected from any file.\n\n" +
                        "No results will be generated.", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Information
                    );
                    // Finalizamos
                    return;
                }

                // -----------------------------
                // Combinar diccionarios
                // -----------------------------

                foreach (var diccionario in dataTot)
                {
                    foreach (var kvp in diccionario)
                    {
                        globalResult[kvp.Key] = kvp.Value;
                    }
                }
                // Validamos
                if (globalResult == null) return;

                // -----------------------------
                // Transformar diccionario
                // -----------------------------

                Dictionary<string, List<List<object>>> dictTransToList =
                    cls_00_ExportToExcel_OpenXml.GetPropertySetExportData(globalResult);
                // Validamos
                if (dictTransToList == null) return;

                // -----------------------------
                // Construir diccionario Str-Obj
                // -----------------------------

                InfoByObjectKeys infoByObjectKeys = InfoByObjectKeys.GetDefaultKeys();
                // Construimos
                List<Dictionary<string, object>> dataJsonByModel = cls_00_ExportToJson.BuildDictByDataStrObj(
                    globalResult, infoByObjectKeys
                );

                // -----------------------------
                // Exportar JSON por Modelo
                // -----------------------------

                msg = isSpanish
                    ? "Generando el archivo JSON con los datos extraídos..."
                    : "Generating the JSON file with the extracted data...";
                // Mensaje
                new AutoCloseMessageForm(msg, 1000).ShowDialog();

                Dictionary<string, object> dictDataByFileToJson = GetFinalJsonDictionary(
                    projectCode, softwareLanguage, dataJsonByModel
                );
                // Exportamos
                cls_00_SaveJson.TrySaveJson(
                    isSpanish, dictDataByFileToJson, projectCode, info.RootFolderName, info.JsonFileNameParamDataExp
                );

                // -----------------------------
                // Enviar JSON por Modelo
                // -----------------------------

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026
                await cls_00_SendJsonByChunk_Param.SendJsonByChunk_Param(
                    isSpanish, dictDataByFileToJson, ateneaEndpoints.EndpointDataByFileElementUrl,
                    keyParamData, keySoftwareVersion, keySoftwareLanguage
                );
#endif

                // -----------------------------
                // Exportar Excel report
                // -----------------------------

                cls_00_ExportFilteredParamDataToExcel.ExportParamDataToExcel(
                    dictDataByFileToJson, keyDataByFileName, keyFileName, infoByObjectKeys.CivilParamData,
                    infoByObjectKeys.Handle, infoByObjectKeys.Layer, infoByObjectKeys.ObjectType,
                    infoByObjectKeys.PropertySetInfo, infoByObjectKeys.Parameters,
                    infoByObjectKeys.ParName, infoByObjectKeys.ParValue
                );

                // -----------------------------
                // Obtener info para Metrics
                // -----------------------------

                // Obtener un set de las propiedades analizadas
                int uniqueParam = dictTransToList.Values
                    .Where(lists => lists.Count > 0 && lists[0].Count >= 5)
                    .Sum(lists => lists[0].Skip(4).Count());

                // Obtener un set de las capas analizadas
                HashSet<object> uniqueLayersSet = dictTransToList.Values
                    .SelectMany(lists => lists.Skip(1)) // Saltar la primera lista de cada grupo
                    .Where(list => list.Count >= 3) // Asegurar que tienen al menos 3 elementos
                    .Select(list => list[2]) // Obtener el tercer elemento (índice 2)
                    .ToHashSet(); // Convertirlo en un HashSet para obtener solo valores únicos

                string message =
                    $"Number of unique parameters: {uniqueParam}\n" +
                    $"Number of unique layers: {uniqueLayersSet.Count}\n\n" +
                    $"Total count: {uniqueParam * uniqueLayersSet.Count}";
                // Mensaje
                new AutoCloseMessageForm(message, 1000).ShowDialog();

                cls_00_MainExportBack_Metrics metrics = new cls_00_MainExportBack_Metrics();
                // Enviar metrics
                metrics.SendMetrics(totalFiles, uniqueParam * uniqueLayersSet.Count, projectCode);

                // -------------------------------
                // Summary
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
            // catch
            catch (Exception ex)
            {
                // Mensaje
                MessageBox.Show(
                    $"ERROR: {ex.GetType().Name}\n{ex.Message}\n{ex.StackTrace}",
                    "Error"
                );
                // Finalizamos
                return;
            }
        }






    }
}
