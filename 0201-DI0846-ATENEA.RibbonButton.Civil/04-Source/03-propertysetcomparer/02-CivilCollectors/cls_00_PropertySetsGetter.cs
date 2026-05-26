using System.Linq;
using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.Aec.PropertyData.DatabaseServices;
using System.Collections.Generic;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.PropertySets
{
    internal class cls_00_PropertySetsGetter
    {
        public Dictionary<string, string> GetPropertySetsInfo(object civilObject)
        {
            Dictionary<string, string> propertySets = new Dictionary<string, string>();

            Autodesk.AutoCAD.DatabaseServices.DBObject dbObject = civilObject as Autodesk.AutoCAD.DatabaseServices.DBObject;
            if (dbObject == null)
            {
                propertySets["Error"] = "Invalid civil object.";
                return propertySets;
            }

            Autodesk.AutoCAD.DatabaseServices.ObjectId extensionDictId = Autodesk.AutoCAD.DatabaseServices.ObjectId.Null;

            if (civilObject is Autodesk.Aec.DatabaseServices.Entity entity)
            {
                extensionDictId = entity.ExtensionDictionary;
            }
            else if (civilObject is Autodesk.AutoCAD.DatabaseServices.Solid3d solid3D)
            {
                extensionDictId = solid3D.ExtensionDictionary;

                // Verificación de contenido del diccionario para Solid3D
                if (extensionDictId.IsValid)
                {
                    using (Transaction tr = dbObject.Database.TransactionManager.StartTransaction())
                    {
                        var extensionDict = (DBDictionary)tr.GetObject(extensionDictId, OpenMode.ForRead);
                        if (extensionDict != null)
                        {
                            foreach (DBDictionaryEntry entry in extensionDict)
                            {
                                propertySets["Solid3D Entry: " + entry.Key] = entry.Value.ToString();
                            }
                        }
                        tr.Commit();
                    }
                }
            }
            else if (civilObject is Autodesk.AutoCAD.DatabaseServices.Body body)
            {
                extensionDictId = body.ExtensionDictionary;

                if (extensionDictId.IsValid)
                {
                    using (Transaction tr = dbObject.Database.TransactionManager.StartTransaction())
                    {
                        var extensionDict = (DBDictionary)tr.GetObject(extensionDictId, OpenMode.ForRead);
                        if (extensionDict != null)
                        {
                            foreach (DBDictionaryEntry entry in extensionDict)
                            {
                                propertySets["Body Entry: " + entry.Key] = entry.Value.ToString();
                            }
                        }
                        tr.Commit();
                    }

                }

            }
            else if (civilObject is Autodesk.AutoCAD.DatabaseServices.BlockReference blockReference)
            {
                extensionDictId = blockReference.ExtensionDictionary;

                // Verificación de contenido del diccionario para BlockReference
                if (extensionDictId.IsValid)
                {
                    using (Transaction tr = dbObject.Database.TransactionManager.StartTransaction())
                    {
                        var extensionDict = (DBDictionary)tr.GetObject(extensionDictId, OpenMode.ForRead);
                        if (extensionDict != null)
                        {
                            foreach (DBDictionaryEntry entry in extensionDict)
                            {
                                propertySets["BlockReference Entry: " + entry.Key] = entry.Value.ToString();
                            }
                        }
                        tr.Commit();
                    }
                }
            }
            else
            {
                propertySets["Error"] = "Invalid civil object.";
                return propertySets;
            }

            if (!extensionDictId.IsValid)
            {
                propertySets["Error"] = "Object has no extension dictionary.";
                return propertySets;
            }

            using (Transaction tr = dbObject.Database.TransactionManager.StartTransaction())
            {
                var extensionDict = (DBDictionary)tr.GetObject(extensionDictId, OpenMode.ForRead);
                if (extensionDict == null)
                {
                    propertySets["Error"] = "Object has no extension dictionary.";
                    return propertySets;
                }

                ReportDictionary(tr, extensionDict, propertySets);
                tr.Commit();
            }

            return propertySets;
        }

        private void ReportDictionary(Transaction tr, DBDictionary dbDictionary, Dictionary<string, string> propertySets)
        {
            foreach (DBDictionaryEntry entry in dbDictionary)
            {
                var dbEntry = tr.GetObject(entry.Value, OpenMode.ForRead);
                if (dbEntry == null)
                    continue;

                if (dbEntry.GetType() == typeof(PropertySet))
                {
                    var ps = dbEntry as PropertySet;
                    if (ps != null)
                    {
                        ReportPropertySet(tr, ps, propertySets);
                    }
                }
                else if (dbEntry.GetType() == typeof(DBDictionary))
                {
                    var dbd = dbEntry as DBDictionary;
                    if (dbd != null)
                    {
                        ReportDictionary(tr, dbd, propertySets);
                    }
                }
            }
        }

        private void ReportPropertySet(Transaction tr, PropertySet ps, Dictionary<string, string> propertySets)
        {
            var idDef = ps.PropertySetDefinition;
            var propSetDef = (PropertySetDefinition)tr.GetObject(idDef, OpenMode.ForRead);
            var propDefList = propSetDef.Definitions.Cast<PropertyDefinition>().ToList();

            var psdc = ps.PropertySetData;
            foreach (PropertySetData psd in psdc)
            {
                var propDef = propDefList.FirstOrDefault(x => x.Id == psd.Id);
                if (propDef != null)
                {
                    propertySets[propDef.Name] = psd.GetData().ToString();
                }
                else
                {
                    propertySets[psd.FieldBucketId.ToString()] = psd.GetData().ToString();
                }
            }
        }

        public void ShowPropertySetsInfo(object civilObject)
        {
            Dictionary<string, string> propertySetsInfo = GetPropertySetsInfo(civilObject);
            StringBuilder propertySetsDetails = new StringBuilder();

            foreach (var item in propertySetsInfo)
            {
                propertySetsDetails.AppendLine($"{item.Key}: {item.Value}");
            }

            MessageBox.Show(propertySetsDetails.ToString(), $"Property Sets for Object: {civilObject.GetType().Name}", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
