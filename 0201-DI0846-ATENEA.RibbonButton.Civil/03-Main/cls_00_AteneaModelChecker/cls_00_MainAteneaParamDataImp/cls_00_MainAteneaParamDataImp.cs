using System;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Application = Autodesk.AutoCAD.ApplicationServices.Application;
using TYPSA.SharedLib.Civil.SetDataFromJson;
using TYPSA.SharedLib.Civil.JsonTools;
using TYPSA.SharedLib.UserForms;
using TYPSA.SharedLib.Autocad.Main;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_00_MainAteneaParamDataImp
    {
        public ProcessResult MainAteneaParamDataImp(string[] selectedFiles, string projectCode)
        {
            // Variables para recopilar métricas
            int totalSelectedFiles = selectedFiles.Length;
            int filesSelectedProcessed = 0;
            int percentage = 0;

            CadSessionInfo info = cls_00_CadInfoHelper.GetCivilSessionInfo();
            // Obtenemos data
            string civilVersion = info.CivilVersion;
            string userName = info.UserName;
            string dateTimeNow = info.DateTimeNow;
            string civilLanguage = info.CivilLanguage;

            // Indicamos la ruta del Json
            string jsonPath = @"C:\Users\psilla\OneDrive - TYPSA\00-PROY_OFERTAS\ATENEA 2.0\ParamDataExporterCivil.json";
            // Leemos el Json
            string jsonContent = File.ReadAllText(jsonPath);
            // Deserializamos el Json
            Root data = JsonConvert.DeserializeObject<Root>(jsonContent);

            // Crear el formulario de la barra de progreso
            using (ProgressBarControl progressBarForm = new ProgressBarControl())
            {
                // Mostramos
                progressBarForm.Show();

                // Iterar sobre los archivos seleccionados
                foreach (string file in selectedFiles)
                {
                    // Obtener el nombre sin extensión
                    string fileName =
                        System.IO.Path.GetFileNameWithoutExtension(file);

                    // Verificar si existe el archivo en el JSON (clave "FileName")
                    FileData matchingFile = data.DataByFileName
                        .FirstOrDefault(d => d.FileName.
                        Equals(fileName, StringComparison.OrdinalIgnoreCase));
                    // Validamos
                    if (matchingFile == null)
                    {
                        // Mensaje
                        new AutoCloseMessageForm(
                            $"❌ File '{fileName}' was not found in the JSON and will be skipped."
                        ).ShowDialog();
                        // Obviamos
                        continue;
                    }
                    // try
                    try
                    {
                        // Abrir el documento
                        using (Document openedDoc = Application.DocumentManager.Open(file, false))
                        {
                            // Validamos documento
                            if (openedDoc == null)
                            {
                                // Mensaje
                                new AutoCloseMessageForm(
                                    $"❌ Document could not be opened: {file}"
                                ).ShowDialog();
                                // Obviamos
                                continue;
                            }

                            // Obtenemos DataBase
                            Database db = openedDoc.Database;

                            bool skipFile = false;
                            // Bloquear el documento
                            using (openedDoc.LockDocument())
                            // Iniciar transacción
                            using (Transaction tr = openedDoc.TransactionManager.StartTransaction())
                            {
                                // try
                                try
                                {
                                    // Extraemos todos los PropertySetName únicos asociados a ese archivo
                                    List<string> psetNames = matchingFile.CivilParamData
                                        .SelectMany(d => d.PropertySetInfo)
                                        .Select(pset => pset.PropertySetName)
                                        .Distinct()
                                        .ToList();
                                    // Validamos 
                                    if (psetNames == null || psetNames.Count == 0)
                                    {
                                        // Mensaje
                                        new AutoCloseMessageForm(
                                            $"❌ No PropertySets found for file '{fileName}' in the JSON."
                                        ).ShowDialog();
                                        // Obviamos
                                        skipFile = true;
                                    }

                                    // Form para personalizar selección
                                    List<string>  psetNamesSelected = cls_00_InstaForm_CheckedListBox.CheckListBoxFormOut(
                                        $"Select the Property Sets to analyze in '{fileName}'. " +
                                        $"Use Ctrl + A / Ctrl + D to Select / Deselect all.",
                                        psetNames
                                    );
                                    // Validamos la selección del usuario
                                    if (psetNamesSelected == null || psetNamesSelected.Count == 0)
                                    {
                                        // Mensaje
                                        new AutoCloseMessageForm(
                                            $"⚠️ No Property Sets were selected for file '{fileName}'. " +
                                            $"Skipping file."
                                        ).ShowDialog();
                                        // Obviamos
                                        skipFile = true;
                                    }

                                    // Obtenemos todos las propiedades de los PropertySets seleccionados
                                    List<string> propNames = matchingFile.CivilParamData
                                        .SelectMany(d => d.PropertySetInfo)
                                        .Where(pset => psetNamesSelected.Contains(pset.PropertySetName))
                                        .SelectMany(pset => pset.Parameters)
                                        .Select(p => p.parName)
                                        .Distinct()
                                        .ToList();
                                    // Validamos 
                                    if (propNames == null || propNames.Count == 0)
                                    {
                                        // Mensaje
                                        new AutoCloseMessageForm(
                                            $"❌ No Properties found in selected " +
                                            $"Property Sets for file '{fileName}'."
                                        ).ShowDialog();
                                        // Obviamos
                                        skipFile = true;
                                    }

                                    // Form para personalizar selección de propiedades
                                    List<string> propNamesSelected = cls_00_InstaForm_CheckedListBox.CheckListBoxFormOut(
                                        $"Select the Properties to analyze in '{fileName}'. " +
                                        $"Use Ctrl + A / Ctrl + D to Select / Deselect all.",
                                        propNames
                                    );

                                    // Validamos la selección del usuario
                                    if (propNamesSelected == null || propNamesSelected.Count == 0)
                                    {
                                        // Mensaje
                                        new AutoCloseMessageForm(
                                            $"⚠️ No Properties were selected for file '{fileName}'. " +
                                            $"Skipping file."
                                        ).ShowDialog();
                                        // Obviamos
                                        skipFile = true;
                                    }

                                    // Diccionario de PropertySetName -> Lista de propiedades seleccionadas dentro de ese PSet
                                    Dictionary<string, List<string>> dictPsetToPropsSelected = matchingFile.CivilParamData
                                        .SelectMany(d => d.PropertySetInfo)
                                        .Where(pset => psetNamesSelected.Contains(pset.PropertySetName))
                                        .GroupBy(pset => pset.PropertySetName)
                                        .ToDictionary(
                                            g => g.Key,
                                            g => g.SelectMany(pset => pset.Parameters)
                                                    .Select(p => p.parName)
                                                    .Where(parName => propNamesSelected.Contains(parName))
                                                    .Distinct() // 🔑 Elimina duplicados
                                                    .ToList()
                                        );

                                    // Obtenemos data
                                    FileData fileData = cls_00_ProcessDocFromJson.ProcessDocFromJson(
                                        openedDoc, db, dictPsetToPropsSelected, 
                                        tr, fileName, matchingFile
                                    );
                                    // Validamos data
                                    if (fileData == null)
                                    {
                                        // Abortamos proceso
                                        tr.Abort();
                                        // Obviamos
                                        skipFile = true;
                                    }
                                    else
                                    {
                                        // Cerramos transaccion
                                        tr.Commit();
                                    }
                                }
                                // catch
                                catch (Exception ex)
                                {
                                    // Mensaje
                                    MessageBox.Show(
                                        $"Error while processing document {file}:" +
                                        $"\n{ex.Message}" +
                                        $"\nDetails:\n{ex.StackTrace}",
                                        "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error
                                    );
                                    // Obviamos
                                    skipFile = true;
                                }
                            }

                            // Al salir del `using`, el Lock y la Transaction ya se han liberado.
                            // Ahora puedes cerrar el documento.
                            if (skipFile)
                            {
                                // Cerramos documento
                                openedDoc.CloseAndDiscard();

                                // Actualizar la barra de progreso
                                filesSelectedProcessed++;
                                percentage = (int)((double)filesSelectedProcessed / totalSelectedFiles * 100);
                                progressBarForm.ProgressValue = percentage;

                                // Obviamos
                                continue;
                            }

                            // Guardar y cerrar si todo fue bien
                            string filePathName = System.IO.Path.GetFullPath(file);
                            openedDoc.CloseAndSave(filePathName);

                            // Actualizar la barra de progreso
                            filesSelectedProcessed++;
                            percentage = (int)((double)filesSelectedProcessed / totalSelectedFiles * 100);
                            progressBarForm.ProgressValue = percentage;
                        }
                    }
                    // catch
                    catch (Exception ex)
                    {
                        // Mensaje
                        MessageBox.Show(
                            $"EXCEPTION while opening document:" +
                            $"\n{ex.Message}\n{ex.StackTrace}"
                        );
                    }
                }

                // Cerramos
                progressBarForm.Close();

                // Agregar un return predeterminado al final
                return new ProcessResult { TotalFilesProcessed = 0, ParametersAnalyzed = 0 };
            }
        }






    }
}
