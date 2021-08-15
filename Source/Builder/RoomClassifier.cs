using System.Collections.Generic;

namespace CustomizacaoMoradias.Source.Builder
{
    public class RoomElement
    {
        public string Name { get; set; }
        public int Score { get; set; }
    }

    class RoomClassifier
    {
        public RoomClassifier()
        {
            RoomScore = 0;
        }

        public string Name { get; set; }
        public List<RoomElement> Element { get; set; }
        public int RoomScore { get; set; }

        public int GetElementScore(string elementName)
        {
            foreach(RoomElement roomElement in Element)
            {
                if (roomElement.Name.Equals(elementName))
                    return roomElement.Score;
            }
            return 0;
        }

    }
}
