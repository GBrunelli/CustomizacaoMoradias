using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.IFC;
using Autodesk.Revit.UI;
using CustomizacaoMoradias.DataModel;
using CustomizacaoMoradias.Source;
using CustomizacaoMoradias.Source.Util;
using Newtonsoft.Json;
using View = Autodesk.Revit.DB.View;

namespace CustomizacaoMoradias
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class ElementPlacer
    {
        private readonly DataAccess db = new DataAccess();

        private Document doc;
        private UIDocument uidoc;

        private Level baseLevel;
        private Level topLevel;

        private Thread scoreThread;
        private List<ScoreDM> roomElementsScore;

        private Thread roomThread;
        private List<RoomDM> roomNames;

        private Thread elementThread;
        private List<ElementDM> elements;

        private double scale;

        private PlanCircuitSet Circuits
        {
            get { return doc.get_PlanTopology(baseLevel).Circuits; }
        }

        private IList<Room> Rooms
        {
            get
            {
                List<Room> rooms = new List<Room>();      
                List<UV> roomPoints = new List<UV>();
                foreach (PlanCircuit circuit in Circuits)
                {
                    roomPoints.Add(circuit.GetPointInside());
                }

                foreach(UV point in roomPoints)
                {
                    XYZ point3D = new XYZ(point.U, point.V, baseLevel.Elevation);
                    Room room = doc.GetRoomAtPoint(point3D);
                    if(room == null)
                    {
                        Room newRoom = doc.Create.NewRoom(baseLevel, point);
                        rooms.Add(newRoom);
                        doc.Create.NewRoomTag(new LinkElementId(newRoom.Id), point, GetBaseLevelView().Id);
                    }
                    else
                    {
                        rooms.Add(room);
                    }
                }
                return rooms;
            }
        }


        public enum RoofDesign
        {
            HiddenButterfly,
            Hip,
            Gable
        }

        /// <summary>
        /// Default contructor.
        /// </summary>
        public ElementPlacer(UIDocument uidoc, string baseLevel, string topLevel, double scale)
        {
            SetProperties(uidoc, baseLevel, topLevel, scale);
            ThreadInit();
        }

        public ElementPlacer()
        {
            ThreadInit();
        }

        public void SetProperties(UIDocument uidoc, string baseLevel, string topLevel, double scale)
        {
            this.uidoc = uidoc;
            this.doc = uidoc.Document;
            this.baseLevel = GetLevelFromName(baseLevel);
            this.topLevel = GetLevelFromName(topLevel);
            this.scale = scale;
        }

        public void ThreadInit()
        {
            scoreThread = new Thread(new ThreadStart(ScoreWorker)) { Name = "Score Thread" };
            scoreThread.Start();

            roomThread = new Thread(new ThreadStart(RoomWorker)) { Name = "Room Thread" };
            roomThread.Start();

            elementThread = new Thread(new ThreadStart(ElementWorker)) { Name = "Element Thread" };
            elementThread.Start();
        }       

        public void ScoreWorker() { roomElementsScore = db.GetRoomElementsScore(); }

        public void RoomWorker() { roomNames = db.GetRooms(); }

        public void ElementWorker() { elements = db.GetElement(); }

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
            {
                return openFileDialog.FileName;
            }

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
            FamilySymbol symbol = (from familySymbol in new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).
                 Cast<FamilySymbol>() where (familySymbol.Name == fsFamilyName) select familySymbol).First();
            return symbol;
        }


        private View GetBaseLevelView()
        {
            View view = (from v in new FilteredElementCollector(doc).OfClass(typeof(View))
                         .Cast<View>() where (v.Name == baseLevel.Name) select v).First();
            return view;
        }

        /// <summary>
        /// Convert a Coordinate to an XYZ object.
        /// </summary>
        private XYZ GetXYZFromProperties(Coordinate coords)
        {
            // Convert the values from the csv file
            double x0 = coords.X;
            x0 = UnitUtils.ConvertToInternalUnits(x0 * scale, UnitTypeId.Meters);

            double y0 = coords.Y;
            y0 = UnitUtils.ConvertToInternalUnits(y0 * scale, UnitTypeId.Meters);

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
        private Wall FindHostingWall(XYZ xyz, Level level)
        {         
            if (xyz is null)
            {
                throw new ArgumentNullException(nameof(xyz));
            }
            
            //xyz = xyz.Subtract(new XYZ(0, 0, xyz.Z));
            xyz = new XYZ(xyz.X, xyz.Y, level.Elevation);

            List<Wall> walls = GetWalls(level);

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
            if (distance < UnitUtils.ConvertToInternalUnits(scale + 0.2, UnitTypeId.Meters))
            {
                return wall;
            }
            return null;
        }

        /// <summary>
        /// Get all walls in the document.
        /// </summary>
        private List<Wall> GetWalls(Level level)
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
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            string jsonText = File.ReadAllText(path);
            ElementDeserializer ed = JsonConvert.DeserializeObject<ElementDeserializer>(jsonText);
            string errorMessage = "";
            foreach (WallProperty wall in ed.WallProperties)
            {
                try { CreateWall(wall, Properties.Settings.Default.WallTypeName); }
                catch { errorMessage += $"Parede {wall}, "; }      
            }
            foreach (WindowProperty window in ed.WindowProperties)
            {
                try { CreateWindow(window); }
                catch { errorMessage += $"{window.Type}, "; }
            }
            foreach (DoorProperty door in ed.DoorProperties)
            {
                try { CreateDoor(door); }
                catch { errorMessage += $"{door.Type}, "; }                    
            }
            foreach (HostedProperty element in ed.HostedProperties)
            {
                try { CreateHostedElement(element); }
                catch { errorMessage += $"{element.Type}, "; }
                
            }
            foreach (FurnitureProperty element in ed.FurnitureProperties)
            {
                try { CreateFurniture(element); }
                catch { errorMessage += $"{element.Type}, "; }
            }

            if (errorMessage.EndsWith(", "))
            {
                errorMessage = errorMessage.Remove(errorMessage.Length - 2, 2);
                MessageBox.Show($"Erro ao inserir elementos: \n{errorMessage}.", "Atenção", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private UV RotateVector(UV vector, double rotation)
        {
            double u2 = Math.Cos(rotation) * vector.U - Math.Sin(rotation) * vector.V;
            double v2 = Math.Sin(rotation) * vector.U + Math.Cos(rotation) * vector.V;
            return new UV(u2, v2);
        }
      
        /// <summary>
        /// Creates a piece of furniture.
        /// </summary>
        /// <param name="properties"> Properties of the object.</param>
        private FamilyInstance CreateFurniture(FurnitureProperty properties)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            // get the properties
            double rotation = DeegreToRadians(properties.Rotation);
            XYZ p0 = GetXYZFromProperties(properties.Coordinate.ElementAt(0));
            XYZ p1 = GetXYZFromProperties(properties.Coordinate.ElementAt(1));
            XYZ point = p0.Add(p1).Divide(2);
            string fsFamilyName = GetFamilySymbolName(properties.Type);
            UV offset = GetFamilyOffset(properties.Type);
            offset = RotateVector(offset, rotation);

            // Creates a point above the furniture to serve as a rotation axis
            XYZ axisPoint = new XYZ(point.X, point.Y, baseLevel.Elevation + 1);
            Line axis = Line.CreateBound(point, axisPoint);
            FamilyInstance furniture;
            try
            {
                FamilySymbol familySymbol = GetFamilySymbol(doc, fsFamilyName);

                Autodesk.Revit.DB.Structure.StructuralType structuralType = Autodesk.Revit.DB.Structure.StructuralType.NonStructural;
                furniture = doc.Create.NewFamilyInstance(point, familySymbol, structuralType);
                ElementTransformUtils.RotateElement(doc, furniture.Id, axis, rotation);            
                ElementTransformUtils.MoveElement(doc, furniture.Id, TransformUVinXYZ(offset));
            }
            catch (Exception e)
            {
                throw new Exception("Erro ao inserir mobiliario \"" + fsFamilyName + "\".", e);
            }
            return furniture;
        }

        private UV GetFamilyOffset(string type)
        {
            elementThread.Join();
            foreach (ElementDM e in elements)
            {
                if (e.ElementID.Trim().Equals(type))
                    return new UV(e.OffsetX, e.OffsetY);
            }
            return null;
        }

        /// <summary>
        /// Creates a wall given its properties.
        /// </summary>
        /// <param name="properties"> Properties of the object.</param>
        private Wall CreateWall(WallProperty properties, string wallTypeName)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            XYZ p0 = GetXYZFromProperties(properties.Coordinate.ElementAt(0));
            XYZ p1 = GetXYZFromProperties(properties.Coordinate.ElementAt(1));
            Wall wall = null;
            try
            {
                Curve curve = Line.CreateBound(p0, p1);
                // sellect wall type
                WallType wallType = GetWallType(wallTypeName);

                // Creating the wall
                double height = UnitUtils.ConvertToInternalUnits(2.8, UnitTypeId.Meters);
                wall = Wall.Create(doc, curve, wallType.Id, baseLevel.Id, height, 0, false, false);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(topLevel.Id);
                wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(UnitUtils.ConvertToInternalUnits(-0.1, UnitTypeId.Meters));

            }
            catch (Exception e)
            {
                throw new Exception($"Erro ao inserir parede {properties}.", e);
            }
            return wall;
        }

        /// <summary>
        /// Create a hosted element on a wall.
        /// </summary>
        /// <param name="properties"> Properties of the object.</param>
        private FamilyInstance CreateHostedElement(HostedProperty properties)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            XYZ point = GetXYZFromProperties(properties.Coordinate);
            string fsName = GetFamilySymbolName(properties.Type);
            double rotation = properties.Rotation;
            UV offset = GetFamilyOffset(properties.Type);
            offset = RotateVector(offset, rotation);
            point += TransformUVinXYZ(offset);

            FamilyInstance instance;

            FamilySymbol familySymbol = GetFamilySymbol(doc, fsName);
            Wall wall = FindHostingWall(point, baseLevel);            

            // Creates the element                   
            instance = doc.Create.NewFamilyInstance(point, familySymbol, wall, baseLevel, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

            // TODO: flip
                
            return instance;
        }

        /// <summary>
        /// Get the FamilySymbol from the settings.
        /// </summary>
        /// <param name="familyType"></param>
        private string GetFamilySymbolName(string familyType)
        {
            elementThread.Join();
            foreach (ElementDM e in elements)
            {
                if (e.ElementID.Trim().Equals(familyType))
                    return e.Name;
            }
            return null;
        }

        /// <summary>
        /// Creates a door given its properties.
        /// </summary>
        /// <param name="properties"> Properties of the object.</param>
        private FamilyInstance CreateDoor(DoorProperty properties)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            HostedProperty hp = ConvertToHosted(properties);
            return CreateHostedElement(hp);
        }

        /// <summary>
        /// Creates a windows given its properties.
        /// </summary>
        /// <param name="properties"> Properties of the object.</param>
        private FamilyInstance CreateWindow(WindowProperty properties)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            HostedProperty hp = ConvertToHosted(properties);
            FamilyInstance window = CreateHostedElement(hp);

            if (window != null)
            {
                window.get_Parameter(BuiltInParameter.INSTANCE_HEAD_HEIGHT_PARAM).Set(UnitUtils.ConvertToInternalUnits(2, UnitTypeId.Meters));
            }

            return window;
        }

        /// <summary>
        /// Converts an Hosted object in a HostedProperty.
        /// </summary>
        private static HostedProperty ConvertToHosted(IHosted obj)
        {
            Coordinate c = obj.Coordinate;
            HostedProperty hp = new HostedProperty()
            {
                Coordinate = c,
                Type = obj.Type
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

        // 
        private IList<IList<BoundarySegment>> GetRoomLoops(Room room)
        {
            SpatialElementBoundaryOptions opt = new SpatialElementBoundaryOptions
            { SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Center };
            return  room.GetBoundarySegments(opt);
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
            // get the floorType
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(FloorType));
            FloorType floorType = collector.First(y => y.Name == floorTypeName) as FloorType;

            foreach (Room room in Rooms)
            {
                var loops = GetRoomLoops(room);
                if((loops != null) && (loops.Count == 1))
                {
                    CurveArray curve = BoundarySegmentToCurveArray(loops.First());
                    doc.Create.NewFloor(curve, floorType, baseLevel, false);
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
        private CircularLinkedList<UV> GetPoints(CurveArray curveArray)
        {
            CircularLinkedList<UV> points = new CircularLinkedList<UV>();
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
        private List<UV> GetNotches(CircularLinkedList<UV> points)
        {
            List<UV> notches = new List<UV>();
            if(points.Count < 5) return notches;

            CircularLinkedListNode<UV> node = points.Head;
            do
            {
                UV p0 = node.Value;
                UV p1 = node.Next.Value;
                UV p2 = node.Next.Next.Value;
                double angle = CalculatesAngle(p0, p1, p2);
                if (angle > Math.PI)           
                    notches.Add(p1);
                node = node.Next;
            } while (node != points.Head);

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
        public List<CurveArray> GetConvexPerimeters(CurveArray curveArray, XYZ preferredOrientation, List<Line> cutLines)
        {
            CircularLinkedList<UV> points = GetPoints(curveArray);
            List<UV> notches = GetNotches(points);
            List<CurveArray> perimeters = new List<CurveArray>();
            if (notches.Count == 0)
            {
                perimeters.Add(curveArray);
                return perimeters;
            }

            List<CurveArray> result = EliminateNotch(notches[0], curveArray, points, preferredOrientation, out Line line);
            cutLines.Add(line);
            foreach (CurveArray array in result)
            {
                perimeters.AddRange(GetConvexPerimeters(array, preferredOrientation, cutLines));
            }

            perimeters.Sort(new SortingDescendingArea());
            return perimeters;
        }

        /// <summary>
        /// Class to sort CurveArray by the area determinated by the curves of the array.
        /// </summary>
        private class SortingDescendingArea : IComparer<CurveArray>
        {
            public int Compare(CurveArray x, CurveArray y)
            {
                IList<CurveLoop> curveLoopX = new List<CurveLoop> { CurveArrayToCurveLoop(x) };
                double areaX = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoopX);

                IList<CurveLoop> curveLoopY = new List<CurveLoop> { CurveArrayToCurveLoop(y) };
                double areaY = ExporterIFCUtils.ComputeAreaOfCurveLoops(curveLoopY);

                if (areaX < areaY)
                {
                    return 1;
                }

                if (areaX > areaY)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }
        }

        private bool SearchForUVInList(List<UV> list, UV key)
        {
            foreach(UV uv in list)
            {
                if(uv.IsAlmostEqualTo(key))
                {
                    return true;
                }
            }
            return false;
        }
       
        private bool PosibleCurve(Curve curve, CircularLinkedListNode<UV> pointNode)
        {
            // the curve cannot contain the 2 points after or before or the pointNode itself
            List<UV> forbiddenPoints = new List<UV>
            {
                pointNode.Value,
                pointNode.Previous.Value,
                pointNode.Previous.Previous.Value,
                pointNode.Next.Value,
                pointNode.Next.Next.Value
            };
            UV p0 = ProjectInPlaneXY(curve.GetEndPoint(0));
            UV p1 = ProjectInPlaneXY(curve.GetEndPoint(1));
            

            return !(SearchForUVInList(forbiddenPoints, p0) && SearchForUVInList(forbiddenPoints, p1));         
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
        private List<CurveArray> EliminateNotch(UV notch, CurveArray curveArray, CircularLinkedList<UV> points, XYZ preferredOrientation, out Line cutLine)
        {
            XYZ notche3D = TransformUVinXYZ(notch);
            Line line1 = Line.CreateUnbound(notche3D, preferredOrientation);

            XYZ otherOrientation = new XYZ(preferredOrientation.Y, preferredOrientation.X, 0);
            Line line2 = Line.CreateUnbound(notche3D, otherOrientation);

            CircularLinkedListNode<UV> notchNode = FindPoint(points, notch);

            // get the posible curves for the new point
            CurveArray posibleCurves = new CurveArray();
            foreach (Curve curve in curveArray)
            {
                if (PosibleCurve(curve, notchNode))
                {
                    posibleCurves.Append(curve);
                }
            }

            // iterate for each possible curve, and if
            // a intersection is found, the point will 
            // added in the linked list
            CircularLinkedListNode<UV> newNode;
            newNode = FindNewNode(ref points, line1, posibleCurves, notch);

            if (newNode == null)
            {
                newNode = FindNewNode(ref points, line2, posibleCurves, notch);
            }

            // generates the 2 new polygons    
            CircularLinkedList<UV> polygonA = CreatePolygonBetweenVertices(newNode, notchNode);
            CircularLinkedList<UV> polygonB = CreatePolygonBetweenVertices(notchNode, newNode);

            // creates the curves
            List<CurveArray> list = new List<CurveArray>
            {
                CreateCurveArrayFromPoints(polygonA),
                CreateCurveArrayFromPoints(polygonB)
            };

            // returns the cutLine
            cutLine = Line.CreateBound(notche3D, TransformUVinXYZ(newNode.Value));

            return list;
        }

        /// <summary>
        /// Finds a point that
        /// </summary>
        /// <param name="points"></param>
        /// <param name="line1"></param>
        /// <param name="posibleCurves"></param>
        /// <returns></returns>
        private static CircularLinkedListNode<UV> FindNewNode(ref CircularLinkedList<UV> points, Line line, CurveArray posibleCurves, UV notch)
        {
            // iterate for each possible curve, and if
            // a intersection is found, the point will 
            // be added in the linked list
            CircularLinkedListNode<UV> newNode = null;

            // get the closest point
            UV newPoint = null, previousPoint = null;
            double minDistance = double.MaxValue;
            foreach (Curve curve in posibleCurves)
            {
                SetComparisonResult intersection = curve.Intersect(line, out IntersectionResultArray resultArray);
                if (intersection == SetComparisonResult.Overlap)
                {
                    IntersectionResultArrayIterator iterator = resultArray.ForwardIterator();
                    iterator.Reset();
                    while (iterator.MoveNext())
                    {
                        IntersectionResult result = iterator.Current as IntersectionResult;
                        UV point = ProjectInPlaneXY(result.XYZPoint);
                        double distance = point.DistanceTo(notch);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            newPoint = point;
                            previousPoint = ProjectInPlaneXY(curve.GetEndPoint(0));
                        }
                    }
                }
            }

            // insert the new point in the list
            var node = FindPoint(points, previousPoint);
            newNode = points.AddAfter(node, newPoint);
            if (newNode.Next.Value.IsAlmostEqualTo(newNode.Value))
                points.Remove(newNode.Next.Value);
            else if (newNode.Previous.Value.IsAlmostEqualTo(newNode.Value))
                points.Remove(newNode.Previous.Value);

            return newNode;
        }

        /// <summary>
        /// Add a new vertice in order in the list of vertices of the polygon given the IntersectionResultArray.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="resultArray"></param>
        /// <param name="curve"></param>
        private static CircularLinkedListNode<UV> AddPointsInList(CircularLinkedList<UV> points, IntersectionResultArray resultArray, Curve curve)
        {
            UV p0 = ProjectInPlaneXY(curve.GetEndPoint(0));
            CircularLinkedListNode<UV> newNode = null;
            CircularLinkedListNode<UV> node = FindPoint(points, p0);

            IntersectionResultArrayIterator iterator = resultArray.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
                IntersectionResult result = iterator.Current as IntersectionResult;
                UV intersectionPoint = ProjectInPlaneXY(result.XYZPoint);
                newNode = points.AddAfter(node, intersectionPoint);
            }
            if (newNode.Next.Value.IsAlmostEqualTo(newNode.Value))
                points.Remove(newNode.Next.Value);
            else if (newNode.Previous.Value.IsAlmostEqualTo(newNode.Value))
                points.Remove(newNode.Previous.Value);

            return newNode;
        }

        /// <summary>
        /// Create a CurveArray given its vertices.
        /// </summary>
        private CurveArray CreateCurveArrayFromPoints(CircularLinkedList<UV> points)
        {
            CurveArray curveArray = new CurveArray();
            CircularLinkedListNode<UV> node = points.Head;
            Line line;
            do {
                // for the cases that the 2 lines are colinear
                if (!node.Value.IsAlmostEqualTo(node.Next.Value))
                {
                    line = Line.CreateBound(TransformUVinXYZ(node.Value), TransformUVinXYZ(node.Next.Value));
                    curveArray.Append(line);
                }
                node = node.Next;
            } while (node != points.Head);
            return curveArray;
        }

        /// <summary>
        /// Create a sub group of the linkedList that starts with node 'first' and ends with node 'last'.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="last"></param>
        /// <returns>
        /// Returns the vertives of the new polygon.
        /// </returns>
        private CircularLinkedList<UV> CreatePolygonBetweenVertices(CircularLinkedListNode<UV> first, CircularLinkedListNode<UV> last)
        {
            CircularLinkedList<UV> polygon = new CircularLinkedList<UV>();
            CircularLinkedListNode<UV> node = first;
            do
            {
                polygon.AddLast(node.Value);
                node = node.Next;
            } while (node != last);
            polygon.AddLast(node.Value);
            return polygon;
        }      

        /// <summary>
        /// Searches for a point in the LinkedList
        /// </summary>
        /// <returns>
        /// Returns the node.
        /// </returns>
        private static CircularLinkedListNode<UV> FindPoint(CircularLinkedList<UV> points, UV key)
        {
            CircularLinkedListNode<UV> node = points.Head;
            do
            {
                UV point = node.Value;
                if(point.IsAlmostEqualTo(key, 0.01))
                    return node;
                node = node.Next;
            } while (node != points.Head);
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
        private static XYZ TransformUVinXYZ(UV uv)
        {
            return new XYZ(uv.U, uv.V, 0);
        }

        /// <summary>
        /// Calculates a CurveArray that corresponds the perimeter of a building given all its internal loops. 
        /// The building MUST be surround by walls.
        /// </summary>
        /// <returns>
        /// Returns a CurveArray that corresponds to the house perimeter.
        /// </returns>
        public CurveArray GetHousePerimeter()
        {
            foreach (Room room in Rooms)
            {
                // if there more than 1 loop, that means that this circuit represents the external area
                var loops = GetRoomLoops(room);
                if(loops.Count > 1)
                {
                    // first of all we find the closed loop with the smaller area
                    double minArea = double.MaxValue;
                    IList<BoundarySegment> perimeterSegments = null;
                    foreach (IList<BoundarySegment> singleLoop in loops)
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
        public CurveArray CreateOffsetedCurveArray(double offset, CurveArray curveArray, List<Line> unchangedLines)
        {
            if (offset < 0 || curveArray.Size < 3)
            {
                return null;
            }

            NormalizeCurveArray(ref curveArray);
            CircularLinkedList<UV> points = GetPoints(curveArray);
            CircularLinkedList<UV> offsetedPoints = OffsetPolygon(points, offset, unchangedLines);

            CircularLinkedList<UV> linkedOffsetedPoints = new CircularLinkedList<UV>(offsetedPoints);
            CurveArray offsetedCurveArray = CreateCurveArrayFromPoints(linkedOffsetedPoints);

            return offsetedCurveArray;
        }

        /// <summary>
        /// Offset a polygon in all directions, except the edge of this polygon that is equal to the unchangedLine parameter.
        /// </summary>
        /// <param name="vertices">A list of 2D coordinates that represents the vertices of the polygon. The list must be ordered counter-clockwise</param>
        /// <param name="offset">The offset value.</param>
        /// <param name="unchangedLine">If an edge of the polygon is collinear to this line, that edge will remain unchanged.</param>
        /// <returns>Returns a List of 2D coordinates that represents the offseted polygon.</returns>
        private static CircularLinkedList<UV> OffsetPolygon(CircularLinkedList<UV> vertices, double offset, List<Line> unchangedLines)
        {
            if (offset == 0) return vertices;

            CircularLinkedList<UV> adjusted_points = new CircularLinkedList<UV>();
            CircularLinkedListNode<UV> node = vertices.Head;
            do
            {
                //find the points before and after our target point.
                var vertexI = node.Previous.Value;
                var vertexJ = node.Value;
                var vertexK = node.Next.Value;

                //the next step is to push out each point based on the position of its surrounding points and then
                //figure out the intersections of the pushed out points             
                UV v1 = vertexJ - vertexI;
                UV v2 = vertexK - vertexJ;

                // verifies if one of the segments ij, ji, jk or kj is the unchangedLine
                if(unchangedLines != null)
                {
                    foreach(Line l in unchangedLines)
                    {
                        UV p0 = ProjectInPlaneXY(l.GetEndPoint(0));
                        UV p1 = ProjectInPlaneXY(l.GetEndPoint(1));

                        if ((vertexI.IsAlmostEqualTo(p0) && vertexJ.IsAlmostEqualTo(p1)) ||
                            (vertexJ.IsAlmostEqualTo(p0) && vertexI.IsAlmostEqualTo(p1)))
                        {
                            v1 = UV.Zero;
                            break;
                        }
                        if ((vertexJ.IsAlmostEqualTo(p0) && vertexK.IsAlmostEqualTo(p1)) || 
                            (vertexK.IsAlmostEqualTo(p0) && vertexJ.IsAlmostEqualTo(p1)))
                        {
                            v2 = UV.Zero;
                            break;
                        }
                    }
                }

                v1 = v1.Normalize() * offset;
                v2 = v2.Normalize() * offset;

                // creates a shifted line that is parallel to the vector v1 
                UV n1 = new UV(-v1.V, v1.U);
                UV pij1 = vertexI + n1;
                UV pij2 = vertexJ + n1;
                Line line1 = Line.CreateBound(TransformUVinXYZ(pij1), TransformUVinXYZ(pij2));
                line1.MakeUnbound();

                // creates a shifted line that is parallel to the vector v2
                UV n2 = new UV(-v2.V, v2.U);
                UV pjk1 = vertexJ + n2;
                UV pjk2 = vertexK + n2;
                Line line2 = Line.CreateBound(TransformUVinXYZ(pjk1), TransformUVinXYZ(pjk2));
                line2.MakeUnbound();

                //see where the shifted lines 1 and 2 intersect
                SetComparisonResult comparisonResult = line1.Intersect(line2, out IntersectionResultArray intersection);

                if (comparisonResult == SetComparisonResult.Overlap)
                {
                    IntersectionResult result = intersection.get_Item(0);
                    UV intersection_point = ProjectInPlaneXY(result.XYZPoint);

                    //add the intersection as our adjusted vert point
                    adjusted_points.AddLast(new UV(intersection_point.U, intersection_point.V));
                }

                node = node.Next;
            } while (node != vertices.Head);
            return adjusted_points;
        }

        /// <summary>
        /// Draw lines in the current view that matches the given curve array.
        /// </summary>
        /// <param name="curveArray"></param>
        private void DrawCurveArray(CurveArray curveArray)
        {
            if (curveArray is null) return;
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
            CurveArray curve = GetHousePerimeter();
            NormalizeCurveArray(ref curve);

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
            if (curve is null)
            {
                throw new ArgumentNullException(nameof(curve));
            }

            XYZ curveStartPoint = curve.GetEndPoint(0);
            XYZ curveEndPoint = curve.GetEndPoint(1);

            double cordX, cordY, cordZ;

            cordX = (curveStartPoint.X + curveEndPoint.X) / 2;
            cordY = (curveStartPoint.Y + curveEndPoint.Y) / 2;
            cordZ = (curveStartPoint.Z + curveEndPoint.Z) / 2;

            return new XYZ(cordX, cordY, cordZ);
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
        /// <param name="slope">
        /// The slope of the the roof.  
        /// </param>
        /// <param name="roofDesign">
        /// The roof type that will be created.
        /// </param>
        /// <returns>
        /// Returns the created FootPrintRoof.
        /// </returns>
        public FootPrintRoof CreateRoof(double overhang, double slope, XYZ slopeDirection, RoofDesign roofDesign)
        {
            FootPrintRoof footPrintRoof = null;
            CurveArray footPrintCurve = GetHousePerimeter();
            NormalizeCurveArray(ref footPrintCurve);

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

        /// <summary>
        /// Create a generic roof in the Top Level.
        /// </summary>
        /// <param name="slopeDirection">
        /// The slope will only be apllied on the edges that is orthogonal to the Slope Direction Vector.
        /// Note tha a 0 vector is ortoghonal to all vectors.
        /// </param>
        /// <param name="slope">
        /// The slope of the the roof.  
        /// </param>
        /// <param name="roofDesign">
        /// The roof type that will be created.
        /// </param>
        /// <returns>
        /// Returns the created FootPrintRoof.
        /// </returns>
        public FootPrintRoof CreateRoof(double slope, XYZ slopeDirection, RoofDesign roofDesign)
        {
            return CreateRoof(0, slope, slopeDirection, roofDesign);
        }


        private void CreateHiddenButterflyRoof(CurveArray footPrint, double slope, XYZ slopeDirection)
        {
            List<Line> cutLines = new List<Line>();
            List<CurveArray> convexFootPrint = GetConvexPerimeters(footPrint, slopeDirection, cutLines);

            if (convexFootPrint.Count == 1)
            {
                XYZ divisionDirection = new XYZ(-slopeDirection.Y, slopeDirection.X, 0);
                convexFootPrint = DivideCurveArrayInHalf(convexFootPrint[0], divisionDirection, out Line cutLine);
                cutLines.Add(cutLine);
            }

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

        private List<CurveArray> DivideCurveArrayInHalf(CurveArray curveArray, XYZ divisionDirection, out Line cutLine)
        {
            NormalizeCurveArray(ref curveArray);
            if (curveArray.Size != 4)
            {
                cutLine = null;
                return null;
            }
                

            CircularLinkedList<UV> points = GetPoints(curveArray);
            List<CircularLinkedListNode<UV>> newPoints = new List<CircularLinkedListNode<UV>>();

            foreach (Curve curve in curveArray)
            {
                // if they are parralel
                if (GetCurveDirection(curve).CrossProduct(divisionDirection).IsZeroLength())
                {
                    XYZ p0 = curve.GetEndPoint(0);
                    XYZ p1 = curve.GetEndPoint(1);
                    UV newPoint = ProjectInPlaneXY((p0 + p1) / 2);
                    newPoints.Add(AddPointBetween(points, ProjectInPlaneXY(p0), ProjectInPlaneXY(p1), newPoint));
                }
            }

            if (newPoints.Count != 2)
            {
                cutLine = null;
                return null;
            }
                     

            CircularLinkedList<UV> newPolygon0 = CreatePolygonBetweenVertices(newPoints[0], newPoints[1]);
            CircularLinkedList<UV> newPolygon1 = CreatePolygonBetweenVertices(newPoints[1], newPoints[0]);

            List<CurveArray> dividedCurveArrays = new List<CurveArray>
            {
                CreateCurveArrayFromPoints(newPolygon0),
                CreateCurveArrayFromPoints(newPolygon1)
            };

            cutLine = Line.CreateBound(TransformUVinXYZ(newPoints[0].Value), TransformUVinXYZ(newPoints[1].Value));
            return dividedCurveArrays;
        }

        private static CircularLinkedListNode<UV> AddPointBetween(CircularLinkedList<UV> points, UV p0, UV p1, UV newPoint)
        {
            CircularLinkedListNode<UV> p0Node = FindPoint(points, p0);
            CircularLinkedListNode<UV> p1Node = FindPoint(points, p1);
            if (p0Node.Next.Equals(p1Node))
                return points.AddAfter(p0Node, newPoint);
            if (p1Node.Next.Equals(p0Node))
                return points.AddAfter(p1Node, newPoint);
            throw new ArgumentException("The points are not sequential.");
        }

        bool AlmostEqual(double a, double b, double delta)
        {
            return Math.Abs(a - b) < delta;
        }

        private bool NormalizeCurveArray(ref CurveArray curveArray)
        {
            bool normalized = true;
            var points = GetPoints(curveArray);
            var node = points.Head;

            do
            {
                UV p0 = node.Previous.Value;
                UV p1 = node.Value;
                UV p2 = node.Next.Value;

                double angle = CalculatesAngle(p0, p1, p2);
                if (AlmostEqual(angle, Math.PI, 0.01) || AlmostEqual(angle, 0, 0.01))
                {
                    points.Remove(p1);
                    normalized = false;
                }
                node = node.Next;
            } while (node != points.Head);

            curveArray = CreateCurveArrayFromPoints(points);

            if (!normalized)
                NormalizeCurveArray(ref curveArray);        

            return normalized;
        }

        private void CreateParapetWall(CurveArray curveArray)
        {
            foreach (Curve curve in curveArray)
            {
                XYZ curveMiddlePoint = GetCurveMiddlePoint(curve);
                WallType wallType = GetWallType(Properties.Settings.Default.WallTypeName);
                Wall parapetWall = Wall.Create(doc, curve, wallType.Id, topLevel.Id, UnitUtils.ConvertToInternalUnits(0.8, UnitTypeId.Meters), 0, false, false);

                Wall wall = FindHostingWall(curveMiddlePoint, baseLevel);
                if (wall != null)
                {
                    try { JoinGeometryUtils.JoinGeometry(doc, wall, parapetWall); }
                    catch { continue; }
                }
            }
        }

        /// <summary>
        /// Creates a Hip Roof within the footPrint.
        /// </summary>
        /// <param name="footPrint"></param>
        /// <param name="overhang"></param>
        /// <param name="slope"></param>
        /// <param name="slopeDirection"></param>
        private void CreateHipRoof(CurveArray footPrint, double overhang, double slope, XYZ slopeDirection)
        {
            CurveArray offsetedFootPrint = CreateOffsetedCurveArray(overhang, footPrint, null);
            CreateFootPrintRoof(overhang, slope, slopeDirection, offsetedFootPrint);
        }

        /// <summary>
        /// Creates a Gable Roof within the footPrint.
        /// </summary>
        /// <param name="footPrint">An curve array that represents the footprint of the roof</param>
        /// <param name="overhang">The overhang value.</param>
        /// <param name="slope">The slope of the roof.</param>
        /// <param name="slopeDirection">The edges that are perpendicular to this vector will have the slope applied.</param>
        private void CreateGableRoof(CurveArray footPrint, double overhang, double slope, XYZ slopeDirection)
        {
            List<FootPrintRoof> roofs = new List<FootPrintRoof>();
            List<Line> cutLines = new List<Line>();
            List<CurveArray> convexFootPrint = GetConvexPerimeters(footPrint, slopeDirection, cutLines);
            List<Wall> gableWalls = new List<Wall>();

            int n = convexFootPrint.Count();

            // create the n convex compenents of the roof
            for (int i = 0; i < n; i++)
            {              
                CurveArray offsetedFootPrint = CreateOffsetedCurveArray(overhang, convexFootPrint[i], cutLines);
                if (offsetedFootPrint != null)
                {
                    FootPrintRoof footPrintRoof = CreateFootPrintRoof(overhang, slope, slopeDirection, offsetedFootPrint);
                    CreateAllGableWalls(slopeDirection, slope, convexFootPrint[i], gableWalls);
                    roofs.Add(footPrintRoof);
                }
            }

            // try to connect the components 
            foreach(FootPrintRoof roof in roofs)
            {
                foreach(FootPrintRoof uniteRoof in roofs)
                {
                    try
                    {
                        JoinGeometryUtils.JoinGeometry(doc, roof, uniteRoof);
                    }
                    catch (Exception) { continue; }
                }
            }
        }

        /// <summary>
        /// Verifies if the curve is collinear and have atleast one point in common with any line in the given list of lines.
        /// </summary>
        /// <param name="curve">The curve that will be compared with the list.</param>
        /// <param name="lines">The list of lines that the first will be compared with.</param>
        /// <returns>Returns true if the curve intersects and is parallel to any line in the array.</returns>
        private bool VerifyIntersectionInArray(Curve curve, List<Line> lines)
        {
            Transform transform = Transform.CreateTranslation(new XYZ(0, 0, -curve.GetEndPoint(0).Z));
            curve = curve.CreateTransformed(transform);
            foreach (Line line in lines)
            {
                // verifiy is the line is equal or a subset of the curve
                SetComparisonResult result = line.Intersect(curve);
                if (result == SetComparisonResult.Equal ||
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

                double elevation = -(overhang - UnitUtils.ConvertToInternalUnits(0.1, UnitTypeId.Meters)) / 3;
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
            foreach(Room room in Rooms)
            {
                var roomLoops = GetRoomLoops(room).Count;
                if (roomLoops == 2)
                {
                    room.Name = "Exterior";
                }
                else if (roomLoops == 1)
                {
                    List<string> elements = GetFurniture(room);
                    scoreThread.Join();
                    roomThread.Join();
                    string roomName = GetRoomFromScore(elements, roomElementsScore, roomNames);
                    if (roomName != null) 
                    {
                        room.Name = roomName;
                    }
                }
            }
        }

        private string GetRoomFromScore(List<string> elements, List<ScoreDM> sdm, List<RoomDM> rooms)
        {
            int[] score = new int[rooms.Count()];

            for (int i = 0; i < elements.Count; i++)
            {
                foreach (ScoreDM s in sdm)
                {
                    if (elements[i].Equals(s.ElementName))
                    {
                        score[s.RoomID] += s.Score;
                    }
                }
            }

            int maxIndex = 0;
            int max = 0;
            for (int i = 0; i < score.Count(); i++)
            {
                if (score[i] > max)
                {
                    max = score[i];
                    maxIndex = i;
                }
            }

            if (max == 0)
                return null;
            else
                return rooms[maxIndex-1].Name;
        }

        /// <summary>
        /// Get all furniture elements inside a room.
        /// </summary>
        /// <param name="room"></param>
        /// <returns>
        /// Returns a List with those elements.
        /// </returns>
        private static List<string> GetFurniture(Room room)
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
            List<string> elementsInsideTheRoom = new List<string>();

            foreach (FamilyInstance instance in collector)
            {
                if (instance.Room != null)
                {
                    if (instance.Room.Id.IntegerValue.Equals(roomid))
                    {
                        elementsInsideTheRoom.Add(instance.Name);
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

            Viewport.Create(doc, viewSheet.Id, doc.ActiveView.Id, new XYZ(0, 2, 0));
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
        public void CreateAllGableWalls(XYZ vectorDirection, double slope, CurveArray perimeter, List<Wall> gableWalls)
        {
            NormalizeCurveArray(ref perimeter);
            foreach (Curve line in perimeter)
            {
                XYZ lineDirection = GetCurveDirection(line);
                if (lineDirection.CrossProduct(vectorDirection).IsZeroLength())
                {
                    Wall newGableWall = CreateGableWall(line, slope);
                    Wall intersectionWall = FindIntersectionWall(gableWalls, newGableWall);
                    bool insert = true;
                    if(intersectionWall != null)
                    {
                        LocationCurve intersectionWallLocation = intersectionWall.Location as LocationCurve;
                        LocationCurve newGableWallLocation = newGableWall.Location as LocationCurve;
                        Wall deleteWall;

                        if(intersectionWallLocation.Curve.Length > newGableWallLocation.Curve.Length)
                        {
                            deleteWall = newGableWall;
                            insert = false;
                        }
                        else
                        {
                            deleteWall = intersectionWall;
                        }
                        gableWalls.Remove(deleteWall);
                        doc.Delete(deleteWall.Id);
                    }
                    if(insert)
                        gableWalls.Add(newGableWall);              
                }
            }
        }

        private Wall FindIntersectionWall(List<Wall> walls, Wall comparisonWall)
        {
            LocationCurve locationCurve = comparisonWall.Location as LocationCurve;
            Curve wallCurve = locationCurve.Curve as Line;
        
            foreach (Wall wall in walls)
            {
                LocationCurve lc = wall.Location as LocationCurve;
                Curve c = lc.Curve;

                SetComparisonResult intersection = wallCurve.Intersect(c, out IntersectionResultArray resultArray);
                if (intersection == SetComparisonResult.Equal || intersection == SetComparisonResult.Overlap || 
                    intersection == SetComparisonResult.Subset || intersection == SetComparisonResult.Superset)
                {
                    return wall;
                }
            }
            return null;
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
            IList<Curve> profile = new List<Curve>(3)
            {
                Line.CreateBound(p0, p1),
                Line.CreateBound(p1, p2),
                Line.CreateBound(p2, p0)
            };

            // get the wall type
            WallType type = GetWallType(Properties.Settings.Default.WallTypeName);

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
        
        public void DimensioningBuilding(double offset, bool normalize)
        {
            var perimeter = GetHousePerimeter();
            if (normalize)
                NormalizeCurveArray(ref perimeter);

            foreach (Curve curve in perimeter)
            {
                XYZ p1 = new XYZ(
                    curve.GetEndPoint(0).X,
                    curve.GetEndPoint(0).Y,
                    curve.GetEndPoint(0).Z);

                XYZ p2 = new XYZ(
                    curve.GetEndPoint(1).X,
                    curve.GetEndPoint(1).Y,
                    curve.GetEndPoint(1).Z);

                XYZ normal = new XYZ(-(p2 - p1).Y, (p2 - p1).X, 0).Normalize();
                Transform transform = Transform.CreateTranslation(normal* offset);               

                Line line = Line.CreateBound(p1, p2);
                line = line.CreateTransformed(transform) as Line;

                Plane plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, XYZ.Zero);
                ModelCurve modelCurve = doc.Create.NewModelCurve(line, SketchPlane.Create(doc, plane));

                ReferenceArray references = new ReferenceArray();
                references.Append(modelCurve.GeometryCurve.GetEndPointReference(0));
                references.Append(modelCurve.GeometryCurve.GetEndPointReference(1));
                doc.Create.NewDimension(doc.ActiveView, line, references);                   
            }
        }
    }
}
