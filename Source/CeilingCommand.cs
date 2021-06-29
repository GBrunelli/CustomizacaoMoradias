﻿using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace CustomizacaoMoradias
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    public class CeilingCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {

                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                ElementPlacer elementPlacer = new ElementPlacer(uidoc.Document, "PLANTA BAIXA", "COBERTURA", 0.3);

                using(Transaction transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "Ceiling Command."))
                {
                    transaction.Start();
                    elementPlacer.CreateCeiling("laje 10 cm - branca");
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