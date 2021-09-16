using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CustomizacaoMoradias.Forms;

namespace CustomizacaoMoradias.Source.Commands
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
                configForm = new ConfigForm();
                ConfigForm.form = configForm;
                configForm.Show();
            }
        }
    }
}
