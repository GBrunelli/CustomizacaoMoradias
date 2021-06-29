using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace CustomizacaoMoradias
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class RoofCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                ElementPlacer elementPlacer = new ElementPlacer(uidoc.Document, "PLANTA BAIXA", "COBERTURA", 0.3);

                double offset = ElementPlacer.MetersToFeet(0.6);
                using (Transaction transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "Roof Command"))
                {
                    transaction.Start();
                    elementPlacer.CreateRoof(offset, 0.3, new XYZ(1, 0, 0), ElementPlacer.RoofDesign.Gable);
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