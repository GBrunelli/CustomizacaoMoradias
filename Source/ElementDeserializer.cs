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
    }

    public class WindowProperty : Hosted
    {
        public List<Coordinate> Coordinate { get; set; }
        public string Type { get; set; }
        public int Rotation { get; set; }

        public Coordinate GetCoordinate()
        {
            return new Coordinate
            {
                X = (Coordinate.ElementAt(0).X + Coordinate.ElementAt(1).X) / 2,
                Y = (Coordinate.ElementAt(0).Y + Coordinate.ElementAt(1).Y) / 2
            };
        }
        public string getType()
        {
            return Type;
        }
    }

    public class DoorProperty : Hosted
    {
        public List<Coordinate> Coordinate { get; set; }
        public string Type { get; set; }
        public int Rotation { get; set; }

        public Coordinate GetCoordinate()
        {
            return new Coordinate
            {
                X = (Coordinate.ElementAt(0).X + Coordinate.ElementAt(1).X) / 2,
                Y = (Coordinate.ElementAt(0).Y + Coordinate.ElementAt(1).Y) / 2
            };
        }

        public string getType()
        {
            return Type;
        }
    }

    public class HostedProperty : Hosted
    {
        public Coordinate Coordinate { get; set; }
        public string Type { get; set; }

        public Coordinate GetCoordinate()
        {
            return Coordinate;
        }

        public string getType()
        {
            return Type;
        }
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

    interface Hosted
    {
        Coordinate GetCoordinate();
        string getType();
    }
}
