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

        

        

        

        





    }
}
