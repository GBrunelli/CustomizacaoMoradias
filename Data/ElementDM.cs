using System.Collections.Generic;

namespace CustomizacaoMoradias.Data
{
    internal class ElementDM
    {
        public string ElementID { get; set; }

        public string Name { get; set; }

        public float OffsetX { get; set; }

        public float OffsetY { get; set; }

        public List<Element_RoomDM> Rooms;
    }
}
