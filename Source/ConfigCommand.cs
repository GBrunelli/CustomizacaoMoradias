using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using CustomizacaoMoradias.Forms;

namespace CustomizacaoMoradias
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class ConfigCommand : IExternalCommand
    {

        private ConfigForm configForm;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            ShowForm();

            return Result.Succeeded;
        }

        private void ShowForm()
        {
            if (configForm == null || configForm.IsDisposed)
            {
                // A new handler to handle request posting by the dialog
                UserInputHandler handler = new UserInputHandler();

                // External Event for the dialog to use (to post requests)
                ExternalEvent exEvent = ExternalEvent.Create(handler);

                // We give the objects to the new dialog;
                // The dialog becomes the owner responsible for disposing them, eventually.
                configForm = new ConfigForm();
                ConfigForm.form = configForm;
                configForm.Show();
            }
        }
    }
}
