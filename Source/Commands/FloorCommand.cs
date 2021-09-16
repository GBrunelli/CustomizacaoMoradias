using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CustomizacaoMoradias.Source.Builder;

namespace CustomizacaoMoradias.Source.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class FloorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            string baseLevel = Properties.Settings.Default.BaseLevelName;
            string topLevel = Properties.Settings.Default.TopLevelName;
            float scale = Properties.Settings.Default.Scale;
            HouseBuilder elementPlacer = new HouseBuilder(uidoc.Document, baseLevel, topLevel, scale);
            try
            {
                using (Transaction transaction = new Transaction(uidoc.Document, "Build Floor"))
                {
                    transaction.Start();
                    elementPlacer.CreateFloor(Properties.Settings.Default.FloorName);
                    transaction.Commit();
                }
                return Result.Succeeded;
            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }
        }
    }
}