using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using OfficeOpenXml;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.ExcelData
{
    internal class cls_00_ExcelData
    {
        public List<string> ReadExcelHeadersProjectInfo(string filePath, string sheetName)
        {
            List<string> headers = new List<string>();

            FileInfo fileInfo = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                {
                    MessageBox.Show("Sheet not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return headers;
                }

                int columnCount = worksheet.Dimension.End.Column;
                // Empieza en 2 para excluir la primera columna
                for (int col = 2; col <= columnCount; col++) 
                {
                    string header = worksheet.Cells[1, col].Text;
                    headers.Add(header);
                }
            }

            return headers;
        }

        public List<string> ReadExcelHeadersPropertySets(string filePath, string sheetName)
        {
            List<string> headers = new List<string>();

            FileInfo fileInfo = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                {
                    MessageBox.Show("Sheet not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return headers;
                }

                int columnCount = worksheet.Dimension.End.Column;
                for (int col = 5; col <= columnCount; col++) // Empieza en 2 para excluir la primera columna
                {
                    string header = worksheet.Cells[1, col].Text;
                    headers.Add(header);
                }
            }

            return headers;
        }

        public void ShowHeadersProjectInfo(string filePath, string sheetName)
        {
            List<string> headers = ReadExcelHeadersProjectInfo(filePath, sheetName);
            if (headers.Count > 0)
            {
                string headerList = string.Join("\n", headers);
                MessageBox.Show(headerList, "Excel Headers", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No headers found or unable to read the file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ShowHeadersPropertySets(string filePath, string sheetName)
        {
            List<string> headers = ReadExcelHeadersPropertySets(filePath, sheetName);
            if (headers.Count > 0)
            {
                string headerList = string.Join("\n", headers);
                MessageBox.Show(headerList, "Excel Headers", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("No headers found or unable to read the file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SetExcelDataProjectInfo(string filePath, string sheetName, string fileName, Dictionary<string, string> customProperties)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                {
                    MessageBox.Show("Sheet not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Leer los encabezados
                List<string> headers = ReadExcelHeadersProjectInfo(filePath, sheetName);

                // Encontrar la siguiente fila vacía
                int nextRow = worksheet.Dimension.End.Row + 1;

                // Escribir el nombre del archivo en la primera columna de la fila
                worksheet.Cells[nextRow, 1].Value = fileName;

                // Comparar cada encabezado con las propiedades personalizadas
                for (int col = 2; col <= headers.Count + 1; col++)
                {
                    string header = headers[col - 2];
                    if (customProperties.TryGetValue(header, out string value))
                    {
                        worksheet.Cells[nextRow, col].Value = string.IsNullOrEmpty(value) ? "NotValueApplied" : value;
                    }
                    else
                    {
                        worksheet.Cells[nextRow, col].Value = "Not Found";
                    }
                }

                package.Save();
            }
        }

        // Método para actualizar el archivo Excel con los Property Sets
        public void SetExcelDataPropertySets(
            string filePath, 
            string sheetName, 
            string fileName, 
            string category, 
            string corridorName, 
            string corridorHandle, 
            Dictionary<string, string> propertySets
        )
        {
            FileInfo fileInfo = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                {
                    MessageBox.Show("Sheet not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Leer los encabezados
                List<string> headers = ReadExcelHeadersPropertySets(filePath, sheetName);

                // Encontrar la siguiente fila vacía
                int nextRow = worksheet.Dimension.End.Row + 1;

                // Escribir el nombre del archivo en la primera columna de la fila
                worksheet.Cells[nextRow, 1].Value = fileName;
                worksheet.Cells[nextRow, 2].Value = category; // Escribir la categoría en la segunda columna
                worksheet.Cells[nextRow, 3].Value = corridorName; // Escribir el nombre del corredor en la tercera columna
                worksheet.Cells[nextRow, 4].Value = corridorHandle; // Escribir el handle del corredor en la cuarta columna

                // Comparar cada encabezado con los Property Sets
                for (int col = 5; col <= headers.Count + 4; col++)
                {
                    string header = headers[col - 5];
                    if (propertySets.TryGetValue(header, out string value))
                    {
                        worksheet.Cells[nextRow, col].Value = string.IsNullOrEmpty(value) ? "NotValueApplied" : value;
                    }
                    else
                    {
                        worksheet.Cells[nextRow, col].Value = "Not Found";
                    }
                }

                package.Save();
            }
        }

        public void RemoveRowsWithFileName(string filePath, string sheetName, string fileName)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                {
                    MessageBox.Show("Sheet not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Buscar y eliminar las filas que contienen el fileName
                for (int row = worksheet.Dimension.Start.Row + 1; row <= worksheet.Dimension.End.Row; row++)
                {
                    if (worksheet.Cells[row, 1].Text.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    {
                        worksheet.DeleteRow(row);
                        row--; // Decrementar el contador de filas para no saltarse ninguna
                    }
                }

                package.Save();
            }
        }

        public int GetExcelHeadersCount(string filePath, string sheetName)
        {
            int headerCount = 0;

            FileInfo fileInfo = new FileInfo(filePath);
            using (ExcelPackage package = new ExcelPackage(fileInfo))
            {
                ExcelWorksheet worksheet = package.Workbook.Worksheets[sheetName];
                if (worksheet == null)
                {
                    MessageBox.Show("Sheet not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return headerCount;
                }

                int columnCount = worksheet.Dimension.End.Column;
                headerCount = columnCount - 1; // Restar 1 para excluir la primera columna
            }

            return headerCount;
        }


    }
}
