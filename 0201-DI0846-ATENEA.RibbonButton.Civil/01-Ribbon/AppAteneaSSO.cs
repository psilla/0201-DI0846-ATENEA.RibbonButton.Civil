using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using System.Reflection;
using static TYPSA.SharedLib.Autocad.cls_00_Ribbon;

namespace TYPSA.PS.RibbonButton.Civil
{
    public class AppAteneaSSO : IExtensionApplication
    {
        // -----------------------------
        // Botones ATENEA Web
        // -----------------------------

        private static Autodesk.Windows.RibbonButton _buttonAteneaMC;
        private static Autodesk.Windows.RibbonSplitButton _parCheckDropdown;
        private static Autodesk.Windows.RibbonSplitButton _parDataDropdown;

        // -----------------------------
        // Initialize
        // -----------------------------

        public void Initialize()
        {
            // -----------------------------
            // Validar Ribbon
            // -----------------------------

            if (Autodesk.Windows.ComponentManager.Ribbon == null)
            {
                Autodesk.Windows.ComponentManager.ItemInitialized +=
                    ComponentManager_ItemInitialized;
            }
            else
            {
                LoadRibbon();
            }
        }

        // -----------------------------
        // Ribbon Initialized
        // -----------------------------

        private void ComponentManager_ItemInitialized(
            object sender,
            Autodesk.Windows.RibbonItemEventArgs e
        )
        {
            // Validamos
            if (Autodesk.Windows.ComponentManager.Ribbon == null) return;

            // -----------------------------
            // Desuscribir evento
            // -----------------------------

            Autodesk.Windows.ComponentManager.ItemInitialized -=
                ComponentManager_ItemInitialized;

            // -----------------------------
            // Cargar Ribbon
            // -----------------------------

            LoadRibbon();
        }

        // -----------------------------
        // Terminate
        // -----------------------------

        public void Terminate()
        {
            Autodesk.Windows.ComponentManager.ItemInitialized -=
                ComponentManager_ItemInitialized;
        }

        // -----------------------------
        // Cargar Ribbon
        // -----------------------------

        private void LoadRibbon()
        {
            // try
            try
            {
                // -----------------------------
                // Debug
                // -----------------------------

                bool showDebug = false;
                if (showDebug)
                {
                    string[] resources = typeof(AppAteneaSSO).Assembly.GetManifestResourceNames();

                    System.Windows.Forms.MessageBox.Show(
                        string.Join(Environment.NewLine, resources),
                        "Embedded Resources"
                    );
                }

                // -----------------------------
                // Crear Ribbon Control
                // -----------------------------

                Autodesk.Windows.RibbonControl ribbonControl = Autodesk.Windows.ComponentManager.Ribbon;
                // Validamos
                if (ribbonControl == null) return;

                // -----------------------------
                // Crear Command Handler
                // -----------------------------

                MyRibbonCommandHandler commandHandler = new MyRibbonCommandHandler();

                // -----------------------------
                // Obtener Assembly de recursos
                // -----------------------------

                Assembly resourceAssembly = typeof(AppAteneaSSO).Assembly;

                // -----------------------------
                // Evitar duplicados
                // -----------------------------

                foreach (Autodesk.Windows.RibbonTab tab in ribbonControl.Tabs)
                {
                    if (tab.Id == "TESTRIBBON_TAB_ID")
                    {
                        return;
                    }
                }

                // -----------------------------
                // Crear Ribbon
                // -----------------------------

                Autodesk.Windows.RibbonTab rtab = new Autodesk.Windows.RibbonTab
                {
                    Title = "TYPSA-PS",
                    Id = "TESTRIBBON_TAB_ID"
                };

                ribbonControl.Tabs.Add(rtab);

                // -----------------------------
                // Crear Panel ATENEA Web
                // -----------------------------

                Autodesk.Windows.RibbonPanelSource rps1 = new Autodesk.Windows.RibbonPanelSource
                {
                    Title = "ATENEA CIVIL WEB"
                };

                Autodesk.Windows.RibbonPanel rp1 = new Autodesk.Windows.RibbonPanel
                {
                    Source = rps1
                };

                rtab.Panels.Add(rp1);


                // -----------------------------
                // ATENEA Register
                // -----------------------------

                Autodesk.Windows.RibbonButton buttonAteneaRegister = CreateRibbonButtonUpdate(
                    name: "Atenea Register",
                    text: "ATENEA Register",
                    imageFileName: "AteneaCompactModels.png",
                    commandParameter: RibbonCommands.ButtonAteneaRegister,
                    tooltipTitle: "ATENEA Login",
                    tooltipContent: "Authenticate your user to access ATENEA tools.",
                    commandHandler: commandHandler,
                    resourceAssembly: resourceAssembly
                );

                rps1.Items.Add(buttonAteneaRegister);
                rps1.Items.Add(new Autodesk.Windows.RibbonSeparator());

                // -----------------------------
                // Atenea Model Checker
                // -----------------------------

                _buttonAteneaMC = CreateRibbonButtonUpdate(
                    name: "Atenea Model Checker",
                    text: "Atenea Model Checker",
                    imageFileName: "AteneaCompactModels.png",
                    commandParameter: RibbonCommands.ButtonAteneaModelChecker,
                    tooltipTitle: "",
                    tooltipContent: "",
                    commandHandler: commandHandler,
                    resourceAssembly: resourceAssembly
                );

                rps1.Items.Add(_buttonAteneaMC);
                rps1.Items.Add(new Autodesk.Windows.RibbonSeparator());

                // -----------------------------
                // Param Check
                // -----------------------------

                Autodesk.Windows.RibbonButton buttonAteneaParCheckExp = CreateRibbonButtonUpdate(
                    name: "Atenea Param Check Export",
                    text: "Atenea Param Check",
                    imageFileName: "AteneaCompactModels.png",
                    commandParameter: RibbonCommands.ButtonAteneaParamCheckExp,
                    tooltipTitle: "",
                    tooltipContent: "",
                    commandHandler: commandHandler,
                    resourceAssembly: resourceAssembly
                );

                Autodesk.Windows.RibbonButton buttonAteneaParCheckImp = CreateRibbonButtonUpdate(
                    name: "Atenea Param Check Import",
                    text: "Atenea Param Check",
                    imageFileName: "AteneaCompactModels.png",
                    commandParameter: RibbonCommands.ButtonAteneaParamCheckImp,
                    tooltipTitle: "",
                    tooltipContent: "",
                    commandHandler: commandHandler,
                    resourceAssembly: resourceAssembly
                );

                _parCheckDropdown = CreateRibbonSplitButtonUpdate(
                    "parCheckDropdown",
                    "Atenea Param Check",
                    imageFileName: "AteneaCompactModels.png",
                    new List<Autodesk.Windows.RibbonButton>
                    {
                        buttonAteneaParCheckExp,
                        buttonAteneaParCheckImp
                    },
                    resourceAssembly: resourceAssembly
                );

                rps1.Items.Add(_parCheckDropdown);
                rps1.Items.Add(new Autodesk.Windows.RibbonSeparator());

                // -----------------------------
                // Param Data
                // -----------------------------

                Autodesk.Windows.RibbonButton buttonAteneaParDataExp = CreateRibbonButtonUpdate(
                    name: "Atenea Param Data Export",
                    text: "Atenea Param Data",
                    imageFileName: "AteneaCompactModels.png",
                    commandParameter: RibbonCommands.ButtonAteneaParamDataExp,
                    tooltipTitle: "Exportador de propiedades",
                    tooltipContent: "Exporta las propiedades de todos los Property Set de los modelos seleccionados a un archivo JSON.",
                    commandHandler: commandHandler,
                    resourceAssembly: resourceAssembly
                );

                Autodesk.Windows.RibbonButton buttonAteneaParDataImp = CreateRibbonButtonUpdate(
                    name: "Atenea Param Data Import",
                    text: "Atenea Param Data",
                    imageFileName: "AteneaCompactModels.png",
                    commandParameter: RibbonCommands.ButtonAteneaParamDataImp,
                    tooltipTitle: "Importador de propiedades",
                    tooltipContent: "Importa las propiedades seleccionadas desde un archivo JSON a los modelos seleccionados.",
                    commandHandler: commandHandler,
                    resourceAssembly: resourceAssembly
                );

                _parDataDropdown = CreateRibbonSplitButtonUpdate(
                    "parDataDropdown",
                    "Atenea Param Data",
                    imageFileName: "AteneaCompactModels.png",
                    new List<Autodesk.Windows.RibbonButton>
                    {
                        buttonAteneaParDataExp,
                        buttonAteneaParDataImp
                    },
                    resourceAssembly: resourceAssembly
                );

                rps1.Items.Add(_parDataDropdown);

                // -----------------------------
                // Bloquear herramientas ATENEA
                // -----------------------------

                SetAteneaToolsEnabled(false);

                // -----------------------------
                // Activar Ribbon
                // -----------------------------

                rtab.IsActive = true;
            }
            // catch
            catch (System.Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    ex.ToString(),
                    "TYPSA Ribbon Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );
            }
        }

