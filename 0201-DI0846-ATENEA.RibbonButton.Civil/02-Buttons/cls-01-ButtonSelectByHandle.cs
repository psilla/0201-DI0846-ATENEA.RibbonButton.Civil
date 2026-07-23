using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using TYPSA.SharedLib.Autocad.Metrics;


namespace TYPSA.PS.RibbonButton.Civil
{
    public class cls_01_ButtonSelectByHandle
    {
        // Método para seleccionar un objeto por su handle
        [CommandMethod(RibbonCommands.SelectByHandle)]
        public void SelectByHandle()
        {
            // Ruta completa del archivo Lisp
            string lispFilePath = @"C:\Autodesk\Automations\0333-DI0937-SelectAndZoomByHandle.lsp";
            string loadLispCommand = $"(load \"{lispFilePath.Replace("\\", "\\\\")}\") ";

            // Step 2: Build the command to execute the LISP function
            string executeLispFunctionCommand = "SelectAndZoomByHandle";

            // Step 3: Combine the commands and send them to the AutoCAD command line
            string combinedCommands = loadLispCommand + executeLispFunctionCommand;
            // Send the combined command to load and execute the LISP, including \n to simulate Enter
            Application.DocumentManager.MdiActiveDocument.SendStringToExecute(combinedCommands + "\n", true, false, false);

            //---------SENDING METRICS--------//

            //Este es el identificador del Script Name para Atenea Model Checker
            string accionId = "67a9f7dcb10174ef740fd3e3";

            //Este es el identificador del Process NAme para Atenea Model Checker - Select By Handle
            string processIdSelectionByHandle = "6671ed0db613f9efa5c59953";

            //Este es el identificador del usuario que está ejecutando el script
            string emailUser = Environment.UserName;

            // Crear el array de executed_process
            var executed_process = new[]
            {
            new
            {
                proceso = processIdSelectionByHandle,
                recuento = 1
            },
            };

            //Create the json addtionalData with the necessary values
            var additionalData = new
            {
                ScriptName = "0381-HY9678-TYPSA.PS.RibbonButton.Civil.csproj",
                FilesProcessed = 1,
                ExecutionStatus = 1,
                Version = "V.01.01",

            };

            //Sending the metrics to the recorder
            cls_00_MetricsSender.SendMetricsAsync(emailUser, accionId, executed_process, additionalData);
        }
    }
}
