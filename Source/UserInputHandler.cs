using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

                string path = PlaceElementsForm.filePath;
                string levelName = PlaceElementsForm.levelName;
                string topLevelName = PlaceElementsForm.topLevelName;
                XYZ roofVector = GetXYZFromString(PlaceElementsForm.roofType);

                ElementPlacer elementPlacer = new ElementPlacer(uidoc, levelName, topLevelName, 0.3);

                elementPlacer.BuildJSON(path);
                elementPlacer.CreateFloor(Properties.Settings.Default.FloorName);
                elementPlacer.CreateCeiling(Properties.Settings.Default.CeilingName);

                double offset = ElementPlacer.MetersToFeet(0.6);

                if(roofVector == null)
                    roofVector = new XYZ(0, 0, 0);

                elementPlacer.CreateRoof(offset, 0.3, roofVector);
                elementPlacer.ClassifyRooms();
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

        private XYZ GetXYZFromString(string s)
        {
            try
            {
                XYZ coordinate = null;
                string[] splitedString = Regex.Split(s, @"\D+");

                int j = 0;
                int[] numbers = new int[3];
                for (int i = 0; i < splitedString.Length; i++)
                {
                    bool isValid = int.TryParse(splitedString[i], out int currentNumber);
                    if (isValid)
                        numbers[j++] = currentNumber;
                }
                coordinate = new XYZ(numbers[0], numbers[1], numbers[2]);
                return coordinate;
            }
            catch(Exception)
            {
                return null;
            }
        }
    }
}
