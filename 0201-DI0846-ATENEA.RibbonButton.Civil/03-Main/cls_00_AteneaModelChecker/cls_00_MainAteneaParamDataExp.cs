using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Newtonsoft.Json;
using TYPSA.MC.RibbonButton.Civil.ExportJSON;
using TYPSA.SharedLib.Civil.DictInfoByObject;
using TYPSA.SharedLib.Civil.ExportToExcel;
using TYPSA.SharedLib.Civil.Main;
using TYPSA.SharedLib.UserForms;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CivilInfoHelper;
using TYPSA.SharedLib.Autocad.DbObjectsByType;
using TYPSA.SharedLib.Autocad.Main;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_00_MainAteneaParamDataExp
    {
        public async Task MainAteneaParamDataExp(
            string[] selectedFiles, 
            string projectCode,
            bool isSpanish,
            CivilSessionInfo info
        )
        {
            // Variables para recopilar métricas
            int totalSelectedFiles = selectedFiles.Length;
            int filesSelectedProcessed = 0;
            int percentage = 0;

            // Diccionario principal para agrupar los resultados de todos los archivos
            Dictionary<string, Dictionary<string, Dictionary<string, object>>> globalResult =
            new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();

            // Creamos una lista vacia para almacenar diccionario por modelo
            List<Dictionary<string, Dictionary<string, Dictionary<string, object>>>> dataTot =
            new List<Dictionary<string, Dictionary<string, Dictionary<string, object>>>>();

            // -----------------------------
            // FORM DE SELECCION
            // -----------------------------

            string formMessage = isSpanish
                ? "Seleccione el modo de filtrado para cada categoría.\n\n" +
                  "• All: se analizarán todos los elementos disponibles.\n" +
                  "• Manual: podrá seleccionar manualmente los elementos a analizar."
                : "Select the filtering mode for each category.\n\n" +
                  "• All: all available elements will be analyzed.\n" +
                  "• Manual: you will be able to manually select the elements to analyze.";
            string formTitle = isSpanish
                ? "Modos de Selección"
                : "Selection Modes";

            // Campos
            var fields = new List<(string propiedad, List<string> options, string valorDefecto)>
            {
                (
                    isSpanish ? "Selección de entidades" : "Entities selection",
                    new List<string> { SelectionModes.All, SelectionModes.Manual },
                    SelectionModes.All
                ),
                (
                    isSpanish ? "Selección de capas" : "Layers selection",
                    new List<string> { SelectionModes.All, SelectionModes.Manual },
                    SelectionModes.All
                ),
                (
                    isSpanish ? "Selección de Property Sets" : "Property Sets selection",
                    new List<string> { SelectionModes.All, SelectionModes.Manual },
                    SelectionModes.All
                ),
                (
                    isSpanish ? "Selección de propiedades" : "Properties selection",
                    new List<string> { SelectionModes.All, SelectionModes.Manual },
                    SelectionModes.All
                )
            };
            // Form
            Dictionary<string, string> selectionResult =
                cls_00_InstaForm_ComboBox.ComboBoxFormOut_NextToLabel(
                    formMessage, fields, formTitle: formTitle
                );
            // Validamos
            if (selectionResult == null) return;

            // -----------------------------
            // KEYS
            // -----------------------------

            string keyEntities = isSpanish ? "Selección de entidades" : "Entities selection";
            string keyLayers = isSpanish ? "Selección de capas" : "Layers selection";
            string keyPsets = isSpanish ? "Selección de Property Sets" : "Property Sets selection";
            string keyProps = isSpanish ? "Selección de propiedades" : "Properties selection";

            // -----------------------------
            // MAPEO
            // -----------------------------

            string entitiesModeByUser = selectionResult[keyEntities];
            string layersModeByUser = selectionResult[keyLayers];
            string psetsModeByUser = selectionResult[keyPsets];
            string propsModeByUser = selectionResult[keyProps];

            // try
            try
            {
                // Crear el formulario de la barra de progreso
                using (ProgressBarControl progressBarForm = new ProgressBarControl())
                {
                    // Mostramos
                    progressBarForm.Show();

                    // Iterar sobre los archivos seleccionados
                    foreach (string file in selectedFiles)
                    {
                        // try
                        try
                        {
                            // Abrimos el modelo en modo lectura para agilizar la apertura
                            using (Document openedDoc = Application.DocumentManager.Open(file, false))
                            {
                                // Validamos
                                if (openedDoc == null)
                                {
                                    // Mensaje
                                    new AutoCloseMessageForm(
                                        $"Error opening document:\n{file}"
                                    ).ShowDialog();

                                    // Actualizar la barra de progreso
                                    filesSelectedProcessed++;
                                    percentage = (int)((double)filesSelectedProcessed / totalSelectedFiles * 100);
                                    progressBarForm.ProgressValue = percentage;

                                    // Obviamos
                                    continue;
                                }

                                // Obtenemos el fileName
                                string fileName = Path.GetFileName(file);

                                // Obtenemos el dict
                                Dictionary<string, Dictionary<string, Dictionary<string, object>>> fileData =
                                    cls_00_DictInfoByObjectPsetProp.dicc_InfoDiccByObjectByFileCustomPsetProp(
                                        openedDoc,
                                        entitiesSelectionMode: entitiesModeByUser, layerSelectionMode: layersModeByUser,
                                        psetSelectionMode: psetsModeByUser, propSelectionMode: propsModeByUser
                                    );
                                // Validamos
                                if (fileData == null)
                                {
                                    // Cerramos documento
                                    openedDoc.CloseAndDiscard();

                                    // Actualizar la barra de progreso
                                    filesSelectedProcessed++;
                                    percentage = (int)((double)filesSelectedProcessed / totalSelectedFiles * 100);
                                    progressBarForm.ProgressValue = percentage;

                                    // Finalizamos
                                    continue;
                                }

                                // Almacenamos el dicc en la lista vacia
                                dataTot.Add(fileData);

                                // Cerramos documento
                                openedDoc.CloseAndDiscard();

                                // Actualizar la barra de progreso
                                filesSelectedProcessed++;
                                percentage = (int)((double)filesSelectedProcessed / totalSelectedFiles * 100);
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

                    // Cerramos
                    progressBarForm.Close();

                    // Validamos dataTot antes de seguir
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

                    // Combinamos todos los diccionarios almacenados
                    foreach (var diccionario in dataTot)
                    {
                        foreach (var kvp in diccionario)
                        {
                            globalResult[kvp.Key] = kvp.Value;
                        }
                    }
                    // Validamos
                    if (globalResult == null) return;

                    // Transformamos el dict
                    Dictionary<string, List<List<object>>> dictTransToList =
                        cls_00_ExportToExcel_OpenXml.GetPropertySetExportData(globalResult);
                    // Validamos
                    if (dictTransToList == null) return;

                    // Construimos el diccionario Str-Obj
                    List<Dictionary<string, object>> dataJsonByModel =
                        cls_00_ExportToJson.BuildDictByDataStrObj(globalResult);

                    // -----------------------------
                    // GENERAR ARCHIVO JSON
                    // -----------------------------

                    // try
                    try
                    {
                        // Preparar diccionario a enviar
                        Dictionary<string, object> dictDataByFileToJson = GetFinalJsonDictionary(
                            projectCode, dataJsonByModel
                        );
                        // Serializar el diccionario a formato JSON con indentación
                        string jsonContent = JsonConvert.SerializeObject(
                            dictDataByFileToJson, Formatting.Indented
                        );
                        // Guardamos el Json
                        SaveJsonToDesktop(
                            isSpanish, jsonContent, projectCode, info.RootFolderName, info.JsonFileNameParamDataExp
                        );
                    }
                    // catch
                    catch (System.Exception ex)
                    {
                        string inner = ex.InnerException != null
                            ? $"\n\nInner:\n{ex.InnerException.Message}"
                            : "";
                        // Mostramos
                        MessageBox.Show(
                            "Error generating JSON\n\n" + ex.Message + inner + "\n\nStack:\n" + ex.StackTrace,
                            "JSON Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error
                        );
                    }

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
                    new AutoCloseMessageForm(
                        message, 2000
                    ).ShowDialog();

                    cls_00_MainExportBack_Metrics metrics = new cls_00_MainExportBack_Metrics();
                    // Enviar métricas
                    metrics.SendMetrics(totalSelectedFiles, uniqueParam * uniqueLayersSet.Count, projectCode);
                }
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
