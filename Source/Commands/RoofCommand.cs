using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CustomizacaoMoradias.Source.Commands
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
                PlaceElementsApplication.thisApp = new PlaceElementsApplication();
                PlaceElementsApplication.thisApp.ShowRoofSelectorForm();
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