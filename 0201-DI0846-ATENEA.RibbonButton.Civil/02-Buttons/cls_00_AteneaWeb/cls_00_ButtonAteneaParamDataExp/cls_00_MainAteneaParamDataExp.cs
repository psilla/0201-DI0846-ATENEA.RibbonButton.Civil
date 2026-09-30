using System.Diagnostics;
using TYPSA.SharedLib.Autocad;
using TYPSA.SharedLib.Civil;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.Excel;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Civil.cls_00_UiTexts;
using static TYPSA.SharedLib.Autocad.cls_00_CadInfoHelper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Linq;


namespace TYPSA.ATENEA.RibbonButton.Civil
{
    internal class cls_00_MainAteneaParamDataExp
    {
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
            AteneaSessionInfo sessionInfo,
            UiTexts uiTexts,
            bool isSpanish
        )
        {
            // ---------------------------------
            // Obtener informacion sesion
            // ---------------------------------

            CadSessionInfo infoCad = GetCivilSessionInfo(sessionInfo);
            cls_00_AteneaEndPointsCivil ateneaEndpoints = new cls_00_AteneaEndPointsCivil();

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
            // Reiniciamos cronometro global
            // -------------------------------

            sessionInfo.StartTime = DateTime.Now;

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
                ? ateneaEndpoints.EndpointGetSetUrl
                : ateneaEndpoints.EndpointGetCustomSetUrl;

            // Aplicamos orden alfabetico solo para el Set original
            bool applyAlphabeticalOrder = selectionModeParFromSetBool;

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026 || CIVIL2027

            // -------------------------------
            // Preparar informacion Web
            // -------------------------------

            ParamExpPreparedDataCivil preparedData = await cls_00_PrepareParamWebDataAsyncCivil.ParamExpMainWebAsync(
                sessionInfo, ateneaEndpoints, selectedEndpoint, isSpanish, validateExistingSet: true
            );
            // Validamos
            if (preparedData == null) return;

#endif

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

            if (!cls_00_TryGetSelectionData.TryGetSelectionData(
                civilParamCheckFromSet, cls_00_AteneaJson.PsetName, psetsModeByUser, propsModeByUser, isSpanish,
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
            // Obtener archivos
            // -------------------------------

            bool filesResult = TYPSA.SharedLib.Autocad.cls_00_GetUserData.GetSelectedFiles(
                sessionInfo.ProjectCode, out List<string> selectedFiles, out string selectedFolderPath,
                customPathLabel: "Please, paste the folder containing the DWG files to analyze"
            );

            // -------------------------------
            // Crear progreso
            // -------------------------------

            ProgressBarControl progressBarForm = new ProgressBarControl();
            // Iniciamos variables
            int totalFiles = selectedFiles.ToArray().Length;
            int processedFiles = 0;

            // Mostramos
            progressBarForm.ProgressValue = 10; // Valor inicial 
            progressBarForm.Show();

            // try
            try
            {
                // -----------------------------
                // Procesar modelos seleccionados
                // -----------------------------

                cls_00_ProcessFilesParamDataExp.ProcessSelectedModels(
                    sessionInfo, selectedFiles.ToArray(), entitiesModeByUser, layersModeByUser, psetsModeByUser, 
                    propsModeByUser, defaultPsetsToUse, defaultPropsToUse, cls_00_AteneaSession.Email,
                    infoByObjectKeys, dataJsonByModel, jsonDataToSendByModel, processDurations,
                    processDurationsByModel, progressBarForm, isSpanish, ref processedFiles
                );

                // -----------------------------
                // Cerrar progreso
                // -----------------------------

                progressBarForm.Close();

                // -----------------------------
                // Preparar informacion modelos
                // -----------------------------

                Dictionary<string, object> dictDataByFileToJson = cls_00_AteneaSessionInfo.GetFinalJsonDictionary(
                    cls_00_AteneaSession.Email, cls_00_AteneaJson.CivilVersion, cls_00_AteneaJson.CivilLanguage, 
                    dataJsonByModel, sessionInfo
                );

                PreparedModelDataResult preparedModelData = cls_00_PrepareDataModelCheck.PrepareModelData(
                    sessionInfo, infoCad.RootFolderName, infoCad.JsonFileNameParamDataExp, dictDataByFileToJson,
                    selectedFolderPath, dataJsonByModel, processDurations, isSpanish
                );
                // Validamos
                if (!preparedModelData.Success) return;

                // -----------------------------
                // Obtener informacion preparada
                // -----------------------------

                List<Dictionary<string, object>> dictDataByFileToJsonToList = preparedModelData.DataByFileToJsonToList;

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
                    dictDataByFileToJson, fixedHeaders, cls_00_AteneaJson.DataByFileName, cls_00_AteneaJson.FileName, infoByObjectKeys.CivilParamData,
                    infoByObjectKeys.Handle, infoByObjectKeys.Layer, infoByObjectKeys.ObjectType,
                    infoByObjectKeys.PropertySetInfo, infoByObjectKeys.Parameters,
                    infoByObjectKeys.ParName, infoByObjectKeys.ParValue
                );

                // Añadimos
                cls_00_ProcessMessages.AddProcessDuration(processDurations, msg, processStopwatch);

                // -----------------------------
                // Construir payload Set personalizado
                // -----------------------------

                Dictionary<string, object> dictToVal = null;
                // Validamos
                if (!selectionModeParFromSetBool && selectedProperties != null && selectedProperties.Any())
                {
                    dictToVal = cls_00_SendJsonByChunk.GetCustomParamSetPayloadCivil(
                        sessionInfo, cls_00_AteneaJson.CivilParamCheck, selectedProperties
                    );
                }

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026 || CIVIL2027

                // -----------------------------
                // Gestionar datos existentes y envío
                // -----------------------------

                bool uploaded = await cls_00_HandleData.HandleData(
                    sessionInfo, cls_00_AteneaSession.AccessToken, isSpanish, dictToVal, dictDataByFileToJson, 
                    dictDataByFileToJsonToList, jsonDataToSendByModel, ateneaEndpoints.EndpointDataByFileElementUrl,
                    ateneaEndpoints.EndpointPostCustomSetUrl, cls_00_AteneaJson.CivilParamData, infoCad.RootFolderName, 
                    selectedFolderPath, selectionModeParFromSetBool, selectedProperties, 
                    fileName => infoCad.GetJsonFileNameParamDataExp(fileName), processDurations, sendDurationsByModel
                );
                // Validamos
                if (!uploaded) return;

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
                    sessionInfo, selectedFolderPath, processDurations, preparedData.UserNameBySso, totalFiles, processedFiles, isSpanish,
                    processDurationsByModel, sendDurationsByModel, includeModelDetails: true
                );

                // -------------------------------
                // Summary
                // -------------------------------

                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - sessionInfo.StartTime;

                // Mensaje
                MessageBox.Show(
                    uiTexts.MsgCompleted +
                    "\nDuration: " + duration.ToString(@"hh\:mm\:ss") +
                    "\nStarted at: " + sessionInfo.StartTime.ToString("HH:mm:ss") +
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
