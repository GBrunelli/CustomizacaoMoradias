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
        public int x { get; set; }
        public int y { get; set; }
    }

    public class WallProperty
    {
        public List<Coordinate> Coordinate { get; set; }
    }

    public class WindowProperty
    {
        public List<Coordinate> Coordinate { get; set; }
        public string Type { get; set; }
        public int Rotation { get; set; }
    }

    public class DoorProperty
    {
        public List<Coordinate> Coordinate { get; set; }
        public string Type { get; set; }
        public int Rotation { get; set; }
    }

    public class HostedProperty
    {
        public Coordinate Coordinate { get; set; }
        public string Type { get; set; }
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




}
