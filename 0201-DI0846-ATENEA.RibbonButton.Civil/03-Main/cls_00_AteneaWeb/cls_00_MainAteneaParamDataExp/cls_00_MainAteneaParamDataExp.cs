using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Newtonsoft.Json;
using TYPSA.MC.RibbonButton.Civil.ExportJSON;
using TYPSA.SharedLib.Autocad.DbObjectsByType;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.DictInfoByObject;
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
        private static List<Dictionary<string, object>> FilterDataByFileNames(
            List<Dictionary<string, object>> dataByFileList,
            IEnumerable<string> fileNames
        )
        {
            HashSet<string> fileNameSet = new HashSet<string>(
                fileNames, StringComparer.OrdinalIgnoreCase
            );

            return dataByFileList
                .Where(modelEntry =>
                {
                    string fileName = modelEntry.TryGetValue(
                        cls_00_AteneaJson.FileName, out object fileNameObj
                    )
                        ? fileNameObj?.ToString()
                        : null;

                    return !string.IsNullOrWhiteSpace(fileName) &&
                           fileNameSet.Contains(fileName);
                })
                .ToList();
        }

        private static void ShowGlobalUploadResultMessage(
            IEnumerable<string> successfullyUploadedFileNames,
            IEnumerable<string> failedFileNames,
            IEnumerable<string> pendingFileNames,
            bool isSpanish
        )
        {
            List<string> successfulModels = successfullyUploadedFileNames?
                .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();

            List<string> failedModels = failedFileNames?
                .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();

            List<string> pendingModels = pendingFileNames?
                .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();

            string successfulModelsText = successfulModels.Any()
                ? string.Join(
                    Environment.NewLine,
                    successfulModels.Select(fileName => $"• {fileName}")
                )
                : isSpanish
                    ? "• Ningún modelo"
                    : "• No models";

            string failedModelsText = failedModels.Any()
                ? string.Join(
                    Environment.NewLine,
                    failedModels.Select(fileName => $"• {fileName}")
                )
                : isSpanish
                    ? "• Ningún modelo"
                    : "• No models";

            string pendingModelsText = pendingModels.Any()
                ? string.Join(
                    Environment.NewLine,
                    pendingModels.Select(fileName => $"• {fileName}")
                )
                : isSpanish
                    ? "• Ningún modelo"
                    : "• No models";

            string message;

            // -----------------------------
            // Todos enviados correctamente
            // -----------------------------

            if (!failedModels.Any() && !pendingModels.Any())
            {
                message = isSpanish
                    ? $"Todos los modelos se han enviado correctamente al JSON global del proyecto.\n\n" +
                      $"{successfulModelsText}"
                    : $"All models were successfully uploaded to the project's global JSON.\n\n" +
                      $"{successfulModelsText}";
            }
            // -----------------------------
            // Existen enviados, fallidos o pendientes
            // -----------------------------
            else
            {
                List<string> messageSections = new List<string>();

                // Modelos enviados correctamente
                if (successfulModels.Any())
                {
                    messageSections.Add(
                        isSpanish
                            ? $"Modelos enviados correctamente al JSON global del proyecto:\n\n" +
                              $"{successfulModelsText}"
                            : $"Models successfully uploaded to the project's global JSON:\n\n" +
                              $"{successfulModelsText}"
                    );
                }
                else
                {
                    messageSections.Add(
                        isSpanish
                            ? "No se ha podido enviar correctamente ningún modelo al JSON global del proyecto."
                            : "No models were successfully uploaded to the project's global JSON."
                    );
                }

                // Modelos que han fallado
                if (failedModels.Any())
                {
                    messageSections.Add(
                        isSpanish
                            ? $"Modelos cuyo envío ha fallado y cuyos datos parciales se eliminarán antes del reintento:\n\n" +
                              $"{failedModelsText}"
                            : $"Models whose upload failed and whose partial data will be deleted before retrying:\n\n" +
                              $"{failedModelsText}"
                    );
                }

                // Modelos pendientes
                if (pendingModels.Any())
                {
                    messageSections.Add(
                        isSpanish
                            ? $"Modelos pendientes que no llegaron a procesarse durante el envío global:\n\n" +
                              $"{pendingModelsText}"
                            : $"Pending models that were not processed during the global upload:\n\n" +
                              $"{pendingModelsText}"
                    );
                }

                messageSections.Add(
                    isSpanish
                        ? "Los modelos fallidos y pendientes se intentarán enviar de forma individual."
                        : "Failed and pending models will be retried individually."
                );

                message = string.Join(
                    $"{Environment.NewLine}{Environment.NewLine}",
                    messageSections
                );
            }

            new AutoCloseMessageForm(message, 3000).ShowDialog();
        }

        private static bool ValidateRetrievedModelData(
            List<Dictionary<string, object>> dataJsonByModel,
            bool isSpanish
        )
        {
            // Validamos
            if (dataJsonByModel != null && dataJsonByModel.Any())
            {
                return true;
            }

            MessageBox.Show(
                isSpanish
                    ? "No se recuperó información de ninguno de los modelos seleccionados.\n" +
                      "No se generará ningún archivo JSON."
                    : "No data was retrieved from any of the selected models.\n" +
                      "No JSON file will be generated.",
                isSpanish ? "Advertencia" : "Warning",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );

            return false;
        }

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

        private static bool? boolParamDataExpOptions(
            bool isSpanish
        )
        {
            object[] opciones = { true, false };
            // Message
            string boolMess = isSpanish
                ? "Elige una opción:\n\n" +
                  "True: Usar los parámetros del Set original.\n" +
                  "False: Usar los parámetros desde la ventana personalizada."
                : "Choose an option:\n\n" +
                  "True: Use the parameters from the original Set.\n" +
                  "False: Use the parameters from the custom modal window in the web.";

            // Form
            object boolUser = cls_00_InstaForm_ComboBox.ComboBoxFormListOut_Atenea(
                boolMess, opciones, defaultValue: true
            );
            // Validamos
            if (boolUser is bool) return Convert.ToBoolean(boolUser);

            // Mensaje
            MessageBox.Show(
                isSpanish ? "No se seleccionó ninguna opción." : "No option was selected.",
                isSpanish ? "Aviso" : "Warning",
                MessageBoxButtons.OK, MessageBoxIcon.Warning
            );

            // Finalizamos
            return null;
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

        private static bool TryGetSelectionData(
            List<Dictionary<string, object>> civilParamCheckFromSet,
            string keyPsetName,
            string psetsModeByUser,
            string propsModeByUser,
            bool isSpanish,
            out List<string> selectedPsets,
            out List<string> selectedProperties
        )
        {
            selectedPsets = null;
            selectedProperties = null;

            // -------------------------------
            // Obtener Property Sets del Set Web
            // -------------------------------

            List<string> existingPsetNamesInSet = cls_00_ParamImpMainWeb_Async.GetPsetNames(
                civilParamCheckFromSet, keyPsetName
            ).OrderBy(x => x).ToList();
            // Validamos
            if (existingPsetNamesInSet.Count == 0)
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
                // Finalizamos
                return false;
            }

            // -------------------------------
            // Resolver seleccion de Psets
            // -------------------------------

            selectedPsets = ResolveSelection(
                psetsModeByUser, existingPsetNamesInSet, GetPsetSelectionFormTitle(isSpanish),
                "Property Set", "Property Set", isSpanish
            );
            // Validamos
            if (selectedPsets == null) return false;

            // -------------------------------
            // Obtener propiedades de los Psets seleccionados
            // -------------------------------

            List<string> propertyNamesFromSelectedPsets = GetPropertyNamesFromSelectedPsets(
                civilParamCheckFromSet, selectedPsets, keyPsetName, cls_00_AteneaJson.PsetData, cls_00_AteneaJson.PropName
            );
            // Validamos
            if (propertyNamesFromSelectedPsets.Count == 0)
            {
                MessageBox.Show(
                    isSpanish
                        ? "Los Property Sets seleccionados no contienen propiedades."
                        : "The selected Property Sets do not contain any properties.",
                    isSpanish
                        ? "Propiedades no encontradas"
                        : "Properties not found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                // Finalizamos
                return false;
            }

            // -------------------------------
            // Resolver seleccion de propiedades
            // -------------------------------

            selectedProperties = ResolveSelection(
                propsModeByUser, propertyNamesFromSelectedPsets, GetPropertySelectionFormTitle(isSpanish),
                "propiedad", "property", isSpanish
            );
            // Validamos
            if (selectedProperties == null) return false;
          
            // return
            return true;
        }

        public static async Task MainAteneaParamDataExp(
            string[] selectedFiles, 
            string selectedFolderPath,
            string projectCode,
            DateTime startTime,
            CadSessionInfo info,
            cls_00_AteneaEndPointsCivil ateneaEndpoints,
            UiTexts uiTexts,
            bool isSpanish
        )
        {
            // -------------------------------
            // Definir variables
            // -------------------------------

            InfoByObjectKeys infoByObjectKeys = InfoByObjectKeys.GetDefaultKeys();

            List<Dictionary<string, object>> dataJsonByModel = new List<Dictionary<string, object>>();

            List<(string FileName, Dictionary<string, object> JsonData)> jsonDataToSendByModel =
                new List<(string FileName, Dictionary<string, object> JsonData)>();

            // Diccionario principal para agrupar los resultados de todos los archivos
            Dictionary<string, Dictionary<string, Dictionary<string, object>>> globalResult =
                new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();

            // Creamos una lista vacia para almacenar diccionario por modelo
            List<Dictionary<string, Dictionary<string, Dictionary<string, object>>>> dataTot =
                new List<Dictionary<string, Dictionary<string, Dictionary<string, object>>>>();

            string msg = string.Empty;

            // -------------------------------
            // Tiempos de ejecución
            // -------------------------------

            // Global
            Dictionary<string, TimeSpan> processDurations = new Dictionary<string, TimeSpan>();
            // Modelo
            Dictionary<string, Dictionary<string, TimeSpan>> processDurationsByModel = new Dictionary<string, Dictionary<string, TimeSpan>>();
            // Envíos por Modelo
            Dictionary<string, Dictionary<string, TimeSpan>> sendDurationsByModel = new Dictionary<string, Dictionary<string, TimeSpan>>();

            Stopwatch totalStopwatch = Stopwatch.StartNew();

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
            string keyPsetName = cls_00_AteneaJson.PsetName;
            string keyParamData = cls_00_AteneaJson.CivilParamData;
            string keyDataByFileName = cls_00_AteneaJson.DataByFileName;
            string keyFileName = cls_00_AteneaJson.FileName;
            string strEndpointGetSetUrl = ateneaEndpoints.EndpointGetSetUrl;
            string strEndpointGetCustomSetUrl = ateneaEndpoints.EndpointGetCustomSetUrl;
            string strEndpointDataByFileElementUrl = ateneaEndpoints.EndpointDataByFileElementUrl;
            string strUserName = info.UserName;
            string strRootFolderName = info.RootFolderName;
            string strJsonFileNameParamDataExp = info.JsonFileNameParamDataExp;

            // -------------------------------
            // Reiniciamos cronometro global
            // -------------------------------

            startTime = DateTime.Now;

            // -------------------------------
            // Form para seleccionar opciones Set
            // -------------------------------

            bool? selectionModeParFromSet = boolParamDataExpOptions(isSpanish);
            // Validamos 
            if (selectionModeParFromSet == null) return;

            // Obtenemos el valor
            bool selectionModeParFromSetBool = selectionModeParFromSet.Value;

            // Endpoint basado en la seleccion
            string selectedEndpoint = selectionModeParFromSetBool
                ? strEndpointGetSetUrl
                : strEndpointGetCustomSetUrl;

            // Aplicamos orden alfabetico solo para el Set original
            bool applyAlphabeticalOrder = selectionModeParFromSetBool;

            // -------------------------------
            // Preparar informacion Web
            // -------------------------------

            ParamExpPreparedData preparedData = await cls_00_ParamExpMainWeb_Async.ParamExpMainWebAsync(
                projectCode, info, ateneaEndpoints, selectedEndpoint, isSpanish, validateExistingSet: true
            );
            // Validamos
            if (preparedData == null) return;

            // -------------------------------
            // Obtener informacion preparada
            // -------------------------------

            List<Dictionary<string, object>> civilParamCheckFromSet = preparedData.CivilParamCheckFromSet;

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
            // Obtener Opciones exportacion
            // -------------------------------

            msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.ConfigureExportOptions);

            Stopwatch processStopwatch = Stopwatch.StartNew();

            if (!TryGetSelectionData(
                civilParamCheckFromSet, keyPsetName, psetsModeByUser, propsModeByUser, isSpanish,
                out List<string> selectedPsets, out List<string> selectedProperties
            )) return;

            // Añadimos
            cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

            // -----------------------------
            // Definir listas según modo seleccionado
            // -----------------------------

            List<string> defaultPsetsToUse = psetsModeByUser == SelectionModes.Default
                ? selectedPsets
                : null;

            List<string> defaultPropsToUse = propsModeByUser == SelectionModes.Default
                ? selectedProperties
                : null;

            // -------------------------------
            // Crear progreso
            // -------------------------------

            ProgressBarControl progressBarForm = new ProgressBarControl();
            // Iniciamos variables
            int totalFiles = selectedFiles.Length;
            int processedFiles = 0;
            int percentage = 0;

            // Mostramos
            progressBarForm.ProgressValue = 10; // Valor inicial 
            progressBarForm.Show();

            // try
            try
            {
                // -----------------------------
                // Iniciar cronometro modelos
                // -----------------------------

                string msgIter = cls_00_ProcessMessages.ShowProcessMessage(
                    isSpanish, cls_00_ProcessMessages.StartDocumentProcessing
                );

                Stopwatch filesProcessingStopwatch = Stopwatch.StartNew();

                // -----------------------------
                // Iterar archivos
                // -----------------------------

                foreach (string file in selectedFiles)
                {
                    // -------------------------------
                    // Definir variables
                    // -------------------------------

                    List<Dictionary<string, object>> extractedData = new List<Dictionary<string, object>>();

                    string fileName = System.IO.Path.GetFileNameWithoutExtension(file);

                    Dictionary<string, TimeSpan> modelDurations = new Dictionary<string, TimeSpan>();

                    // try
                    try
                    {
                        // -------------------------------
                        // Abrir documento
                        // -------------------------------

                        msg = cls_00_ProcessMessages.ShowProcessMessage(
                            isSpanish, cls_00_ProcessMessages.OpenDocument, processedFiles + 1, totalFiles, fileName
                        );

                        Stopwatch modelProcessStopwatch = Stopwatch.StartNew();

                        using (Document openedDoc = Application.DocumentManager.Open(file, false))
                        {
                            cls_00_ProcessMessages.AddProcessDuration(
                                modelDurations, msg, modelProcessStopwatch
                            );

                            // -------------------------------
                            // Validar documento
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

                            msg = cls_00_ProcessMessages.ShowProcessMessage(
                                isSpanish, cls_00_ProcessMessages.AnalyzeDocument, processedFiles + 1, totalFiles, fileName
                            );

                            // -------------------------------
                            // Procesar informacion
                            // -------------------------------

                            msg = cls_00_ProcessMessages.ShowProcessMessage(
                                isSpanish, cls_00_ProcessMessages.CollectModelData, fileName
                            );

                            modelProcessStopwatch = Stopwatch.StartNew();

                            Dictionary<string, Dictionary<string, Dictionary<string, object>>> fileDataJson =
                                cls_00_DictInfoByObjectPsetProp.dicc_InfoDiccByObjectByFileCustomPsetProp(
                                    openedDoc, entitiesSelectionMode: entitiesModeByUser, layerSelectionMode: layersModeByUser,
                                    psetSelectionMode: psetsModeByUser, defaultPsets: defaultPsetsToUse,
                                    propSelectionMode: propsModeByUser, defaultProps: defaultPropsToUse
                                );
                            // Validamos
                            if (fileDataJson == null)
                            {
                                // Cerramos documento
                                SkipNullFileData(
                                    openedDoc, ref processedFiles, totalFiles, percentage, progressBarForm
                                );
                                // Obviamos
                                continue;
                            }

                            // Añadimos
                            cls_00_ProcessMessages.AddProcessDuration(modelDurations, msg, modelProcessStopwatch);

                            // -------------------------------
                            // Construir y almacenar informacion
                            // -------------------------------

                            if (fileDataJson != null && fileDataJson.Any())
                            {
                                // -------------------------------
                                // Obtener variable para JSON
                                // -------------------------------

                                msg = cls_00_ProcessMessages.ShowProcessMessage(
                                    isSpanish, cls_00_ProcessMessages.TransformModelData, fileName
                                );

                                modelProcessStopwatch = Stopwatch.StartNew();

                                List<Dictionary<string, object>> modelData = cls_00_ExportToJson.BuildDictByDataStrObj(
                                    fileDataJson, infoByObjectKeys
                                );
                                // Validamos
                                if (modelData != null && modelData.Any())
                                {
                                    // -------------------------------
                                    // Construir JSON por modelo
                                    // -------------------------------

                                    Dictionary<string, object> modelDataToJson = GetFinalJsonDictionary(
                                        projectCode, softwareLanguage, modelData
                                    );

                                    // -------------------------------
                                    // Almacenar por modelo
                                    // -------------------------------

                                    jsonDataToSendByModel.Add((fileName, modelDataToJson));
                                    dataJsonByModel.AddRange(modelData);

                                }

                                // Añadimos el tiempo de transformación y almacenamiento
                                cls_00_ProcessMessages.AddProcessDuration(
                                    modelDurations, msg, modelProcessStopwatch
                                );
                            }

                            // -----------------------------
                            // Cerrar documento
                            // -----------------------------

                            msg = cls_00_ProcessMessages.ShowProcessMessage(
                                isSpanish, cls_00_ProcessMessages.CloseDocument, processedFiles + 1, totalFiles, fileName
                            );

                            Stopwatch closeDocumentStopwatch = Stopwatch.StartNew();

                            openedDoc.CloseAndDiscard();

                            // Añadimos
                            cls_00_ProcessMessages.AddProcessDuration(modelDurations, msg, closeDocumentStopwatch);

                            // -------------------------------
                            // Tiempo total de procesos registrados
                            // -------------------------------

                            if (modelDurations.Any())
                            {
                                TimeSpan modelTotalDuration = TimeSpan.FromTicks(
                                    modelDurations.Values.Sum(x => x.Ticks)
                                );

                                modelDurations["Total"] = modelTotalDuration;

                                // Guardamos los tiempos del modelo
                                processDurationsByModel[fileName] = modelDurations;
                            }

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

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msgIter, filesProcessingStopwatch);

                // -----------------------------
                // Cerrar progreso
                // -----------------------------

                progressBarForm.Close();

                // -----------------------------
                // Validar informacion Global
                // -----------------------------

                msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.ValidateJson);

                processStopwatch = Stopwatch.StartNew();

                // Validamos
                if (!ValidateRetrievedModelData(dataJsonByModel, isSpanish)) return;

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

                // -----------------------------
                // Generar JSON Global
                // -----------------------------

                msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.GenerateJson);

                processStopwatch = Stopwatch.StartNew();

                Dictionary<string, object> dictDataByFileToJson = GetFinalJsonDictionary(
                    projectCode, softwareLanguage, dataJsonByModel
                );

                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

                // -----------------------------
                // Exportar Excel report
                // -----------------------------

                List<string> fixedHeaders = new List<string>
                {
                    "FileName", "Handle", "Layer", "ObjectType"
                };

                msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.GenerateExcel);

                processStopwatch = Stopwatch.StartNew();

                cls_00_ExportFilteredParamDataToExcel.ExportParamDataToExcel(
                    dictDataByFileToJson, fixedHeaders, keyDataByFileName, keyFileName, infoByObjectKeys.CivilParamData,
                    infoByObjectKeys.Handle, infoByObjectKeys.Layer, infoByObjectKeys.ObjectType,
                    infoByObjectKeys.PropertySetInfo, infoByObjectKeys.Parameters,
                    infoByObjectKeys.ParName, infoByObjectKeys.ParValue
                );

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

                // -----------------------------
                // Exportar JSON Global
                // -----------------------------

                msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.ExportJson);

                processStopwatch = Stopwatch.StartNew();

                bool jsonGenerated = cls_00_SaveJson.TrySaveJson(
                    isSpanish, dictDataByFileToJson, projectCode, strRootFolderName, strJsonFileNameParamDataExp,
                    selectedFolderPath
                );

                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);


