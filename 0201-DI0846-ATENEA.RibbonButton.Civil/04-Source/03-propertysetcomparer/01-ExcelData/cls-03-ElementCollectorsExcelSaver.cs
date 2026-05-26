using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Civil.DatabaseServices;
using System.Collections.Generic;
using TYPSA.PS.RibbonButton.Civil.Source.Class.ExcelData;
using TYPSA.PS.RibbonButton.Civil.Source.Class.PropertySets;
using TYPSA.SharedLib.Civil.CivilCollector;
using TYPSA.SharedLib.UserForms;

namespace TYPSA.PS.RibbonButton.Civil._05_Source._02_Collector
{
    internal class cls_03_ElementCollectorsExcelSaver
    {
        public void CollectAndProcessElements(
            Document openedDoc,
            string projectCode,
            string excelPath,
            string sheetName,
            string fileName,
            cls_00_ExcelData excelData,
            HashSet<string> uniqueLayers
        )
        {
            // Obtenemos las clases
            cls_00_CivilCollector elementsCollector = new cls_00_CivilCollector();
            cls_00_AlignmentsCollector alignmentsCollector = new cls_00_AlignmentsCollector();
            cls_00_AssembliesCollector assembliesCollector = new cls_00_AssembliesCollector();
            cls_00_BlocksCollector blocksCollector = new cls_00_BlocksCollector();
            cls_00_BodiesCollector bodiesCollector = new cls_00_BodiesCollector();
            cls_00_CorridorsCollector corridorsCollector = new cls_00_CorridorsCollector();
            cls_00_PipesCollector pipesCollector = new cls_00_PipesCollector();
            cls_00_StructuresCollector structuresCollector = new cls_00_StructuresCollector();
            cls_00_SolidsCollector solidsCollector = new cls_00_SolidsCollector();
            cls_00_SubassembliesCollector subassembliesCollector = new cls_00_SubassembliesCollector();

            // Obtenemos los colectores
            List<(Alignment alignment, string category, string alignmentName, string alignmentHandle)> alignments = 
                alignmentsCollector.AlignmentsCollector(openedDoc, projectCode);
            List<(Autodesk.Civil.DatabaseServices.Assembly assembly, string category, string assemblyName, string assemblyHandle)> assemblies =
                assembliesCollector.AssembliesCollector(openedDoc, projectCode);
            List<(BlockReference block, string category, string blockName, string blockHandle)> blocks = 
                blocksCollector.BlocksCollector(openedDoc, projectCode);
            List<(Body body, string category, string bodyName, string bodyHandle)> bodies = 
                bodiesCollector.BodiesCollector(openedDoc, projectCode);
            List<(Corridor corridor, string category, string corridorName, string corridorHandle)> corridors = 
                corridorsCollector.CorridorsCollector(openedDoc, projectCode);
            List<(Pipe pipe, string category, string pipeName, string pipeHandle)> pipes = 
                pipesCollector.PipesCollector(openedDoc, projectCode);
            List<(Structure structure, string category, string structureName, string structureHandle)> structures = 
                structuresCollector.StructuresCollector(openedDoc, projectCode);
            List<(Solid3d solid, string category, string solidName, string solidHandle)> solids = 
                solidsCollector.SolidsCollector(openedDoc, projectCode);
            List<(Subassembly subassembly, string category, string subassemblyName, string subassemblyHandle, Dictionary<string, string> parameters)> subassemblies =
                subassembliesCollector.SubassembliesCollector(openedDoc, projectCode);

            // Contadores por clase
            int countAlignments = alignments.Count;
            int countAssemblies = assemblies.Count;
            int countBlocks = blocks.Count;
            int countBodies = bodies.Count;
            int countCorridors = corridors.Count;
            int countPipes = pipes.Count;
            int countStructures = structures.Count;
            int countSolids = solids.Count;
            int countSubassemblies = subassemblies.Count;

            // Total general
            int total = 
                countAlignments + countAssemblies + countBlocks + countBodies +
                countCorridors + countPipes + countStructures + countSolids + countSubassemblies;

            // Construcción del mensaje
            string message =
                $"✔ Summary of collected Entities\n\n" +
                $"- Alignments: {countAlignments}\n" +
                $"- Assemblies: {countAssemblies}\n" +
                $"- Blocks: {countBlocks}\n" +
                $"- Bodies: {countBodies}\n" +
                $"- Corridors: {countCorridors}\n" +
                $"- Pipes: {countPipes}\n" +
                $"- Structures: {countStructures}\n" +
                $"- Solids: {countSolids}\n" +
                $"- Subassemblies: {countSubassemblies}\n\n" +
                $"🟦 Total: {total} entities";

            // Mostrar mensaje con autocierre
            new AutoCloseMessageForm(message).ShowDialog();

            // Agregar capas únicas recolectadas
            uniqueLayers.UnionWith(elementsCollector.GetCollectedLayers());
            uniqueLayers.UnionWith(alignmentsCollector.GetCollectedLayers());
            uniqueLayers.UnionWith(assembliesCollector.GetCollectedLayers());
            uniqueLayers.UnionWith(blocksCollector.GetCollectedLayers());
            uniqueLayers.UnionWith(bodiesCollector.GetCollectedLayers());
            uniqueLayers.UnionWith(corridorsCollector.GetCollectedLayers());
            uniqueLayers.UnionWith(pipesCollector.GetCollectedLayers());
            uniqueLayers.UnionWith(structuresCollector.GetCollectedLayers());
            uniqueLayers.UnionWith(solidsCollector.GetCollectedLayers());
            uniqueLayers.UnionWith(subassembliesCollector.GetCollectedLayers());

            // Procesar y guardar en Excel
            SaveToExcel(alignments, fileName, excelPath, sheetName, excelData, new cls_00_PropertySetsGetter());
            SaveToExcel(assemblies, fileName, excelPath, sheetName, excelData, new cls_00_PropertySetsGetter());
            SaveToExcel(blocks, fileName, excelPath, sheetName, excelData, new cls_00_PropertySetsGetter());
            SaveToExcel(bodies, fileName, excelPath, sheetName, excelData, new cls_00_PropertySetsGetter());
            SaveToExcel(corridors, fileName, excelPath, sheetName, excelData, new cls_00_PropertySetsGetter());
            SaveToExcel(pipes, fileName, excelPath, sheetName, excelData, new cls_00_PropertySetsGetter());
            SaveToExcel(structures, fileName, excelPath, sheetName, excelData, new cls_00_PropertySetsGetter());
            SaveToExcel(solids, fileName, excelPath, sheetName, excelData, new cls_00_PropertySetsGetter());

            // Iteramos
            foreach (var (subassembly, category, subassemblyName, subassemblyHandle, parameters) in subassemblies)
            {
                // Exportamos
                excelData.SetExcelDataPropertySets(
                    excelPath, sheetName, fileName, category, subassemblyName, subassemblyHandle, parameters
                );
            }
        }

        private void SaveToExcel<T>(
            List<(T element, string category, string name, string handle)> elements,
            string fileName,
            string excelPath,
            string sheetName,
            cls_00_ExcelData excelData,
            cls_00_PropertySetsGetter propertySets)
        {
            foreach (var (element, category, name, handle) in elements)
            {
                Dictionary<string, string> propertySetsInfo = 
                    propertySets.GetPropertySetsInfo(element);
                excelData.SetExcelDataPropertySets(
                    excelPath, sheetName, fileName, category, name, handle, propertySetsInfo
                );
            }
        }
    }
}

