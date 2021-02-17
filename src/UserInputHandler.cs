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

                PlaceElementsUtil.ReadCSV(path, doc, uidoc, level, topLevel);
 
                PlaceElementsUtil.CreateRoomsAtLevel(level, topLevel, doc);
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
