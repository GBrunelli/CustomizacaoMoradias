using System;
using System.Data;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace CustomizacaoMoradias.Forms
{
    public partial class RoomConfigControl : UserControl
    {
        string connectionString = Properties.Settings.Default.PropertiesDatabaseConnectionString;

        public RoomConfigControl()
        {
            InitializeComponent();
            PopulateDataGridView();
        }

        private void roomDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if(roomDataGridView.CurrentRow != null)
            {
                DataGridViewRow dgvRow = roomDataGridView.CurrentRow;
                if (dgvRow.Cells["nameDataGridViewTextBoxColumn"].Value == DBNull.Value)
                {
                    DeleteSelectecRow(dgvRow);
                }
                else
                {
                    ChangeSelectedRow(dgvRow);
                }  
            }
            PopulateDataGridView();
        }
        

        private void roomDataGridView_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            DataGridViewRow dgvRow = roomDataGridView.CurrentRow;
            DeleteSelectecRow(dgvRow);
            PopulateDataGridView();
        }

        private void ChangeSelectedRow(DataGridViewRow dgvRow)
        {
            using (SqlConnection sqlCon = new SqlConnection(connectionString))
            {
                sqlCon.Open();
                SqlCommand sqlCmd = new SqlCommand("[dbo].[RoomAddOrEdit]", sqlCon) { CommandType = CommandType.StoredProcedure };

                // Insert
                if (dgvRow.Cells["roomIDDataGridViewTextBoxColumn"].Value == DBNull.Value)
                    sqlCmd.Parameters.AddWithValue("RoomID", 0);
                // Update  
                else
                    sqlCmd.Parameters.AddWithValue("RoomID", Convert.ToInt32((dgvRow.Cells["roomIDDataGridViewTextBoxColumn"].Value)));

                sqlCmd.Parameters.AddWithValue("Name", dgvRow.Cells["nameDataGridViewTextBoxColumn"].Value == DBNull.Value ?
                    "" : dgvRow.Cells["nameDataGridViewTextBoxColumn"].Value.ToString());
                sqlCmd.ExecuteNonQuery();
            }
        }

        private void DeleteSelectecRow(DataGridViewRow dgvRow)
        {
            try
            {
                if (dgvRow.Cells["roomIDDataGridViewTextBoxColumn"].Value != DBNull.Value)
                {
                    if (MessageBox.Show("Tem certeza que deseja deletar esse ambiente?", "Atenção!", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        using (SqlConnection sqlCon = new SqlConnection(connectionString))
                        {
                            sqlCon.Open();
                            SqlCommand sqlCmd = new SqlCommand("[dbo].[RoomDeleteByID]", sqlCon) { CommandType = CommandType.StoredProcedure };
                            sqlCmd.Parameters.AddWithValue("@RoomID",
                                Convert.ToInt32(dgvRow.Cells["roomIDDataGridViewTextBoxColumn"].Value));
                            sqlCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (SqlException)
            {
                MessageBox.Show("Você está tentando deletar um ambiente que está sendo usado na classificação de ambientes, " +
                    "delete todas as referências desse ambiente primeiro.", "Erro", MessageBoxButtons.OK);
            }
        }
        private void PopulateDataGridView()
        {
            using (SqlConnection sqlCon = new SqlConnection(connectionString))
            {
                sqlCon.Open();
                SqlDataAdapter sqlDa = new SqlDataAdapter("SELECT * FROM Room", sqlCon);
                DataTable dtbl = new DataTable();
                sqlDa.Fill(dtbl);
                roomDataGridView.DataSource = dtbl;
            }
        }
    }
}
