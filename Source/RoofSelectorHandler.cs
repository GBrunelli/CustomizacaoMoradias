using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using CustomizacaoMoradias.Forms;

namespace CustomizacaoMoradias.Source
{
    class RoofSelectorHandler : IExternalEventHandler
    {

        public ElementPlacer elementPlacer = new ElementPlacer();

        public void Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc     = uidoc.Document;
                             
            var baseLevel    = Properties.Settings.Default.BaseLevelName;
            var topLevel     = Properties.Settings.Default.TopLevelName;
            var scale        = Properties.Settings.Default.Scale;
            double overhang  = Properties.Settings.Default.Overhang;
                             
            var slopeVector  = BuildRoofForm.SlopeVector;
            var roofDesign   = BuildRoofForm.RoofDesign;
            double slope     = RoofSelector.GetSlopeByType(roofDesign);

            elementPlacer.SetProperties(uidoc, baseLevel, topLevel, scale);
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
