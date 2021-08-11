using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CustomizacaoMoradias.Forms;
using System;
using System.Collections.Generic;
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

            string errorMessage = "";
            using (Transaction transaction = new Transaction(doc, "Contruir JSON"))
            {
                transaction.Start();
                try
                {
                    elementPlacer.BuildJSON(path);
                    elementPlacer.PlaceRoomSeparatorsInOpenWalls();
                }
                catch (Exception e)
                {
                    errorMessage += $"\nErro ao construir elementos do JSON: \"{e.Message}\"";
                }
                try
                {
                    elementPlacer.CreateFloor(Properties.Settings.Default.FloorName);
                }
                catch (Exception e)
                {
                    errorMessage += $"\nErro ao construir piso: \"{e.Message}\"";
                }
                try
                {
                    elementPlacer.CreateCeiling(Properties.Settings.Default.CeilingName);
                }
                catch (Exception e)
                {
                    errorMessage += $"\nErro ao construir laje: \"{e.Message}\"";
                }
                try
                {
                    elementPlacer.ClassifyRooms();
                }
                catch (Exception e)
                {
                    errorMessage += $"\nErro ao classificar ambientes: \"{e.Message}\"";
                }      
                transaction.Commit();
            }
            using (Transaction transaction = new Transaction(doc, "Contruir Telhado"))
            {
                transaction.Start();

                try
                {
                    elementPlacer.CreateRoof(overhang, slope, roofVector, roofDesign);
                }
                catch (Exception e)
                {
                    errorMessage += $"\nErro ao construir telhado: \"{e.Message}\"";
                }                
                transaction.Commit();
            }

            if (errorMessage.Length > 1)
                MessageBox.Show(errorMessage, "Erro!", MessageBoxButtons.OK, MessageBoxIcon.Error);

            PlaceElementsForm.CloseForm();
        }

        public string GetName()
        {
            return "User Input Handler";
        }
    }
}
