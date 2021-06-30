using System;
using System.Reflection;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Interop;
using System.Windows;

namespace CustomizacaoMoradias
{
    public class PlaceElementsApplication : IExternalApplication
    {
        // class instance
        public static PlaceElementsApplication thisApp = null;

        // ModelessForm instance
        private PlaceElementsForm selectorForm;

        public Result OnStartup(UIControlledApplication application)
        {
            // Creates the tab
            application.CreateRibbonTab("Customização de Moradias");
            string path = Assembly.GetExecutingAssembly().Location;

            #region Main pannel

            // Creates the main panel
            RibbonPanel elementPlacerRibbonPanel = application.CreateRibbonPanel("Customização de Moradias", "Geração Completa");

            // Main button
            PushButtonData elementPlacerButtonData = new PushButtonData("ElementPlacerButton", "Contruir JSON", path, "CustomizacaoMoradias.ElementPlacerCommand");
            PushButton elementPlacerButton = elementPlacerRibbonPanel.AddItem(elementPlacerButtonData) as PushButton;
            elementPlacerButton.LargeImage = ImageSourceFromBitmap(Properties.Resources.floor_plan_32px);

            // Config button
            PushButtonData configButtonData = new PushButtonData("configButton", "Opções", path, "CustomizacaoMoradias.ConfigCommand");
            PushButton configButton = elementPlacerRibbonPanel.AddItem(configButtonData) as PushButton;
            configButton.LargeImage = ImageSourceFromBitmap(Properties.Resources.config);

            #endregion

            #region Standalone pannel

            // Creates the standalone panel
            RibbonPanel standaloneRibbonPanel = application.CreateRibbonPanel("Customização de Moradias", "Funções");
            
            // Floor button
            PushButtonData floorButtonData = new PushButtonData("floorButton", "Criar Piso", path, "CustomizacaoMoradias.FloorCommand");
            PushButton floorButton = standaloneRibbonPanel.AddItem(floorButtonData) as PushButton;
            floorButton.LargeImage = ImageSourceFromBitmap(Properties.Resources.parquet);

            // Ceiling button
            PushButtonData ceilingButtonData = new PushButtonData("ceilingButton", "Criar Laje", path, "CustomizacaoMoradias.CeilingCommand");
            PushButton ceilingButton = standaloneRibbonPanel.AddItem(ceilingButtonData) as PushButton;
            ceilingButton.LargeImage = ImageSourceFromBitmap(Properties.Resources.ceiling);

            // Roof button
            PushButtonData roofButtonData = new PushButtonData("roofButton", "Criar Telhado", path, "CustomizacaoMoradias.RoofCommand");
            PushButton roofButton = standaloneRibbonPanel.AddItem(roofButtonData) as PushButton;
            roofButton.LargeImage = ImageSourceFromBitmap(Properties.Resources.roof);

            // Rooms classification button
            PushButtonData roomButtonData = new PushButtonData("roomButton", "Classificar Ambientes", path, "CustomizacaoMoradias.ClassifyRoomsCommand");
            PushButton roomButton = standaloneRibbonPanel.AddItem(roomButtonData) as PushButton;
            roomButton.LargeImage = ImageSourceFromBitmap(Properties.Resources.blueprint);

            #endregion

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            if (selectorForm != null && selectorForm.Visible)
            {
                selectorForm.Close();
            }
            return Result.Succeeded;
        }       

        // The external command invokes this on the end-user's request
        public void ShowForm(UIApplication uiapp)
        {
            // If we do not have a dialog yet, create and show it
            if (selectorForm == null || selectorForm.IsDisposed)
            {
                // A new handler to handle request posting by the dialog
                UserInputHandler handler = new UserInputHandler();

                // External Event for the dialog to use (to post requests)
                ExternalEvent exEvent = ExternalEvent.Create(handler);

                // We give the objects to the new dialog;
                // The dialog becomes the owner responsible for disposing them, eventually.
                selectorForm = new PlaceElementsForm(exEvent, handler);
                PlaceElementsForm.form = selectorForm;
                selectorForm.Show();
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);
        public static ImageSource ImageSourceFromBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally 
            { 
                DeleteObject(handle); 
            }
        }
    }
}
