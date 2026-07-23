using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Interop;
using Autodesk.Aec.PropertyData.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Newtonsoft.Json;
using TYPSA.SharedLib.Autocad.GetDocument;
using TYPSA.SharedLib.Autocad.Main;
using TYPSA.SharedLib.Civil.GetPropertyData;
using TYPSA.SharedLib.Civil.GetPsetData;
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_00_MainAteneaParamCheckImp
    {
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

        private static void AppendUpdatedPsetInfo(
            StringBuilder sbModelParamCheckImpInfo,
            string pSetNameFromJson,
            bool categoriesUpdated,
            HashSet<string> pSetCatFromDoc,
            HashSet<string> pSetCatFromJson,
            List<string> addedProperties,
            List<string> removedProperties
        )
        {
            // Property Set
            sbModelParamCheckImpInfo.AppendLine($"Property Set: {pSetNameFromJson}");

            // Validamos si ya estaba actualizado
            if (!categoriesUpdated && addedProperties.Count == 0 && removedProperties.Count == 0)
            {
                sbModelParamCheckImpInfo.AppendLine("  ✔ Property Set already up to date.");

                sbModelParamCheckImpInfo.AppendLine();
                return;
            }

            // -----------------------------
            // Categorías actualizadas
            // -----------------------------

            if (categoriesUpdated)
            {
                sbModelParamCheckImpInfo.AppendLine("  Categories updated:");

                sbModelParamCheckImpInfo.AppendLine("    Previous:");
                foreach (string cat in pSetCatFromDoc.OrderBy(x => x))
                {
                    sbModelParamCheckImpInfo.AppendLine($"      - {cat}");
                }

                sbModelParamCheckImpInfo.AppendLine("    Current:");
                foreach (string cat in pSetCatFromJson.OrderBy(x => x))
                {
                    sbModelParamCheckImpInfo.AppendLine($"      - {cat}");
                }
            }

            // -----------------------------
            // Propiedades añadidas
            // -----------------------------

            if (addedProperties.Count > 0)
            {
                sbModelParamCheckImpInfo.AppendLine("  Properties added:");

                foreach (string propName in addedProperties.OrderBy(x => x))
                {
                    sbModelParamCheckImpInfo.AppendLine($"    - {propName}");
                }
            }

            // -----------------------------
            // Propiedades eliminadas
            // -----------------------------

            if (removedProperties.Count > 0)
            {
                sbModelParamCheckImpInfo.AppendLine("  Properties removed:");

                foreach (string propName in removedProperties.OrderBy(x => x))
                {
                    sbModelParamCheckImpInfo.AppendLine($"    - {propName}");
                }
            }

            sbModelParamCheckImpInfo.AppendLine();
        }

        private static void AppendCreatedPsetInfo(
            StringBuilder sbModelParamCheckImpInfo,
            string pSetNameFromJson,
            List<string> entToApplyFromJson,
            Dictionary<string, Autodesk.Aec.PropertyData.DataType> pSetPropFromJson
        )
        {
            sbModelParamCheckImpInfo.AppendLine($"Property Set: {pSetNameFromJson}");
            sbModelParamCheckImpInfo.AppendLine("  ✔ Property Set created.");

            sbModelParamCheckImpInfo.AppendLine("  Categories:");
            foreach (string cat in entToApplyFromJson.OrderBy(x => x))
            {
                sbModelParamCheckImpInfo.AppendLine($"    - {cat}");
            }

            sbModelParamCheckImpInfo.AppendLine("  Properties:");
            foreach (string propName in pSetPropFromJson.Keys.OrderBy(x => x))
            {
                sbModelParamCheckImpInfo.AppendLine($"    - {propName}");
            }

            sbModelParamCheckImpInfo.AppendLine();
        }

        private static Dictionary<string, object> GetJsonData(string jsonSetPath)
        {
            // Leemos JSON
            string json = File.ReadAllText(jsonSetPath);
            // return
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
        }

        private static List<string> GetSelectedPsets(
            List<string> existingPsetNamesInSet,
            bool isSpanish
        )
        {
            // -------------------------------
            // Obtener info
            // -------------------------------

            string formTitle = isSpanish
                ? "Selecciona los Property Sets que quieres analizar en la web"
                : "Select the Property Sets you want to analyze on the web";
            // return
            return cls_00_InstaForm_CheckedListBox.CheckListBoxFormSearchOut(
                formTitle, existingPsetNamesInSet, existingPsetNamesInSet
            );
        }

        private static List<Dictionary<string, object>> GetFilteredPsetsFromJson(
            List<Dictionary<string, object>> civilParamCheckFromSet,
            List<string> selectedPsets
        )
        {
            // -------------------------------
            // Obtener info
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

        public static async Task MainAteneaParamCheckImp(
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

            StringBuilder sbParamCheckImpInfo = new StringBuilder();
            Dictionary<string, Autodesk.Aec.PropertyData.DataType> dataTypeDict = cls_00_GetDictDataType.GetDataTypeDictionary();
            List<string> allowedDataTypes = new List<string> { "Text", "Integer", "Real", "List", "TrueFalse" };
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
            string keyPsetData = cls_00_AteneaJson.PsetData;
            string keyCategories = cls_00_AteneaJson.Categories;

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
            // Seleccion personalizada pSets del JSON
            // -------------------------------

            // Form
            List<string> selectedPsets = GetSelectedPsets(
                existingPsetNamesInSet, isSpanish
            );
            // Validamos
            if (selectedPsets == null || selectedPsets.Count == 0) return;

            // -------------------------------
            // Filtrar Psets seleccionados
            // -------------------------------

            List<Dictionary<string, object>> filteredPsets = GetFilteredPsetsFromJson(
                civilParamCheckFromSet, selectedPsets
            );

            // -------------------------------
            // Diccionario DataType As String: DataType Value
            // -------------------------------

            Dictionary<string, Autodesk.Aec.PropertyData.DataType> dataTypeDictFiltered = dataTypeDict.Where(
                x => allowedDataTypes.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value
            );

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

                        StringBuilder sbModelParamCheckImpInfo = new StringBuilder();
                        bool modelHasUpdates = false;

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

                            // -------------------------------
                            // Obtener info
                            // -------------------------------

                            Database db = openedDoc.Database;
                            Editor ed = cls_00_DocumentInfo.GetEditor(openedDoc);

                            // -------------------------------
                            // Obtener diccionario de Property Sets de la base de datos
                            // -------------------------------

                            DictionaryPropertySetDefinitions dictPropSetDef = new DictionaryPropertySetDefinitions(db);

                            // -------------------------------
                            // Bloquear el documento
                            // -------------------------------

                            using (openedDoc.LockDocument())
                            using (Transaction tr = openedDoc.TransactionManager.StartTransaction())
                            {
                                // try
                                try
                                {
                                    // Obtener BlockTable
                                    BlockTable bt = cls_00_DocumentInfo.GetBlockTableForRead(tr, db);

                                    // -----------------------------
                                    // Iterar Psets seleccionados
                                    // -----------------------------
                                
                                    foreach (Dictionary<string, object> pset in filteredPsets)
                                    {
                                        // -------------------------------
                                        // Definir variables
                                        // -------------------------------

                                        ObjectId propSetDefId = ObjectId.Null;
                                        Dictionary<string, Autodesk.Aec.PropertyData.DataType> pSetPropFromJson =
                                            new Dictionary<string, Autodesk.Aec.PropertyData.DataType>();
                                        Dictionary<string, Autodesk.Aec.PropertyData.DataType> pSetPropFromDoc =
                                            new Dictionary<string, Autodesk.Aec.PropertyData.DataType>();

                                        // -------------------------------
                                        // Validar info json
                                        // -------------------------------

                                        if (!pset.ContainsKey(keyPsetName) || pset[keyPsetName] == null) continue;
                                        if (!pset.ContainsKey(keyPsetData) || pset[keyPsetData] == null) continue;
                                        if (!pset.ContainsKey(keyCategories) || pset[keyCategories] == null) continue;
                                     
                                        // -------------------------------
                                        // Obtener info json
                                        // -------------------------------

                                        string pSetNameFromJson = pset[keyPsetName].ToString();
                                        List<Dictionary<string, object>> pSetDataFromJson = 
                                            JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(pset[keyPsetData].ToString());
                                        List<Dictionary<string, object>> psetcatfromjson = 
                                            JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(pset[keyCategories].ToString());

                                        // -------------------------------
                                        // Obtener Properties Pset
                                        // -------------------------------
                                           
                                        pSetPropFromJson = cls_00_GetDataParamCheckImp.GetPropFromJson(
                                            pSetDataFromJson, dataTypeDictFiltered
                                        );
                                        // Validamos 
                                        if (pSetPropFromJson.Count == 0) continue;

                                        // -------------------------------
                                        // Obtener entidades a aplicar desde JSON
                                        // -------------------------------

                                        List<string> entToApplyFromJson = cls_00_GetDataParamCheckImp.GetEntitiesToApplyFromJson(psetcatfromjson);
                                        // Validamos
                                        if (entToApplyFromJson.Count == 0) continue;
                                      
                                        // Convertimos
                                        HashSet<string> pSetCatFromJson = new HashSet<string>(entToApplyFromJson);

                                        // -------------------------------
                                        // Comprobar existencia del Pset
                                        // -------------------------------

                                        bool propSetExistsInDoc = dictPropSetDef.Has(pSetNameFromJson, tr);
                                        // Existe
                                        if (propSetExistsInDoc)
                                        {
                                            // -------------------------------
                                            // Obtener def del Pset
                                            // -------------------------------

                                            propSetDefId = dictPropSetDef.GetAt(pSetNameFromJson);
                                            // Obtenemos 
                                            PropertySetDefinition propSetDef = tr.GetObject(propSetDefId, OpenMode.ForWrite) as PropertySetDefinition;
                                            // Validamos
                                            if (propSetDef == null) continue;
                                          
                                            // -----------------------------
                                            // Obtener entidades del PSet del documento
                                            // -----------------------------

                                            HashSet<string> pSetCatFromDoc = cls_00_GetAllObjectCat.GetAppliesToSet(propSetDef);

                                            // -----------------------------
                                            // Comparar entidades json vs documento
                                            // -----------------------------

                                            bool categoriesUpdated = false;
                                            if (!pSetCatFromJson.SetEquals(pSetCatFromDoc))
                                            {
                                                // Actualizamos las entidades a las que aplica el PSet
                                                cls_00_GetDataParamCheckImp.UpdatePsetAppliesTo(propSetDef, entToApplyFromJson);
                                                // Definimos
                                                categoriesUpdated = true;
                                            }

                                            // -------------------------------
                                            // Obtener propiedades del documento
                                            // -------------------------------

                                            pSetPropFromDoc = cls_00_GetDataParamCheckImp.GetExistingProperties(propSetDef);

                                            // -------------------------------
                                            // Revisar propiedades existentes
                                            // -------------------------------

                                            List<string> addedProperties;
                                            List<string> removedProperties;
                                            cls_00_GetDataParamCheckImp.UpdateExistingPsetDefinition(
                                                propSetDef, pSetPropFromJson, pSetPropFromDoc, out addedProperties, out removedProperties
                                            );

                                            // Mostramos info
                                            AppendUpdatedPsetInfo(
                                                sbModelParamCheckImpInfo, pSetNameFromJson, categoriesUpdated, pSetCatFromDoc, 
                                                pSetCatFromJson, addedProperties, removedProperties
                                            );
                                            // Obviamos
                                            continue;

                                        }

                                        // -------------------------------
                                        // Crear Pset
                                        // -------------------------------

                                        propSetDefId = cls_00_GetDataParamCheckImp.CreatePsetDefinition(
                                            tr, db, pSetNameFromJson, pSetPropFromJson, entToApplyFromJson, dictPropSetDef, propSetDefId
                                        );
                                        // Validamos
                                        if (propSetDefId.IsNull)
                                        {
                                            continue;
                                        }

                                        // Mostramos
                                        AppendCreatedPsetInfo(
                                            sbModelParamCheckImpInfo, pSetNameFromJson, entToApplyFromJson, pSetPropFromJson
                                        );
                                    }

                                    // -----------------------------
                                    // Cerrar transaccion
                                    // -----------------------------

                                    tr.Commit();
                                }
                                // catch
                                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                                {
                                    // Mensaje
                                    string inner = ex.InnerException != null
                                        ? $"\n\nInner:\n{ex.InnerException.Message}"
                                        : "";
                                    new AutoCloseMessageForm(
                                        $"Error while processing '{file}':\n\n" +
                                        $"{ex.Message}\n\n" +
                                        $"Stack:\n{ex.StackTrace}" +
                                        inner, 3000
                                    ).ShowDialog();
                                }
                            }

                            // Mostramos
                            if (sbModelParamCheckImpInfo.Length > 0)
                            {
                                sbParamCheckImpInfo.AppendLine("========================================");
                                sbParamCheckImpInfo.AppendLine($"Model: {fileName}");
                                sbParamCheckImpInfo.AppendLine("========================================");
                                sbParamCheckImpInfo.AppendLine(sbModelParamCheckImpInfo.ToString());
                            }

                            // -----------------------------
                            // Cerrar documento
                            // -----------------------------

                            string filePathName = System.IO.Path.GetFullPath(file);
                            openedDoc.CloseAndSave(filePathName);

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

                // Mostramos
                if (sbParamCheckImpInfo.Length > 0)
                {
                    ShowStringBuilder.ShowInfo(
                        "⚠ Property Set Updates Found:", sbParamCheckImpInfo.ToString()
                    );
                }
            }
            // catch
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        
    }
}
