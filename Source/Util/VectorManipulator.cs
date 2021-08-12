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


    }
}
