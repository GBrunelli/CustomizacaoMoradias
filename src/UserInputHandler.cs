using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CustomizacaoMoradias
{
    public class UserInputHandler : IExternalEventHandler
    {
        public void Execute(UIApplication app)
        {
            UIDocument uidoc = app.ActiveUIDocument;
            Document doc = uidoc.Document;
            string path = PlaceElementsForm.filePath;
            string levelName = PlaceElementsForm.levelName;
            Level level = PlaceElementsUtil.GetLevelFromName(levelName, doc);
            PlaceElementsUtil.ReadCSV(path, doc, uidoc, level);
            PlaceElementsForm.CloseForm();
        }

        public string GetName()
        {
            return "External User Input Event";
        }
    }
}
