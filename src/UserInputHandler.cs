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
                Level level = PlaceElementsUtil.GetLevelFromName(levelName, doc);

                string topLevelName = PlaceElementsForm.topLevelName;
                Level topLevel = PlaceElementsUtil.GetLevelFromName(topLevelName, doc);

                PlaceElementsUtil.BuildCSV(path, doc, uidoc, level, topLevel);
 
                PlaceElementsUtil.CreateFloor(doc, level, "piso 10 cm - ceramico 40x40");

                PlaceElementsUtil.CreateCeiling(doc, level, topLevel, "laje 10 cm - branca");

                double offset = PlaceElementsUtil.MetersToFeet(0.6);
                XYZ offsetVector = new XYZ(0, 1, 0);
                PlaceElementsUtil.CreateRoof(doc, level, topLevel, offset, offsetVector);

                PlaceElementsUtil.ClassifyRooms(doc, level);

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
