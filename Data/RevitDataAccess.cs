using System;
using System.Linq;
using Autodesk.Revit.DB;

namespace CustomizacaoMoradias.Data
{
    class RevitDataAccess
    {
        private Document doc;

        public RevitDataAccess(Document doc)
        {
            this.doc = doc;
        }

        /// <summary>
        /// Get the FamilySymbol given its name.
        /// </summary>
        public FamilySymbol GetFamilySymbol(string fsFamilyName)
        {
            // Retrieve the familySymbol of the piece of furniture
            FamilySymbol symbol = (from familySymbol in new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).
                 Cast<FamilySymbol>()
                                   where (familySymbol.Name == fsFamilyName)
                                   select familySymbol).First();
            return symbol;
        }

        public View GetLevelView(string levelName)
        {
            View view = (from v in new FilteredElementCollector(doc).OfClass(typeof(View))
                         .Cast<View>()
                         where (v.Name == levelName)
                         select v).First();
            return view;
        }

        /// <summary>
        /// Searches in the document data base for a Wall Type.
        /// </summary>
        /// <returns>
        /// Returns the Wall Type corresponding to the string.
        /// </returns>
        public WallType GetWallType(string wallTypeName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(WallType));
            WallType wallType = collector.First(y => y.Name == wallTypeName) as WallType;
            return wallType;
        }

        /// <summary>
        /// Finds a level from its name
        /// </summary>
        /// <param name="levelName">
        /// The name as it is on Revit.
        /// </param>
        /// <returns>
        /// Returns the Level.
        /// </returns>
        public Level GetLevel(string levelName)
        {
            Level level;
            try
            {
                level = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Levels)
                    .WhereElementIsNotElementType()
                    .Cast<Level>()
                    .First(x => x.Name == levelName);
            }
            catch (Exception e)
            {
                throw new LevelNotFoundException("Nível \"" + levelName + "\" não encontrado.", e);
            }
            return level;
        }
    }
}
