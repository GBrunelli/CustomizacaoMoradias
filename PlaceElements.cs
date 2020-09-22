using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Windows.Forms;
using System.IO;
using System.Globalization;

namespace CustomizacaoMoradias
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class PlaceElements : IExternalCommand
    {
        /*
         *  Convert from meters to feet.
         */
        private double MetersToFeet(double meters)
        {
            return UnitUtils.Convert(meters, DisplayUnitType.DUT_METERS, DisplayUnitType.DUT_DECIMAL_FEET);
        }

        /*
         *  Read a CSV file containing the definitions of the build, then starts and commits a transaction to the open document.
         */
        private Result readCSV(Document doc, UIDocument uidoc, Level level)
        {
            // Get the filename 
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV|*.csv";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                String path = openFileDialog.FileName;

                // Get a line from the table
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    // Split the line into strings
                    string[] columns = line.Split(',');
                    // Analyzes the line
                    switch (columns[0])
                    {
                        case "Parede":
                            CreateWall(columns, doc, level);
                            break;

                        case "Janela":
                            CreateHostedElement(columns, uidoc, doc, level);
                            break;

                        case "Porta":
                            CreateHostedElement(columns, uidoc, doc, level);
                            break;
                    }
                }
                return Result.Succeeded;
            }
            return Result.Failed;
        }

        /*
         *  Creates a wall given a array of string containg its properties.
         */
        private void CreateWall(string[] column, Document doc, Level level)
        {
            #region Reding the data from the array
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            XYZ p1 = new XYZ(MetersToFeet(Convert.ToDouble(column[1], provider)), MetersToFeet(Convert.ToDouble(column[2], provider)), 0);
            XYZ p2 = new XYZ(MetersToFeet(Convert.ToDouble(column[3], provider)), MetersToFeet(Convert.ToDouble(column[4], provider)), 0);
            Curve curve = Line.CreateBound(p1, p2);
            #endregion

            #region Creating the wall
            try
            {
                using (Transaction transaction = new Transaction(doc, "Place Wall"))
                {
                    transaction.Start();
                    Wall.Create(doc, curve, level.Id, false);
                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            #endregion
        }

        /*
         *  Returns the Wall in the XYZ coords. Returns null if no wall was found.
         */
        private Wall FindHostingWall(XYZ xyz, Document doc, Level level)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(Wall));
            List<Wall> walls = collector.Cast<Wall>().Where(wl => wl.LevelId == level.Id).ToList();
            Wall wall = null;
            double distance = double.MaxValue;
            foreach (Wall w in walls)
            {
                double proximity = (w.Location as LocationCurve).Curve.Distance(xyz);
                if (proximity < distance)
                {
                    distance = proximity;
                    wall = w;
                }
            }
            if(distance < 1)
            {
                return wall;
            }
            return null;
        }
      
        /*
         *  Creates a hosted element in a wall.
         */
        private void CreateHostedElement(string[] properties, UIDocument uidoc, Document doc, Level level)
        {
            #region Reding the data from the array
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            string xCoord = properties[1];
            string yCoord = properties[2];
            string fsName = properties[3];
            string fsFamilyName = properties[4];
            #endregion

            #region LINQ to find the window's FamilySymbol by its type name.
            FamilySymbol familySymbol = (from fs in new FilteredElementCollector(doc).
                 OfClass(typeof(FamilySymbol)).
                 Cast<FamilySymbol>()
                                         where (fs.Family.Name == fsFamilyName && fs.Name == fsName)
                                         select fs).First();
            #endregion

            #region Convert coordinates to double and create XYZ point.
            double x = MetersToFeet(Convert.ToDouble(xCoord, provider));
            double y = MetersToFeet(Convert.ToDouble(yCoord, provider));
            XYZ xyz = new XYZ(x, y, level.Elevation);
            #endregion

            #region Find the hosting Wall (nearst wall to the insertion point)
            Wall wall = FindHostingWall(xyz, doc, level);
            if (wall == null)
            {
                return;
            }
            #endregion

            #region Create the element
            using (Transaction transaction = new Transaction(doc, "Place " + properties[0]))
            {
                transaction.Start();

                if (!familySymbol.IsActive)
                {
                    // Ensure the family symbol is activated.
                    familySymbol.Activate();
                    doc.Regenerate();
                }

                // Create window
                FamilyInstance window = doc.Create.NewFamilyInstance(xyz, familySymbol, wall, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                if(properties[0] == "Janela") window.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).Set(MetersToFeet(2.00));
                transaction.Commit();
            }
            #endregion
        }

        /*
         *  Executes the command.
         */
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            Level level;

            #region Get the level
            try
            {
                level = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Levels)
                    .WhereElementIsNotElementType()
                    .Cast<Level>()
                    .First(x => x.Name == "PISO 1");
            }
            catch (Exception e) {
                message = e.Message;
                return Result.Failed;
            }
            #endregion

            return readCSV(doc, uidoc, level);
        }
    }
}
