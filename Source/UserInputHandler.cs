using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CustomizacaoMoradias.Forms;
using System;
using System.Windows.Forms;

namespace CustomizacaoMoradias
{
    public class UserInputHandler : IExternalEventHandler
    {
        public ElementPlacer elementPlacer = new ElementPlacer();

        public void Execute(UIApplication app)
        {
            UIDocument uidoc    = app.ActiveUIDocument;
            Document doc        = uidoc.Document;

            float scale         = Properties.Settings.Default.Scale;
            double overhang     = UnitUtils.ConvertToInternalUnits(Properties.Settings.Default.Overhang, UnitTypeId.Meters);
            string levelName    = Properties.Settings.Default.BaseLevelName;
            string topLevelName = Properties.Settings.Default.TopLevelName;

            string path         = PlaceElementsForm.filePath;
            XYZ roofVector      = PlaceElementsForm.roofSelector.SlopeVector;
            var roofDesign      = PlaceElementsForm.roofSelector.RoofStyle;
            double slope        = RoofSelector.GetSlopeByType(roofDesign);

            elementPlacer.SetProperties(uidoc, levelName, topLevelName, scale);

            try
            {
                using (Transaction transaction = new Transaction(doc, "Contruir JSON"))
                {
                    transaction.Start();
                    elementPlacer.BuildJSON(path);
                    elementPlacer.ClassifyRooms();
                    elementPlacer.CreateFloor(Properties.Settings.Default.FloorName);
                    elementPlacer.CreateCeiling(Properties.Settings.Default.CeilingName);
                    transaction.Commit();
                }
                using (Transaction transaction = new Transaction(doc, "Contruir Telhado"))
                {
                    transaction.Start();
                    elementPlacer.CreateRoof(overhang, slope, roofVector, roofDesign);
                    transaction.Commit();
                }
            }
            catch (LevelNotFoundException lvlEx)
            {
                MessageBox.Show(lvlEx.Message, "Erro");
                throw lvlEx;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Erro");
            }

            PlaceElementsForm.CloseForm();
        }

        public string GetName()
        {
            return "User Input Handler";
        }
    }
}
