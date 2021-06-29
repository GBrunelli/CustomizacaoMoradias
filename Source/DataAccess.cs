using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CustomizacaoMoradias.DataModel;
using Dapper;

namespace CustomizacaoMoradias.Source
{
    class DataAccess
    {
        public List<RoomDM> GetRooms()
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.PropertiesDatabaseConnectionString))
            {
                return connection.Query<RoomDM>("SELECT * FROM Room").ToList();
            }
        }


        public List<ScoreDM> GetRoomElementsScore()
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.PropertiesDatabaseConnectionString))
            {
                return connection.Query<ScoreDM>(
                    "SELECT r.RoomID, r.Name as 'RoomName', e.Name as 'ElementName', er.Score " +
                    "FROM Room r " +
                    "JOIN Element_Room er ON r.RoomID = er.RoomID " +
                    "JOIN Element e ON er.ElementID = e.ElementID"
                    ).ToList();
            }
        }

        public List<ElementDM> GetElement()
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.PropertiesDatabaseConnectionString))
            {
                return connection.Query<ElementDM>("SELECT * FROM Element").ToList();
            }
        }

        public List<ElementDM> GetElement(string elementID)
        {
            using (IDbConnection connection = new System.Data.SqlClient.SqlConnection(Properties.Settings.Default.PropertiesDatabaseConnectionString))
            {
                return connection.Query<ElementDM>(
                    "SELECT * FROM Element e " +
                    $"WHERE e.ElementID = '{elementID}'"
                    ).ToList();
            }
        }
    }
}
