using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CustomizacaoMoradias.Forms;
using CustomizacaoMoradias.Source.Builder;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace CustomizacaoMoradias.Source.Handlers
{
    public class UserInputHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            UIDocument uidoc    = app.ActiveUIDocument;
            Document doc        = uidoc.Document;

            float scale = 0;
            double overhang = 0;
            string levelName = "", topLevelName = "";

            try
            {
                scale = Properties.Settings.Default.Scale;
                overhang = UnitUtils.ConvertToInternalUnits(Properties.Settings.Default.Overhang, UnitTypeId.Meters);
                levelName = Properties.Settings.Default.BaseLevelName;
                topLevelName = Properties.Settings.Default.TopLevelName;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            string path         = PlaceElementsForm.filePath;
            XYZ roofVector      = PlaceElementsForm.roofSelector.SlopeVector;
            var roofDesign      = PlaceElementsForm.roofSelector.RoofStyle;
            double slope        = RoofSelector.GetSlopeByType(roofDesign);

            HouseBuilder builder = new HouseBuilder(doc, levelName, topLevelName, scale);

            string errorMessage = "";
            using (Transaction transaction = new Transaction(doc, "Contruir JSON"))
            {
                transaction.Start();
                try
                {
                    builder.BuildJSON(path);
                }
                catch (Exception e)
                {
                    errorMessage += $"\nErro ao construir elementos do JSON: \"{e.Message}\"";
                }
                try
                {
                    builder.CreateFloor(Properties.Settings.Default.FloorName);
                }
                catch (Exception e)
                {
                    errorMessage += $"\nErro ao construir piso: \"{e.Message}\"";
                }
                try
                {
                    builder.CreateCeiling(Properties.Settings.Default.CeilingName);
                }
                catch (Exception e)
                {
                    errorMessage += $"\nErro ao construir laje: \"{e.Message}\"";
                }
                try
                {
                    builder.ClassifyRooms();
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
                    builder.CreateRoof(overhang, slope, roofVector, roofDesign);
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
