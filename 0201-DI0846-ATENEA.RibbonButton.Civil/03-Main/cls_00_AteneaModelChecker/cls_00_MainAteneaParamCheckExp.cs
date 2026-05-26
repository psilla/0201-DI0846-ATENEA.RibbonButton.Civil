using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Forms;
using Autodesk.Aec.PropertyData.DatabaseServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Newtonsoft.Json;
using TYPSA.SharedLib.Autocad.GetDocument;
using TYPSA.SharedLib.UserForms;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CivilInfoHelper;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using TYPSA.SharedLib.Autocad.Main;


namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_00_MainAteneaParamCheckExp
    {
        private static StringCollection GetCategoriesFromDrawing(Transaction tr, Database db)
        {
            HashSet<string> categories = new HashSet<string>();

            try
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId entId in btr)
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;

                        if (ent == null) continue;

                        string className = ent.GetRXClass().Name;

                        categories.Add(className);
                    }
                }
            }
            catch
            {
                // opcional log
            }

            StringCollection result = new StringCollection();

            foreach (string cat in categories)
                result.Add(cat);

            return result;
        }

        private static HashSet<string> GetAppliesToSet(PropertySetDefinition propSetDef)
        {
            HashSet<string> appliesSet = new HashSet<string>();

            if (!propSetDef.AppliesToAll)
            {
                var appliesTo = propSetDef.AppliesToFilter;

                if (appliesTo != null)
                {
                    foreach (string cls in appliesTo)
                    {
                        appliesSet.Add(cls);
                    }
                }
            }

            return appliesSet;
        }

        public ProcessResult MainAteneaParamCheckExp(
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

            // Creamos una lista vacia para almacenar diccionario global
            List<Dictionary<string, object>> dataJsonByModel = new List<Dictionary<string, object>>();

            // Crear el formulario de la barra de progreso
            using (ProgressBarControl progressBarForm = new ProgressBarControl())
            {
                // Mostramos barra de progreso
                progressBarForm.Show();

                // try
                try
                {
                    // Iterar sobre los archivos seleccionados
                    foreach (string file in selectedFiles)
                    {
                        // Obtener el nombre sin extensión
                        string fileName = System.IO.Path.GetFileNameWithoutExtension(file);

                        // try
                        try
                        {
                            // Abrir el documento
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

                                // Obtenemos variables
                                Database db = openedDoc.Database;
                                Editor ed = cls_00_DocumentInfo.GetEditor(openedDoc);

                                // Bloquear el documento
                                using (openedDoc.LockDocument())
                                using (Transaction tr = openedDoc.TransactionManager.StartTransaction())
                                {
                                    // try
                                    try
                                    {
                                        // Obtener BlockTable
                                        BlockTable bt = cls_00_DocumentInfo.GetBlockTableForRead(tr, db);

                                        // Inicializamos la lista
                                        List<Dictionary<string, object>> extractedData = new List<Dictionary<string, object>>();

                                        // -----------------------------
                                        // DICCIONARIO PSETS
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

                                                // Iteramos
                                                foreach (PropertyDefinition propDef in propSetDef.Definitions)
                                                {
                                                    // Obtenemos entidades a las que aplica el Pset
                                                    HashSet<string> appliesSet = GetAppliesToSet(propSetDef);

                                                    List<Dictionary<string, object>> categories = new List<Dictionary<string, object>>();
                                                    // Iteramos
                                                    foreach (string cat in GetCategoriesFromDrawing(tr, db))
                                                    {
                                                        // Comprobamos si aplica
                                                        bool applies = propSetDef.AppliesToAll || appliesSet.Contains(cat);
                                                        // Almacenamos
                                                        categories.Add(new Dictionary<string, object>
                                                        {
                                                            { AteneaJson.CategoryName, cat },
                                                            { AteneaJson.CategoryValue, applies }
                                                        });
                                                    }

                                                    object unitType = null;
                                                    object isAutomatic = null;
                                                    object isVisible = null;
                                                    object isReadOnly = null;
                                                    try { unitType = propDef.UnitType; } catch { }
                                                    try { isAutomatic = propDef.Automatic; } catch { }
                                                    try { isVisible = propDef.IsVisible; } catch { }
                                                    try { isReadOnly = propDef.IsReadOnly; } catch { }
                                                    // Creamos objeto
                                                    Dictionary<string, object> row = new Dictionary<string, object>
                                                    {
                                                        { AteneaJson.PsetName, propSetName },
                                                        { AteneaJson.PsetId, propSetDefId.Handle.ToString() },

                                                        { AteneaJson.PropId, propDef.Id.ToString() },
                                                        { AteneaJson.PropName, propDef.Name },
                                                        { AteneaJson.DataType, propDef.DataType.ToString() },

                                                        { AteneaJson.PropDefaultValue, propDef.DefaultData },
                                                        { AteneaJson.PropDescription, propDef.Description },
                                                        { AteneaJson.PropIsAutomatic, isAutomatic },
                                                        { AteneaJson.PropIsVisible, isVisible },
                                                        { AteneaJson.PropIsReadOnly, isReadOnly },
                                                        //{ AteneaJson.PropUnitType, unitType },

                                                        { AteneaJson.Categories, categories }
                                                    };

                                                    // Almacenamos
                                                    extractedData.Add(row);
                                                }
                                            }
                                        }

                                        // -----------------------------
                                        // CREAR ESTRUCTURA JSON BY FILE
                                        // -----------------------------

                                        // Creamos la estructura
                                        Dictionary<string, object> fileDataByDoc = new Dictionary<string, object>
                                        {
                                            { AteneaJson.FileName, fileName },
                                            { AteneaJson.CivilParamCheck, extractedData }
                                        };

                                        // Añadimos a la lista global
                                        dataJsonByModel.Add(fileDataByDoc);

                                        // Cerramos
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

                                // Cerramos y descartamos documento
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
                            isSpanish, jsonContent, projectCode, info.RootFolderName, info.JsonFileNameParamCheckExp
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

                    // return
                    return new ProcessResult
                    {
                        TotalFilesProcessed = filesSelectedProcessed,
                        ParametersAnalyzed = filesSelectedProcessed
                    };
                }
                // catch
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                // Por defecto
                return new ProcessResult();
            }
        }

    }
}
