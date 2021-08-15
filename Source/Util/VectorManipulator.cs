using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace CustomizacaoMoradias.Source.Util
{
    static class VectorManipulator
    {
        public static UV RotateVector(UV vector, double rotation)
        {
            double u2 = Math.Cos(rotation) * vector.U - Math.Sin(rotation) * vector.V;
            double v2 = Math.Sin(rotation) * vector.U + Math.Cos(rotation) * vector.V;
            return new UV(u2, v2);
        }

        public static UV RotateVector(XYZ vector, double rotation)
        {
            UV vector2D = ProjectInPlaneXY(vector);
            return RotateVector(vector2D, rotation);
        }

        /// <summary>
        /// Calculates de angle between the vectors (p0, p1) and (p1, p2)
        /// </summary>
        /// <returns>
        /// Returns the angle in radians.
        /// </returns>
        public static double CalculatesAngle(UV p0, UV p1, UV p2)
        {
            UV vector1 = p1.Subtract(p0);
            UV vector2 = p2.Subtract(p1);
            return Math.PI + Math.Atan2(vector1.CrossProduct(vector2), vector1.DotProduct(vector2));
        }

        /// <summary>
        /// Project the point XYZ in the plane XY.
        /// </summary>
        public static UV ProjectInPlaneXY(XYZ xyz)
        {
            return new UV(xyz.X, xyz.Y);
        }

        /// <summary>
        /// Transforms a 2D point in a 3D point, with the Z component set to 0.
        /// </summary>
        public static XYZ TransformUVinXYZ(UV uv)
        {
            return new XYZ(uv.U, uv.V, 0);
        }

        /// <summary>
        /// Calculates the vector that is formed by the start point and the end point of the curve.
        /// </summary>
        /// <param name="curve"></param>
        /// <returns>
        /// Returns the vector.
        /// </returns>
        public static XYZ GetCurveDirection(Curve curve)
        {
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);
            XYZ curveDirection = new XYZ(startPoint.X - endPoint.X, startPoint.Y - endPoint.Y, 0);
            return curveDirection;
        }

        /// <summary>
        /// Calculate the third point to form a triangle that obein the expression: tan(slope) = 2 * height / base.
        /// </summary>
        public static XYZ FormTriangle(double slope, XYZ p0, XYZ p1)
        {
            double p2x = (p0.X + p1.X) / 2;
            double p2y = (p0.Y + p1.Y) / 2;
            XYZ baseVector = p1.Subtract(p0);
            double p2z = (slope * baseVector.GetLength()) / 2;
            return new XYZ(p2x, p2y, p2z);
        }

        public static XYZ CalculateNormal(XYZ vector)
        {
            return vector.CrossProduct(XYZ.BasisZ).Normalize();
        }

        public static XYZ GetClosesetPointInLine(XYZ point, Line line)
        {
            XYZ normal = VectorManipulator.CalculateNormal(line.Direction);
            Line crossLine = Line.CreateUnbound(point, normal);
            if (line.Intersect(crossLine, out var resultArray) == SetComparisonResult.Overlap)
            {
                var result = resultArray.get_Item(0);
                return result.XYZPoint;
            }
            return null;
        }
    }
}
