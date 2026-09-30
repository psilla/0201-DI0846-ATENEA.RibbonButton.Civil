namespace TYPSA.ATENEA.RibbonButton.Civil
{
    public static class AteneaRibbonCommands
    {
        // ATENEA CIVIL WEB
        public const string ButtonAteneaModelChecker = "ButtonAteneaModelChecker";
        public const string ButtonAteneaParamCheckExp = "ButtonAteneaParamCheckExp";
        public const string ButtonAteneaParamCheckImp = "ButtonAteneaParamCheckImp";
        public const string ButtonAteneaParamDataExp = "ButtonAteneaParamDataExp";
        public const string ButtonAteneaParamDataImp = "ButtonAteneaParamDataImp";

        // Process
        public const string ProcessAteneaModelChecker = "Atenea Model Checker";
        public const string ProcessAteneaParamCheckExp = "Atenea Param Check Exp";
        public const string ProcessAteneaParamCheckImp = "Atenea Param Check Imp";
        public const string ProcessAteneaParamDataExp = "Atenea Param Data Exp";
        public const string ProcessAteneaParamDataImp = "Atenea Param Data Imp";
    }

    public class AteneaRibbonButtonInfo
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public string ImageFileName { get; set; }
        public string CommandParameter { get; set; }
        public string TooltipTitle { get; set; }
        public string TooltipContent { get; set; }
    }

    public static class cls_00_AteneaRibbon
    {
        // -----------------------------
        // ATENEA Model Checker
        // -----------------------------

        public static AteneaRibbonButtonInfo GetAteneaModelChecker()
        {
            return new AteneaRibbonButtonInfo
            {
                Name = "Atenea Model Checker",
                Text = "Atenea Model Checker",
                ImageFileName = "AteneaCompactModels.png",
                CommandParameter = AteneaRibbonCommands.ButtonAteneaModelChecker,
                TooltipTitle = "",
                TooltipContent = ""
            };
        }

        // -----------------------------
        // Param Check Export
        // -----------------------------

        public static AteneaRibbonButtonInfo GetAteneaParamCheckExp()
        {
            return new AteneaRibbonButtonInfo
            {
                Name = "Atenea Param Check Export",
                Text = "Atenea Param Check",
                ImageFileName = "AteneaCompactModels.png",
                CommandParameter = AteneaRibbonCommands.ButtonAteneaParamCheckExp,
                TooltipTitle = "",
                TooltipContent = ""
            };
        }

        // -----------------------------
        // Param Check Import
        // -----------------------------

        public static AteneaRibbonButtonInfo GetAteneaParamCheckImp()
        {
            return new AteneaRibbonButtonInfo
            {
                Name = "Atenea Param Check Import",
                Text = "Atenea Param Check",
                ImageFileName = "AteneaCompactModels.png",
                CommandParameter = AteneaRibbonCommands.ButtonAteneaParamCheckImp,
                TooltipTitle = "",
                TooltipContent = ""
            };
        }

        // -----------------------------
        // Param Data Export
        // -----------------------------

        public static AteneaRibbonButtonInfo GetAteneaParamDataExp()
        {
            return new AteneaRibbonButtonInfo
            {
                Name = "Atenea Param Data Export",
                Text = "Atenea Param Data",
                ImageFileName = "AteneaCompactModels.png",
                CommandParameter = AteneaRibbonCommands.ButtonAteneaParamDataExp,
                TooltipTitle = "Exportador de propiedades",
                TooltipContent =
                    "Exporta las propiedades de todos los Property Set " +
                    "de los modelos seleccionados a un archivo JSON."
            };
        }

        // -----------------------------
        // Param Data Import
        // -----------------------------

        public static AteneaRibbonButtonInfo GetAteneaParamDataImp()
        {
            return new AteneaRibbonButtonInfo
            {
                Name = "Atenea Param Data Import",
                Text = "Atenea Param Data",
                ImageFileName = "AteneaCompactModels.png",
                CommandParameter = AteneaRibbonCommands.ButtonAteneaParamDataImp,
                TooltipTitle = "Importador de propiedades",
                TooltipContent =
                    "Importa las propiedades seleccionadas desde un archivo JSON " +
                    "a los modelos seleccionados."
            };
        }
    }
}