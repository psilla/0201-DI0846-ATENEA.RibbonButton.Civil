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
using TYPSA.SharedLib.Civil;
using _0201_DI0846_ATENEA.RibbonButton.Civil.Properties;
using TYPSA.PS.RibbonButton.Civil;

namespace TYPSA.ATENEA.RibbonButton.Civil
{
    public static class RibbonCommands
    {
        public const string ProjectInfoComparer = "ProjectInfoComparer";
        public const string PropertySetComparer = "PropertySetComparer";
        public const string SelectByHandle = "SelectByHandle";

        // ATENEA REGISTER
        public const string ButtonAteneaRegister = "ButtonAteneaRegister";
    }

    public class AppAtenea : IExtensionApplication
    {
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
        // Load Ribbon
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
            // Panel 1 - TYPSA UTILS
            // -----------------------------

            Autodesk.Windows.RibbonPanelSource rps1 =
                new Autodesk.Windows.RibbonPanelSource
                {
                    Title = "TYPSA UTILS"
                };

            Autodesk.Windows.RibbonPanel rp1 =
                new Autodesk.Windows.RibbonPanel
                {
                    Source = rps1
                };

            rtab.Panels.Add(rp1);

            // -----------------------------
            // Panel 2 - Properties Comparer
            // -----------------------------

            Autodesk.Windows.RibbonPanelSource rps2 =
                new Autodesk.Windows.RibbonPanelSource
                {
                    Title = "ATENEA PROPERTIES COMPARER"
                };

            Autodesk.Windows.RibbonPanel rp2 =
                new Autodesk.Windows.RibbonPanel
                {
                    Source = rps2
                };

            rtab.Panels.Add(rp2);

            // -----------------------------
            // Panel 3 - Properties Excel
            // -----------------------------

            Autodesk.Windows.RibbonPanelSource rps3 =
                new Autodesk.Windows.RibbonPanelSource
                {
                    Title = "ATENEA PROPERTIES EXCHANGE EXCEL"
                };

            Autodesk.Windows.RibbonPanel rp3 =
                new Autodesk.Windows.RibbonPanel
                {
                    Source = rps3
                };

            rtab.Panels.Add(rp3);

            // -----------------------------
            // Select By Handle
            // -----------------------------

            Autodesk.Windows.RibbonButton buttonSelectByHandle =
                CreateRibbonButton(
                    name: "Select by Handle",
                    text: "Select by Handle",
                    image: Resources.ArrowClickerIcon02,
                    commandParameter: RibbonCommands.SelectByHandle,
                    tooltipTitle: "Selección por Handle",
                    tooltipContent:
                        "Selecciona objetos escribiendo directamente su Handle."
                );

            rps1.Items.Add(buttonSelectByHandle);

            // -----------------------------
            // Project Info Comparer
            // -----------------------------

            Autodesk.Windows.RibbonButton buttonProjectInfoComparer =
                CreateRibbonButton(
                    name: "Project Info Comparer",
                    text: "Project Info Comparer",
                    image: Resources.AteneaModelChecker,
                    commandParameter: RibbonCommands.ProjectInfoComparer,
                    tooltipTitle: "Comparación de parámetros",
                    tooltipContent:
                        "Compara los valores de Project Information entre modelos."
                );

            rps2.Items.Add(buttonProjectInfoComparer);

            // Separador
            rps2.Items.Add(
                new Autodesk.Windows.RibbonSeparator()
            );

            // -----------------------------
            // Property Set Comparer
            // -----------------------------

            Autodesk.Windows.RibbonButton buttonPropertySetComparer =
                CreateRibbonButton(
                    name: "Property Set Comparer",
                    text: "Property Set Comparer",
                    image: Resources.AteneaCompactModels,
                    commandParameter: RibbonCommands.PropertySetComparer,
                    tooltipTitle: "Comparación de Property Sets",
                    tooltipContent:
                        "Compara Property Sets definidos entre varios modelos."
                );

            rps2.Items.Add(buttonPropertySetComparer);

            // -----------------------------
            // Properties Exporter Excel
            // Document
            // -----------------------------

            Autodesk.Windows.RibbonButton button4 =
                CreateRibbonButton(
                    name: "Properties Exporter Doc",
                    text: "Properties Exporter",
                    image: Resources.ParamExcel,
                    commandParameter:
                        cls_00_ButtonParamDataExpExcelDoc
                            .RibbonCommands
                            .ButtonParamDataExpExcelDoc,
                    tooltipTitle:
                        "Exportador de propiedades del modelo activo",
                    tooltipContent:
                        "Exporta las propiedades de todos los Property Set " +
                        "del modelo activo a un archivo Excel."
                );

            // -----------------------------
            // Properties Exporter Excel
            // Background
            // -----------------------------

            Autodesk.Windows.RibbonButton button5 =
                CreateRibbonButton(
                    name: "Properties Exporter Back",
                    text: "Properties Exporter",
                    image: Resources.ParamExcel,
                    commandParameter:
                        cls_00_ButtonParamDataExpExcelBack
                            .RibbonCommands
                            .ButtonParamDataExpExcelBack,
                    tooltipTitle: "Exportador de propiedades",
                    tooltipContent:
                        "Exporta las propiedades de todos los Property Set " +
                        "de los modelos seleccionados a un archivo Excel."
                );

            // -----------------------------
            // Export Excel Dropdown
            // -----------------------------

            RibbonSplitButton expExcelDropdown =
                CreateRibbonSplitButton(
                    "ExpExcelDropdown",
                    "Properties Exporter Excel",
                    Resources.ParamExcel,
                    new List<Autodesk.Windows.RibbonButton>
                    {
                        button4,
                        button5
                    }
                );