#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026

                // -----------------------------
                // Preparar JSON Global para el envío
                // -----------------------------

                msg = cls_00_ProcessMessages.ShowProcessMessage(isSpanish, cls_00_ProcessMessages.PrepareJsonForUpload);

                processStopwatch = Stopwatch.StartNew();

                List<Dictionary<string, object>> dictDataByFileToJsonToList = cls_00_GetDataListFromJson.ConvertJsonToDictionaryList(
                    dictDataByFileToJson, keyDataByFileName, isSpanish
                );

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

                // Validamos
                if (dictDataByFileToJsonToList == null || !dictDataByFileToJsonToList.Any()) return;

                // -----------------------------
                // Borrar datos previos
                // -----------------------------

                string msgDeletePreviousData = cls_00_ProcessMessages.ShowProcessMessage(
                    isSpanish, cls_00_ProcessMessages.DeletePreviousModelData
                );

                Stopwatch deletePreviousDataStopwatch = Stopwatch.StartNew();

                try
                {
                    bool deleted = await cls_00_DeleteData.DeleteAll(
                        isSpanish, dictDataByFileToJson, strEndpointDataByFileElementUrl
                    );
                    // Validamos
                    if (!deleted) return;
                }
                finally
                {
                    cls_00_ProcessMessages.AddProcessDuration(
                        processDurations, msgDeletePreviousData, deletePreviousDataStopwatch
                    );
                }

                // -----------------------------
                // Enviar JSON Global
                // -----------------------------

                string msgSendGlobal = cls_00_ProcessMessages.ShowProcessMessage(
                    isSpanish, cls_00_ProcessMessages.SendGlobalJsonByModelToServer
                );

                Stopwatch sendGlobalStopwatch = Stopwatch.StartNew();

                JsonUploadResult globalSendResult = null;

                // try
                try
                {
                    globalSendResult = await cls_00_SendJsonByChunk.SendJsonByChunk_ParamAsync(
                        isSpanish, dictDataByFileToJsonToList, dictDataByFileToJson, strEndpointDataByFileElementUrl,
                        keyParamData, keySoftwareVersion, keySoftwareLanguage, chunkSize: 5000
                    );
                }
                // catch
                catch
                {
                    globalSendResult = new JsonUploadResult();

                    /*
                     * El error se ha producido fuera del control detallado del envío.
                     * No podemos confirmar que los modelos hayan comenzado a enviarse,
                     * por lo que se registran como pendientes.
                     */
                    for (
                        int modelIndex = 0;
                        modelIndex < dictDataByFileToJsonToList.Count;
                        modelIndex++
                    )
                    {
                        Dictionary<string, object> modelEntry = dictDataByFileToJsonToList[modelIndex];

                        string fileName = modelEntry.TryGetValue(
                            cls_00_AteneaJson.FileName, out object fileNameObj
                        )
                            ? fileNameObj?.ToString()
                            : null;

                        if (string.IsNullOrWhiteSpace(fileName))
                        {
                            fileName = $"Unknown_{modelIndex}";
                        }

                        globalSendResult.PendingFileNames.Add(fileName);
                    }
                }
                // finally
                finally
                {
                    cls_00_ProcessMessages.AddProcessDuration(
                        processDurations, msgSendGlobal, sendGlobalStopwatch
                    );
                }

                // -----------------------------
                // Validar resultado global
                // -----------------------------

                if (globalSendResult == null)
                {
                    MessageBox.Show(
                        isSpanish
                            ? "No se pudo obtener el resultado del envío global."
                            : "The global upload result could not be retrieved.",
                        isSpanish ? "Error de envío" : "Send error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error
                    );
                    return;
                }

                // -----------------------------
                // Mostrar resultado del envío global
                // -----------------------------

                ShowGlobalUploadResultMessage(
                    globalSendResult.SucceededFileNames, globalSendResult.FailedFileNames,
                    globalSendResult.PendingFileNames, isSpanish
                );

                // -----------------------------
                // Gestionar fallo del envio global
                // -----------------------------

                if (!globalSendResult.DataUploadSucceeded)
                {
                    // -----------------------------
                    // Obtener modelos para reintentar
                    // -----------------------------

                    HashSet<string> retryFileNames = new HashSet<string>(
                        globalSendResult.FailedFileNames, StringComparer.OrdinalIgnoreCase
                    );
                    // Añadimos
                    retryFileNames.UnionWith(globalSendResult.PendingFileNames);

                    // -----------------------------
                    // Obtener JSON de modelos para reintentar
                    // -----------------------------

                    List<(string FileName, Dictionary<string, object> JsonData)> retryJsonDataByModel =
                        jsonDataToSendByModel.Where(modelData => !string.IsNullOrWhiteSpace(modelData.FileName) &&
                            retryFileNames.Contains(modelData.FileName)
                        ).ToList();

                    // -----------------------------
                    // Validar modelos para reintentar
                    // -----------------------------

                    if (!retryJsonDataByModel.Any())
                    {
                        MessageBox.Show(
                            isSpanish
                                ? "No se pudieron localizar los datos de los modelos que deben reintentarse."
                                : "The data for the models to retry could not be located.",
                            isSpanish ? "Error" : "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error
                        );
                        return;
                    }

                    // -----------------------------
                    // Borrar datos parciales de modelos fallidos
                    // -----------------------------

                    if (globalSendResult.FailedFileNames.Any())
                    {
                        List<Dictionary<string, object>> failedDataByFileList = FilterDataByFileNames(
                            dictDataByFileToJsonToList, globalSendResult.FailedFileNames
                        );
                        // Validamos
                        if (failedDataByFileList == null || !failedDataByFileList.Any()
                        )
                        {
                            MessageBox.Show(
                                isSpanish
                                    ? "No se pudieron localizar los datos de los modelos fallidos."
                                    : "The failed model data could not be located.",
                                isSpanish ? "Error" : "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                            return;
                        }

                        bool partialDataDeleted = await cls_00_DeleteData.DeleteByModelParamData(
                            isSpanish, failedDataByFileList, dictDataByFileToJson,
                            strEndpointDataByFileElementUrl, keyParamData
                        );
                        // Validamos
                        if (!partialDataDeleted) return;
                    }

                    // -----------------------------
                    // Enviar individualmente modelos fallidos y pendientes
                    // -----------------------------

                    string msgSendModels = cls_00_ProcessMessages.ShowProcessMessage(
                        isSpanish, cls_00_ProcessMessages.SendRetryModelsToServer
                    );

                    Stopwatch sendModelsStopwatch = Stopwatch.StartNew();

                    // -----------------------------
                    // Control de resultados
                    // -----------------------------

                    List<string> failedModels = new List<string>();

                    // -----------------------------
                    // Iterar modelos fallidos y pendientes
                    // -----------------------------

                    foreach (
                        (string FileName, Dictionary<string, object> JsonData) modelJsonInfo
                        in retryJsonDataByModel
                    )
                    {
                        // -----------------------------
                        // Crear tiempos del modelo
                        // -----------------------------

                        Dictionary<string, TimeSpan> modelSendDurations = new Dictionary<string, TimeSpan>();

                        // -----------------------------
                        // Exportar JSON del modelo
                        // -----------------------------

                        string msgGenerateModel = cls_00_ProcessMessages.ShowProcessMessage(
                            isSpanish, cls_00_ProcessMessages.GenerateJsonByModel, modelJsonInfo.FileName
                        );

                        Stopwatch modelGenerateStopwatch = Stopwatch.StartNew();

                        bool generated = cls_00_SaveJson.TrySaveJson(
                            isSpanish, modelJsonInfo.JsonData, projectCode, strRootFolderName, info.GetJsonFileNameParamDataExp(modelJsonInfo.FileName),
                            selectedFolderPath
                        );

                        cls_00_ProcessMessages.AddProcessDuration(
                            modelSendDurations, msgGenerateModel, modelGenerateStopwatch
                        );

                        /*
                         * Aunque falle la exportación local, continuamos.
                         * El envío utiliza los datos almacenados en memoria.
                         */

                        // -----------------------------
                        // Obtener datos del modelo actual
                        // -----------------------------

                        List<Dictionary<string, object>> currentModelDataList = FilterDataByFileNames(
                            dictDataByFileToJsonToList, new[] { modelJsonInfo.FileName }
                        );
                        // Validamos
                        if (currentModelDataList == null || !currentModelDataList.Any()
                        )
                        {
                            failedModels.Add(modelJsonInfo.FileName);

                            MessageBox.Show(
                                isSpanish
                                    ? $"No se pudieron localizar los datos del modelo " +
                                      $"'{modelJsonInfo.FileName}'."
                                    : $"The data for model " +
                                      $"'{modelJsonInfo.FileName}' could not be located.",
                                isSpanish ? "Datos no encontrados" : "Data not found",
                                MessageBoxButtons.OK, MessageBoxIcon.Error
                            );
                            continue;
                        }

                        // -----------------------------
                        // Avisar envío del modelo
                        // -----------------------------

                        string msgSendModel = cls_00_ProcessMessages.ShowProcessMessage(
                            isSpanish, cls_00_ProcessMessages.SendJsonByModelToServer, modelJsonInfo.FileName
                        );

                        Stopwatch modelSendStopwatch = Stopwatch.StartNew();

                        JsonUploadResult modelSendResult = null;

                        // -----------------------------
                        // Enviar unicamente este modelo
                        // -----------------------------

                        try
                        {
                            // No enviamos el Set personalizado por cada modelo
                            modelSendResult = await cls_00_SendJsonByChunk.SendJsonByChunk_ParamAsync(
                            isSpanish, currentModelDataList, modelJsonInfo.JsonData, strEndpointDataByFileElementUrl, 
                            keyParamData, keySoftwareVersion, keySoftwareLanguage, chunkSize: 5000
                        );
                        }
                        catch
                        {
                            modelSendResult = null;
                        }
                        finally
                        {
                            // Añadimos el tiempo del intento de envío
                            cls_00_ProcessMessages.AddProcessDuration(
                                modelSendDurations, msgSendModel, modelSendStopwatch
                            );
                        }

                        // -----------------------------
                        // Tiempo total del modelo
                        // -----------------------------

                        TimeSpan modelSendTotal = TimeSpan.FromTicks(
                            modelSendDurations.Values.Sum(x => x.Ticks)
                        );

                        modelSendDurations["Total"] = modelSendTotal;

                        sendDurationsByModel[modelJsonInfo.FileName] = modelSendDurations;

                        // -----------------------------
                        // Validar resultado del reenvío
                        // -----------------------------

                        bool modelUploadedCorrectly = modelSendResult != null &&
                            modelSendResult.DataUploadSucceeded &&
                            modelSendResult.SucceededFileNames.Contains(modelJsonInfo.FileName);
                        // Validamos
                        if (!modelUploadedCorrectly)
                        {
                            failedModels.Add(modelJsonInfo.FileName);

                            MessageBox.Show(
                                isSpanish
                                    ? $"No se pudo enviar correctamente la información del modelo " +
                                      $"'{modelJsonInfo.FileName}'."
                                    : $"The information for model " +
                                      $"'{modelJsonInfo.FileName}' could not be uploaded successfully.",
                                isSpanish ? "Error de envío" : "Send error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error
                            );
                        }
                    }

                    // -----------------------------
                    // Tiempo total del reenvío
                    // -----------------------------

                    cls_00_ProcessMessages.AddProcessDuration(
                        processDurations, msgSendModels, sendModelsStopwatch
                    );

                    // -----------------------------
                    // Validar resultado final
                    // -----------------------------

                    bool allModelsUploadedCorrectly = failedModels.Count == 0;
                    // Validamos
                    if (!allModelsUploadedCorrectly)
                    {
                        string failedModelsText = string.Join(Environment.NewLine, failedModels.Select(x => $"• {x}")
                        );

                        MessageBox.Show(
                            isSpanish
                                ? $"Los siguientes modelos no pudieron enviarse:\n\n" +
                                  $"{failedModelsText}"
                                : $"The following models could not be uploaded:\n\n" +
                                  $"{failedModelsText}",
                            isSpanish
                                ? "Modelos no enviados"
                                : "Models not uploaded",
                            MessageBoxButtons.OK, MessageBoxIcon.Error
                        );
                        // Finalizamos
                        return;
                    }
                }

                // -----------------------------
                // Enviar Set personalizado
                // -----------------------------

                if (!selectionModeParFromSetBool && selectedProperties != null && selectedProperties.Any())
                {
                    bool customSetSent = await cls_00_SendJsonByChunk.SendCustomParamSet(
                        isSpanish, ateneaEndpoints.EndpointPostCustomSetUrl, dictDataByFileToJson, keyParamCheck, selectedProperties
                    );

                    // Validamos
                    if (!customSetSent) return;
                }
#endif
                // -------------------------------
                // Tiempo total
                // -------------------------------

                totalStopwatch.Stop();

                processDurations["Total"] = totalStopwatch.Elapsed;

                // -------------------------------
                // Exportar informe de tiempos
                // -------------------------------

                cls_00_ExportProcessTimesToHtml.ExportProcessTimesToHtml(
                    processDurations, projectCode, strUserName, totalFiles, processedFiles, isSpanish,
                    processDurationsByModel, sendDurationsByModel, includeModelDetails: true
                );

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
