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
        public static Result ReadCSV(string path, Document doc, UIDocument uidoc, Level level, Level topLevel)
        {
            #region Null parameters test 
            if (path is null) throw new ArgumentNullException(nameof(path));

            if (doc is null) throw new ArgumentNullException(nameof(doc));

            if (uidoc is null) throw new ArgumentNullException(nameof(uidoc));

            if (level is null) throw new ArgumentNullException(nameof(level));
            #endregion

            double scale = 0.3;

            if (path != null)
            {
                // Get a line from the table
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    // Split the line into strings
                    string[] columns = line.Split(',');

                    // Analyzes the line
                    try
                    {
                        switch (columns[0])
                        {
                            case "Parede":
                                CreateWall(columns, doc, level, topLevel, scale);
                                break;

                            case "Janela":
                                CreateHostedElement(columns, uidoc, doc, level, scale);
                                break;

                            case "Porta":
                                CreateHostedElement(columns, uidoc, doc, level, scale);
                                break;

                            case "Mobiliario":
                                CreateFurniture(columns, doc, level, scale);
                                break;
                        }
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show(e.Message, "Erro");
                    }
                }
                return Result.Succeeded;
            }
            return Result.Failed;
        }

        /// <summary>
        /// Creates a piece of furniture
        /// </summary>
        private static void CreateFurniture(string[] properties, Document doc, Level level, double scale)
        {

            NumberFormatInfo provider = new NumberFormatInfo
            {
                NumberDecimalSeparator = "."
            };
            string xCoord = properties[1];
            string yCoord = properties[2];
            string rotation = properties[3];
            string fsName = properties[4];
            string fsFamilyName = properties[5];

            // Get the rotation in radians
            double radians = 0;
            switch (rotation)
            {
                case "DIREITA":
                    radians = 0.5;
                    break;

                case "BAIXO":
                    radians = 1;
                    break;

                case "ESQUERDA":
                    radians = 1.5;
                    break;
            }
            radians *= Math.PI;

            // Convert the values from the csv file
            double x = MetersToFeet(Convert.ToDouble(xCoord, provider) * scale);
            double y = MetersToFeet(Convert.ToDouble(yCoord, provider) * scale);

            // Creates the point where the piece of furniture will be inserted
            XYZ point = new XYZ(x, y, level.Elevation);

            // Creates a point above the furniture to serve as a rotation axis
            XYZ axisPoint = new XYZ(x, y, level.Elevation + 1);
            Line axis = Line.CreateBound(point, axisPoint);

            try
            {
                // Retrieve the familySymbol of the piece of furniture
                FamilySymbol familySymbol = (from fs in new FilteredElementCollector(doc).
                    OfClass(typeof(FamilySymbol)).
                    Cast<FamilySymbol>()
                    where (fs.Family.Name == fsFamilyName)
                    select fs).First();

                using (Transaction transaction = new Transaction(doc, "Place Piece of Furniture"))
                {
                    transaction.Start();

                    FamilyInstance furniture = doc.Create.NewFamilyInstance(point, familySymbol, 
                        Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    
                    ElementTransformUtils.RotateElement(doc, furniture.Id, axis, radians);

                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Erro ao inserir mobiliario \"" + fsFamilyName + "\".", e);
            }
        }

        /// <summary>
        /// Creates a wall given a array of string containg its properties.
        /// </summary>
        private static void CreateWall(string[] properties, Document doc, Level level, Level topLevel, double scale)
        {
            #region Null parameters test 
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            if (doc is null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            if (level is null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if(topLevel is null)
            {
                throw new ArgumentNullException(nameof(topLevel));
            }
            #endregion

            #region Reding the data from the array
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";

            XYZ p1 = new XYZ(MetersToFeet(Convert.ToDouble(properties[1], provider)) * scale, // x
                             MetersToFeet(Convert.ToDouble(properties[2], provider)) * scale, // y
                             level.Elevation);                                                // z

            XYZ p2 = new XYZ(MetersToFeet(Convert.ToDouble(properties[3], provider)) * scale, // x
                             MetersToFeet(Convert.ToDouble(properties[4], provider)) * scale, // y
                             level.Elevation);                                                // z

            Curve curve = Line.CreateBound(p1, p2);
            #endregion

            #region Creating the wall
            try
            {
                using (Transaction transaction = new Transaction(doc, "Place Wall"))
                {
                    transaction.Start();
                    Wall newWall = Wall.Create(doc, curve, level.Id, false);
                    newWall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(topLevel.Id);
                    newWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(MetersToFeet(-0.15));
                    transaction.Commit();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Erro ao inserir parede de coodenadas: (" + p1 + ", " + p2 + ").", e);
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
        private static void CreateHostedElement(string[] properties, UIDocument uidoc, Document doc, Level level, double scale)
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
                double x = MetersToFeet(Convert.ToDouble(xCoord, provider) * scale);
                double y = MetersToFeet(Convert.ToDouble(yCoord, provider) * scale);
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
                    FamilyInstance instance = doc.Create.NewFamilyInstance(xyz, familySymbol, wall, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    if (properties[0] == "Janela") instance.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).Set(MetersToFeet(2.00));

                    

                    transaction.Commit();
                }
                #endregion
            }
            catch (Exception e)
            {
                throw new Exception("Erro ao inserir elemento hospedeiro \"" + fsFamilyName + "\".", e);
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
                throw new Exception("Nível \"" + levelName + "\" não encontrado.", e);
            }
            return level;
        }

        /// <summary>
        /// Get all the plan circuits of an level.
        /// </summary>
        public static PlanCircuitSet getDocPlanCircuitSet(Document doc, Level level)
        {
            PhaseArray phases = doc.Phases;

            // get the last phase
            Phase createRoomsInPhase = phases.get_Item(phases.Size - 1);

            if (createRoomsInPhase is null)
                throw new Exception("Não foi encontrada nenhuma fase no documento atual.");

            PlanTopology topology = doc.get_PlanTopology(level, createRoomsInPhase);
            PlanCircuitSet circuitSet = topology.Circuits;

            return circuitSet;
        }

        /// <summary>
        /// Returns the loops in a circuit, if there is a room located in that circuit, returns null.
        /// </summary>
        public static IList<IList<BoundarySegment>> GetLoopsInCircuit(Document doc, PlanCircuit circuit)
        {
            Room room;
            IList<IList<BoundarySegment>> loops = null;

            using (Transaction transaction = new Transaction(doc, "Create room"))
            {
                if (circuit.IsRoomLocated)
                {
                    UV point2D = circuit.GetPointInside();
                    XYZ point = new XYZ(point2D.U, point2D.V, 0);
                    room = doc.GetRoomAtPoint(point);
                }
                else
                {
                    transaction.Start();

                    room = doc.Create.NewRoom(null, circuit);

                    #region Elevation Mark TEST

                    /* TEST 1
                    ViewFamilyType viewFamilyType = null;

                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    ICollection<Element> views = collector.OfClass(typeof(ViewFamilyType)).ToElements();                   

                    foreach(Element element in views)
                    {
                        if (element.Name == "Floor Plan")
                        {
                            viewFamilyType = element as ViewFamilyType;
                        }
                    }

                    BoundingBoxXYZ roomBoundingBox = room.get_BoundingBox(null);
                    XYZ center = (roomBoundingBox.Max + roomBoundingBox.Min) / 2;
                    
                    */

                    /* TEST 2

                    ViewFamilyType viewFamilyType;
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    collector.OfClass(typeof(ViewFamilyType));
                    List<ViewFamilyType> viewFamilyTypes = collector.Cast<ViewFamilyType>().Where(view => view.Name == "Plantas de piso").ToList();
                    viewFamilyType = viewFamilyTypes.First();

                    BoundingBoxXYZ roomBoundingBox = room.get_BoundingBox(null);
                    XYZ center = (roomBoundingBox.Max + roomBoundingBox.Min) / 2;

                    */


                    // ElevationMarker marker = ElevationMarker.CreateElevationMarker(doc, viewFamilyType.Id, center, 2);

                    #endregion

                    // TODO: ROOM NAME

                    transaction.Commit();
                }

                SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions();
                opt.SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center;
                loops = room.GetBoundarySegments(opt);

            }

            return loops;
        }

        /// <summary>
        /// Create a floor given the Boundary Segments of the document
        /// </summary>
        public static Floor CreateFloorInLoop(Document doc, Level level)
        {
            Floor floor = null;

            PlanCircuitSet circuitSet = getDocPlanCircuitSet(doc, level);

            foreach (PlanCircuit circuit in circuitSet)
            {
                IList<IList<BoundarySegment>> loops = GetLoopsInCircuit(doc, circuit);

                if(loops != null)
                {
                    using (Transaction transaction = new Transaction(doc, "Create Floor"))
                    {
                        transaction.Start();

                        // creates a floor if in a single room there is only one loop
                        if (loops.Count == 1)
                        {
                            CurveArray curve = new CurveArray();
                            foreach (IList<BoundarySegment> loop in loops)
                            {
                                foreach (BoundarySegment seg in loop)
                                {
                                    curve.Append(seg.GetCurve());
                                }
                                floor = doc.Create.NewFloor(curve, false);
                            }
                        }
                        transaction.Commit();
                    }
                }
            }
            return floor;
        }

        /// <summary>
        /// Returns a CurveArray that corresponds to the house perimeter
        /// </summary>
        public static CurveArray GetHousePerimeterCurveArray(Document doc, Level level, IList<IList<BoundarySegment>> loops, bool offset)
        {
            double minArea = double.MaxValue;
            IList<BoundarySegment> ceilingLoop = null;
            foreach (IList<BoundarySegment> loop in loops)
            {
                double area = 0;
                CurveLoop currentCurve = new CurveLoop();

                foreach (BoundarySegment seg in loop)
                {
                    currentCurve.Append(seg.GetCurve());
                }                             

                IList<CurveLoop> curveLoopList = new List<CurveLoop>();
                curveLoopList.Add(currentCurve);
                area = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoopList);

                if (area < minArea)
                {
                    minArea = area;
                    ceilingLoop = loop;
                }
            }

            CurveArray housePerimeter = new CurveArray();
            foreach (BoundarySegment seg in ceilingLoop)
            {
                Curve curve = seg.GetCurve();

                if(offset)
                {
                    // finds the middle point of the current edge of the roof
                    XYZ middlePoint = GetCurveMiddlePoint(curve);

                    // subtracts de Z value of the point
                    middlePoint = middlePoint.Subtract(new XYZ(0, 0, middlePoint.Z));

                    // finds the wall below that edge
                    Wall wall = FindHostingWall(middlePoint, doc, level);

                    XYZ wallNormalVector = wall.Orientation;

                    curve = curve.CreateOffset(MetersToFeet(0.6), wallNormalVector);
                }
               

                housePerimeter.Append(curve);
            }

            return housePerimeter;
        }

        public static CurveArray CurveLoopToCurveArray(CurveLoop loop)
        {
            CurveArray array = new CurveArray();
            foreach(Curve curve in loop)
            {
                array.Append(curve);
            }
            return array;
        }

        public static CurveLoop CurveArrayToCurveLoop(CurveArray array)
        {
            CurveLoop loop = new CurveLoop();
            foreach (Curve curve in array)
            {
                loop.Append(curve);
            }
            return loop;
        }

        /// <summary>
        /// Create the ceiling of a house given the loops of the active document
        /// </summary>
        public static Floor CreateCeilingInLoop(Document doc, Level level, Level topLevel)
        {
            Floor ceiling = null;
            PlanCircuitSet circuitSet = getDocPlanCircuitSet(doc, level);

            foreach (PlanCircuit circuit in circuitSet)
            {

                IList<IList<BoundarySegment>> loops = GetLoopsInCircuit(doc, circuit);

                if(loops != null)
                {
                    using (Transaction transaction = new Transaction(doc, "Create Ceiling"))
                    {
                        transaction.Start();

                        // creates a ceiling if in a room there is more than one loop,
                        // and finds the smallest loop
                        if (loops.Count > 1)
                        {

                            CurveArray curve = GetHousePerimeterCurveArray(doc, level, loops, false);

                            // create a floor type

                            //
                            // TIRAR DAQUI
                            //
                            FilteredElementCollector collector = new FilteredElementCollector(doc);
                            collector.OfClass(typeof(FloorType));
                            FloorType floorType = collector.First(y => y.Name == "10cm concreto  SEM ACAB") as FloorType;

                            // create the ceiling
                            ceiling = doc.Create.NewFloor(curve, floorType, topLevel, false);
                            ElementTransformUtils.MoveElement(doc, ceiling.Id, new XYZ(0, 0, topLevel.Elevation));
                        }
                        transaction.Commit();
                    }
                }
            }              
            return ceiling;
        }

        /// <summary>
        /// Returns the middle point of a Model Curve
        /// </summary>
        private static XYZ GetCurveMiddlePoint(Curve curve)
        {
            if (curve is null) throw new ArgumentNullException(nameof(curve));

            XYZ curveStartPoint = curve.GetEndPoint(0);
            XYZ curveEndPoint = curve.GetEndPoint(1);

            double cordX, cordY, cordZ;

            cordX = (curveStartPoint.X + curveEndPoint.X) / 2;
            cordY = (curveStartPoint.Y + curveEndPoint.Y) / 2;
            cordZ = (curveStartPoint.Z + curveEndPoint.Z) / 2;

            return new XYZ(cordX, cordY, cordZ);
        }

        /// <summary>
        /// Create the roof of a house given the loops of the active document
        /// </summary>
        public static FootPrintRoof CreateRoofInLoop(Document doc, Level level, Level topLevel)
        {
            FootPrintRoof footPrintRoof = null;
            PlanCircuitSet circuitSet = getDocPlanCircuitSet(doc, level);

            foreach (PlanCircuit circuit in circuitSet)
            {
                IList<IList<BoundarySegment>> loops = GetLoopsInCircuit(doc, circuit);
                if(loops != null)
                {
                    using (Transaction transaction = new Transaction(doc, "Create Roof"))
                    {
                        transaction.Start();

                        if (loops.Count > 1)
                        {
                            CurveArray curve = GetHousePerimeterCurveArray(doc, level, loops, true);

                            // create a roof type
                            FilteredElementCollector collector = new FilteredElementCollector(doc);
                            collector.OfClass(typeof(RoofType));
                            RoofType roofType = collector.FirstElement() as RoofType;

                            // create the foot print of the roof
                            ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
                            footPrintRoof = doc.Create.NewFootPrintRoof(curve, topLevel, roofType, out footPrintToModelCurveMapping);

                            // creates a iterator to add the roof slope
                            ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
                            iterator.Reset();

                            while (iterator.MoveNext())
                            {
                                ModelCurve modelCurve = iterator.Current as ModelCurve;
                                footPrintRoof.set_DefinesSlope(modelCurve, true);
                                footPrintRoof.set_SlopeAngle(modelCurve, 0.3);                                                      


                                #region Platibanda
                                /*
                                // get curve middle point   
                                XYZ curveMiddlePoint = GetModelCurveMiddlePoint(modelCurve);
                                XYZ curveMiddlePointWithoutZ = new XYZ(curveMiddlePoint.X, curveMiddlePoint.Y, 0);

                                // retrieves the wall corresponding to that point
                                Wall perimeterWall = FindHostingWall(curveMiddlePointWithoutZ, doc, level);                      

                                //set the new height
                                perimeterWall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).Set(MetersToFeet(0.8));
                                */
                                #endregion

                                // TODO: OVERHANG
                            }
                        }

                        transaction.Commit();
                    }
                }
            }
            return footPrintRoof;
        }
    }
}
