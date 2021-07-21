using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomizacaoMoradias.Source
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
        public List<Coordinate> Coordinate { get; set; }
        public string Type { get; set; }
        public int Rotation { get; set; }

        Coordinate IHosted.Coordinate => new Coordinate
        {
            
            /*X = (Coordinate.ElementAt(0).X + Coordinate.ElementAt(1).X) / 2,
            Y = (Coordinate.ElementAt(0).Y + Coordinate.ElementAt(1).Y) / 2*/
            X = (Coordinate.ElementAt(1).X),
            Y = (Coordinate.ElementAt(1).Y)
        };
    }

    public class DoorProperty : IHosted
    {
        public List<Coordinate> Coordinate { get; set; }
        public string Type { get; set; }
        public int Rotation { get; set; }

        Coordinate IHosted.Coordinate => new Coordinate
        {
            /*X = (Coordinate.ElementAt(0).X + Coordinate.ElementAt(1).X) / 2,
            Y = (Coordinate.ElementAt(0).Y + Coordinate.ElementAt(1).Y) / 2*/
            X = (Coordinate.ElementAt(1).X),
            Y = (Coordinate.ElementAt(1).Y)
        };      
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
        public ElementDeserializer()
        {
            WallProperties = new List<WallProperty>();
            WindowProperties = new List<WindowProperty>();
            DoorProperties = new List<DoorProperty>();
            HostedProperties = new List<HostedProperty>();
            FurnitureProperties = new List<FurnitureProperty>();
        }

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
