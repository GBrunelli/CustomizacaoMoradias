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
using Newtonsoft.Json;

namespace CustomizacaoMoradias
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class ElementPlacer
    {
        private UIDocument uidoc;
        private Level level;
        private Level topLevel;
        private double scale;
        private PlanCircuitSet docPlanCircuitSet;
        private FootPrintRoof roof;

        public ElementPlacer(UIDocument uidoc, string level, string topLevel, double scale)
        {
            this.uidoc = uidoc;
            this.level = GetLevelFromName(level);
            this.topLevel = GetLevelFromName(topLevel);
            this.scale = scale;
            this.docPlanCircuitSet = null;
            this.roof = null;
        }

        /// <summary>
        /// Convert from meters to feet.
        /// </summary>
        /// <param name="meters"></param>
        public static double MetersToFeet(double meters)
        {
            return UnitUtils.Convert(meters, UnitTypeId.Meters, UnitTypeId.Feet);
        }

        /// <summary>
        /// Opens the Explorer to the user select a file.
        /// </summary>
        /// <returns>
        /// Returns a string with the file path.
        /// </returns>
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
        /// Read a CSV file containing the definitions of the building, then starts and commits a transaction to the open document.
        /// </summary>
        /// <param name="path">
        /// The complete path to the CSV file.
        /// </param>
        public void BuildCSV(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            Document doc = uidoc.Document;

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
                            CreateWall(columns, "parede 15 cm - branca");
                            break;

                        case "Janela":
                            CreateHostedElement(columns);
                            break;

                        case "Porta":
                            CreateHostedElement(columns);
                            break;

                        case "Mobiliario":
                            CreateFurniture(columns);
                            break;
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message, "Erro");
                }
            }
        }

        /// <summary>
        /// Creates a piece of furniture.
        /// </summary>
        /// <param name="properties">
        /// [0]: Name of the element;
        /// [1]: x coordinate;
        /// [2]: y coordinate;
        /// [3]: rotation;
        /// [4]: type;
        /// [5]: family name;
        /// </param>
        private void CreateFurniture(string[] properties)
        {
            if (properties is null) throw new ArgumentNullException(nameof(properties));

            Document doc = uidoc.Document;

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
        /// <param name="properties">
        /// [0]: "Parede"
        /// [1]: x1;
        /// [2]: y1;
        /// [3]: x2;
        /// [4]: y2;
        /// </param>
        private void CreateWall(string[] properties, string wallTypeName)
        {
            if (properties is null) throw new ArgumentNullException(nameof(properties));

            Document doc = uidoc.Document;

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
                // sllect wall type
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(WallType));
                WallType wallType = collector.First(y => y.Name == wallTypeName) as WallType;

                using (Transaction transaction = new Transaction(doc, "Place Wall"))
                {
                    transaction.Start();
                    Wall newWall = Wall.Create(doc, curve, wallType.Id, level.Id, MetersToFeet(2.8), 0, false, false);
                    newWall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(topLevel.Id);
                    newWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(MetersToFeet(-0.10));
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
        /// Get the wall in an specific coordinate.
        /// </summary>
        /// <param name="xyz">
        /// The Z compenent must be in the same level of the wall.
        /// </param>
        /// <returns>
        /// Returns the Wall in the XYZ coords. Returns null if no wall was found.
        /// </returns>
        private Wall FindHostingWall(XYZ xyz)
        {
            if (xyz is null) throw new ArgumentNullException(nameof(xyz));

            Document doc = uidoc.Document;
            List<Wall> walls = GetWalls();

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
        /// Get all walls in the document.
        /// </summary>
        /// <returns></returns>
        private List<Wall> GetWalls()
        {
            Document doc = uidoc.Document;
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(Wall));
            List<Wall> walls = collector.Cast<Wall>().Where(wl => wl.LevelId == level.Id).ToList();
            return walls;
        }

        /// <summary>
        /// Create a hosted element on a wall.
        /// </summary>
        /// <param name="properties">
        /// [0]: Element type
        /// [1]: x coordinate;
        /// [2]: y coordinate;
        /// [3]: type;
        /// [4}: family name;
        /// </param>
        private void CreateHostedElement(string[] properties)
        {
            if (properties is null) throw new ArgumentNullException(nameof(properties));

            Document doc = uidoc.Document;

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
                Wall wall = FindHostingWall(xyz);
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
        /// Finds a level from its name
        /// </summary>
        /// <param name="levelName">
        /// The name as it is on Revit.
        /// </param>
        /// <returns>
        /// Returns the Level.
        /// </returns>
        public Level GetLevelFromName(string levelName)
        {
            Document doc = uidoc.Document;
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
                throw new LevelNotFoundException("Nível \"" + levelName + "\" não encontrado.", e);
            }
            return level;
        }

        /// <summary>
        /// Get all the plan circuits of an level. 
        /// </summary>
        /// <param name="update">
        /// If the update flag is true, the circuit will be recalculated,
        /// use it if new walls were added after the last time that this method was called.
        /// </param>
        /// <returns>
        /// Return a PlanCircuitSet with all circuits of the level.
        /// </returns>
        private PlanCircuitSet GetDocPlanCircuitSet(bool update)
        {
            if (docPlanCircuitSet == null || update == true)
            {
                Document doc = uidoc.Document;
                PhaseArray phases = doc.Phases;

                // get the last phase
                Phase createRoomsInPhase = phases.get_Item(phases.Size - 1);

                if (createRoomsInPhase is null)
                    throw new Exception("Não foi encontrada nenhuma fase no documento atual.");

                PlanTopology topology = doc.get_PlanTopology(level, createRoomsInPhase);
                PlanCircuitSet circuitSet = topology.Circuits;

                return circuitSet;
            }
            else
            {
                return docPlanCircuitSet;
            }
        }

        /// <summary>
        /// Get the the loops in a determined circuit, if there is not room located in that circuit, 
        /// it will create a room with a genetic name. If the room is composed by more than 1 loop,
        /// it will name the room "Exterior 0".
        /// </summary>
        /// <param name="circuit">
        /// The PlanCircuit of the active document. See also GetDocPlanCircuitSet().
        /// </param>
        private IList<IList<BoundarySegment>> GetLoopsInCircuit(PlanCircuit circuit)
        {
            Document doc = uidoc.Document;
            Room room;
            IList<IList<BoundarySegment>> loops = null;
            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center
            };

            using (Transaction transaction = new Transaction(doc, "Create room"))
            {


                if (circuit.IsRoomLocated)
                {
                    UV point2D = circuit.GetPointInside();
                    XYZ point = new XYZ(point2D.U, point2D.V, 0);
                    room = doc.GetRoomAtPoint(point);
                    loops = room.GetBoundarySegments(opt);
                }
                else
                {
                    transaction.Start();
                    room = doc.Create.NewRoom(null, circuit);
                    loops = room.GetBoundarySegments(opt);

                    if (loops.Count > 1)
                    {
                        room.Name = "Exterior";
                        room.Number = "0";
                    }

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
 
                    transaction.Commit();
                }
            }
            return loops;
        }

        /// <summary>
        /// Create a floor in every circuit of the document at the base level.
        /// </summary>
        /// <returns>
        /// Retuns the new floor.
        /// </returns>
        /// <param name="floorTypeName">
        /// The name of the floorType as it is on Revit.
        /// </param>
        public void CreateFloor(string floorTypeName)
        {
            Document doc = uidoc.Document;
            PlanCircuitSet circuitSet = GetDocPlanCircuitSet(false);

            // get the floorType
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FloorType));
            FloorType floorType = collector.First(y => y.Name == floorTypeName) as FloorType;

            foreach (PlanCircuit circuit in circuitSet)
            {
                IList<IList<BoundarySegment>> loops = GetLoopsInCircuit(circuit);

                if (loops != null)
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
                                doc.Create.NewFloor(curve, floorType, level, true);
                            }
                        }
                        transaction.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// Calculates a CurveArray that corresponds the perimeter of a building given all its internal loops.
        /// </summary>
        /// <param name="offset">
        /// A real value that reprends the offset of the perimeter.
        /// </param>
        /// <param name="offsetVector">
        /// A vector that will defines the offset. The spacing will be applied on the orthogonal edges to the offsetVector. 
        /// The magnitude of the vector doesn't matter, just the direction will affect the offset.
        /// If the vector is 0, the offset will be applied for all the sides. 
        /// </param>
        /// <returns>
        /// Returns a CurveArray that corresponds to the house perimeter.
        /// </returns>
        public CurveArray GetHousePerimeterCurveArray(double offset, XYZ offsetVector)
        {
            Document doc = uidoc.Document;
            // retrives the circuit set of the active document
            PlanCircuitSet circuitSet = GetDocPlanCircuitSet(false);

            foreach (PlanCircuit circuit in circuitSet)
            {
                // get all the closed loops in the circuit
                IList<IList<BoundarySegment>> loopsSegments = GetLoopsInCircuit(circuit);

                // if there more than 1 loop, that means that this circuit is the perimeter circuit
                if(loopsSegments.Count > 1)
                {
                    // first of all we find the closed loop with the smaller area
                    double minArea = double.MaxValue;
                    IList<BoundarySegment> perimeterSegments = null;
                    foreach (IList<BoundarySegment> singleLoop in loopsSegments)
                    {
                        double area = 0;

                        // transforms the boundary segments in a CurveLoop
                        CurveLoop currentCurve = new CurveLoop();
                        foreach (BoundarySegment seg in singleLoop)
                        {
                            currentCurve.Append(seg.GetCurve());
                        }

                        IList<CurveLoop> curveLoopList = new List<CurveLoop>();
                        curveLoopList.Add(currentCurve);
                        area = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoopList);
                        if (area < minArea)
                        {
                            minArea = area;
                            perimeterSegments = singleLoop;
                        }
                    }

                    // and then we create a curve array with that loop
                    CurveArray housePerimeter = new CurveArray();
                    foreach (BoundarySegment seg in perimeterSegments)
                    {
                        Curve curve = seg.GetCurve();

                        // aplies the offset for each curve
                        if (offset > 0)
                            curve = CreateOffsetedCurve(housePerimeter, curve, offset, offsetVector);

                        housePerimeter.Append(curve);
                    }
                    return housePerimeter;
                }
            }
            return null;
        }

        /// <summary>
        /// Transform a curve to be offsetted.
        /// </summary>
        /// <param name="housePerimeter"></param>
        /// <param name="curve"></param>
        /// <param name="offset"></param>
        /// <param name="offsetVector"></param>
        /// <returns>
        /// Returns the offsetted curve.
        /// </returns>
        private Curve CreateOffsetedCurve(CurveArray housePerimeter, Curve curve, double offset, XYZ offsetVector)
        {
            Document doc = uidoc.Document;
            // finds the middle point of the current curve
            XYZ middlePoint = GetCurveMiddlePoint(curve);

            // subtracts de Z value of the point, because the method FindHosringWall 
            // calculates the distance based on the base of the wall
            middlePoint = middlePoint.Subtract(new XYZ(0, 0, middlePoint.Z));

            // finds the wall below that curve, and retrives its normal vector
            Wall wall = FindHostingWall(middlePoint);
            XYZ wallNormalVector = wall.Orientation;

            // Makes sure that the exterior of the wall is pointed to the exterior of the house
            XYZ roomPoint = middlePoint.Add(wallNormalVector);
            Room room = doc.GetRoomAtPoint(roomPoint);
            if (room.Name != "Exterior 0")
            {
                wall.Flip();
                wallNormalVector = wall.Orientation;
            }

            // makes the curve 60cm bigger at each end, since we assume that all walls are in right angles
            curve.MakeBound(curve.GetEndParameter(0) - offset, curve.GetEndParameter(1) + offset);

            if (wallNormalVector.CrossProduct(offsetVector).IsZeroLength())
            {
                // aplies the offset
                wallNormalVector = wallNormalVector.Multiply(offset);
                Transform transform = Transform.CreateTranslation(wallNormalVector);
                curve = curve.CreateTransformed(transform);
            }       

            // verifies that the new curve intersects with other curves in the array,
            // if that happens, both curves are ajusted to align perfectly 
            foreach (Curve iterationCurve in housePerimeter)
            {
                // calculates and analyzes the intersections
                IntersectionResultArray intersectionResultArray;
                SetComparisonResult setComparisonResult = curve.Intersect(iterationCurve, out intersectionResultArray);
                if (setComparisonResult == SetComparisonResult.Overlap)
                {
                    IntersectionResultArrayIterator iterator = intersectionResultArray.ForwardIterator();
                    while (iterator.MoveNext())
                    {
                        IntersectionResult result = iterator.Current as IntersectionResult;
                        XYZ intersectionPoint = result.XYZPoint;

                        // remove the overlap of the both curves
                        RemoveCurveOverlap(curve, intersectionPoint);
                        RemoveCurveOverlap(iterationCurve, intersectionPoint);
                    }
                }
            }
            return curve;
        }

        /// <summary>
        /// Set the bound of a curve to remove overlaps. This method is used to make CurveLoops.
        /// </summary>
        /// <seealso cref="CreateOffsetedCurve(CurveArray, Curve, double, XYZ)"/>
        /// <param name="curve"></param>
        /// <param name="intersectionPoint"></param>
        private static void RemoveCurveOverlap(Curve curve, XYZ intersectionPoint)
        {
            double distanceToStart = curve.GetEndPoint(0).DistanceTo(intersectionPoint);
            double distanceToEnd = curve.GetEndPoint(1).DistanceTo(intersectionPoint);

            // case the star point is closer
            if (distanceToStart < distanceToEnd)
            {
                curve.MakeBound(curve.GetEndParameter(0) + distanceToStart, curve.GetEndParameter(1));
            }
            // case the end point is closer
            else
            {
                curve.MakeBound(curve.GetEndParameter(0), curve.GetEndParameter(1) - distanceToEnd);
            }
        }

        /// <summary>
        /// Draw lines in the current view that matches the given curve array.
        /// </summary>
        /// <param name="curveArray"></param>
        public void DrawCurveArray(CurveArray curveArray)
        {
            Document doc = uidoc.Document;
            Autodesk.Revit.DB.View currentView = doc.ActiveView;
            foreach (Curve curve in curveArray)
            {
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);
                Line L1 = Line.CreateBound(startPoint, endPoint);
                doc.Create.NewDetailCurve(currentView, L1);
            }
        }

        /// <summary>
        /// Converts a CurveLoop to CurveArray.
        /// </summary>
        /// <param name="loop"></param>
        /// <returns>
        /// Returns a CurveArray with the same curves of the CurveLoop.
        /// </returns>
        private static CurveArray CurveLoopToCurveArray(CurveLoop loop)
        {
            CurveArray array = new CurveArray();
            foreach (Curve curve in loop)
            {
                array.Append(curve);
            }
            return array;
        }

        /// <summary>
        /// Converts a CurveArray to a CurveLoop.
        /// </summary>
        /// <param name="array"></param>
        /// <returns> 
        /// Returns a CurveLoop with the same curves of the CurveArray.
        /// </returns>
        private static CurveLoop CurveArrayToCurveLoop(CurveArray array)
        {
            CurveLoop loop = new CurveLoop();
            foreach (Curve curve in array)
            {
                loop.Append(curve);
            }
            return loop;
        }

        /// <summary>
        /// Create the ceiling of a building given the loops of the active document. The buildding must be surrounded by walls.
        /// </summary>
        /// <param name="floorTypeName"></param>
        /// <returns>
        /// Returns the created floor.
        /// </returns>
        public Floor CreateCeiling(string floorTypeName)
        {
            Document doc = uidoc.Document;
            Floor ceiling = null;
            PlanCircuitSet circuitSet = GetDocPlanCircuitSet(false);

            using (Transaction transaction = new Transaction(doc, "Create Ceiling"))
            {
                transaction.Start();

                // creates a ceiling if in a room there is more than one loop,
                // and finds the smallest loop

                CurveArray curve = GetHousePerimeterCurveArray(0, new XYZ(0, 0, 0));

                // create a floor type
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(FloorType));
                FloorType floorType = collector.First(y => y.Name == floorTypeName) as FloorType;

                // create the ceiling
                ceiling = doc.Create.NewFloor(curve, floorType, topLevel, false);
                ElementTransformUtils.MoveElement(doc, ceiling.Id, new XYZ(0, 0, topLevel.Elevation));

                transaction.Commit();
            }

            return ceiling;
        }

        /// <summary>
        /// Calculates the middle point of a Curve.
        /// </summary>
        /// <param name="curve"></param>
        /// <returns>
        /// Returns the XYZ coords.
        /// </returns>
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
        /// Create a generic roof in a building given the level of the walls.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="offsetVector"></param>
        /// <returns>
        /// Returns the created FootPrintRood.
        /// </returns>
        public FootPrintRoof CreateRoof(double offset, XYZ offsetVector)
        {
            Document doc = uidoc.Document;
            FootPrintRoof footPrintRoof = null;
            PlanCircuitSet circuitSet = GetDocPlanCircuitSet(false);     

            using (Transaction transaction = new Transaction(doc, "Create Roof"))
            {
                transaction.Start();

                CurveArray footPrintCurve = GetHousePerimeterCurveArray(offset, new XYZ(0, 0, 0));

                // create a roof type
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(RoofType));
                RoofType roofType = collector.FirstElement() as RoofType;

                // create the foot print of the roof
                ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
                footPrintRoof = doc.Create.NewFootPrintRoof(footPrintCurve, topLevel, roofType, out footPrintToModelCurveMapping);

                // creates a iterator to add the roof slope
                ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
                iterator.Reset();

                while (iterator.MoveNext())
                {                           
                    ModelCurve modelCurve = iterator.Current as ModelCurve;

                    Curve curve = modelCurve.GeometryCurve;
                    XYZ startPoint = curve.GetEndPoint(0);
                    XYZ endPoint = curve.GetEndPoint(1);
                    XYZ curveDirection = new XYZ(startPoint.X - endPoint.X, startPoint.Y - endPoint.Y, 0);

                    if(curveDirection.DotProduct(offsetVector) == 0)
                    {
                        footPrintRoof.set_DefinesSlope(modelCurve, true);
                        footPrintRoof.set_SlopeAngle(modelCurve, 0.3);
                    }

                    double elevation = - (offset - MetersToFeet(0.1)) / 3;
                    footPrintRoof.set_Offset(modelCurve, elevation);
                }              
                transaction.Commit();
            }
            this.roof = footPrintRoof;

            CreateGableWall();

            return footPrintRoof;
        }

        /// <summary>
        /// Classify the rooms of a project based on the elements inside it.
        /// </summary>
        public void ClassifyRooms()
        {
            Document doc = uidoc.Document;
            string jsonElementClassifier = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "elementClassifier.json");
            List<RoomClassifier> deserializedRoomClassifier = JsonConvert.DeserializeObject<List<RoomClassifier>>(jsonElementClassifier);         

            List<Room> rooms = GetRoomsAtLevel(level).ToList();
            foreach (Room room in rooms)
            {
                if(room.Area > 0)
                {
                    
                    string roomName = null;

                    List<Element> elements = GetFurniture(room);
                    foreach (Element element in elements)
                    {

                        int roomScore = 0;

                        foreach (RoomClassifier roomClassifier in deserializedRoomClassifier)
                        {
                            foreach (RoomElement furniture in roomClassifier.Element)
                            {
                                if (furniture.Name == element.Name)
                                {
                                    roomClassifier.RoomScore += furniture.Score;
                                }
                            }
                            if (roomClassifier.RoomScore > roomScore)
                            {
                                 roomName = roomClassifier.Name;                             
                            }
                        }
                    }
                    using (Transaction transaction = new Transaction(doc, "Classify Room"))
                    {
                        transaction.Start();
                        if (roomName != null)
                        {
                            room.Name = roomName;
                        }                        
                        transaction.Commit();
                    }
                }
            }
        }

        /// <summary>
        /// Get all the rooms in a determined level.
        /// </summary>
        /// <param name="level"></param>
        /// <returns>
        /// Returns a IEnumarable list of the the rooms.
        /// </returns>
        private IEnumerable<Room> GetRoomsAtLevel(Level level)
        {
            Document doc = uidoc.Document;
            ElementId levelId = level.Id;

            FilteredElementCollector collector = new FilteredElementCollector(doc);

            IEnumerable<Room> rooms = collector
              .WhereElementIsNotElementType()
              .OfClass(typeof(SpatialElement))
              .Where(e => e.GetType() == typeof(Room))
              .Where(e => e.LevelId.IntegerValue.Equals(
               levelId.IntegerValue))
              .Cast<Room>();
            return rooms;
        }

        /// <summary>
        /// Get all furniture elements inside a room.
        /// </summary>
        /// <param name="room"></param>
        /// <returns>
        /// Returns a List with those elements.
        /// </returns>
        private static List<Element> GetFurniture(Room room)
        {
            Document doc = room.Document;
            BoundingBoxXYZ boundingBox = room.get_BoundingBox(null);
            Outline outline = new Outline(boundingBox.Min, boundingBox.Max);
            BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);

            FilteredElementCollector collector
              = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .WhereElementIsViewIndependent()
                .OfClass(typeof(FamilyInstance))
                .WherePasses(filter);

            int roomid = room.Id.IntegerValue;
            List<Element> elementsInsideTheRoom = new List<Element>();

            foreach (FamilyInstance instance in collector)
            {
                if(instance.Room != null)
                {
                    if (instance.Room.Id.IntegerValue.Equals(roomid))
                    {
                        elementsInsideTheRoom.Add(instance);
                    }
                }
            }
            return elementsInsideTheRoom;
        }

        public void CreateNewSheet()
        {
            Document doc = uidoc.Document;
            using (Transaction transaction = new Transaction(doc, "New Sheet"))
            {
                transaction.Start();

                // create a filter to get all the title block type
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.OfCategory(BuiltInCategory.OST_TitleBlocks);
                collector.WhereElementIsElementType();

                // get elementid of first title block type
                ElementId titleblockid = collector.FirstElementId();

                // create the sheet
                ViewSheet viewSheet = ViewSheet.Create(doc, titleblockid);
                viewSheet.Name = "NEW SHEET TEST";
                viewSheet.SheetNumber = "A-01";

                Viewport viewport = Viewport.Create(doc, viewSheet.Id, doc.ActiveView.Id, new XYZ(0, 2, 0));

                transaction.Commit();
            }
        }

        public void CreateGableWall()
        {
            if (roof == null) throw new RoofNotDefinedException();
            Options op = new Options
            {
                ComputeReferences = true
            };

            GeometryElement roofGeometry = roof.get_Geometry(op);

            List<Wall> walls = GetWalls();
            using(Transaction transaction = new Transaction(uidoc.Document, "Create Gable Walls"))
            {
                transaction.Start();
                foreach (Wall wall in walls)
                {
                    GeometryElement wallGeometry = wall.get_Geometry(op);
                }
                transaction.Commit();
            }           
        }
    }
}
