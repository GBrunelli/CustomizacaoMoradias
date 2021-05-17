using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using CustomizacaoMoradias.Source;
using Newtonsoft.Json;
using System.Diagnostics;

namespace CustomizacaoMoradias
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class ElementPlacer
    {
        private UIDocument uidoc;
        private Document doc;

        private Level baseLevel;
        private Level topLevel;

        private PlanCircuitSet docPlanCircuitSet;

        private double scale;

        /// <summary>
        /// Default contructor.
        /// </summary>
        public ElementPlacer(UIDocument uidoc, string level, string topLevel, double scale)
        {
            this.uidoc = uidoc;
            this.doc = uidoc.Document;
            this.baseLevel = GetLevelFromName(level);
            this.topLevel = GetLevelFromName(topLevel);
            this.scale = scale;
            this.docPlanCircuitSet = null;
        }

        /// <summary>
        /// Convert from meters to feet.
        /// </summary>
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
                Filter = "json|*.json"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                return openFileDialog.FileName;
            return null;
        }

        /// <summary>
        /// Converts an angle in deegre to radians.
        /// </summary>
        public double DeegreToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        /// <summary>
        /// Get the FamilySymbol given its name.
        /// </summary>
        private static FamilySymbol GetFamilySymbol(Document doc, string fsFamilyName)
        {
            // Retrieve the familySymbol of the piece of furniture
            var symbol = (from familySymbol in new FilteredElementCollector(doc).
                 OfClass(typeof(FamilySymbol)).
                 Cast<FamilySymbol>()
                          where (familySymbol.Name == fsFamilyName)
                          select familySymbol).First();
            return symbol;
        }

        /// <summary>
        /// Convert a Coordinate to an XYZ object.
        /// </summary>
        private XYZ GetXYZFromProperties(Coordinate coords)
        {
            // Convert the values from the csv file
            double x0 = coords.X;
            x0 = MetersToFeet(x0 * scale);

            double y0 = coords.Y;
            y0 = MetersToFeet(y0 * scale);

            // Creates the point where the piece of furniture will be inserted
            return new XYZ(x0, y0, baseLevel.Elevation);
        }

        /// <summary>
        /// Searches in the document data base for a Wall Type.
        /// </summary>
        /// <returns>
        /// Returns the Wall Type corresponding to the string.
        /// </returns>
        private WallType GetWallType(string wallTypeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(WallType));
            WallType wallType = collector.First(y => y.Name == wallTypeName) as WallType;
            return wallType;
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
            xyz = xyz.Subtract(new XYZ(0, 0, xyz.Z));
            if (xyz is null) throw new ArgumentNullException(nameof(xyz));

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
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(Wall));
            List<Wall> walls = collector.Cast<Wall>().Where(wl => wl.LevelId == baseLevel.Id).ToList();
            return walls;
        }

        /// <summary>
        /// Builds the elements defined on a JSON file.
        /// </summary>
        public void BuildJSON(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));
            try
            {
                string jsonText = File.ReadAllText(path);
                ElementDeserializer ed = JsonConvert.DeserializeObject<ElementDeserializer>(jsonText);

                foreach (WallProperty wall in ed.WallProperties)
                    CreateWall(wall, Properties.Settings.Default.WallTypeName);

                foreach (WindowProperty window in ed.WindowProperties)
                    CreateWindow(window);

                foreach (DoorProperty door in ed.DoorProperties)
                    CreateDoor(door);

                foreach (HostedProperty element in ed.HostedProperties)
                    CreateHostedElement(element);

                foreach (FurnitureProperty element in ed.FurnitureProperties)
                    CreateFurniture(element);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Erro");
            }
        }

        /// <summary>
        /// Creates a piece of furniture.
        /// </summary>
        /// <param name="properties"> Properties of the object.</param>
        private FamilyInstance CreateFurniture(FurnitureProperty properties)
        {
            if (properties is null) throw new ArgumentNullException(nameof(properties));

            // get the properties
            double rotation = DeegreToRadians(properties.Rotation);
            XYZ p0 = GetXYZFromProperties(properties.Coordinate.ElementAt(0));
            XYZ p1 = GetXYZFromProperties(properties.Coordinate.ElementAt(1));
            XYZ point = p0.Add(p1).Divide(2);
            string fsFamilyName = GetFamilySymbolName(properties.Type);

            // Creates a point above the furniture to serve as a rotation axis
            XYZ axisPoint = new XYZ(point.X, point.Y, baseLevel.Elevation + 1);
            Line axis = Line.CreateBound(point, axisPoint);
            FamilyInstance furniture = null;

            try
            {
                FamilySymbol familySymbol = GetFamilySymbol(doc, fsFamilyName);

                var structuralType = Autodesk.Revit.DB.Structure.StructuralType.NonStructural;
                furniture = doc.Create.NewFamilyInstance(point, familySymbol, structuralType);
                ElementTransformUtils.RotateElement(doc, furniture.Id, axis, rotation);
            }
            catch (Exception e)
            {
                throw new Exception("Erro ao inserir mobiliario \"" + fsFamilyName + "\".", e);
            }
            return furniture;
        }

        /// <summary>
        /// Creates a wall given its properties.
        /// </summary>
        /// <param name="properties"> Properties of the object.</param>
        private Wall CreateWall(WallProperty properties, string wallTypeName)
        {
            if (properties is null) throw new ArgumentNullException(nameof(properties));

            XYZ p0 = GetXYZFromProperties(properties.Coordinate.ElementAt(0));
            XYZ p1 = GetXYZFromProperties(properties.Coordinate.ElementAt(1));
            Wall wall = null;
            try
            {
                Curve curve = Line.CreateBound(p0, p1);
                // sellect wall type
                WallType wallType = GetWallType(wallTypeName);

                // Creating the wall

                wall = Wall.Create(doc, curve, wallType.Id, baseLevel.Id, MetersToFeet(2.8), 0, false, false);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(topLevel.Id);
                wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(MetersToFeet(-0.10));

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Erro");
                //throw new Exception("Erro ao inserir parede de coodenadas: (" + p0 + ", " + p1 + ").", e);
            }
            return wall;
        }

        /// <summary>
        /// Create a hosted element on a wall.
        /// </summary>
        /// <param name="properties"> Properties of the object.</param>
        private FamilyInstance CreateHostedElement(HostedProperty properties)
        {
            if (properties is null) throw new ArgumentNullException(nameof(properties));
            XYZ point = GetXYZFromProperties(properties.Coordinate);
            string fsFamilyName = GetFamilySymbolName(properties.Type);

            FamilyInstance instance = null;
            try
            {
                FamilySymbol familySymbol = GetFamilySymbol(doc, fsFamilyName);
                Wall wall = FindHostingWall(point);
                if (wall == null) return null;

                // Create the element
                instance = doc.Create.NewFamilyInstance(point, familySymbol, wall, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
            }
            catch (Exception e)
            {
                throw new Exception("Erro ao inserir elemento hospedeiro \"" + fsFamilyName + "\".", e);
            }
            return instance;
        }

        /// <summary>
        /// Get the FamilySynbol from the settings.
        /// </summary>
        /// <param name="familyType"></param>
        private static string GetFamilySymbolName(string familyType)
        {
            foreach (SettingsProperty currentProperty in Properties.Settings.Default.Properties)
            {
                if (familyType == currentProperty.Name)
                {
                    return (string)currentProperty.DefaultValue;
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a door given its properties.
        /// </summary>
        /// <param name="properties"> Properties of the object.</param>
        private FamilyInstance CreateDoor(DoorProperty properties)
        {
            if (properties is null) throw new ArgumentNullException(nameof(properties));
            HostedProperty hp = ConvertToHosted(properties);
            return CreateHostedElement(hp);
        }

        /// <summary>
        /// Creates a windows given its properties.
        /// </summary>
        /// <param name="properties"> Properties of the object.</param>
        private FamilyInstance CreateWindow(WindowProperty properties)
        {
            if (properties is null) throw new ArgumentNullException(nameof(properties));
            HostedProperty hp = ConvertToHosted(properties);
            FamilyInstance window = CreateHostedElement(hp);

            if (window != null)
                window.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).Set(MetersToFeet(2.00));

            return window;
        }

        /// <summary>
        /// Converts an Hosted object in a HostedProperty.
        /// </summary>
        private static HostedProperty ConvertToHosted(Hosted obj)
        {
            Coordinate c = obj.GetCoordinate();
            HostedProperty hp = new HostedProperty()
            {
                Coordinate = c,
                Type = obj.getType()
            };
            return hp;
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
                PhaseArray phases = doc.Phases;

                // get the last phase
                Phase createRoomsInPhase = phases.get_Item(phases.Size - 1);

                if (createRoomsInPhase is null)
                    throw new Exception("Não foi encontrada nenhuma fase no documento atual.");

                PlanTopology topology = doc.get_PlanTopology(baseLevel, createRoomsInPhase);
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
            Room room;
            IList<IList<BoundarySegment>> loops = null;
            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center
            };

            if (circuit.IsRoomLocated)
            {
                UV point2D = circuit.GetPointInside();
                XYZ point = new XYZ(point2D.U, point2D.V, 0);
                room = doc.GetRoomAtPoint(point);
                loops = room.GetBoundarySegments(opt);
            }
            else
            {
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
                            doc.Create.NewFloor(curve, floorType, baseLevel, true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the vertices of the CurveArray
        /// </summary>
        /// <param name="curveArray">
        /// The CurveArray must be closed.
        /// </param>
        /// <returns>
        /// Returns a Linked List of UV points ordered in clock wise order.
        /// </returns>
        private LinkedList<UV> GetPoints(CurveArray curveArray)
        {
            LinkedList<UV> points = new LinkedList<UV>();
            foreach (Curve curve in curveArray)
            {
                UV point2D = ProjectInPlaneXY(curve.GetEndPoint(0));
                points.AddLast(point2D);
            }
            return points;
        }

        /// <summary>
        /// Calculates de angle between the vectors (p0, p1) and (p1, p2)
        /// </summary>
        /// <returns>
        /// Returns the angle in radians.
        /// </returns>
        private double CalculatesAngle(UV p0, UV p1, UV p2)
        {
            UV vector1 = p1.Subtract(p0);
            UV vector2 = p2.Subtract(p1);
            return Math.PI + Math.Atan2(vector1.CrossProduct(vector2), vector1.DotProduct(vector2));
        }

        /// <summary>
        /// Get all notches in a list of ordered points that represent the vertices of
        /// a polygon. A notch is a point that the reflex angle in internal.
        /// </summary>
        /// <param name="points">
        /// The poitns in the LinkedList must be ordered in clockwise order.
        /// </param>
        /// <returns>
        /// Returns a List with the notches.
        /// </returns>
        private List<UV> GetNotches(LinkedList<UV> points)
        {
            List<UV> notches = new List<UV>();
            int n = points.Count();
            double angle;

            // calculates the angle for the first node
            UV p0 = points.Last();
            UV p1 = points.First();
            UV p2 = points.First.Next.Value;
            angle = CalculatesAngle(p0, p1, p2);
            if (angle > Math.PI)
                notches.Add(p1);

            // calculates the angle for the middle nodes
            LinkedListNode<UV> node = points.First;
            for (int i = 1; i < n - 1; i++)
            {
                node = node.Next;
                p0 = node.Previous.Value;
                p1 = node.Value;
                p2 = node.Next.Value;
                angle = CalculatesAngle(p0, p1, p2);
                if (angle > Math.PI)
                    notches.Add(p1);
            }
            // calculates the angle fot the last node
            p0 = points.Last.Previous.Value;
            p1 = points.Last();
            p2 = points.First();
            angle = CalculatesAngle(p0, p1, p2);
            if (angle > Math.PI)
                notches.Add(p1);

            return notches;
        }

        /// <summary>
        /// Decompose a non convex perimiter into its convex components. There is no 
        /// garantee that it will be the mininum number of convex polygons. All the
        /// angles of the perimeter must be a multiple of PI/2 radians.
        /// </summary>
        /// <returns>
        /// Returns a List of CurveArrays that represents the convex compenents.
        /// </returns>
        public List<CurveArray> GetConvexPerimeters(CurveArray curveArray, XYZ preferredOrientation, out List<Line> cutLines)
        {
            LinkedList<UV> points = GetPoints(curveArray);
            List<UV> notches = GetNotches(points);
            List<CurveArray> perimeters = new List<CurveArray>();
            cutLines = new List<Line>();
            foreach (UV notche in notches)
            {
                var result = EliminateNotch(notche, curveArray, points, preferredOrientation, out Line line);
                cutLines.Add(line);
                foreach (CurveArray array in result)
                {
                    perimeters.Add(array);
                }
            }

            perimeters.Sort(new SortingDescendingArea());
            return perimeters;
        }

        private class SortingDescendingArea : IComparer<CurveArray>
        {
            public int Compare(CurveArray x, CurveArray y)
            {
                IList<CurveLoop> curveLoopX = new List<CurveLoop> { CurveArrayToCurveLoop(x) };
                double areaX = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoopX);

                IList<CurveLoop> curveLoopY = new List<CurveLoop> { CurveArrayToCurveLoop(y) };
                double areaY = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoopY);

                if (areaX < areaY)
                    return 1;
                if (areaX > areaY)
                    return -1;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Eliminates a notch by creating a new line between the notch and an edge of the polygon.
        /// </summary>
        /// <param name="notch">Coordinates of the notch.</param>
        /// <param name="curveArray">The polygon.</param>
        /// <param name="points">The vertices of the polygon.</param>
        /// <param name="preferredOrientation">The method will try to make a cut that is parallel to this vector.</param>
        /// <param name="cutLine">The line that cut the polygon.</param>
        /// <returns>
        /// Returns the list of the CurveArrays.
        /// </returns>
        private List<CurveArray> EliminateNotch(UV notch, CurveArray curveArray, LinkedList<UV> points, XYZ preferredOrientation, out Line cutLine)
        {
            XYZ notche3D = TranformIn3D(notch);
            Line line1 = Line.CreateUnbound(notche3D, preferredOrientation);
            XYZ otherOrientation = new XYZ(preferredOrientation.Y, preferredOrientation.X, 0);
            Line line2 = Line.CreateUnbound(notche3D, otherOrientation);
            LinkedListNode<UV> notchNode = FindPoint(points, notch);

            // get the posible curves for the new point
            CurveArray posibleCurves = new CurveArray();
            foreach (Curve curve in curveArray)
            {
                UV p0 = ProjectInPlaneXY(curve.GetEndPoint(0));
                UV p1 = ProjectInPlaneXY(curve.GetEndPoint(1));

                // the curve will be appended if none of the
                // end points is equal to the notche
                if (!p0.Subtract(notch).IsZeroLength() &&
                   !p1.Subtract(notch).IsZeroLength())
                {
                    posibleCurves.Append(curve);
                }
            }

            // iterate for each possible curve, and if
            // a intersection is found, the point will 
            // added in the linked list
            LinkedListNode<UV> newNode;
            newNode = FindNewNode(points, line1, posibleCurves);

            if (newNode == null)         
                newNode = FindNewNode(points, line2, posibleCurves);

            // generates the 2 new polygons    
            LinkedList<UV> polygonA = CreatePolygonBetweenVertices(points, newNode, notchNode);
            LinkedList<UV> polygonB = CreatePolygonBetweenVertices(points, notchNode, newNode);

            // creates the curves
            List<CurveArray> list = new List<CurveArray>
            {
                CreateCurveArrayFromPoints(polygonA),
                CreateCurveArrayFromPoints(polygonB)
            };

            // returns the cutLine
            cutLine = Line.CreateBound(notche3D, TranformIn3D(newNode.Value));

            return list;
        }

        /// <summary>
        /// Finds a point that
        /// </summary>
        /// <param name="points"></param>
        /// <param name="line1"></param>
        /// <param name="posibleCurves"></param>
        /// <returns></returns>
        private static LinkedListNode<UV> FindNewNode(LinkedList<UV> points, Line line1, CurveArray posibleCurves)
        {
            // iterate for each possible curve, and if
            // a intersection is found, the point will 
            // added in the linked list
            LinkedListNode<UV> newNode = null;
            foreach (Curve curve in posibleCurves)
            {
                var intersection = curve.Intersect(line1, out IntersectionResultArray resultArray);
                if (intersection == SetComparisonResult.Overlap)
                {
                    newNode = AddPointsInList(points, resultArray, curve);
                    break;
                }
            }
            return newNode;
        }

        /// <summary>
        /// Create a CurveArray given its vertices.
        /// </summary>
        private CurveArray CreateCurveArrayFromPoints(LinkedList<UV> points)
        {
            CurveArray curveArray = new CurveArray();
            int n = points.Count();
            var node = points.First;
            Line line;
            for (int i = 1; i < n; i++)
            {
                line = Line.CreateBound(TranformIn3D(node.Value), TranformIn3D(node.Next.Value));
                curveArray.Append(line);
                node = node.Next;
            }
            line = Line.CreateBound(TranformIn3D(node.Value), TranformIn3D(points.First.Value));
            curveArray.Append(line);
            return curveArray;
        }

        /// <summary>
        /// Create a sub group of the linkedList that starts with node 'first' and ends with node 'last'.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns>
        /// Returns the vertives of the new polygon.
        /// </returns>
        private LinkedList<UV> CreatePolygonBetweenVertices(LinkedList<UV> points, LinkedListNode<UV> first, LinkedListNode<UV> last)
        {
            LinkedList<UV> polygon = new LinkedList<UV>();
            LinkedListNode<UV> node = first;

            while(node != last)
            {            
                // TEMPORARY: change for a circular list
                if(node == null)
                {
                    node = points.First;
                }
                polygon.AddLast(node.Value);
                node = node.Next;
            }
            polygon.AddLast(node.Value);
            return polygon;
        }

        /// <summary>
        /// Add a new vertice in order in the list of vertices of the polygon given the IntersectionResultArray.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="resultArray"></param>
        /// <param name="curve"></param>
        private static LinkedListNode<UV> AddPointsInList(LinkedList<UV> points, IntersectionResultArray resultArray, Curve curve)
        {
            UV p0 = ProjectInPlaneXY(curve.GetEndPoint(0));
            LinkedListNode<UV> newNode = null;
            var node = FindPoint(points, p0);
            var iterator = resultArray.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
                IntersectionResult result = iterator.Current as IntersectionResult;
                var intersectionPoint = ProjectInPlaneXY(result.XYZPoint);
                newNode = points.AddAfter(node, intersectionPoint);
            }
            return newNode;
        }

        /// <summary>
        /// Searches for a point in the LinkedList
        /// </summary>
        /// <returns>
        /// Returns the node.
        /// </returns>
        private static LinkedListNode<UV> FindPoint(LinkedList<UV> points, UV key)
        {
            var node = points.First;
            while(node != null)
            {
                var point = node.Value;
                if (point.U == key.U && point.V == key.V)
                    return node;
                node = node.Next;
            }
            return null;
        }

        /// <summary>
        /// Project the point XYZ in the plane XY.
        /// </summary>
        private static UV ProjectInPlaneXY(XYZ xyz)
        {
            return new UV(xyz.X, xyz.Y);
        }

        /// <summary>
        /// Transforms a 2D point in a 3D point, with the Z component set to 0.
        /// </summary>
        private static XYZ TranformIn3D(UV uv)
        {
            return new XYZ(uv.U, uv.V, 0);
        }

        /// <summary>
        /// Calculates a CurveArray that corresponds the perimeter of a building given all its internal loops. 
        /// The building MUST be surround by walls.
        /// </summary>
        /// <param name="offset">
        /// A real value that reprends the offset of the perimeter.
        /// If this value is 0, the user may not pass an offsetVector.
        /// </param>
        /// <param name="offsetVector">
        /// A vector that will defines the offset. The offset will be applied on the orthogonal lines to the offsetVector. 
        /// The magnitude of the vector doesn't matter, just the direction will affect the offset.
        /// If the vector is 0, the offset will be applied for all sides. 
        /// </param>
        /// <returns>
        /// Returns a CurveArray that corresponds to the house perimeter.
        /// </returns>
        public CurveArray GetHousePerimeter()
        {
            // retrives the circuit set of the active document
            PlanCircuitSet circuitSet = GetDocPlanCircuitSet(false);

            foreach (PlanCircuit circuit in circuitSet)
            {
                // get all the closed loops in the circuit
                IList<IList<BoundarySegment>> loopsSegments = GetLoopsInCircuit(circuit);

                // if there more than 1 loop, that means that this circuit represents the external area
                if (loopsSegments.Count > 1)
                {
                    // first of all we find the closed loop with the smaller area
                    double minArea = double.MaxValue;
                    IList<BoundarySegment> perimeterSegments = null;
                    foreach (IList<BoundarySegment> singleLoop in loopsSegments)
                    {
                        double area = 0;

                        // transforms the boundary segments into a CurveLoop
                        CurveLoop currentCurve = new CurveLoop();
                        foreach (BoundarySegment seg in singleLoop)
                        {
                            currentCurve.Append(seg.GetCurve());
                        }

                        // save the segments with the smaller area, which represents the house perimeter
                        IList<CurveLoop> curveLoopList = new List<CurveLoop> { currentCurve };
                        area = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoopList);
                        if (area < minArea)
                        {
                            minArea = area;
                            perimeterSegments = singleLoop;
                        }
                    }

                    // and then we create a curve array with the boundary segments of that loop
                    return BoundarySegmentToCurveArray(perimeterSegments);
                }
            }
            return null;
        }

        /// <summary>
        /// Creates a CurveArray given a List of BoundarySegments
        /// </summary>
        private CurveArray BoundarySegmentToCurveArray(IList<BoundarySegment> perimeterSegments)
        {
            CurveArray curveArray = new CurveArray();
            foreach (BoundarySegment seg in perimeterSegments)
            {
                Curve curve = seg.GetCurve();
                curveArray.Append(curve);
            }
            return curveArray;
        }

        /// <summary>
        /// Creates a Curve Array given the Boundary Segments. 
        /// </summary>
        /// <param name="offset">
        /// A positive value that represents the offset that the curve array will have in a direction. 
        /// If this value is 0, the user may not pass an offsetVector.
        /// <param name="curveArray">
        /// The reference Curve Array.
        /// </param>
        /// <returns>
        /// Returns the offseted Curve Array.
        /// </returns>
        public CurveArray CreateOffsetedCurveArray(double offset, CurveArray curveArray, Line unchangedLine)
        {
            if (offset < 0) return null;
            List<UV> points = GetPoints(curveArray).ToList() ;
            List<UV> offsetedPoints = OffsetPolygon(points, offset, unchangedLine);
            LinkedList<UV> linkedOffsetedPoints = new LinkedList<UV>(offsetedPoints);
            CurveArray offsetedCurveArray = CreateCurveArrayFromPoints(linkedOffsetedPoints);
            return offsetedCurveArray;
        }

        /// <summary>
        /// Offset a polygon in all directions.
        /// </summary>
        private static List<UV> OffsetPolygon(List<UV> vertices, double offset, Line unchangedLine)
        {
            int num_points = vertices.Count();
            List<UV> adjusted_points = new List<UV>(num_points);

            for (int j = 0; j < num_points; j++)
            {
                //find the points before and after our target point.
                int i = (j - 1);
                if (i < 0)
                    i += num_points;
                int k = (j + 1) % num_points;

                //the next step is to push out each point based on the position of its surrounding points and then
                //figure out the intersections of the pushed out points
                UV n1, n2, v1, v2, pij1, pij2, pjk1, pjk2;

                // calculates the vector between i and j
                v1 = vertices[j] - vertices[i];
                v1 = v1.Normalize();
                v1 *= offset;

                // calculates the vector between j and k
                v2 = vertices[k] - vertices[j];
                v2 = v2.Normalize();
                v2 *= offset;

                // verifies if one of the segments ij, ji, jk or kj is the unchangedLine
                double offsetBuffer = offset;
                if (unchangedLine != null)
                {
                    UV p0 = ProjectInPlaneXY(unchangedLine.GetEndPoint(0));
                    UV p1 = ProjectInPlaneXY(unchangedLine.GetEndPoint(1));
                    
                    if ((vertices[i].IsAlmostEqualTo(p0) && vertices[j].IsAlmostEqualTo(p1)) || 
                        (vertices[j].IsAlmostEqualTo(p0) && vertices[i].IsAlmostEqualTo(p1)))
                        v1 = UV.Zero;

                    if((vertices[j].IsAlmostEqualTo(p0) && vertices[k].IsAlmostEqualTo(p1)) || 
                        (vertices[k].IsAlmostEqualTo(p0) && vertices[j].IsAlmostEqualTo(p1)))
                        v2 = UV.Zero;
                }                                  

                // creates a shifted line that is parallel to the vector v1 
                n1 = new UV(-v1.V, v1.U);
                pij1 = vertices[i] + n1;
                pij2 = vertices[j] + n1;
                Line line1 = Line.CreateBound(TranformIn3D(pij1), TranformIn3D(pij2));
                line1.MakeUnbound();

                // creates a shifted line that is parallel to the vector v2
                n2 = new UV(-v2.V, v2.U);
                pjk1 = vertices[j] + n2;
                pjk2 = vertices[k] + n2;
                Line line2 = Line.CreateBound(TranformIn3D(pjk1), TranformIn3D(pjk2));
                line2.MakeUnbound();

                //see where the shifted lines 1 and 2 intersect
                SetComparisonResult comparisonRsult = line1.Intersect(line2, out IntersectionResultArray intersection);

                if(comparisonRsult == SetComparisonResult.Overlap)
                {
                    IntersectionResult result = intersection.get_Item(0);
                    UV intersection_point = ProjectInPlaneXY(result.XYZPoint);

                    //add the intersection as our adjusted vert point
                    adjusted_points.Add(new UV(intersection_point.U, intersection_point.V));
                }
                offset = offsetBuffer;
            }
            return adjusted_points;
        }

        /// <summary>
        /// Draw lines in the current view that matches the given curve array.
        /// </summary>
        /// <param name="curveArray"></param>
        private void DrawCurveArray(CurveArray curveArray)
        {
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
        /// <returns>
        /// Returns the created floor.
        /// </returns>
        public Floor CreateCeiling(string floorTypeName)
        {

            Floor ceiling = null;
            PlanCircuitSet circuitSet = GetDocPlanCircuitSet(false);

            // creates a ceiling if in a room there is more than one loop,
            // and finds the smallest loop

            CurveArray curve = GetHousePerimeter();

            // create a floor type
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FloorType));
            FloorType floorType = collector.First(y => y.Name == floorTypeName) as FloorType;

            // create the ceiling
            ceiling = doc.Create.NewFloor(curve, floorType, topLevel, false);
            ElementTransformUtils.MoveElement(doc, ceiling.Id, new XYZ(0, 0, topLevel.Elevation));

            return ceiling;
        }

        /// <summary>
        /// Calculates the middle point of a Curve.
        /// </summary>
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

        public enum RoofDesign
        {
            HiddenButterfly,
            Hip,
            Gable
        }

        /// <summary>
        /// Create a generic roof in the Top Level.
        /// </summary>
        /// <param name="overhang">
        /// The value of the overhang. The overhang will be applied in all sides.
        /// If this value is 0, the user may not pass the slopeDirection argument.
        /// </param>
        /// <param name="slopeDirection">
        /// The slope will only be apllied on the edges that is orthogonal to the Slope Direction Vector.
        /// Note tha a 0 vector is ortoghonal to all vectors.
        /// </param>
        /// <returns>
        /// Returns the created FootPrintRoof.
        /// </returns>
        public FootPrintRoof CreateRoof(double overhang, double slope, XYZ slopeDirection, RoofDesign roofDesign)
        {
            FootPrintRoof footPrintRoof = null;
            CurveArray footPrintCurve = GetHousePerimeter();

            switch (roofDesign)
            {
                case RoofDesign.Hip:
                    CreateHipRoof(footPrintCurve, overhang, slope, slopeDirection);
                    break;

                case RoofDesign.Gable:
                    CreateGableRoof(footPrintCurve, overhang, slope, slopeDirection);
                    break;

                case RoofDesign.HiddenButterfly:
                    CreateHiddenButterflyRoof(footPrintCurve, slope, slopeDirection);
                    break;       
            }
            return footPrintRoof;
        }        

        private void CreateHiddenButterflyRoof(CurveArray footPrint, double slope, XYZ slopeDirection)
        {
            List<CurveArray> convexFootPrint = GetConvexPerimeters(footPrint, slopeDirection, out List<Line> cutLines);

            if(convexFootPrint.Count == 1)      
                convexFootPrint = DivideCurveArrayInHalf(convexFootPrint[0], slopeDirection);

            foreach (CurveArray curveArray in convexFootPrint)
            {
                // get a roof type
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                collector.OfClass(typeof(RoofType));
                RoofType roofType = collector.FirstElement() as RoofType;

                // create the foot print of the roof
                ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
                FootPrintRoof footPrintRoof = doc.Create.NewFootPrintRoof(curveArray, topLevel, roofType, out footPrintToModelCurveMapping);

                // apply the slope for the cutLines
                ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
                iterator.Reset();
                while (iterator.MoveNext())
                {
                    ModelCurve modelCurve = iterator.Current as ModelCurve;
                    Curve curve = modelCurve.GeometryCurve;

                    if (VerifyIntersectionInArray(curve, cutLines))
                    {
                        footPrintRoof.set_DefinesSlope(modelCurve, true);
                        footPrintRoof.set_SlopeAngle(modelCurve, slope);
                    }
                }               
            }
            CreateParapetWall(footPrint);
        }

        private List<CurveArray> DivideCurveArrayInHalf(CurveArray curveArray, XYZ divisionDirection)
        {
            if (curveArray.Size != 4)
                return null;

            List<XYZ> newPoints = new List<XYZ>();
            foreach(Curve curve in curveArray)
            {
                if(GetCurveDirection(curve).CrossProduct(divisionDirection).IsZeroLength())
                {
                    XYZ p0 = curve.GetEndPoint(0);
                    XYZ p1 = curve.GetEndPoint(1);
                    XYZ newPoint = (p0 + p1) / 2;
                    newPoints.Add(newPoint);
                }
            }

            if (newPoints.Count != 2)
                return null;

            var points = GetPoints(curveArray);

            var node0 = FindPoint(points, ProjectInPlaneXY(newPoints[0]));
            var node1 = FindPoint(points, ProjectInPlaneXY(newPoints[1]));                    

            var newPolygon0 = CreatePolygonBetweenVertices(points, node0, node1);
            var newPolygon1 = CreatePolygonBetweenVertices(points, node1, node0);

            List<CurveArray> dividedCurveArrays = new List<CurveArray>
            {
                CreateCurveArrayFromPoints(newPolygon0),
                CreateCurveArrayFromPoints(newPolygon1)
            };

            return dividedCurveArrays;
        }

        private void CreateParapetWall(CurveArray curveArray)
        {
            foreach(Curve curve in curveArray)
            {
                XYZ curveMiddlePoint = GetCurveMiddlePoint(curve);
                Wall wall = FindHostingWall(curveMiddlePoint);
                if (wall != null)
                {
                    wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).Set(MetersToFeet(0.8));
                }
                else
                {
                    WallType wallType = GetWallType(Properties.Settings.Default.WallTypeName);
                    Wall.Create(doc, curve, wallType.Id, topLevel.Id, MetersToFeet(0.8), 0, false, false);
                }
            }                     
        }

        private void CreateHipRoof(CurveArray footPrint, double overhang, double slope, XYZ slopeDirection)
        {
            CurveArray offsetedFootPrint = CreateOffsetedCurveArray(overhang, footPrint, null);
            CreateFootPrintRoof(overhang, slope, slopeDirection, offsetedFootPrint);
        }

        private void CreateGableRoof(CurveArray footPrint, double overhang, double slope, XYZ slopeDirection)
        {
            List<FootPrintRoof> roofs = new List<FootPrintRoof>();
            List<CurveArray> convexFootPrint = GetConvexPerimeters(footPrint, slopeDirection, out List<Line> cutLines);

            int n = convexFootPrint.Count();

            // create the n convex compenents of the roof
            for (int i = 0; i < n; i++)
            {
                Line unchangedLine = i % 2 == 0 ? null : cutLines[0];
                CurveArray offsetedFootPrint = CreateOffsetedCurveArray(overhang, convexFootPrint[i], unchangedLine);
                FootPrintRoof footPrintRoof = CreateFootPrintRoof(overhang, slope, slopeDirection, offsetedFootPrint);
                CreateAllGableWalls(slopeDirection, slope, convexFootPrint[i]);
                roofs.Add(footPrintRoof);
            }

            // connect the components if possible
            for (int i = 0; i < n - 1; i++)
            {
                try
                {
                    JoinGeometryUtils.JoinGeometry(doc, roofs[i], roofs[i + 1]);
                }
                catch (Exception) { continue; }
            }
        }

        private bool VerifyIntersectionInArray(Curve curve, List<Line> lines)
        {
            Transform transform = Transform.CreateTranslation( new XYZ(0, 0, -curve.GetEndPoint(0).Z));
            curve = curve.CreateTransformed(transform);
            foreach(Line line in lines)
            {
                // verifiy is the line is equal or a subset of the curve
                var result = line.Intersect(curve);
                if(result == SetComparisonResult.Equal ||
                    result == SetComparisonResult.Subset)
                {
                    return true;
                }
                // verify if the line intersects and is parallel
                if (result == SetComparisonResult.Overlap && (line.Direction.CrossProduct(GetCurveDirection(curve)).IsZeroLength()))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Creates a RoofFootPrint.
        /// </summary>
        /// <param name="overhang">The overhang value.</param>
        /// <param name="slope">The slope of the roof.</param>
        /// <param name="slopeDirection">The direction of the slope.</param>
        /// <param name="footPrint">The footprint.</param>
        /// <returns>
        /// Returns a reference of the roof generated.
        /// </returns>
        private FootPrintRoof CreateFootPrintRoof(double overhang, double slope, XYZ slopeDirection, CurveArray footPrint)
        {

            // get a roof type
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(RoofType));
            RoofType roofType = collector.FirstElement() as RoofType;

            // create the foot print of the roof
            ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
            FootPrintRoof footPrintRoof = doc.Create.NewFootPrintRoof(footPrint, topLevel, roofType, out footPrintToModelCurveMapping);

            // create the slope
            ApplySlope(overhang, slope, slopeDirection, footPrintRoof, footPrintToModelCurveMapping);              

            return footPrintRoof;
        }

        /// <summary>
        /// Aplies the slope in a determined FootPrintRoof.
        /// </summary>
        /// <param name="overhang">The overhang value.</param>
        /// <param name="slope">The slope of the roof</param>
        /// <param name="slopeDirection">The vector that represents the directions that the slope should be applied.</param>
        /// <param name="footPrintRoof">The Roof</param>
        /// <param name="footPrintToModelCurveMapping">The ModelCurveArray generated with the roof instance.</param>
        private static void ApplySlope(double overhang, double slope, XYZ slopeDirection, FootPrintRoof footPrintRoof, ModelCurveArray footPrintToModelCurveMapping)
        {
            ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
            iterator.Reset();

            while (iterator.MoveNext())
            {
                ModelCurve modelCurve = iterator.Current as ModelCurve;
                Curve curve = modelCurve.GeometryCurve;
                XYZ curveDirection = GetCurveDirection(curve);

                if (curveDirection.DotProduct(slopeDirection) == 0)
                {
                    footPrintRoof.set_DefinesSlope(modelCurve, true);
                    footPrintRoof.set_SlopeAngle(modelCurve, slope);
                }

                double elevation = -(overhang - MetersToFeet(0.1)) / 3;
                footPrintRoof.set_Offset(modelCurve, elevation);
            }
        }

        /// <summary>
        /// Calculates the vector that is formed by the start point and the end point of the curve.
        /// </summary>
        /// <param name="curve"></param>
        /// <returns>
        /// Returns the vector.
        /// </returns>
        private static XYZ GetCurveDirection(Curve curve)
        {
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);
            XYZ curveDirection = new XYZ(startPoint.X - endPoint.X, startPoint.Y - endPoint.Y, 0);
            return curveDirection;
        }

        /// <summary>
        /// Classify the rooms of a project based on the elements inside it.
        /// </summary>
        public void ClassifyRooms()
        {
            string jsonElementClassifier = Properties.Resources.ElementClassifierConfig;
            List<RoomClassifier> deserializedRoomClassifier = JsonConvert.DeserializeObject<List<RoomClassifier>>(jsonElementClassifier);
            List<Room> rooms = GetRoomsAtLevel(baseLevel).ToList();
            foreach (Room room in rooms)
            {
                if (room.Area > 0)
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
                    if (roomName != null)
                    {
                        room.Name = roomName;
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
                if (instance.Room != null)
                {
                    if (instance.Room.Id.IntegerValue.Equals(roomid))
                    {
                        elementsInsideTheRoom.Add(instance);
                    }
                }
            }
            return elementsInsideTheRoom;
        }

        /// <summary>
        /// Generate a new sheet of the Base Level.
        /// </summary>
        public void CreateNewSheet()
        {
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
        }

        /// <summary>
        /// Create the gables walls of a building.
        /// </summary>
        /// <param name="vectorDirection">
        /// The gable walls will be construct at the walls that its normal 
        /// vector is parallel with the vectorDirection.
        /// </param>
        /// <param name="slope">
        /// The slope of the gable, must match the slope of the roof.
        /// </param>
        public void CreateAllGableWalls(XYZ vectorDirection, double slope, CurveArray perimeter)
        {
            CurveArray normalizedCurve = CreateOffsetedCurveArray(0, perimeter, null);
            foreach (Curve line in normalizedCurve)
            {
                XYZ lineDirection = GetCurveDirection(line);
                if (lineDirection.CrossProduct(vectorDirection).IsZeroLength())
                {
                    CreateGableWall(line, slope);
                }
            }
        }

        /// <summary>
        /// Create a single gable wall.
        /// </summary>
        /// <param name="line">
        /// The base line of the gable wall.
        /// </param>
        /// <param name="slope">
        /// The slope of this gable wall.
        /// </param>
        /// <returns></returns>
        private Wall CreateGableWall(Curve line, double slope)
        {

            // create the gable wall profile
            XYZ p0 = line.GetEndPoint(0);
            XYZ p1 = line.GetEndPoint(1);
            XYZ p2 = FormTriangle(slope, p0, p1);
            IList<Curve> profile = new List<Curve>(3);
            profile.Add(Line.CreateBound(p0, p1));
            profile.Add(Line.CreateBound(p1, p2));
            profile.Add(Line.CreateBound(p2, p0));

            // get the wall type
            WallType type = GetWallType("parede 15 cm - branca");

            // create the gable wall
            return Wall.Create(doc, profile, type.Id, topLevel.Id, false);
        }

        /// <summary>
        /// Calculate the third point to form a triangle that obein the expression: tan(slope) = 2 * height / base.
        /// </summary>
        private static XYZ FormTriangle(double slope, XYZ p0, XYZ p1)
        {
            double p2x = (p0.X + p1.X) / 2;
            double p2y = (p0.Y + p1.Y) / 2;
            XYZ baseVector = p1.Subtract(p0);
            double p2z = (slope * baseVector.GetLength()) / 2;
            return new XYZ(p2x, p2y, p2z);
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
        [Obsolete("This Method is Deprecated. Use OffsetPolygon", true)]
        private Curve CreateOffsetedCurve(CurveArray housePerimeter, Curve curve, double offset, XYZ offsetVector)
        {
            // finds the middle point of the current curve
            XYZ middlePoint = GetCurveMiddlePoint(curve);

            // expand the curve
            curve.MakeBound(curve.GetEndParameter(0) - offset, curve.GetEndParameter(1) + offset);

            // finds the wall below that curve, and retrives its normal vector
            Wall wall = FindHostingWall(middlePoint);
            if (wall != null)
            {
                XYZ wallNormalVector = wall.Orientation;

                // Makes sure that the exterior of the wall is pointed to the exterior of the house
                XYZ roomPoint = middlePoint + wallNormalVector;
                Room room = doc.GetRoomAtPoint(roomPoint);
                if (room.Name != "Exterior 0")
                {
                    wall.Flip();
                    wallNormalVector = wall.Orientation;
                }

                if (wallNormalVector.CrossProduct(offsetVector).IsZeroLength())
                {
                    // aplies the offset
                    wallNormalVector = wallNormalVector * offset;
                    Transform transform = Transform.CreateTranslation(wallNormalVector);
                    curve = curve.CreateTransformed(transform);
                }
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
        [Obsolete("This Method is Deprecated")]
        private static void RemoveCurveOverlap(Curve curve, XYZ intersectionPoint)
        {
            double distanceToStart = curve.GetEndPoint(0).DistanceTo(intersectionPoint);
            double distanceToEnd = curve.GetEndPoint(1).DistanceTo(intersectionPoint);

            // In the case the start point is closer
            if (distanceToStart < distanceToEnd)
            {
                curve.MakeBound(curve.GetEndParameter(0) + distanceToStart, curve.GetEndParameter(1));
            }
            // In the case the end point is closer
            else
            {
                curve.MakeBound(curve.GetEndParameter(0), curve.GetEndParameter(1) - distanceToEnd);
            }
        }

        class ClockWisePointComparer : IComparer
        {
            public static UV centroid;

            [Obsolete("This Method is Deprecated")]
            public int Compare(object x, object y)
            {
                UV a = x as UV;
                UV b = y as UV;

                if (a.U - centroid.U >= 0 && b.U - centroid.U < 0)
                    return -1;
                if (a.U - centroid.U < 0 && b.U - centroid.U >= 0)
                    return 1;
                if (a.U - centroid.U == 0 && b.U - centroid.U == 0)
                {
                    if (a.V - centroid.V >= 0 || b.V - centroid.V >= 0)
                        return Convert.ToInt32(a.V - b.V);
                    return Convert.ToInt32(b.V - a.V);
                }

                // compute the cross product of vectors (center -> a) x (center -> b)
                double det = (a.U - centroid.U) * (b.V - centroid.V) - (b.U - centroid.U) * (a.V - centroid.V);
                if (det < 0)
                    return 1;
                if (det > 0)
                    return -1;

                // points a and b are on the same line from the center
                // check which point is closer to the center
                double d1 = (a.U - centroid.U) * (a.U - centroid.U) + (a.V - centroid.V) * (a.V - centroid.V);
                double d2 = (b.U - centroid.U) * (b.U - centroid.U) + (b.V - centroid.V) * (b.V - centroid.V);
                return Convert.ToInt32(d1 - d2);

            }

            [Obsolete("This Method is Deprecated")]
            public static UV Compute2DPolygonCentroid(LinkedList<UV> points)
            {
                centroid = new UV(0, 0);
                int vertexCount = points.Count();
                UV[] vertices = new UV[vertexCount];
                points.CopyTo(vertices, 0);
                double signedArea = 0.0;
                double u0 = 0.0; // Current vertex X
                double v0 = 0.0; // Current vertex Y
                double u1 = 0.0; // Next vertex X
                double v1 = 0.0; // Next vertex Y
                double a = 0.0;  // Partial signed area

                // For all vertices
                for (int i = 0; i < vertexCount; i++)
                {
                    u0 = vertices[i].U;
                    v0 = vertices[i].V;
                    u1 = vertices[(i + 1) % vertexCount].U;
                    v1 = vertices[(i + 1) % vertexCount].V;
                    a = u0 * v1 - u1 * v0;
                    signedArea += a;
                    double tempU = (u0 + u1) * a;
                    double tempV = (v0 + v1) * a;
                    centroid.Add(new UV(tempU, tempV));
                }

                signedArea *= 0.5;
                double newU = centroid.U / (6.0 * signedArea);
                double newV = centroid.V / (6.0 * signedArea);
                centroid = new UV(newU, newV);

                return centroid;
            }
        }
    }
}
