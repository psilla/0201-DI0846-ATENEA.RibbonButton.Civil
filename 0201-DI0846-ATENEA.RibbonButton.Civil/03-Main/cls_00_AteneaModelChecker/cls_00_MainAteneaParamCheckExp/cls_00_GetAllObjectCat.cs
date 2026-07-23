using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Autodesk.Aec.PropertyData.DatabaseServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Runtime;
using TYPSA.SharedLib.UserForms;
using TYPSA.SharedLib.Civil26;
using TYPSA.SharedLib.Civil25;
using TYPSA.SharedLib.Civil24;
using TYPSA.SharedLib.Civil23;

namespace TYPSA.PS.RibbonButton.Civil.Source.Class.Main
{
    internal class cls_00_GetAllObjectCat
    {
        public static StringCollection GetAllObjectCatForPsetsByVersion()
        {
#if CIVIL2026
            return cls_00_GetAllObjectCatForPsets_2026.GetAllObjectCatForPsets();
#elif CIVIL2025
            return cls_00_GetAllObjectCatForPsets_2025.GetAllObjectCatForPsets();
#elif CIVIL2024
            return cls_00_GetAllObjectCatForPsets_2024.GetAllObjectCatForPsets();
#elif CIVIL2023
            return cls_00_GetAllObjectCatForPsets_2023.GetAllObjectCatForPsets();
#elif CIVIL2022
            return cls_00_GetAllObjectCatForPsets_2022.GetAllObjectCatForPsets();
#elif CIVIL2021
            return cls_00_GetAllObjectCatForPsets_2021.GetAllObjectCatForPsets();
#elif CIVIL2020
            return cls_00_GetAllObjectCatForPsets_2020.GetAllObjectCatForPsets();
#else
            return cls_00_GetAllObjectCat.GetAllObjectCatForPsets();
#endif
        }

        public static StringCollection GetAllObjectCat()
        {
            HashSet<string> categories = new HashSet<string>();

            // try
            try
            {
                RXClass entityRxClass = RXObject.GetClass(typeof(Entity));

                // Recorremos todas las clases registradas en AutoCAD/Civil 3D
                foreach (DictionaryEntry entry in SystemObjects.ClassDictionary)
                {
                    RXClass rxClass = entry.Value as RXClass;
                    // Validamos
                    if (rxClass == null) continue;

                    // Solo clases derivadas de Entity
                    if (!rxClass.IsDerivedFrom(entityRxClass)) continue;

                    // Añadimos nombre RXClass
                    categories.Add(rxClass.Name);
                }
            }
            catch
            {
                // opcional log
            }

            StringCollection result = new StringCollection();

            foreach (string cat in categories.OrderBy(x => x))
            {
                result.Add(cat);
            }

            return result;
        }

        private static HashSet<string> GetApplies(
            PropertySetDefinition propSetDef
        )
        {
            HashSet<string> appliesSet = new HashSet<string>();

            StringCollection appliesTo = propSetDef.AppliesToFilter;

            if (appliesTo != null)
            {
                foreach (string cls in appliesTo)
                {
                    appliesSet.Add(cls);
                }
            }

            // return
            return appliesSet;
        }

        private static List<string> GetCategoriesAppliedToPropertySet(
            PropertySetDefinition propSetDef
        )
        {
            List<string> categories = new List<string>();
            // Validamos
            if (propSetDef == null) return categories;

            // Si aplica a todo, devolvemos todas las clases registradas
            if (propSetDef.AppliesToAll)
            {
                StringCollection allCategories = GetAllObjectCatForPsetsByVersion();

                foreach (string cat in allCategories)
                {
                    categories.Add(cat);
                }

                return categories.OrderBy(x => x).ToList();
            }

            try
            {
                foreach (string category in GetApplies(propSetDef))
                {
                    categories.Add(category);
                }
            }
            catch
            {
            }

            return categories.OrderBy(x => x).ToList();
        }

        public static void DebugPropertySetCategories(
            PropertySetDefinition propSetDef
        )
        {
            List<string> categories = GetCategoriesAppliedToPropertySet(propSetDef);

            StringBuilder sb = new StringBuilder();

            foreach (string cat in categories)
            {
                sb.AppendLine(cat);
            }

            ShowStringBuilder.ShowInfo(
                $"Property Set: {propSetDef.Name}",
                sb.ToString()
            );
        }

        private static StringCollection GetCategoriesFromDrawing(
            Transaction tr,
            Database db
        )
        {
            HashSet<string> categories = new HashSet<string>();
            // try
            try
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                foreach (ObjectId btrId in bt)
                {
                    BlockTableRecord btr = tr.GetObject(btrId, OpenMode.ForRead) as BlockTableRecord;

                    foreach (ObjectId entId in btr)
                    {
                        Entity ent = tr.GetObject(entId, OpenMode.ForRead) as Entity;

                        if (ent == null) continue;

                        string className = ent.GetRXClass().Name;

                        categories.Add(className);
                    }
                }
            }
            // catch
            catch
            {
                // opcional log
            }

            StringCollection result = new StringCollection();
            // Iteramos
            foreach (string cat in categories)
                // Almacenamos
                result.Add(cat);

            // return
            return result;
        }



        public static HashSet<string> GetAppliesToSet(
            PropertySetDefinition propSetDef
        )
        {
            HashSet<string> appliesSet = new HashSet<string>();

            if (!propSetDef.AppliesToAll)
            {
                StringCollection appliesTo = propSetDef.AppliesToFilter;

                if (appliesTo != null)
                {
                    foreach (string cls in appliesTo)
                    {
                        appliesSet.Add(cls);
                    }
                }
            }
            // return
            return appliesSet;
        }
    }
}
