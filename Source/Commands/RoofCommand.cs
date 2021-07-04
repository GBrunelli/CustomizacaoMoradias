using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using CustomizacaoMoradias.Forms;
using System;

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
                PlaceElementsApplication.thisApp = new PlaceElementsApplication();
                PlaceElementsApplication.thisApp.ShowRoofSelectorForm();
                return Result.Succeeded;
            }
            catch(Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }
            
        }
    }
}