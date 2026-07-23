using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
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
using TYPSA.SharedLib.EndPoints;
using TYPSA.SharedLib.Excel;
using TYPSA.SharedLib.Json;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_00_MainAteneaParamCheckExp
    {
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

        private static void ExportCategoriesToJson(StringCollection categories)
        {
            // Convertimos a lista
            List<string> catToExport = categories.Cast<string>()
                .OrderBy(x => x).ToList();

            // Serializamos
            string json = JsonConvert.SerializeObject(catToExport, Formatting.Indented);

            // Guardamos en un archivo temporal
            string jsonPath = Path.Combine(
                Path.GetTempPath(), $"Civil3D_Categories_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            );

            File.WriteAllText(jsonPath, json);

            // Abrimos el JSON
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = jsonPath,
                    UseShellExecute = true
                });
            }
            catch
            {
                ShowStringBuilder.ShowInfo(
                    "JSON exportado", $"Archivo generado correctamente:\n{jsonPath}"
                );
            }
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

        public static async Task MainAteneaParamCheckExp(
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

            List<Dictionary<string, object>> dataJsonByModel = new List<Dictionary<string, object>>();
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
            string keyFileName = cls_00_AteneaJson.FileName;
            string keyPsetName = cls_00_AteneaJson.PsetName;
            string keyPsetData = cls_00_AteneaJson.PsetData;
            string keyPropName = cls_00_AteneaJson.PropName;
            string keyDataType = cls_00_AteneaJson.DataType;
            string keyCategoryName = cls_00_AteneaJson.CategoryName;
            string keyCategoryValue = cls_00_AteneaJson.CategoryValue;
            string keyPropId = cls_00_AteneaJson.PropId;
            string keyPropDefaultValue = cls_00_AteneaJson.PropDefaultValue;
            string keyPropDescription = cls_00_AteneaJson.PropDescription;
            string keyPropIsAutomatic = cls_00_AteneaJson.PropIsAutomatic;
            string keyPropIsVisible = cls_00_AteneaJson.PropIsVisible;
            string keyPropIsReadOnly = cls_00_AteneaJson.PropIsReadOnly;
            string keyPsetId = cls_00_AteneaJson.PsetId;
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

            // try
            try
            {
                // -----------------------------
                // Obtener categorias registradas en API
                // -----------------------------

                StringCollection allCategories = cls_00_GetAllObjectCat.GetAllObjectCatForPsetsByVersion();

                bool exportToJson = false;
                if (exportToJson)
                    // Exportamos a JSON 
                    ExportCategoriesToJson(allCategories);

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
                    // -------------------------------
                    // Definir variables
                    // -------------------------------

                    // Inicializamos la lista
                    List<Dictionary<string, object>> extractedData = new List<Dictionary<string, object>>();

                    // Obtener el nombre sin extensión
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(file);

                    // try
                    try
                    {
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
                                    // Diccionario Psets
                                    // -----------------------------

                                    // Obtener el diccionario de definiciones de Property Sets
                                    DictionaryPropertySetDefinitions dictPropSetDef = new DictionaryPropertySetDefinitions(db);

                                    // Acceder al DBDictionary interno para iterar
                                    DBDictionary dbDict = tr.GetObject(dictPropSetDef.DictionaryId, OpenMode.ForRead) as DBDictionary;
                                    // Validamos
                                    if (dbDict != null)
                                    {
                                        // Iteramos
                                        foreach (DBDictionaryEntry entry in dbDict)
                                        {
                                            // Obtenemos info
                                            string propSetName = entry.Key;
                                            ObjectId propSetDefId = entry.Value;

                                            // Obtenemos PropertySetDefinition
                                            PropertySetDefinition propSetDef = tr.GetObject(propSetDefId, OpenMode.ForRead) as PropertySetDefinition;
                                            // Validamos
                                            if (propSetDef == null) continue;

                                            // -----------------------------
                                            // Debug solo ATENEA
                                            // -----------------------------

                                            bool showInfo = false;
                                            if (showInfo)
                                                if (propSetName.Equals("ATENEA", StringComparison.OrdinalIgnoreCase))
                                                {
                                                    cls_00_GetAllObjectCat.DebugPropertySetCategories(propSetDef);
                                                }

                                            // -----------------------------
                                            // Obtener categorías del PSet
                                            // -----------------------------

                                            // Obtenemos entidades a las que aplica el Pset
                                            HashSet<string> appliesSet = cls_00_GetAllObjectCat.GetAppliesToSet(propSetDef);

                                            List<Dictionary<string, object>> categories = new List<Dictionary<string, object>>();
                                            // Iteramos
                                            foreach (string cat in allCategories)
                                            {
                                                // Comprobamos si aplica
                                                bool applies = propSetDef.AppliesToAll || appliesSet.Contains(cat);

                                                // Almacenamos
                                                categories.Add(new Dictionary<string, object>
                                                {
                                                    { keyCategoryName, cat },
                                                    { keyCategoryValue, applies }
                                                });
                                            }

                                            // -----------------------------
                                            // Crear array de propiedades
                                            // -----------------------------

                                            List<Dictionary<string, object>> psetData = new List<Dictionary<string, object>>();
                                            // Iteramos propiedades del PSet
                                            foreach (PropertyDefinition propDef in propSetDef.Definitions)
                                            {
                                                object unitType = null;
                                                object isAutomatic = null;
                                                object isVisible = null;
                                                object isReadOnly = null;

                                                try { unitType = propDef.UnitType; } catch { }
                                                try { isAutomatic = propDef.Automatic; } catch { }
                                                try { isVisible = propDef.IsVisible; } catch { }
                                                try { isReadOnly = propDef.IsReadOnly; } catch { }

                                                // Creamos objeto de propiedad
                                                Dictionary<string, object> propData = new Dictionary<string, object>
                                                    {
                                                        { keyPropId, propDef.Id.ToString() },
                                                        { keyPropName, propDef.Name },
                                                        { keyDataType, propDef.DataType.ToString() },
                                                        { keyPropDefaultValue, propDef.DefaultData },
                                                        { keyPropDescription, propDef.Description },
                                                        { keyPropIsAutomatic, isAutomatic },
                                                        { keyPropIsVisible, isVisible },
                                                        { keyPropIsReadOnly, isReadOnly },
                                                    };
                                                // Almacenamos info propiedad
                                                psetData.Add(propData);
                                            }

                                            // -----------------------------
                                            // Crear objeto PSet
                                            // -----------------------------

                                            Dictionary<string, object> propSetData = new Dictionary<string, object>
                                            {
                                                { keyPsetName, propSetName },
                                                { keyPsetId, propSetDefId.Handle.ToString() },
                                                { keyCategories, categories },
                                                { keyPsetData, psetData }
                                            };

                                            // Almacenamos PSet
                                            extractedData.Add(propSetData);
                                        }
                                    }

                                    // -----------------------------
                                    // Crear estructura JSON by file
                                    // -----------------------------

                                    Dictionary<string, object> fileDataByDoc = new Dictionary<string, object>
                                    {
                                        { keyFileName, fileName },
                                        { keyParamCheck, extractedData }
                                    };

                                    // -----------------------------
                                    // Añadir Json
                                    // -----------------------------

                                    dataJsonByModel.Add(fileDataByDoc);

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
                                        inner, 1500
                                    ).ShowDialog();
                                }
                            }

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
                // Validar info json
                // -----------------------------

                msg = isSpanish
                    ? "Validando el archivo JSON con los datos extraídos..."
                    : "Validando the JSON file with the extracted data...";
                // Mensaje
                new AutoCloseMessageForm(msg, 1000).ShowDialog();

                if (!dataJsonByModel.Any())
                {
                    // Mensaje
                    MessageBox.Show(
                        isSpanish
                        ? "No se han obtenido datos de ninguno de los modelos seleccionados. " +
                            "No se generará ningún archivo JSON."
                        : "No data was retrieved from any of the selected models. " +
                            "No JSON file will be generated.",
                        isSpanish ? "Advertencia" : "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning
                    );
                    // Finalizamos
                    return;
                }

                // -----------------------------
                // Filtrar archivo JSON por Modelo sin Categorias
                // -----------------------------

                List<Dictionary<string, object>> dataJsonByModelNoCategories = cls_00_FilterJsonBySelectedPsets.RemoveCategoriesFromJson(
                    dataJsonByModel
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
                    projectCode, softwareLanguage, dataJsonByModelNoCategories
                );
                // Exportamos
                cls_00_SaveJson.TrySaveJson(
                    isSpanish, dictDataByFileToJson, projectCode, info.RootFolderName, info.JsonFileNameParamCheckExp
                );

                // -----------------------------
                // Filtrar Psets seleccionados para el Set
                // -----------------------------

                List<Dictionary<string, object>> dataJsonByModelFiltered = cls_00_FilterJsonBySelectedPsets.FilterJsonBySelectedPsets(
                    dataJsonByModel, civilParamCheckFromSet, currentSetStatus, isSpanish
                );
                // Validamos
                if (dataJsonByModelFiltered == null) return;

                // -----------------------------
                // Exportar JSON Set 
                // -----------------------------

                msg = isSpanish
                    ? "Generando el archivo JSON del Set de Parametros con los datos extraídos..."
                    : "Generating the Parameters Set JSON file with the extracted data...";
                // Mensaje
                new AutoCloseMessageForm(msg, 1000).ShowDialog();

                int updatedSetStatus;
                Dictionary<string, object> dictDataSetToJson = cls_00_GetFinalJsonDictCivilParamCheckCons.GetFinalJsonPsetSet(
                    projectCode, softwareLanguage, dataJsonByModelFiltered, civilParamCheckFromSet, 
                    currentSetStatus, out updatedSetStatus
                );

                // -----------------------------
                // Exportar JSON Set 
                // -----------------------------

                cls_00_SaveJson.TrySaveJson(
                    isSpanish, dictDataSetToJson, projectCode, info.RootFolderName, info.JsonFileNameParamCheckSet
                );

                // -----------------------------
                // Enviar JSON por Modelo
                // -----------------------------

