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
                configForm = new ConfigForm();
                ConfigForm.form = configForm;
                configForm.Show();
            }
        }
    }
}
