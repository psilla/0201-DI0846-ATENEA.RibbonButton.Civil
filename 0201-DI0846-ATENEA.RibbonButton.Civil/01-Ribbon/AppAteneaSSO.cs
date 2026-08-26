using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.Runtime;
using _0201_DI0846_ATENEA.RibbonButton.Civil.Properties;

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
            LoadRibbon();
        }

        // -----------------------------
        // Terminate
        // -----------------------------

        public void Terminate()
        {
        }

        // -----------------------------
        // Cargar Ribbon
        // -----------------------------

        private void LoadRibbon()
        {
            Autodesk.Windows.RibbonControl ribbonControl =
                Autodesk.Windows.ComponentManager.Ribbon;

            // Validamos
            if (ribbonControl == null) return;

            // -----------------------------
            // Crear Ribbon
            // -----------------------------

            Autodesk.Windows.RibbonTab rtab =
                new Autodesk.Windows.RibbonTab
                {
                    Title = "TYPSA-PS",
                    Id = "TESTRIBBON_TAB_ID"
                };

            ribbonControl.Tabs.Add(rtab);

            // -----------------------------
            // Crear Panel ATENEA Web
            // -----------------------------

            Autodesk.Windows.RibbonPanelSource rps4 =
                new Autodesk.Windows.RibbonPanelSource
                {
                    Title = "ATENEA CIVIL WEB"
                };

            Autodesk.Windows.RibbonPanel rp4 =
                new Autodesk.Windows.RibbonPanel
                {
                    Source = rps4
                };

            rtab.Panels.Add(rp4);

           
            // -----------------------------
            // ATENEA Register
            // -----------------------------

            Autodesk.Windows.RibbonButton buttonAteneaRegister =
                CreateRibbonButton(
                    name: "Atenea Register",
                    text: "ATENEA Register",
                    image: Resources.AteneaCompactModels,
                    commandParameter: RibbonCommands.ButtonAteneaRegister,
                    tooltipTitle: "ATENEA Login",
                    tooltipContent: "Authenticate your user to access ATENEA tools."
                );

            // Añadimos button
            rps4.Items.Add(buttonAteneaRegister);

            // Separador visual
            rps4.Items.Add(new Autodesk.Windows.RibbonSeparator());

            // -----------------------------
            // Atenea Model Checker
            // -----------------------------

            _buttonAteneaMC = CreateRibbonButton(
                name: "Atenea Model Checker",
                text: "Atenea Model Checker",
                image: Resources.AteneaCompactModels,
                commandParameter: RibbonCommands.ButtonAteneaModelChecker,
                tooltipTitle: "",
                tooltipContent: ""
            );

            // Añadimos button
            rps4.Items.Add(_buttonAteneaMC);

            // Separador visual
            rps4.Items.Add(new Autodesk.Windows.RibbonSeparator());

            // -----------------------------
            // Param Check
            // -----------------------------

            Autodesk.Windows.RibbonButton buttonAteneaParCheckExp =
                CreateRibbonButton(
                    name: "Atenea Param Check Export",
                    text: "Atenea Param Check",
                    image: Resources.AteneaCompactModels,
                    commandParameter: RibbonCommands.ButtonAteneaParamCheckExp,
                    tooltipTitle: "",
                    tooltipContent: ""
                );

            Autodesk.Windows.RibbonButton buttonAteneaParCheckImp =
                CreateRibbonButton(
                    name: "Atenea Param Check Import",
                    text: "Atenea Param Check",
                    image: Resources.AteneaCompactModels,
                    commandParameter: RibbonCommands.ButtonAteneaParamCheckImp,
                    tooltipTitle: "",
                    tooltipContent: ""
                );

            _parCheckDropdown = CreateRibbonSplitButton(
                "parCheckDropdown",
                "Atenea Param Check",
                Resources.exporter,
                new List<Autodesk.Windows.RibbonButton>
                {
                    buttonAteneaParCheckExp,
                    buttonAteneaParCheckImp
                }
            );

            // Añadimos button
            rps4.Items.Add(_parCheckDropdown);

            // Separador visual
            rps4.Items.Add(new Autodesk.Windows.RibbonSeparator());

            // -----------------------------
            // Param Data
            // -----------------------------

            Autodesk.Windows.RibbonButton buttonAteneaParDataExp =
                CreateRibbonButton(
                    name: "Atenea Param Data Export",
                    text: "Atenea Param Data",
                    image: Resources.exporter,
                    commandParameter: RibbonCommands.ButtonAteneaParamDataExp,
                    tooltipTitle: "Exportador de propiedades",
                    tooltipContent: "Exporta las propiedades de todos los Property Set de los modelos seleccionados a un archivo JSON."
                );

            Autodesk.Windows.RibbonButton buttonAteneaParDataImp =
                CreateRibbonButton(
                    name: "Atenea Param Data Import",
                    text: "Atenea Param Data",
                    image: Resources.importer,
                    commandParameter: RibbonCommands.ButtonAteneaParamDataImp,
                    tooltipTitle: "Importador de propiedades",
                    tooltipContent: "Importa las propiedades seleccionadas desde un archivo JSON a los modelos seleccionados."
                );

            _parDataDropdown = CreateRibbonSplitButton(
                "parDataDropdown",
                "Atenea Param Data",
                Resources.exporter,
                new List<Autodesk.Windows.RibbonButton>
                {
                    buttonAteneaParDataExp,
                    buttonAteneaParDataImp
                }
            );

            // Añadimos button
            rps4.Items.Add(_parDataDropdown);

            // -----------------------------
            // Bloquear herramientas ATENEA
            // -----------------------------

            SetAteneaToolsEnabled(false);

            // -----------------------------
            // Activar Ribbon
            // -----------------------------

            rtab.IsActive = true;
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
                if (!(parameter is Autodesk.Windows.RibbonButton ribbonButton))
                    return;

                // Obtenemos comando
                string command =
                    ribbonButton.CommandParameter as string;

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

                        cls_00_ButtonAteneaParamCheckExp.ButtonAteneaParamDataExp();

                        break;

                    // Param Check Import
                    case RibbonCommands.ButtonAteneaParamCheckImp:

                        cls_00_ButtonAteneaParamCheckImp.ButtonAteneaParamDataImp();

                        break;

                    // Param Data Export
                    case RibbonCommands.ButtonAteneaParamDataExp:

                        cls_00_ButtonAteneaParamDataExp.PropExportBackToJSON();

                        break;

                    // Param Data Import
                    case RibbonCommands.ButtonAteneaParamDataImp:

                        cls_00_ButtonAteneaParamDataImp.PropImportBackFromJSON();

                        break;

                    default:
                        break;
                }
            }
        }

        // -----------------------------
        // Obtener ImageSource
        // -----------------------------

        private BitmapImage GetImageSource(
            Image img
        )
        {
            try
            {
                // Validamos
                if (img == null)
                    throw new ArgumentNullException(nameof(img));

                using (MemoryStream ms = new MemoryStream())
                {
                    using (Bitmap bitmap = new Bitmap(img))
                    {
                        bitmap.Save(
                            ms,
                            System.Drawing.Imaging.ImageFormat.Png
                        );
                    }

                    ms.Position = 0;

                    BitmapImage bmpImg =
                        new BitmapImage();

                    bmpImg.BeginInit();

                    bmpImg.CacheOption =
                        BitmapCacheOption.OnLoad;

                    bmpImg.StreamSource = ms;

                    bmpImg.EndInit();
                    bmpImg.Freeze();

                    // return
                    return bmpImg;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"ERROR en GetImageSource: {ex.Message}",
                    "Error de Conversión de Imagen"
                );

                return null;
            }
        }

        // -----------------------------
        // Crear Ribbon Button
        // -----------------------------

        private Autodesk.Windows.RibbonButton CreateRibbonButton(
            string name,
            string text,
            Image image,
            string commandParameter,
            string tooltipTitle,
            string tooltipContent
        )
        {
            ImageSource imageSource =
                GetImageSource(image);

            Autodesk.Windows.RibbonButton button =
                new Autodesk.Windows.RibbonButton
                {
                    Name = name,
                    ShowText = true,
                    Text = text,
                    ShowImage = true,
                    LargeImage = imageSource,
                    Size = Autodesk.Windows.RibbonItemSize.Large,
                    CommandHandler = new MyRibbonCommandHandler(),
                    CommandParameter = commandParameter,

                    ToolTip = new Autodesk.Windows.RibbonToolTip
                    {
                        Title = tooltipTitle,
                        Content = tooltipContent,
                        IsHelpEnabled = false
                    }
                };

            // return
            return button;
        }

        // -----------------------------
        // Crear Ribbon Split Button
        // -----------------------------

        private Autodesk.Windows.RibbonSplitButton CreateRibbonSplitButton(
            string name,
            string text,
            Image image,
            List<Autodesk.Windows.RibbonButton> buttons
        )
        {
            ImageSource imageSource =
                GetImageSource(image);

            Autodesk.Windows.RibbonSplitButton splitButton =
                new Autodesk.Windows.RibbonSplitButton
                {
                    Name = name,
                    Text = text,
                    ShowImage = true,
                    ShowText = true,
                    Size = Autodesk.Windows.RibbonItemSize.Large,
                    LargeImage = imageSource
                };

            // Añadimos botones
            foreach (Autodesk.Windows.RibbonButton button in buttons)
            {
                splitButton.Items.Add(button);
            }

            // return
            return splitButton;
        }
    }
}