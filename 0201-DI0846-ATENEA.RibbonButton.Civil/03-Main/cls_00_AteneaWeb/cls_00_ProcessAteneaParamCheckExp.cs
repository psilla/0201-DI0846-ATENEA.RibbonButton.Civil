using System.Collections.Generic;
using System.Collections.Specialized;
using Autodesk.Aec.PropertyData.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices;
using TYPSA.SharedLib.Civil.SetDataFromJson;
using TYPSA.SharedLib.EndPoints;

namespace TYPSA.PS.RibbonButton.Civil
{
    internal class cls_00_ProcessAteneaParamCheckExp
    {
        public static List<Dictionary<string, object>> ProcessAteneaParamCheckExp(
            Transaction tr,
            DictionaryPropertySetDefinitions dictPropSetDef,
            StringCollection allCategories
        )
        {
            // -------------------------------
            // Definir keys
            // -------------------------------

            string keyPsetName = cls_00_AteneaJson.PsetName;
            string keyPsetData = cls_00_AteneaJson.PsetData;
            string keyPropName = cls_00_AteneaJson.PropName;
            string keyDataType = cls_00_AteneaJson.DataType;
            string keyPropDefaultValue = cls_00_AteneaJson.PropDefaultValue;
            string keyPropDescription = cls_00_AteneaJson.PropDescription;
            string keyPropIsAutomatic = cls_00_AteneaJson.PropIsAutomatic;
            string keyPropIsVisible = cls_00_AteneaJson.PropIsVisible;
            string keyPropIsReadOnly = cls_00_AteneaJson.PropIsReadOnly;
            string keyPsetId = cls_00_AteneaJson.PsetId;
            string keyPropId = cls_00_AteneaJson.PropId;
            string keyCategoryName = cls_00_AteneaJson.CategoryName;
            string keyCategoryValue = cls_00_AteneaJson.CategoryValue;
            string keyCategories = cls_00_AteneaJson.Categories;

            // -------------------------------
            // Definir variables
            // -------------------------------

            List<Dictionary<string, object>> extractedData = new List<Dictionary<string, object>>();

            // -------------------------------
            // Obtener diccionario de Psets
            // -------------------------------

            DBDictionary dbDict = tr.GetObject(dictPropSetDef.DictionaryId, OpenMode.ForRead) as DBDictionary;
            // Validamos
            if (dbDict == null) return extractedData;

            // -------------------------------
            // Iterar Property Sets
            // -------------------------------

            foreach (DBDictionaryEntry entry in dbDict)
            {
                // -------------------------------
                // Obtener informacion
                // -------------------------------

                string propSetName = entry.Key;
                ObjectId propSetDefId = entry.Value;

                // Obtenemos PropertySetDefinition
                PropertySetDefinition propSetDef = tr.GetObject(propSetDefId, OpenMode.ForRead) as PropertySetDefinition;
                // Validamos
                if (propSetDef == null) continue;

                // -----------------------------
                // Obtener categorías del PSet
                // -----------------------------

                // Obtenemos entidades a las que aplica el Pset
                HashSet<string> appliesSet = cls_00_ProcessDocFromJson.GetAppliesToSet(propSetDef);

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
                    // Almacenamos informacion propiedad
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

            // return
            return extractedData;
        }
    }



}
