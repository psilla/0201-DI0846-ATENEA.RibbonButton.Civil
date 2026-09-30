using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TYPSA.PS.RibbonButton.Civil.Source.Class.ExcelData;
using TYPSA.SharedLib.UserForms;

namespace TYPSA.PS.RibbonButton.Civil._05_Source.Class.ExcelData
{
    public class cls_00_ExcelManager
    {
        public string ExcelPath { get; private set; }
        private cls_00_ExcelData _excelData = new cls_00_ExcelData();

        public (string ExcelPath, List<string> FilesToProcess) PrepareExcel(string[] selectedFiles, string sheetName)
        {
            // Seleccionar el directorio del Excel
            string excelDirectory = SelectExcelDirectory();
            if (string.IsNullOrEmpty(excelDirectory))
            {
                return (null, null);
            }

            // Seleccionar el archivo de Excel
            ExcelPath = SelectExcelFile(excelDirectory);
            if (string.IsNullOrEmpty(ExcelPath))
            {
                return (null, null);
            }

            // Mostrar los headers del Excel
            ShowHeaders(sheetName);

            // Verificar si los archivos existen en el Excel
            List<string> existingFiles = 
                VerifyFilesInExcel(selectedFiles, sheetName);

            // Manejar los archivos existentes
            List<string> filesToProcess = 
                HandleExistingFiles(existingFiles, selectedFiles, sheetName);

            return (ExcelPath, filesToProcess);
        }

        public static string SelectExcelDirectory()
        {
            using (var form = new ExcelPathEntry())
            {
                // Validamos
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // return
                    return form.ExcelPath;
                }
                // En caso de no validar
                else
                {
                    // Mensaje
                    MessageBox.Show(
                        "No directory path provided. Process aborted.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error
                    );
                    // Finalizamos
                    return null;
                }
            }
        }

        public static string SelectExcelFile(string initialDirectory)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = initialDirectory,
                Title = "Select Excel File to Analyze",
                Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Multiselect = false
            })
            {
                // Validamos
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // return
                    return openFileDialog.FileName;
                }
                // En caso de no validar
                else
                {
                    // Mensaje
                    MessageBox.Show(
                        "No Excel file selected. Process aborted.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error
                    );
                    // Finalizamos
                    return null;
                }
            }
        }

        private void ShowHeaders(string sheetName)
        {
            if (!string.IsNullOrEmpty(ExcelPath))
            {
                _excelData.ShowHeadersPropertySets(ExcelPath, sheetName);
            }
        }

        private List<string> VerifyFilesInExcel(string[] selectedFiles, string sheetName)
        {
            List<string> existingFiles = new List<string>();

            foreach (string file in selectedFiles)
            {
                string fileName = Path.GetFileName(file);
                if (CheckIfFileNameExistsInExcel(fileName, sheetName))
                {
                    existingFiles.Add(fileName);
                }
            }

            return existingFiles;
        }

        private List<string> HandleExistingFiles(List<string> existingFiles, string[] selectedFiles, string sheetName)
        {
            List<string> filesToProcess = new List<string>(selectedFiles);

            if (existingFiles.Count > 0)
            {
                string message = "The following files already exist in the Excel:\n\n" +
                                 string.Join("\n", existingFiles) +
                                 "\n\nDo you want to overwrite them or skip them?";
                DialogResult dialogResult = MessageBox.Show(message, "File Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Yes)
                {
                    RemoveExistingFilesFromExcel(existingFiles, sheetName);
                }
                else
                {
                    filesToProcess = filesToProcess.Where(file => !existingFiles.Contains(Path.GetFileName(file))).ToList();
                }
            }

            return filesToProcess;
        }

        private void RemoveExistingFilesFromExcel(List<string> existingFiles, string sheetName)
        {
            foreach (string fileName in existingFiles)
            {
                _excelData.RemoveRowsWithFileName(ExcelPath, sheetName, fileName);
            }
        }

        private bool CheckIfFileNameExistsInExcel(string fileName, string sheetName)
        {
            FileInfo fileInfo = new FileInfo(ExcelPath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                {
                    MessageBox.Show($"Sheet '{sheetName}' not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                for (int row = worksheet.Dimension.Start.Row + 1; row <= worksheet.Dimension.End.Row; row++)
                {
                    if (worksheet.Cells[row, 1].Text.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}

