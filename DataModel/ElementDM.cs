using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomizacaoMoradias.DataModel;

namespace CustomizacaoMoradias.Source
{
    class ElementDM
    {
        public string ElementID { get; set; }

        public string Name { get; set; }

        public List<Element_RoomDM> Rooms;
    }
}
