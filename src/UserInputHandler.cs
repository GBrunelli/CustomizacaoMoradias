using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Windows.Forms;

namespace CustomizacaoMoradias
{
    public class UserInputHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            try
            {
                UIDocument uidoc = app.ActiveUIDocument;
                Document doc = uidoc.Document;

                string path = PlaceElementsForm.filePath;

                string levelName = PlaceElementsForm.levelName;

                string topLevelName = PlaceElementsForm.topLevelName;

                ElementPlacer elementPlacer = new ElementPlacer(uidoc, levelName, topLevelName, 0.3);

                elementPlacer.BuildCSV(path);
 
                elementPlacer.CreateFloor("piso 10 cm - ceramico 40x40");

                elementPlacer.CreateCeiling("laje 10 cm - branca");

                double offset = ElementPlacer.MetersToFeet(0.6);
                XYZ offsetVector = new XYZ(0, 1, 0);
                elementPlacer.CreateRoof(offset, offsetVector);

                elementPlacer.ClassifyRooms();

                //PlaceElementsUtil.CreateNewSheet(doc);
            }
            catch(LevelNotFoundException lvlEx)
            {
                MessageBox.Show(lvlEx.Message, "Erro");
                throw lvlEx;

            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "Erro");
            }

            PlaceElementsForm.CloseForm();
        }

        public string GetName()
        {
            return "External User Input Event";
        }
    }
}
