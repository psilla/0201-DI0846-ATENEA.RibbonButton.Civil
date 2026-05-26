using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using TYPSA.PS.RibbonButton.Civil.Buttons;
using TYPSA.SharedLib.Civil.Buttons;
using _0201_DI0846_ATENEA.RibbonButton.Civil.Properties;

namespace TYPSA.PS.RibbonButton.Civil
{
    public static class RibbonCommands
    {
        public const string ProjectInfoComparer = "ProjectInfoComparer";
        public const string PropertySetComparer = "PropertySetComparer";
        public const string SelectByHandle = "SelectByHandle";
        
        // ATENEA CIVIL WEB

        public const string ButtonAteneaModelChecker = "ButtonAteneaModelChecker";
        public const string ButtonAteneaParamCheckExp = "ButtonAteneaParamCheckExp";
        public const string ButtonAteneaParamDataExp = "ButtonAteneaParamDataExp";
        public const string ButtonAteneaParamDataImp = "ButtonAteneaParamDataImp";
    }

    public class App : IExtensionApplication
    {
        public void Initialize()
        {

            LoadRibbon();
        }

        public void Terminate()
        {
            // Aquí puedes realizar acciones cuando la aplicación se descarga.
        }

        private void LoadRibbon()
        {
            Autodesk.Windows.RibbonControl ribbonControl = Autodesk.Windows.ComponentManager.Ribbon;
            if (ribbonControl != null)
            {
                /////////////// CREAR RIBBON //////////////////////

                Autodesk.Windows.RibbonTab rtab = new Autodesk.Windows.RibbonTab();
                rtab.Title = "TYPSA-PS";
                rtab.Id = "TESTRIBBON_TAB_ID";
                ribbonControl.Tabs.Add(rtab);

                /////////////// CREAR PANELES //////////////////////

                // Panel1
                Autodesk.Windows.RibbonPanelSource rps1 = new Autodesk.Windows.RibbonPanelSource();
                rps1.Title = "TYPSA UTILS"; 
                Autodesk.Windows.RibbonPanel rp1 = new Autodesk.Windows.RibbonPanel();
                rp1.Source = rps1;
                rtab.Panels.Add(rp1);

                // Panel2
                Autodesk.Windows.RibbonPanelSource rps2 = new Autodesk.Windows.RibbonPanelSource();
                rps2.Title = "ATENEA PROPERTIES COMPARER"; 
                Autodesk.Windows.RibbonPanel rp2 = new Autodesk.Windows.RibbonPanel();
                rp2.Source = rps2;
                rtab.Panels.Add(rp2);

                // Panel3
                Autodesk.Windows.RibbonPanelSource rps3 = new Autodesk.Windows.RibbonPanelSource();
                rps3.Title = "ATENEA PROPERTIES EXCHANGE EXCEL"; 
                Autodesk.Windows.RibbonPanel rp3 = new Autodesk.Windows.RibbonPanel();
                rp3.Source = rps3;
                rtab.Panels.Add(rp3);

                // Panel4
                Autodesk.Windows.RibbonPanelSource rps4 = new Autodesk.Windows.RibbonPanelSource();
                rps4.Title = "ATENEA PROPERTIES EXCHANGE WEB"; 
                Autodesk.Windows.RibbonPanel rp4 = new Autodesk.Windows.RibbonPanel();
                rp4.Source = rps4;
                rtab.Panels.Add(rp4);

                // Panel5
                Autodesk.Windows.RibbonPanelSource rps5 = new Autodesk.Windows.RibbonPanelSource();
                rps5.Title = "ATENEA MODEL CHECKER";
                Autodesk.Windows.RibbonPanel rp5 = new Autodesk.Windows.RibbonPanel();
                rp5.Source = rps5;
                rtab.Panels.Add(rp5);

                /////////////// ADDING BUTTONS //////////////////////

                // SELECT BY HANDLE

                Autodesk.Windows.RibbonButton buttonSelectByHandle = CreateRibbonButton(
                    name: "Select by Handle",
                    text: "Select by Handle",
                    image: Resources.ArrowClickerIcon02,
                    commandParameter: RibbonCommands.SelectByHandle,
                    tooltipTitle: "Selección por Handle",
                    tooltipContent: "Selecciona objetos escribiendo directamente su Handle."
                );
                // Añadimos button
                rps1.Items.Add(buttonSelectByHandle);

                // DATA EXTRACTION

                Autodesk.Windows.RibbonButton buttonProjectInfoComparer = CreateRibbonButton(
                    name: "Project Info Comparer",
                    text: "Project Info Comparer",
                    image: Resources.AteneaModelChecker,
                    commandParameter: RibbonCommands.ProjectInfoComparer,
                    tooltipTitle: "Comparación de parámetros",
                    tooltipContent: "Compara los valores de Project Information entre modelos."
                );
                // Añadimos button
                rps2.Items.Add(buttonProjectInfoComparer);

                // Separador visual
                rps2.Items.Add(new Autodesk.Windows.RibbonSeparator());

                // UNITS & ZONE

                Autodesk.Windows.RibbonButton buttonPropertySetComparer = CreateRibbonButton(
                    name: "Property Set Comparer",
                    text: "Property Set Comparer",
                    image: Resources.AteneaCompactModels,
                    commandParameter: RibbonCommands.PropertySetComparer,
                    tooltipTitle: "Comparación de Property Sets",
                    tooltipContent: "Compara Property Sets definidos entre varios modelos."
                );
                // Añadimos button
                rps2.Items.Add(buttonPropertySetComparer);

                // Param Data Exporter Excel
                // Document
                Autodesk.Windows.RibbonButton button4 = CreateRibbonButton(
                    name: "Properties Exporter Doc",
                    text: "Properties Exporter",
                    image: Resources.ParamExcel,
                    commandParameter: cls_00_ButtonParamDataExpExcelDoc.RibbonCommands.ButtonParamDataExpExcelDoc,
                    tooltipTitle: "Exportador de propiedades del modelo activo",
                    tooltipContent: "Exporta las propiedades de todos los Property Set del modelo activo a un archivo Excel."
                );
                // Back
                Autodesk.Windows.RibbonButton button5 = CreateRibbonButton(
                    name: "Properties Exporter Back",
                    text: "Properties Exporter",
                    image: Resources.ParamExcel,
                    commandParameter: cls_00_ButtonParamDataExpExcelBack.RibbonCommands.ButtonParamDataExpExcelBack,
                    tooltipTitle: "Exportador de propiedades",
                    tooltipContent: "Exporta las propiedades de todos los Property Set de los modelos seleccionados a un archivo Excel."
                );
                // Crear dropdown 
                RibbonSplitButton expExcelDropdown = CreateRibbonSplitButton(
                    "ExpExcelDropdown",
                    "Properties Exporter Excel",
                    Resources.ParamExcel,
                    new List<Autodesk.Windows.RibbonButton> { button4, button5 }
                );
                // Añadimos button
                rps3.Items.Add(expExcelDropdown);

                // Separador visual
                rps3.Items.Add(new Autodesk.Windows.RibbonSeparator());

                // Param Data Importer Excel
                // Document
                Autodesk.Windows.RibbonButton button6 = CreateRibbonButton(
                    name: "Properties Importer Doc",
                    text: "Properties Importer",
                    image: Resources.ParamExcel,
                    commandParameter: cls_00_ButtonParamDataImpExcelDoc.RibbonCommands.ButtonParamDataImpExcelDoc,
                    tooltipTitle: "Importador de propiedades al modelo activo",
                    tooltipContent: "Importa las propiedades seleccionadas desde un archivo Excel al modelo activo."
                );
                // Back
                Autodesk.Windows.RibbonButton button7 = CreateRibbonButton(
                    name: "Properties Importer Back",
                    text: "Properties Importer",
                    image: Resources.ParamExcel,
                    commandParameter: cls_00_ButtonParamDataImpExcelBack.RibbonCommands.ButtonParamDataImpExcelBack,
                    tooltipTitle: "Importador de propiedades",
                    tooltipContent: "Importa las propiedades seleccionadas desde un archivo Excel a los modelos seleccionados."
                );
                // Crear dropdown 
                RibbonSplitButton impExcelDropdown = CreateRibbonSplitButton(
                    "ImpExcelDropdown",
                    "Properties Importer Excel",
                    Resources.ParamExcel,
                    new List<Autodesk.Windows.RibbonButton> { button6, button7 }
                );
                // Añadimos button
                rps3.Items.Add(impExcelDropdown);

                // Param Data Exporter Web
                // Back
                Autodesk.Windows.RibbonButton button8 = CreateRibbonButton(
                    name: "Properties Exporter Back",
                    text: "Properties Exporter",
                    image: Resources.exporter,
                    commandParameter: RibbonCommands.ButtonAteneaParamDataExp,
                    tooltipTitle: "Exportador de propiedades",
                    tooltipContent: "Exporta las propiedades de todos los Property Set de los modelos seleccionados a un archivo JSON."
                );
                // Crear dropdown 
                RibbonSplitButton expWebDropdown = CreateRibbonSplitButton(
                    "expWebDropdown",
                    "Properties Exporter Web",
                    Resources.exporter,
                    new List<Autodesk.Windows.RibbonButton> { button8 }
                );
                // Añadimos button
                rps4.Items.Add(expWebDropdown);

                // Separador visual
                rps4.Items.Add(new Autodesk.Windows.RibbonSeparator());

                // Param Data Importer Web
                // Back
                Autodesk.Windows.RibbonButton button9 = CreateRibbonButton(
                    name: "Properties Importer Back",
                    text: "Properties Importer",
                    image: Resources.importer,
                    commandParameter: RibbonCommands.ButtonAteneaParamDataImp,
                    tooltipTitle: "Importador de propiedades",
                    tooltipContent: "Importa las propiedades seleccionadas desde un archivo JSON a los modelos seleccionados."
                );
                // Crear dropdown 
                RibbonSplitButton impWebDropdown = CreateRibbonSplitButton(
                    "impWebDropdown",
                    "Properties Importer Web",
                    Resources.importer,
                    new List<Autodesk.Windows.RibbonButton> { button9 }
                );
                // Añadimos button
                rps4.Items.Add(impWebDropdown);

                // ATENEA MC
                // General Analysis
                Autodesk.Windows.RibbonButton buttonAteneaMC = CreateRibbonButton(
                    name: "Atenea Model Checker",
                    text: "Atenea Model Checker",
                    image: Resources.AteneaCompactModels,
                    commandParameter: RibbonCommands.ButtonAteneaModelChecker,
                    tooltipTitle: "",
                    tooltipContent: ""
                );
                // Añadimos button
                rps5.Items.Add(buttonAteneaMC);

                // Separador visual
                rps5.Items.Add(new Autodesk.Windows.RibbonSeparator());

                // Param Check Exporter
                Autodesk.Windows.RibbonButton buttonAteneaParDataExp = CreateRibbonButton(
                    name: "Atenea Param Check Exporter",
                    text: "Atenea Param Check Exporter",
                    image: Resources.AteneaCompactModels,
                    commandParameter: RibbonCommands.ButtonAteneaParamCheckExp,
                    tooltipTitle: "",
                    tooltipContent: ""
                );
                // Añadimos button
                rps5.Items.Add(buttonAteneaParDataExp);

                /////////////// ACTIVAR RIBBON //////////////////////

                rtab.IsActive = true;
            }
        }