#if CIVIL2020 || CIVIL2021 || CIVIL2022 || CIVIL2023 || CIVIL2024 || CIVIL2025 || CIVIL2026
                await cls_00_SendJsonByChunk_Param.SendJsonByChunk_Param(
                    isSpanish, dictDataByFileToJson, ateneaEndpoints.EndpointDataByFileUrl, 
                    keyParamCheck, keySoftwareVersion, keySoftwareLanguage
                );
                // Validamos
                if (updatedSetStatus != 3)
                {
                    // -----------------------------
                    // Enviar JSON Set
                    // -----------------------------

                    await cls_00_SendJsonByChunk_ParamCheckSet.SendJsonByChunk_ParamCheckSet(
                        isSpanish, dictDataSetToJson, currentSetStatus, ateneaEndpoints.EndpointSetUrl,
                        keySoftwareVersion, keySoftwareLanguage, keyParamCheck
                    );
                }

#endif

                // -----------------------------
                // Exportar Excel report
                // -----------------------------

                List<string> headers = new List<string>
                {
                    keyFileName, keyPsetName, keyPropName, keyDataType
                };
                // Exportamos
                cls_00_ExportFilteredParamCheckToExcel.ExportFilteredParamCheckToExcel(
                    dataJsonByModel, headers, keyFileName, keyParamCheck, keyPsetName, keyPsetData, keyPropName, keyDataType
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
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // Por defecto
            return;
            
        }

    }
}

