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
            application.CreateRibbonTab("My Commands");
            string path = Assembly.GetExecutingAssembly().Location;
            PushButtonData elementPlacerButtonData = new PushButtonData("ElementPlacerButton", "Place Elements", path, "CustomizacaoMoradias.ElementPlacerCommand");
            RibbonPanel elementPlacerRibbonPanel = application.CreateRibbonPanel("My Commands", "Commands");
            PushButton elementPlacerButton = elementPlacerRibbonPanel.AddItem(elementPlacerButtonData) as PushButton;
            elementPlacerButton.LargeImage = ImageSourceFromBitmap(Properties.Resources.floor_plan_32px);

            selectorForm = null;   // no dialog needed yet; the command will bring it
            thisApp = this;  // static access to this application instance
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
            finally { DeleteObject(handle); }
        }
    }
}
