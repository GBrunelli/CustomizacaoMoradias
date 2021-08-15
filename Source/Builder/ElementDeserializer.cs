using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizacaoMoradias.Source.Builder
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Coordinate
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class WallProperty
    {
        public List<Coordinate> Coordinate { get; set; }

        override public string ToString()
        {
            int x0 = Coordinate[0].X;
            int y0 = Coordinate[0].Y;

            int x1 = Coordinate[1].X;
            int y1 = Coordinate[1].Y;
            return $"({x0}, {y0}), ({x1}, {y1})";
        }
    }

    public class WindowProperty : IHosted
    {
        public Coordinate Coordinate { get; set; }
        public string Type { get; set; }
        public int Rotation { get; set; }

    }

    public class DoorProperty : IHosted
    {
        public Coordinate Coordinate { get; set; }
        public string Type { get; set; }
        public int Rotation { get; set; }
        public bool OpenLeft { get; set; }
    }

    public class HostedProperty : IHosted
    {
        public Coordinate Coordinate { get; set; }
        public string Type { get; set; }
        public int Rotation { get; set; }

    }

    public class FurnitureProperty
    {
        public List<Coordinate> Coordinate { get; set; }
        public string Type { get; set; }
        public int Rotation { get; set; }
    }

    public class ElementDeserializer
    {
        public List<WallProperty> WallProperties { get; set; }
        public List<WindowProperty> WindowProperties { get; set; }
        public List<DoorProperty> DoorProperties { get; set; }
        public List<HostedProperty> HostedProperties { get; set; }
        public List<FurnitureProperty> FurnitureProperties { get; set; }
    }

    interface IHosted
    {
        Coordinate Coordinate { get; }

        string Type { get; }

        int Rotation { get; }

    }
}