            rps3.Items.Add(expExcelDropdown);

            // Separador
            rps3.Items.Add(
                new Autodesk.Windows.RibbonSeparator()
            );

            // -----------------------------
            // Properties Importer Excel
            // Document
            // -----------------------------

            Autodesk.Windows.RibbonButton button6 =
                CreateRibbonButton(
                    name: "Properties Importer Doc",
                    text: "Properties Importer",
                    image: Resources.ParamExcel,
                    commandParameter:
                        cls_00_ButtonParamDataImpExcelDoc
                            .RibbonCommands
                            .ButtonParamDataImpExcelDoc,
                    tooltipTitle:
                        "Importador de propiedades al modelo activo",
                    tooltipContent:
                        "Importa las propiedades seleccionadas desde un archivo " +
                        "Excel al modelo activo."
                );

            // -----------------------------
            // Properties Importer Excel
            // Background
            // -----------------------------

            Autodesk.Windows.RibbonButton button7 =
                CreateRibbonButton(
                    name: "Properties Importer Back",
                    text: "Properties Importer",
                    image: Resources.ParamExcel,
                    commandParameter:
                        cls_00_ButtonParamDataImpExcelBack
                            .RibbonCommands
                            .ButtonParamDataImpExcelBack,
                    tooltipTitle: "Importador de propiedades",
                    tooltipContent:
                        "Importa las propiedades seleccionadas desde un archivo " +
                        "Excel a los modelos seleccionados."
                );

            // -----------------------------
            // Import Excel Dropdown
            // -----------------------------

            RibbonSplitButton impExcelDropdown =
                CreateRibbonSplitButton(
                    "ImpExcelDropdown",
                    "Properties Importer Excel",
                    Resources.ParamExcel,
                    new List<Autodesk.Windows.RibbonButton>
                    {
                        button6,
                        button7
                    }
                );

            rps3.Items.Add(impExcelDropdown);

            // -----------------------------
            // Activar Ribbon
            // -----------------------------

            rtab.IsActive = true;
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

                // -----------------------------
                // Obtener comando
                // -----------------------------

                string command =
                    ribbonButton.CommandParameter as string;

                // -----------------------------
                // Procesar comando
                // -----------------------------

                switch (command)
                {
                    case RibbonCommands.ProjectInfoComparer:

                        cls_02_ButtonProjectInfoComparer buttonProjectInfoComparer =
                            new cls_02_ButtonProjectInfoComparer();

                        buttonProjectInfoComparer.ProjectInfoComparer();
                        break;

                    case RibbonCommands.PropertySetComparer:

                        cls_03_ButtonPropertySetComparer buttonPropertySetComparer =
                            new cls_03_ButtonPropertySetComparer();

                        buttonPropertySetComparer.PropertySetComparer();
                        break;

                    case RibbonCommands.SelectByHandle:

                        cls_01_ButtonSelectByHandle selectByHandle =
                            new cls_01_ButtonSelectByHandle();

                        selectByHandle.SelectByHandle();
                        break;

                    // -----------------------------
                    // ATENEA CIVIL EXCEL
                    // -----------------------------

                    case cls_00_ButtonParamDataExpExcelDoc
                        .RibbonCommands
                        .ButtonParamDataExpExcelDoc:

                        cls_00_ButtonParamDataExpExcelDoc
                            .ButtonParamDataExpExcelDoc();

                        break;

                    case cls_00_ButtonParamDataExpExcelBack
                        .RibbonCommands
                        .ButtonParamDataExpExcelBack:

                        cls_00_ButtonParamDataExpExcelBack
                            .ButtonParamDataExpExcelBack();

                        break;

                    case cls_00_ButtonParamDataImpExcelDoc
                        .RibbonCommands
                        .ButtonParamDataImpExcelDoc:

                        cls_00_ButtonParamDataImpExcelDoc
                            .ButtonParamDataImpExcelDoc();

                        break;

                    case cls_00_ButtonParamDataImpExcelBack
                        .RibbonCommands
                        .ButtonParamDataImpExcelBack:

                        cls_00_ButtonParamDataImpExcelBack
                            .ButtonParamDataImpExcelBack();

                        break;

                    default:
                        break;
                }
            }
        }

        // -----------------------------
        // Get Image Source
        // -----------------------------

        private BitmapImage GetImageSource(
            Image img
        )
        {
            try
            {
                // Validamos
                if (img == null)
                {
                    throw new ArgumentNullException(
                        nameof(img),
                        "La imagen no puede ser null."
                    );
                }

                // -----------------------------
                // Convertir imagen
                // -----------------------------

                using (MemoryStream ms = new MemoryStream())
                {
                    Bitmap bitmap =
                        new Bitmap(img);

                    bitmap.Save(
                        ms,
                        System.Drawing.Imaging.ImageFormat.Png
                    );

                    ms.Position = 0;

                    BitmapImage bmpImg =
                        new BitmapImage();

                    bmpImg.BeginInit();
                    bmpImg.CacheOption = BitmapCacheOption.OnLoad;
                    bmpImg.StreamSource = ms;
                    bmpImg.EndInit();
                    bmpImg.Freeze();

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

        // -----------------------------
        // Create Ribbon Button
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

            return button;
        }

        // -----------------------------
        // Create Ribbon Split Button
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

            // -----------------------------
            // Añadir botones
            // -----------------------------

            foreach (Autodesk.Windows.RibbonButton button in buttons)
            {
                splitButton.Items.Add(button);
            }

            return splitButton;
        }
    }
}