        // Define a command handler class implementing the ICommand interface for ribbon button actions.
        public class MyRibbonCommandHandler : System.Windows.Input.ICommand
        {
            // Determines whether the command can be executed. Always returns true in this case.
            public bool CanExecute(object parameter)
            {
                return true;
            }

            // Event that must be declared when implementing ICommand, 
            // but it's not used here. It's for handling changes in command execution state.
            public event EventHandler CanExecuteChanged;

            // Executes the actual command logic based on the parameter passed, typically a ribbon button.
            public void Execute(object parameter)
            {
                // Check if the parameter is a RibbonButton from Autodesk's UI components.
                if (parameter is Autodesk.Windows.RibbonButton ribbonButton)
                {
                    // Retrieve the command parameter from the ribbon button, expected to be a string.
                    string command = ribbonButton.CommandParameter as string;
                    switch (command)
                    {
                        case RibbonCommands.ProjectInfoComparer:
                            // Instanciamos la clase
                            cls_02_ButtonProjectInfoComparer buttonProjectInfoComparer = new cls_02_ButtonProjectInfoComparer();
                            buttonProjectInfoComparer.ProjectInfoComparer();
                            break;

                        case RibbonCommands.PropertySetComparer:
                            // Instanciamos la clase
                            cls_03_ButtonPropertySetComparer buttonPropertySetComparer = new cls_03_ButtonPropertySetComparer();
                            buttonPropertySetComparer.PropertySetComparer();
                            break;

                        case RibbonCommands.SelectByHandle:
                            // Instanciamos la clase
                            cls_01_ButtonSelectByHandle selectByHandle = new cls_01_ButtonSelectByHandle();
                            selectByHandle.SelectByHandle();
                            break;

                        // ATENEA CIVIL EXCEL

                        case cls_00_ButtonParamDataExpExcelDoc.RibbonCommands.ButtonParamDataExpExcelDoc:
                            // Instanciamos la clase
                            cls_00_ButtonParamDataExpExcelDoc.ButtonParamDataExpExcelDoc();
                            break;

                        case cls_00_ButtonParamDataExpExcelBack.RibbonCommands.ButtonParamDataExpExcelBack:
                            // Instanciamos la clase
                            cls_00_ButtonParamDataExpExcelBack.ButtonParamDataExpExcelBack();
                            break;

                        case cls_00_ButtonParamDataImpExcelDoc.RibbonCommands.ButtonParamDataImpExcelDoc:
                            // Instanciamos la clase
                            cls_00_ButtonParamDataImpExcelDoc.ButtonParamDataImpExcelDoc();
                            break;

                        case cls_00_ButtonParamDataImpExcelBack.RibbonCommands.ButtonParamDataImpExcelBack:
                            // Instanciamos la clase
                            cls_00_ButtonParamDataImpExcelBack.ButtonParamDataImpExcelBack();
                            break;

                        // ATENEA CIVIL WEB

                        case RibbonCommands.ButtonAteneaModelChecker:
                            // Instanciamos la clase
                            cls_00_ButtonAteneaModelChecker.ButtonAteneaModelChecker();
                            break;

                        case RibbonCommands.ButtonAteneaParamCheckExp:
                            // Instanciamos la clase
                            cls_00_ButtonAteneaParamCheckExp.ButtonAteneaParamDataExp();
                            break;

                        case RibbonCommands.ButtonAteneaParamDataExp:
                            // Instanciamos la clase
                            cls_00_ButtonAteneaParamDataExp.PropExportBackToJSON();
                            break;

                        case RibbonCommands.ButtonAteneaParamDataImp:
                            // Instanciamos la clase
                            cls_00_ButtonAteneaParamDataImp.PropImportBackFromJSON();
                            break;

                        // Default case for unhandled commands. No action is taken.
                        default:
                            break;
                    }
                }
            }
        }

