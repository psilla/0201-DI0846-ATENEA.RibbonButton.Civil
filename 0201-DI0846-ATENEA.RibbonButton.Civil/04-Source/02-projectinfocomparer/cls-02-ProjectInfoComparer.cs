using Autodesk.AutoCAD.ApplicationServices;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using System.Collections;
using TYPSA.PS.RibbonButton.Civil.Source.Class.ExcelData;
using TYPSA.SharedLib.Civil.LogErrors;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.ProjectInfo
{
    internal class cls_02_ProjectInfoComparer
    {
        private cls_00_LogErrors logger = new cls_00_LogErrors();

        public void GetProjectInfo(Document doc, string projectCode, string excelPath)
        {
            string fileName = Path.GetFileName(doc.Name);

            //string message = $"El código del proyecto es {projectCode} y el nombre del archivo es {fileName}";
            //System.Windows.MessageBox.Show(message, "Info");

            Editor ed = doc.Editor;

            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                try
                {
                    // Abre la base de datos del documento para lectura
                    Database db = doc.Database;

                    // Obtener las propiedades personalizadas usando la extensión
                    Dictionary<string, string> customProperties = db.GetCustomProperties();

                    // Construye el mensaje con las propiedades personalizadas
                    StringBuilder messageProperties = new StringBuilder();
                    messageProperties.AppendLine("Propiedades Personalizadas:");

                    if (customProperties.Count > 0)
                    {
                        foreach (var prop in customProperties)
                        {
                            messageProperties.AppendLine($"{prop.Key}: {prop.Value}");
                        }

                        // Mostrar las propiedades en una ventana emergente
                        //System.Windows.MessageBox.Show(messageProperties.ToString(), "Propiedades Personalizadas");
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("No se encontraron propiedades personalizadas en el documento.", "Información");
                    }

                    // Leer los encabezados del archivo Excel y actualizar el archivo con las propiedades personalizadas
                    cls_00_ExcelData excelData = new cls_00_ExcelData();               
                    string sheetName = "C3D-ProjectInfo";

                    excelData.SetExcelDataProjectInfo(excelPath, sheetName, fileName, customProperties);

                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError: {ex.Message}");
                    System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error");
                }
            }
        }
    }

    // Métodos de extensión para la clase Database
    public static class DatabaseExtension
    {
        public static Dictionary<string, string> GetCustomProperties(this Database db)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            IDictionaryEnumerator dictEnum = db.SummaryInfo.CustomProperties;
            while (dictEnum.MoveNext())
            {
                DictionaryEntry entry = dictEnum.Entry;
                result.Add((string)entry.Key, (string)entry.Value);
            }
            return result;
        }

    }
    
}

