﻿using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CustomizacaoMoradias
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    class DimensionCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var uidoc = commandData.Application.ActiveUIDocument;
                var baseLevel = Properties.Settings.Default.BaseLevelName;
                var topLevel = Properties.Settings.Default.TopLevelName;
                var scale = Properties.Settings.Default.Scale;
                HouseBuilder elementPlacer = new HouseBuilder(uidoc.Document, baseLevel, topLevel, scale);
                using (Transaction transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "Dimensioning"))
                {
                    transaction.Start();
                    elementPlacer.DimensioningBuilding(2, false);
                    elementPlacer.DimensioningBuilding(4, true);
                    transaction.Commit();
                }
                return Result.Succeeded;
            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }
        }
    }
}
