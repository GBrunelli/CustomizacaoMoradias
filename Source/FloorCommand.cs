using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace CustomizacaoMoradias
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class FloorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                ElementPlacer elementPlacer = new ElementPlacer(uidoc, "PLANTA BAIXA", "COBERTURA", 0.3);
                using (Transaction transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, ""))
                {
                    transaction.Start();
                    elementPlacer.CreateFloor("piso 10 cm - ceramico 40x40");
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