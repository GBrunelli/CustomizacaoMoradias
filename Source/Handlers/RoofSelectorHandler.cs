using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CustomizacaoMoradias.Forms;
using CustomizacaoMoradias.Source.Builder;

namespace CustomizacaoMoradias.Source.Handlers
{
    internal class RoofSelectorHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;

            string baseLevel = Properties.Settings.Default.BaseLevelName;
            string topLevel = Properties.Settings.Default.TopLevelName;
            float scale = Properties.Settings.Default.Scale;
            double overhang = UnitUtils.ConvertToInternalUnits(Properties.Settings.Default.Overhang, UnitTypeId.Meters); ;

            XYZ slopeVector = BuildRoofForm.SlopeVector;
            RoofDesign roofDesign = BuildRoofForm.RoofDesign;
            double slope = RoofSelector.GetSlopeByType(roofDesign);

            HouseBuilder elementPlacer = new HouseBuilder(doc, baseLevel, topLevel, scale);
            try
            {
                using (Transaction transaction = new Transaction(doc, "Roof Command"))
                {
                    transaction.Start();
                    elementPlacer.CreateRoof(overhang, slope, slopeVector, roofDesign);
                    transaction.Commit();
                }
            }
            catch { }
        }

        public string GetName()
        {
            return "Roof Selector Handler";
        }
    }
}
