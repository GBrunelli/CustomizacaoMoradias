﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using CustomizacaoMoradias.Source.Util;

namespace CustomizacaoMoradias.Source.Builder
{
    class RoofCreator
    {
        public enum RoofDesign
        {
            HiddenButterfly,
            Hip,
            Gable
        }

        private Document document;
        private Level baseLevel;
        private Level roofLevel;

        public RoofCreator(Document document, Level baseLevel, Level roofLevel)
        {
            this.document = document;
            this.baseLevel = baseLevel;
            this.roofLevel = roofLevel;
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
            // List<CurveArray> convexFootPrint = GetConvexPerimeters(footPrint, slopeDirection, cutLines);

            XYZ divisionDirection = new XYZ(-slopeDirection.Y, slopeDirection.X, 0);
            List<CurveArray> convexFootPrint = DivideCurveArrayInHalf(footPrint, divisionDirection, out Line cutLine);
            cutLines.Add(cutLine);

            foreach (CurveArray curveArray in convexFootPrint)
            {
                // get a roof type
                FilteredElementCollector collector = new FilteredElementCollector(document);
                collector.OfClass(typeof(RoofType));
                RoofType roofType = collector.FirstElement() as RoofType;

                // create the foot print of the roof
                ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
                FootPrintRoof footPrintRoof = document.Create.NewFootPrintRoof(curveArray, roofLevel, roofType, out footPrintToModelCurveMapping);

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

        private List<CurveArray> DivideCurveArrayInHalf(CurveArray curveArray, XYZ divisionDirection, out Line cutLine)
        {
            NormalizeCurveArray(ref curveArray);

            CircularLinkedList<UV> points = GetPoints(curveArray);
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

       

        private void CreateParapetWall(CurveArray curveArray)
        {
            foreach (Curve curve in curveArray)
            {
                XYZ curveMiddlePoint = GetCurveMiddlePoint(curve);
                WallType wallType = GetWallType(Properties.Settings.Default.WallTypeName);
                Wall parapetWall = Wall.Create(document, curve, wallType.Id, roofLevel.Id, UnitUtils.ConvertToInternalUnits(0.8, UnitTypeId.Meters), 0, false, false);

                Wall wall = FindHostingWall(curveMiddlePoint, baseLevel);
                if (wall != null)
                {
                    try { JoinGeometryUtils.JoinGeometry(document, wall, parapetWall); }
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
            foreach (FootPrintRoof roof in roofs)
            {
                foreach (FootPrintRoof uniteRoof in roofs)
                {
                    try
                    {
                        JoinGeometryUtils.JoinGeometry(document, roof, uniteRoof);
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
                if (result == SetComparisonResult.Overlap && (line.Direction.CrossProduct(VectorManipulator.GetCurveDirection(curve)).IsZeroLength()))
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
            FilteredElementCollector collector = new FilteredElementCollector(document);
            collector.OfClass(typeof(RoofType));
            RoofType roofType = collector.FirstElement() as RoofType;

            // create the foot print of the roof
            ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
            FootPrintRoof footPrintRoof = document.Create.NewFootPrintRoof(footPrint, roofLevel, roofType, out footPrintToModelCurveMapping);

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
                XYZ curveDirection = VectorManipulator.GetCurveDirection(curve);

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
                XYZ lineDirection = VectorManipulator.GetCurveDirection(line);
                if (lineDirection.CrossProduct(vectorDirection).IsZeroLength())
                {
                    Wall newGableWall = CreateGableWall(line, slope);
                    Wall intersectionWall = FindIntersectionWall(gableWalls, newGableWall);
                    bool insert = true;
                    if (intersectionWall != null)
                    {
                        LocationCurve intersectionWallLocation = intersectionWall.Location as LocationCurve;
                        LocationCurve newGableWallLocation = newGableWall.Location as LocationCurve;
                        Wall deleteWall;

                        if (intersectionWallLocation.Curve.Length > newGableWallLocation.Curve.Length)
                        {
                            deleteWall = newGableWall;
                            insert = false;
                        }
                        else
                        {
                            deleteWall = intersectionWall;
                        }
                        gableWalls.Remove(deleteWall);
                        document.Delete(deleteWall.Id);
                    }
                    if (insert)
                        gableWalls.Add(newGableWall);
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
            XYZ p2 = VectorManipulator.FormTriangle(slope, p0, p1);
            IList<Curve> profile = new List<Curve>(3)
            {
                Line.CreateBound(p0, p1),
                Line.CreateBound(p1, p2),
                Line.CreateBound(p2, p0)
            };

            // get the wall type
            WallType type = GetWallType(Properties.Settings.Default.WallTypeName);

            // create the gable wall
            return Wall.Create(document, profile, type.Id, roofLevel.Id, false);
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
    }
}
