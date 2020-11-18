using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Autodesk.Revit.DB.Architecture;

namespace CustomizacaoMoradias
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public static class PlaceElementsUtil
    {
        /// <summary>
        /// Convert from meters to feet.
        /// </summary>
        private static double MetersToFeet(double meters)
        {
            return UnitUtils.Convert(meters, UnitTypeId.Meters, UnitTypeId.Feet);
        }

        /// <summary>
        /// Opens the Explorer to the user select a file, returns the file path.
        /// </summary>
        public static string SelectFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "CSV|*.csv"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK) return openFileDialog.FileName;
            return null;
        }

        /// <summary>
        /// Read a CSV file containing the definitions of the build, then starts and commits a transaction to the open document.
        /// </summary>
        public static Result ReadCSV(string path, Document doc, UIDocument uidoc, Level level)
        {
            #region Null parameters test 
            if (path is null) throw new ArgumentNullException(nameof(path));

            if (doc is null) throw new ArgumentNullException(nameof(doc));

            if (uidoc is null) throw new ArgumentNullException(nameof(uidoc));

            if (level is null) throw new ArgumentNullException(nameof(level));
            #endregion

            if (path != null)
            {
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

        /// <summary>
        /// Creates a wall given a array of string containg its properties.
        /// </summary>
        private static void CreateWall(string[] column, Document doc, Level level)
        {
            #region Null parameters test 
            if (column is null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            if (doc is null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            if (level is null)
            {
                throw new ArgumentNullException(nameof(level));
            }
            #endregion

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
                    Wall newWall = Wall.Create(doc, curve, level.Id, false);
                    Level height = PlaceElementsUtil.GetLevelFromName("COBERTURA", doc);
                    newWall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(height.Id);
                    newWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(MetersToFeet(-0.15));
                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            #endregion
        }

        /// <summary>
        /// Returns the Wall in the XYZ coords. Returns null if no wall was founded.
        /// </summary>
        private static Wall FindHostingWall(XYZ xyz, Document doc, Level level)
        {
            #region Null parameters test 
            if (xyz is null) throw new ArgumentNullException(nameof(xyz));

            if (doc is null) throw new ArgumentNullException(nameof(doc));

            if (level is null) throw new ArgumentNullException(nameof(level));
            #endregion

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
            if (distance < 1)
            {
                return wall;
            }
            return null;
        }

        /// <summary>
        /// Create a hosted element in a wall.
        /// </summary>
        private static void CreateHostedElement(string[] properties, UIDocument uidoc, Document doc, Level level)
        {
            #region Null parameters test 
            if (properties is null) throw new ArgumentNullException(nameof(properties));

            if (uidoc is null) throw new ArgumentNullException(nameof(uidoc));

            if (doc is null) throw new ArgumentNullException(nameof(doc));

            if (level is null) throw new ArgumentNullException(nameof(level));
            #endregion

            #region Reding the data from the array
            NumberFormatInfo provider = new NumberFormatInfo
            {
                NumberDecimalSeparator = "."
            };
            string xCoord = properties[1];
            string yCoord = properties[2];
            string fsName = properties[3];
            string fsFamilyName = properties[4];
            #endregion

            try
            {
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
                if (wall == null) return;
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
                    if (properties[0] == "Janela") window.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).Set(MetersToFeet(2.00));
                    transaction.Commit();
                }
                #endregion
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Returns a Level from its name.
        /// </summary>
        public static Level GetLevelFromName(string levelName, Document doc)
        {
            Level level;
            try
            {
                level = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Levels)
                    .WhereElementIsNotElementType()
                    .Cast<Level>()
                    .First(x => x.Name == levelName);
            }
            catch (Exception e)
            {
                throw e;
            }
            return level;
        }
    
        /// <summary>
        /// Creates rooms in a level.
        /// </summary>
        public static void CreateRoomsAtLevel(Level level, Document doc)
        {
            PhaseArray phases = doc.Phases;
            Phase createRoomsInPhase = phases.get_Item(phases.Size - 1);
            int x = 0;
            try
            {
                using (Transaction transaction = new Transaction(doc, "Create Rooms"))
                {
                    PlanTopology topology = doc.get_PlanTopology(level, createRoomsInPhase);
                    PlanCircuitSet circuitSet = topology.Circuits;
                    transaction.Start();
                    foreach (PlanCircuit circuit in circuitSet)
                    {
                        if (!circuit.IsRoomLocated)
                        {
                            Room room = doc.Create.NewRoom(null, circuit);
                            room.Name = "Room name: " + x;

                            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
                            opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;
                            IList<IList<BoundarySegment>> loops = room.GetBoundarySegments(opt);

                            // creates a floor if in a single room there is only one loop
                            if(loops.Count == 1)
                            {
                                CurveArray curve = new CurveArray();
                                foreach (IList<BoundarySegment> loop in loops)
                                {
                                    foreach (BoundarySegment seg in loop)
                                    {
                                        curve.Append(seg.GetCurve());
                                    }
                                    doc.Create.NewFloor(curve, false);
                                }
                            }
                            // creates a ceiling if in a room there is more than one loop,
                            // and finds the smallest loop
                            else
                            {
                                double minArea = double.MaxValue;
                                IList<BoundarySegment> ceilingLoop = null;
                                foreach (IList<BoundarySegment> loop in loops)
                                {
                                    double area = 0;
                                    CurveLoop currentCurve = new CurveLoop();
                                    foreach (BoundarySegment seg in loop)
                                        currentCurve.Append(seg.GetCurve());
                                    IList<CurveLoop> curveLoopList = new List<CurveLoop>();
                                    curveLoopList.Add(currentCurve);
                                    area = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoopList);

                                    if (area < minArea)
                                    {
                                        minArea = area;
                                        ceilingLoop = loop;
                                    }
                                }

                                CurveArray curve = new CurveArray();
                                foreach (BoundarySegment seg in ceilingLoop)
                                {
                                    curve.Append(seg.GetCurve());
                                }

                                // create a floor type
                                FilteredElementCollector collector = new FilteredElementCollector(doc);
                                collector.OfClass(typeof(FloorType));
                                FloorType floorType = collector.First(y => y.Name == "20cm concreto") as FloorType;

                                // create the ceiling
                                Floor ceiling = doc.Create.NewFloor(curve, floorType, level, false);
                                ElementTransformUtils.MoveElement(doc, ceiling.Id, new XYZ(0, 0, MetersToFeet(2.8)));
                            }
                            x++;
                        }
                    }
                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
