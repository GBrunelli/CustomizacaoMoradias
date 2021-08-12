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

        private double baseRotation = Math.PI / 2;

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

        private List<Wall> Walls = new List<Wall>();

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

            Wall wall = null;
            double distance = double.MaxValue;
            foreach (Wall w in Walls)
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
        /// Builds the elements defined on a JSON file.
        /// </summary>
        public void BuildJSON(string path)
        {
            if (path is null)
                throw new ArgumentNullException(nameof(path));
            string jsonText = File.ReadAllText(path);
            ElementDeserializer ed = JsonConvert.DeserializeObject<ElementDeserializer>(jsonText);
            string errorMessage = "";
            if (ed.WallProperties != null)
            {
                foreach (WallProperty wall in ed.WallProperties)
                {
                    try { CreateWall(wall, Properties.Settings.Default.WallTypeName); }
                    catch { errorMessage += $"Parede {wall}, "; }
                }
            }
            if(ed.WindowProperties != null)
            {
                foreach (WindowProperty window in ed.WindowProperties)
                {
                    try { CreateWindow(window); }
                    catch { errorMessage += $"{window.Type}, "; }
                }
            }
            if(ed.DoorProperties != null)
            {
                foreach (DoorProperty door in ed.DoorProperties)
                {
                    try { CreateDoor(door); }
                    catch { errorMessage += $"{door.Type}, "; }
                }
            }
            
            if(ed.HostedProperties != null)
            {
                foreach (HostedProperty element in ed.HostedProperties)
                {
                    try { CreateHostedElement(element, true); }
                    catch { errorMessage += $"{element.Type}, "; }

                }
            }
            
            if(ed.FurnitureProperties != null)
            {
                foreach (FurnitureProperty element in ed.FurnitureProperties)
                {
                    try { CreateFurniture(element); }
                    catch { errorMessage += $"{element.Type}, "; }
                }
            }
            

            if (errorMessage.EndsWith(", "))
            {
                errorMessage = errorMessage.Remove(errorMessage.Length - 2, 2);
                MessageBox.Show($"Erro ao inserir elementos: \n{errorMessage}.", "Erro!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }  
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
            offset = new UV(UnitUtils.ConvertToInternalUnits(offset.U, UnitTypeId.Meters),
                UnitUtils.ConvertToInternalUnits(offset.V, UnitTypeId.Meters));
            offset = VectorManipulator.RotateVector(offset, rotation + baseRotation);

            // Creates a point above the furniture to serve as a rotation axis
            XYZ axisPoint = new XYZ(point.X, point.Y, baseLevel.Elevation + 1);
            Line axis = Line.CreateBound(point, axisPoint);
            FamilyInstance furniture;
            try
            {
                FamilySymbol familySymbol = GetFamilySymbol(doc, fsFamilyName);

                Autodesk.Revit.DB.Structure.StructuralType structuralType = Autodesk.Revit.DB.Structure.StructuralType.NonStructural;
                furniture = doc.Create.NewFamilyInstance(point, familySymbol, structuralType);
                ElementTransformUtils.RotateElement(doc, furniture.Id, axis, rotation + baseRotation);            
                ElementTransformUtils.MoveElement(doc, furniture.Id, VectorManipulator.TransformUVinXYZ(offset));
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
            Wall wall;
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
            Walls.Add(wall);
            return wall;
        }

        /// <summary>
        /// Create a hosted element on a wall.
        /// </summary>
        /// <param name="properties"> Properties of the object.</param>
        private FamilyInstance CreateHostedElement(HostedProperty properties, bool correctPosition)
        {
            if (properties is null)
            {
                throw new ArgumentNullException(nameof(properties));
            }

            // get the parameters from properties
            XYZ point = GetXYZFromProperties(properties.Coordinate);
            string fsName = GetFamilySymbolName(properties.Type);
            double rotation = properties.Rotation;
            UV offset = GetFamilyOffset(properties.Type);

            offset = VectorManipulator.RotateVector(offset, rotation);
            point += VectorManipulator.TransformUVinXYZ(offset);
            FamilySymbol familySymbol = GetFamilySymbol(doc, fsName);

            Wall wall = FindHostingWall(point, baseLevel);
            
            // Creates the element                   
            FamilyInstance instance = doc.Create.NewFamilyInstance(point, familySymbol, wall, baseLevel, 
                Autodesk.Revit.DB.Structure.StructuralType.NonStructural);

            if(correctPosition)
                CorrectHostedPosition(point, wall, instance);

            return instance;
        }

        private void CorrectHostedPosition(XYZ point, Wall wall, FamilyInstance instance)
        {
            LocationCurve wallCurve = wall.Location as LocationCurve;
            Line wallLine = wallCurve.Curve as Line;

            XYZ wallStartPoint = wallLine.GetEndPoint(0);
            XYZ wallEndPoint = wallLine.GetEndPoint(1);
            XYZ wallNormal = (wallStartPoint - wallEndPoint).CrossProduct(XYZ.BasisZ).Normalize();
            double angle = Math.Atan2(wallNormal.Y, wallNormal.X);

            // get the initial orientation of the new family instance
            var p = (instance.Location as LocationPoint).Point;

            XYZ insertedDirection = VectorManipulator.TransformUVinXYZ(VectorManipulator.RotateVector(VectorManipulator.ProjectInPlaneXY(p), angle)).Normalize();

            // get the supposed orientation of the instance
            XYZ insertionPoint = wallStartPoint + (wallLine.Direction * instance.HostParameter);
            XYZ correctDirection = (point - insertionPoint).Normalize();

            // if the initial orientation and the correct orientation are not equal, flip the instance
            if (!insertedDirection.IsAlmostEqualTo(correctDirection))
            {
                instance.flipFacing();
            }
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
            FamilyInstance door = CreateHostedElement(hp, false);

            XYZ facing = door.FacingOrientation;
            double angle = Math.Atan2(facing.Y, facing.X);
            if (Math.Abs(angle - DeegreToRadians(properties.Rotation)) < 0.001)
                door.flipFacing();

            if(properties.OpenLeft)
                door.flipHand();
            
            return door;
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
            FamilyInstance window = CreateHostedElement(hp, false);

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
                UV point2D = VectorManipulator.ProjectInPlaneXY(curve.GetEndPoint(0));
                points.AddLast(point2D);
            }
            return points;
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
                double angle = VectorManipulator.CalculatesAngle(p0, p1, p2);
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
            UV p0 = VectorManipulator.ProjectInPlaneXY(curve.GetEndPoint(0));
            UV p1 = VectorManipulator.ProjectInPlaneXY(curve.GetEndPoint(1));
            

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
            XYZ notche3D = VectorManipulator.TransformUVinXYZ(notch);
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
            cutLine = Line.CreateBound(notche3D, VectorManipulator.TransformUVinXYZ(newNode.Value));

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
                        UV point = VectorManipulator.ProjectInPlaneXY(result.XYZPoint);
                        double distance = point.DistanceTo(notch);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            newPoint = point;
                            previousPoint = VectorManipulator.ProjectInPlaneXY(curve.GetEndPoint(0));
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
            UV p0 = VectorManipulator.ProjectInPlaneXY(curve.GetEndPoint(0));
            CircularLinkedListNode<UV> newNode = null;
            CircularLinkedListNode<UV> node = FindPoint(points, p0);

            IntersectionResultArrayIterator iterator = resultArray.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
                IntersectionResult result = iterator.Current as IntersectionResult;
                UV intersectionPoint = VectorManipulator.ProjectInPlaneXY(result.XYZPoint);
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
                    line = Line.CreateBound(VectorManipulator.TransformUVinXYZ(node.Value), VectorManipulator.TransformUVinXYZ(node.Next.Value));
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
                        UV p0 = VectorManipulator.ProjectInPlaneXY(l.GetEndPoint(0));
                        UV p1 = VectorManipulator.ProjectInPlaneXY(l.GetEndPoint(1));

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
                Line line1 = Line.CreateBound(VectorManipulator.TransformUVinXYZ(pij1), VectorManipulator.TransformUVinXYZ(pij2));
                line1.MakeUnbound();

                // creates a shifted line that is parallel to the vector v2
                UV n2 = new UV(-v2.V, v2.U);
                UV pjk1 = vertexJ + n2;
                UV pjk2 = vertexK + n2;
                Line line2 = Line.CreateBound(VectorManipulator.TransformUVinXYZ(pjk1), VectorManipulator.TransformUVinXYZ(pjk2));
                line2.MakeUnbound();

                //see where the shifted lines 1 and 2 intersect
                SetComparisonResult comparisonResult = line1.Intersect(line2, out IntersectionResultArray intersection);

                if (comparisonResult == SetComparisonResult.Overlap)
                {
                    IntersectionResult result = intersection.get_Item(0);
                    UV intersection_point = VectorManipulator.ProjectInPlaneXY(result.XYZPoint);

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
            View currentView = doc.ActiveView;
            foreach (Curve curve in curveArray)
            {
                XYZ startPoint = curve.GetEndPoint(0);
                XYZ endPoint = curve.GetEndPoint(1);
                Line L1 = Line.CreateBound(startPoint, endPoint);
                doc.Create.NewDetailCurve(currentView, L1);
            }
        }

        private void DrawCurveArray(Line line)
        {
            if (line is null) return;
            CurveArray curveArray = new CurveArray();
            curveArray.Append(line);
            DrawCurveArray(curveArray);
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
                    else
                    {
                        room.Name = "Varanda de serviço";
                    }
                }
            }
        }

        private string GetRoomFromScore(List<string> elements, List<ScoreDM> sdm, List<RoomDM> rooms)
        {
            Dictionary<int, int> scoreDict = new Dictionary<int, int>(rooms.Count);

            for (int i = 0; i < elements.Count; i++)
            {
                foreach (ScoreDM s in sdm)
                {
                    if (elements[i].Equals(s.ElementName))
                    {
                        if(scoreDict.TryGetValue(s.RoomID, out int currentScore))
                            scoreDict[s.RoomID] += s.Score;
                        else
                            scoreDict.Add(s.RoomID, s.Score);
                    }
                }
            }

            int roomId = 0, max = 0;
            foreach (var pair in scoreDict)
            {
                if(pair.Value > max)
                {
                    max = pair.Value;
                    roomId = pair.Key;
                }
            }

            if (max != 0)
            {
                foreach (RoomDM room in rooms)
                {
                    if (room.RoomID == roomId)
                        return room.Name;
                }
            }
            return null;
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

                double angle = VectorManipulator.CalculatesAngle(p0, p1, p2);
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

        bool AlmostEqual(double a, double b, double delta)
        {
            return Math.Abs(a - b) < delta;
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

        public List<(Wall, bool[])> GetOpenWalls(List<Wall> walls)
        {
            List<(Wall, bool[])> openWalls = new List<(Wall, bool[])>();

            foreach (Wall wall in walls)
            {
                Line wallLine = (wall.Location as LocationCurve).Curve as Line;
                bool connectedAt0 = false;
                bool connectedAt1 = false;

                XYZ p0 = wallLine.GetEndPoint(0);
                XYZ p1 = wallLine.GetEndPoint(1);

                foreach (Wall w in walls)
                {
                    
                    Line wLine = (w.Location as LocationCurve).Curve as Line;
                    var result = wallLine.Intersect(wLine, out var resultArray);

                    if ((result != SetComparisonResult.Disjoint) && (result != SetComparisonResult.Equal))
                    {
                        IntersectionResultArrayIterator iterator = resultArray.ForwardIterator();
                        IntersectionResult intersection = resultArray.get_Item(0);
                        XYZ intersectionPoint = intersection.XYZPoint;

                        if (intersectionPoint.IsAlmostEqualTo(p0))
                            connectedAt0 = true;
                        else if (intersectionPoint.IsAlmostEqualTo(p1))
                            connectedAt1 = true;
                    }
                }
                if(!(connectedAt0 && connectedAt1))
                {
                    bool[] openSide = { connectedAt0, connectedAt1 };
                    (Wall, bool[]) openWall = (wall, openSide);
                    openWalls.Add(openWall);
                }
            }
            return openWalls;
        }

        private static Line GetWallLine(Wall wall)
        {
            if (wall is null) 
                return null;
            return (wall.Location as LocationCurve).Curve as Line;
        }

        public void PlaceRoomSeparatorsInOpenWalls()
        {
            List<(Wall, bool[])> openWalls = GetOpenWalls(Walls);

            foreach (var openWall in openWalls)
            {
                // inital filtering
                (Wall wall, bool[] connected) = openWall;
                Line wallLine = GetWallLine(wall);
                List<Wall> candidates = new List<Wall>();
                foreach (Wall w in Walls)
                {
                    Line wLine = GetWallLine(w);
                    var result = wallLine.Intersect(wLine);
                    if (result == SetComparisonResult.Disjoint)
                    {
                        candidates.Add(w);
                    }
                }

                // criar o vetor e a linha
                XYZ p0, p1;  
                if (!connected[0])
                {
                    p0 = wallLine.GetEndPoint(1);
                    p1 = wallLine.GetEndPoint(0);
                    PlaceRoomSeparatorInOpenWall(wallLine, candidates, p0, p1);
                }
                if (!connected[1])
                {
                    p0 = wallLine.GetEndPoint(0);
                    p1 = wallLine.GetEndPoint(1);
                    PlaceRoomSeparatorInOpenWall(wallLine, candidates, p0, p1);
                }
            }
        }

        private void PlaceRoomSeparatorInOpenWall(Line wallLine, List<Wall> candidates, XYZ p0, XYZ p1)
        {
            XYZ wallVector = (p1 - p0).Normalize();
            if (!CalculateRoomBoundary(wallLine, candidates, p0, p1, wallVector))
            {
                wallVector = new XYZ(-wallVector.Y, wallVector.X, wallVector.Z);
                if(!CalculateRoomBoundary(wallLine, candidates, p0, p1, wallVector))
                {
                    CalculateRoomBoundary(wallLine, candidates, p0, p1, -wallVector);
                }
            }
        }

        private bool CalculateRoomBoundary(Line wallLine, List<Wall> candidates, XYZ p0, XYZ p1, XYZ wallVector)
        {
            Line unbound = Line.CreateUnbound(p1, wallVector);
            List<Wall> candidates2 = FilterWalls(candidates, unbound, p0, p1);
            Line closestWallLine = GetClosestLine(p0, candidates2);
            if (closestWallLine != null)
                return CreateRoomBoundary(wallLine, p1, closestWallLine);
            return false;
        }

        private bool CreateRoomBoundary(Line wallLine, XYZ p1, Line closestWallLine)
        {
            // cria o separador de ambientes
            XYZ p;
            try
            {
                IList<ClosestPointsPairBetweenTwoCurves> resultList = new List<ClosestPointsPairBetweenTwoCurves>();
                closestWallLine.ComputeClosestPoints(wallLine, true, true, false, out resultList);
                p = resultList[0].XYZPointOnFirstCurve;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException)
            {
                XYZ start = closestWallLine.GetEndPoint(0);
                XYZ end = closestWallLine.GetEndPoint(1);
                if (start.DistanceTo(p1) < end.DistanceTo(p1))
                    p = start;
                else
                    p = end;
            }
            
            Line l = Line.CreateBound(p1, p);
            CurveArray array = new CurveArray();

            array.Append(l);
            View view = GetBaseLevelView();

            WallType wallType = GetWallType(Properties.Settings.Default.WallTypeName);
            double height = UnitUtils.ConvertToInternalUnits(2.8, UnitTypeId.Meters);
            Wall wall = Wall.Create(doc, l, wallType.Id, baseLevel.Id, height, 0, false, false);
            wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(topLevel.Id);
            wall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET).Set(UnitUtils.ConvertToInternalUnits(2.5, UnitTypeId.Meters));

            return (doc.Create.NewRoomBoundaryLines(view.SketchPlane, array, view) != null);
        }

        private static Line GetClosestLine(XYZ p0, List<Wall> candidates2)
        {
            // calcula a menor distancia entre as paredes
            double minDist = double.MaxValue;
            Wall closestWall = null;
            foreach (Wall candidate in candidates2)
            {
                Line candidateLine = GetWallLine(candidate);
                double dist = candidateLine.Distance(p0);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestWall = candidate;
                }
            }
            if (closestWall is null) 
                return null;
            Line closestWallLine = GetWallLine(closestWall);
            return closestWallLine;
        }

        private static List<Wall> FilterWalls(List<Wall> candidates, Line unbounded, XYZ p0, XYZ p1)
        {
            // filtra as possiveis parades
            List<Wall> candidates2 = new List<Wall>();
            foreach (Wall candidate in candidates)
            {
                Line candidateLine = GetWallLine(candidate);
                var result = unbounded.Intersect(candidateLine, out var resultArray);
                if ((result == SetComparisonResult.Overlap) && (resultArray.get_Item(0).UVPoint.U > 0))
                {
                    candidates2.Add(candidate);
                }
                else if (result == SetComparisonResult.Superset)
                {
                    if (p0.DistanceTo(candidateLine.GetEndPoint(0)) >
                        p1.DistanceTo(candidateLine.GetEndPoint(0)))
                    {
                        candidates2.Add(candidate);
                    }
                }

            }
            return candidates2;
        }
    }
}