        private BitmapImage GetImageSource(Image img)
        {
            try
            {
                if (img == null)
                {
                    throw new ArgumentNullException(nameof(img), "La imagen no puede ser null.");
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    Bitmap bitmap = new Bitmap(img);
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;

                    BitmapImage bmpImg = new BitmapImage();
                    bmpImg.BeginInit();
                    bmpImg.CacheOption = BitmapCacheOption.OnLoad;
                    bmpImg.StreamSource = ms;
                    bmpImg.EndInit();
                    bmpImg.Freeze(); // Evita problemas de hilos en WPF y AutoCAD

                    return bmpImg;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    $"❌ ERROR en GetImageSource: {ex.Message}",
                    "Error de Conversión de Imagen"
                );
                return null;
            }
        }

        private Autodesk.Windows.RibbonButton CreateRibbonButton(
            string name,
            string text,
            Image image,
            string commandParameter,
            string tooltipTitle,
            string tooltipContent
        )
        {
            ImageSource imageSource = GetImageSource(image);

            Autodesk.Windows.RibbonButton button = new Autodesk.Windows.RibbonButton
            {
                Name = name,
                ShowText = true,
                Text = text,
                ShowImage = true,
                LargeImage = imageSource,
                Size = Autodesk.Windows.RibbonItemSize.Large,
                CommandHandler = new MyRibbonCommandHandler(),
                CommandParameter = commandParameter,
                // Tooltip
                ToolTip = new Autodesk.Windows.RibbonToolTip
                {
                    Title = tooltipTitle,
                    Content = tooltipContent,
                    IsHelpEnabled = false
                }
            };

            return button;
        }

        private Autodesk.Windows.RibbonSplitButton CreateRibbonSplitButton(
            string name,
            string text,
            Image image,
            List<Autodesk.Windows.RibbonButton> buttons
        )
        {
            ImageSource imageSource = GetImageSource(image);

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

            // Iteramos
            foreach (var button in buttons)
            {
                splitButton.Items.Add(button);
            }

            // return
            return splitButton;
        }





    }
}