        // -----------------------------
        // Habilitar / deshabilitar ATENEA
        // -----------------------------

        public static void SetAteneaToolsEnabled(
            bool enabled
        )
        {
            if (_buttonAteneaMC != null)
                _buttonAteneaMC.IsEnabled = enabled;

            if (_parCheckDropdown != null)
                _parCheckDropdown.IsEnabled = enabled;

            if (_parDataDropdown != null)
                _parDataDropdown.IsEnabled = enabled;
        }

        // -----------------------------
        // Ribbon Command Handler
        // -----------------------------

        public class MyRibbonCommandHandler :
            System.Windows.Input.ICommand
        {
            public bool CanExecute(
                object parameter
            )
            {
                return true;
            }

            public event EventHandler CanExecuteChanged;

            public void Execute(
                object parameter
            )
            {
                // Validamos
                if (!(parameter is Autodesk.Windows.RibbonButton ribbonButton)) return;

                // Obtenemos comando
                string command = ribbonButton.CommandParameter as string;

                // -----------------------------
                // Procesar comando
                // -----------------------------

                switch (command)
                {
                    // ATENEA Register
                    case RibbonCommands.ButtonAteneaRegister:

                        cls_00_ButtonAteneaRegister.ButtonAteneaRegister();
                        break;

                    // Model Checker
                    case RibbonCommands.ButtonAteneaModelChecker:

                        cls_00_ButtonAteneaModelChecker.ButtonAteneaModelChecker();
                        break;

                    // Param Check Export
                    case RibbonCommands.ButtonAteneaParamCheckExp:

                        cls_00_ButtonAteneaParamCheckExp.ButtonAteneaParamCheckExp();
                        break;

                    // Param Check Import
                    case RibbonCommands.ButtonAteneaParamCheckImp:

                        cls_00_ButtonAteneaParamCheckImp.ButtonAteneaParamCheckImp();
                        break;

                    // Param Data Export
                    case RibbonCommands.ButtonAteneaParamDataExp:

                        cls_00_ButtonAteneaParamDataExp.ButtonAteneaParamDataExp();
                        break;

                    // Param Data Import
                    case RibbonCommands.ButtonAteneaParamDataImp:

                        cls_00_ButtonAteneaParamDataImp.ButtonAteneaParamDataImp();
                        break;

                    default:
                        break;
                }
            }
        }

       

      


    }
}