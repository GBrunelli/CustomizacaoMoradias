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
            UIDocument uidoc = app.ActiveUIDocument;
            using (Transaction transaction = new Transaction(uidoc.Document, "Contruir JSON"))
            {
                transaction.Start();
                try
                {                
                    string path = PlaceElementsForm.filePath;
                    string levelName = PlaceElementsForm.levelName;
                    string topLevelName = PlaceElementsForm.topLevelName;
                    XYZ roofVector = GetXYZFromString(PlaceElementsForm.roofType);
                    ElementPlacer.RoofDesign roofDesign = GetRoofDesignFromString(PlaceElementsForm.roofStyle);

                    ElementPlacer elementPlacer = new ElementPlacer(uidoc, levelName, topLevelName, 0.3);

                    elementPlacer.BuildJSON(path);                  
                    elementPlacer.CreateFloor(Properties.Settings.Default.FloorName);
                    elementPlacer.CreateCeiling(Properties.Settings.Default.CeilingName);
                    elementPlacer.ClassifyRooms();

                    double offset = ElementPlacer.MetersToFeet(0.6);
                    double slope = GetSlopeByType(roofDesign);                       
                    elementPlacer.CreateRoof(offset, slope, roofVector, roofDesign);                 
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

                transaction.Commit();
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
        
        private ElementPlacer.RoofDesign GetRoofDesignFromString(string s)
        {
            switch (s)
            {
                case "Hip Roof":
                    return ElementPlacer.RoofDesign.Hip;
                case "Gable Roof":
                    return ElementPlacer.RoofDesign.Gable;
                case "Hidden Roof":
                    return ElementPlacer.RoofDesign.HiddenButterfly;
            }
            return ElementPlacer.RoofDesign.Gable;
        }

        private double GetSlopeByType(ElementPlacer.RoofDesign roofDesign)
        {
            switch (roofDesign)
            {
                case ElementPlacer.RoofDesign.Gable:
                    return 0.3;
                case ElementPlacer.RoofDesign.Hip:
                    return 0.3;
                case ElementPlacer.RoofDesign.HiddenButterfly:
                    return 0.05;
            }
            return 0.3;
        }
    }
}
