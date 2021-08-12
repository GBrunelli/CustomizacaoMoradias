using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace CustomizacaoMoradias.Source.Util
{
    class VectorManipulator
    {
        private static UV RotateVector(UV vector, double rotation)
        {
            double u2 = Math.Cos(rotation) * vector.U - Math.Sin(rotation) * vector.V;
            double v2 = Math.Sin(rotation) * vector.U + Math.Cos(rotation) * vector.V;
            return new UV(u2, v2);
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
    }
}
