using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Autodesk.Aec.PropertyData.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices;
using TYPSA.SharedLib.Civil.GetPropertyData;
using TYPSA.SharedLib.Civil.GetPsetData;
using static TYPSA.SharedLib.Autocad.Main.cls_00_CadInfoHelper;
using TYPSA.SharedLib.EndPoints;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_00_GetDataParamCheckImp
    {
        private static bool RemovePropertyFromPropertySet(
            string propertyName,
            PropertySetDefinition propSetDef
        )
        {
            // Validamos
            if (propSetDef == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return false;
            }

            // Iteramos por las definiciones
            for (int i = propSetDef.Definitions.Count - 1; i >= 0; i--)
            {
                PropertyDefinition propDef = propSetDef.Definitions[i] as PropertyDefinition;
                // Validamos
                if (propDef == null)
                {
                    continue;
                }

                // Encontrada
                if (propDef.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    propSetDef.Definitions.RemoveAt(i);
                    return true;
                }
            }

            // No encontrada
            return false;
        }

        public static List<string> GetEntitiesToApplyFromJson(
            List<Dictionary<string, object>> categories
        )
        {
            List<string> entitiesToApply = new List<string>();
            // Iteramos categorías
            foreach (Dictionary<string, object> category in categories)
            {
                // Validamos nombre
                if (!category.ContainsKey(cls_00_AteneaJson.CategoryName) || category[cls_00_AteneaJson.CategoryName] == null)
                {
                    continue;
                }

                // Validamos valor
                if (!category.ContainsKey(cls_00_AteneaJson.CategoryValue) || category[cls_00_AteneaJson.CategoryValue] == null)
                {
                    continue;
                }

                // Obtenemos valor bool
                bool categoryValue = Convert.ToBoolean(category[cls_00_AteneaJson.CategoryValue]);

                // Si esta activo
                if (categoryValue)
                {
                    // Añadimos
                    entitiesToApply.Add(category[cls_00_AteneaJson.CategoryName].ToString());
                }
            }

            // return
            return entitiesToApply;
        }

        public static Dictionary<string, Autodesk.Aec.PropertyData.DataType> GetPropFromJson(
            List<Dictionary<string, object>> psetData,
            Dictionary<string, Autodesk.Aec.PropertyData.DataType> dataTypeDictFiltered
        )
        {
            Dictionary<string, Autodesk.Aec.PropertyData.DataType> pSetPropFromJson =
                new Dictionary<string, Autodesk.Aec.PropertyData.DataType>();
            // Iteramos
            foreach (Dictionary<string, object> propData in psetData)
            {
                // Validamos
                if (!propData.ContainsKey(cls_00_AteneaJson.PropName) || propData[cls_00_AteneaJson.PropName] == null)
                {
                    continue;
                }
                // Validamos
                if (!propData.ContainsKey(cls_00_AteneaJson.DataType) || propData[cls_00_AteneaJson.DataType] == null)
                {
                    continue;
                }

                // Obtenemos info
                string propName = propData[cls_00_AteneaJson.PropName].ToString();
                string dataTypeName = propData[cls_00_AteneaJson.DataType].ToString();
                // Validamos y obtenemos el DataType
                if (!dataTypeDictFiltered.TryGetValue(dataTypeName, out Autodesk.Aec.PropertyData.DataType dataType))
                {
                    continue;
                }

                // Añadimos
                if (!pSetPropFromJson.ContainsKey(propName))
                {
                    pSetPropFromJson.Add(propName, dataType);
                }
            }

            // return
            return pSetPropFromJson;
        }

        public static void UpdateExistingPsetDefinition(
            PropertySetDefinition propSetDef,
            Dictionary<string, Autodesk.Aec.PropertyData.DataType> propFromJson,
            Dictionary<string, Autodesk.Aec.PropertyData.DataType> propFromDoc,
            out List<string> addedProperties,
            out List<string> removedProperties
        )
        {
            // Valores por defecto
            addedProperties = new List<string>();
            removedProperties = new List<string>();

            // -----------------------------
            // Añadir propiedades nuevas
            // -----------------------------

            // Iteramos
            foreach (var entry in propFromJson)
            {
                // Obtenemos info
                string jsonPropName = entry.Key;
                Autodesk.Aec.PropertyData.DataType jsonDataType = entry.Value;

                // Validamos si existe en documento
                if (!propFromDoc.ContainsKey(jsonPropName))
                {
                    cls_00_AddPropToPset.AddPropertyToPropertySet(
                        jsonPropName, jsonDataType, propSetDef, addedProperties
                    );
                }
            }

            // -----------------------------
            // Eliminar propiedades sobrantes
            // -----------------------------

            foreach (var entry in propFromDoc)
            {
                string docPropName = entry.Key;
                // Validamos
                if (!propFromJson.ContainsKey(docPropName))
                {
                    // Eliminamos
                    bool removed = RemovePropertyFromPropertySet(
                        docPropName, propSetDef
                    );
                    // Validamos
                    if (removed)
                    {
                        removedProperties.Add(docPropName);
                    }
                }
            }

            // -----------------------------
            // Revisar tipos de dato
            // -----------------------------

            foreach (var entry in propFromJson)
            {
                string jsonPropName = entry.Key;
                Autodesk.Aec.PropertyData.DataType jsonDataType = entry.Value;
                // Validamos
                if (!propFromDoc.ContainsKey(jsonPropName))
                {
                    continue;
                }

                Autodesk.Aec.PropertyData.DataType docDataType = propFromDoc[jsonPropName];
                // Validamos
                if (docDataType != jsonDataType)
                {
                    // Actualizar tipo o registrar aviso
                }
            }
        }

        public static Dictionary<string, Autodesk.Aec.PropertyData.DataType> GetExistingProperties(
            PropertySetDefinition propSetDef
        )
        {
            // Propiedades existentes en el documento
            Dictionary<string, Autodesk.Aec.PropertyData.DataType> existingProps = new Dictionary<string, Autodesk.Aec.PropertyData.DataType>(
                StringComparer.OrdinalIgnoreCase
            );
            // Iteramos
            foreach (PropertyDefinition propDef in propSetDef.Definitions)
            {
                // Validamos
                if (propDef == null || string.IsNullOrWhiteSpace(propDef.Name))
                {
                    // Obviamos
                    continue;
                }
                // Validamos
                if (!existingProps.ContainsKey(propDef.Name))
                {
                    existingProps.Add(propDef.Name, propDef.DataType);
                }
            }

            // return
            return existingProps;
        }

        public static ObjectId CreatePsetDefinition(
            Transaction tr,
            Database db,
            string propSetDefName,
            Dictionary<string, Autodesk.Aec.PropertyData.DataType> propWithDataDict,
            IEnumerable<string> entityTypes,
            DictionaryPropertySetDefinitions dictPropSetDef,
            ObjectId propSetDefId
        )
        {
            // -----------------------------
            // Crear Pset
            // -----------------------------
         
            PropertySetDefinition propSetDef = cls_00_CreatePsetDef.CreatePsetDef(db);

            // Aplicar el Pset a las entidades
            cls_00_ApplyPsetToEntities.ApplyPsetToEntities(
                propSetDef, entityTypes
            );

            List<string> addedProperties = new List<string>();
            // Crear propiedades en el Pset
            foreach (var entry in propWithDataDict)
            {
                // Añadimos la propiedad al Pset
                cls_00_AddPropToPset.AddPropertyToPropertySet(
                    entry.Key, entry.Value, propSetDef, addedProperties
                );
            }

            // -----------------------------
            // Agregar al diccionario
            // -----------------------------

            // Agregar el PropertySet al diccionario de los Psets
            dictPropSetDef.AddNewRecord(propSetDefName, propSetDef);
            // Añadimos
            tr.AddNewlyCreatedDBObject(
                propSetDef, true
            );

            // Obtenemos Id de la definición del Pset
            propSetDefId = propSetDef.ObjectId;

            // return
            return propSetDefId;
        }

        public static void UpdatePsetAppliesTo(
            PropertySetDefinition propSetDef,
            List<string> entToApplyFromJson
        )
        {
            // Limpiamos las entidades actuales
            propSetDef.SetAppliesToFilter(new StringCollection(), false);

            StringCollection appliesTo = new StringCollection();
            // Iteramos
            foreach (string entName in entToApplyFromJson)
            {
                // Validamos
                if (string.IsNullOrWhiteSpace(entName))
                {
                    continue;
                }
                // Añadimos
                appliesTo.Add(entName);
            }

            // Aplicamos nuevas entidades
            propSetDef.SetAppliesToFilter(appliesTo, false);
        }





    }
}
