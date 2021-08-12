using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.IFC;

namespace CustomizacaoMoradias.Source.Util
{
    class Polygon
    {

        private CurveArray curveArray;

        public IList<UV> Vertices 
        { 
            get 
            {
                return GetPoints().ToList();
            } 
        }

        public IList<UV> Notches
        {
            get
            {
                return GetNotches(GetPoints());
            }
        }

        public CurveArray CurveArray
        {
            get
            {
                return curveArray;
            }
        }

        public UV Centroid
        {
            get
            {
                return CalculatePolygonCentroid(GetPoints());
            }
        }


        public Polygon(CurveArray curveArray)
        {
            this.curveArray = curveArray;
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
        private CircularLinkedList<UV> GetPoints()
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
            if (points.Count < 5) return notches;

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
        public List<CurveArray> GetConvexPerimeters(XYZ preferredOrientation, List<Line> cutLines)
        {
            CircularLinkedList<UV> points = GetPoints();
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
                perimeters.AddRange(GetConvexPerimeters(preferredOrientation, cutLines));
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
            foreach (UV uv in list)
            {
                if (uv.IsAlmostEqualTo(key))
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
        public CurveArray CreateCurveArrayFromPoints(CircularLinkedList<UV> points)
        {
            CurveArray curveArray = new CurveArray();
            CircularLinkedListNode<UV> node = points.Head;
            Line line;
            do
            {
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
                if (point.IsAlmostEqualTo(key, 0.01))
                    return node;
                node = node.Next;
            } while (node != points.Head);
            return null;
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
        /// Creates a Curve Array given the Boundary Segments. 
        /// </summary>
        /// <param name="offset">
        /// A positive value that represents the offset that the curve array will have in a direction. 
        /// If this value is 0, the user may not pass an offsetVector.
        /// <returns>
        /// Returns the offseted Curve Array.
        /// </returns>
        public CurveArray CreateOffsetedCurveArray(double offset, List<Line> unchangedLines)
        {
            if (offset < 0 || curveArray.Size < 3)
            {
                return null;
            }

            Normalize();
            CircularLinkedList<UV> points = GetPoints();
            CircularLinkedList<UV> offsetedPoints = OffsetPolygon(points, offset, unchangedLines);

            CircularLinkedList<UV> linkedOffsetedPoints = new CircularLinkedList<UV>(offsetedPoints);
            CurveArray offsetedCurveArray = CreateCurveArrayFromPoints(linkedOffsetedPoints);

            return offsetedCurveArray;
        }

        /// <summary>
        /// Creates a Curve Array given the Boundary Segments. 
        /// </summary>
        /// <param name="offset">
        /// A positive value that represents the offset that the curve array will have in a direction. 
        /// If this value is 0, the user may not pass an offsetVector.
        /// <returns>
        /// Returns the offseted Curve Array.
        /// </returns>
        public CurveArray CreateOffsetedCurveArray(double offset)
        {
            return CreateOffsetedCurveArray(offset, null);
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
                if (unchangedLines != null)
                {
                    foreach (Line l in unchangedLines)
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

        public bool Normalize()
        {
            bool normalized = true;
            var points = GetPoints();
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
                Normalize();

            return normalized;
        }

        private static bool AlmostEqual(double a, double b, double delta)
        {
            return Math.Abs(a - b) < delta;
        }

        private UV CalculatePolygonCentroid(CircularLinkedList<UV> vertices)
        {
            var node = vertices.Head;
            UV sum = UV.Zero;
            do
            {
                UV vertex = node.Value;
                sum += vertex;
                node = node.Next;
            } while (node != vertices.Head);
            sum /= vertices.Count;
            return sum;
        }

        public List<CurveArray> DividePolygonInHalf(XYZ divisionDirection, out Line cutLine)
        {
            Normalize();

            CircularLinkedList<UV> points = GetPoints();
            List<CircularLinkedListNode<UV>> newPoints = new List<CircularLinkedListNode<UV>>();

            UV centroid = CalculatePolygonCentroid(points);
            cutLine = Line.CreateUnbound(VectorManipulator.TransformUVinXYZ(centroid), divisionDirection);

            foreach (Curve curve in curveArray)
            {
                var result = cutLine.Intersect(curve, out var resultArray);
                if (result == SetComparisonResult.Overlap)
                {
                    UV newPoint = VectorManipulator.ProjectInPlaneXY(resultArray.get_Item(0).XYZPoint);
                    XYZ p0 = curve.GetEndPoint(0);
                    XYZ p1 = curve.GetEndPoint(1);
                    newPoints.Add(AddPointBetween(points, VectorManipulator.ProjectInPlaneXY(p0), VectorManipulator.ProjectInPlaneXY(p1), newPoint));
                }
            }

            CircularLinkedList<UV> newPolygon0 = CreatePolygonBetweenVertices(newPoints[0], newPoints[1]);
            CircularLinkedList<UV> newPolygon1 = CreatePolygonBetweenVertices(newPoints[1], newPoints[0]);

            List<CurveArray> dividedCurveArrays = new List<CurveArray>
            {
                CreateCurveArrayFromPoints(newPolygon0),
                CreateCurveArrayFromPoints(newPolygon1)
            };

            cutLine = Line.CreateBound(VectorManipulator.TransformUVinXYZ(newPoints[0].Value), VectorManipulator.TransformUVinXYZ(newPoints[1].Value));
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
    }
}
