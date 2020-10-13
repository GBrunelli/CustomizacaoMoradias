using Autodesk.Revit.UI;

namespace CustomizacaoMoradias
{
    public class PlaceElementsApplication : IExternalApplication
    {
        // class instance
        public static PlaceElementsApplication thisApp = null;

        // ModelessForm instance
        private PlaceElementsForm selectorForm;

        public Result OnShutdown(UIControlledApplication application)
        {
            if (selectorForm != null && selectorForm.Visible)
            {
                selectorForm.Close();
            }

            return Result.Succeeded;
        }

        public Result OnStartup(UIControlledApplication application)
        {
            selectorForm = null;   // no dialog needed yet; the command will bring it
            thisApp = this;  // static access to this application instance

            return Result.Succeeded;
        }

        //   The external command invokes this on the end-user's request
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
    }
}
