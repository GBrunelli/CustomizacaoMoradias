using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomizacaoMoradias.Forms
{
    public partial class ElementConfigControl : UserControl
    {
        string connectionString = Properties.Settings.Default.PropertiesDatabaseConnectionString;

        public ElementConfigControl()
        {
            InitializeComponent();
            PopulateDataGridView();
        }

        private void elementDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (elementDataGridView.CurrentRow != null)
            {
                DataGridViewRow dgvRow = elementDataGridView.CurrentRow;
                bool id = dgvRow.Cells["elementIDDataGridViewTextBoxColumn"].Value != DBNull.Value;
                bool name = dgvRow.Cells["nameDataGridViewTextBoxColumn"].Value != DBNull.Value;
                     
                if (!name && !id)
                {
                    DeleteSelectecRow(dgvRow);
                    PopulateDataGridView();
                }
                else if(name && id)
                {
                    ChangeSelectedRow(dgvRow);
                    PopulateDataGridView();
                }
            }
        }

        private void elementDataGridView_UserDeletedRow(object sender, DataGridViewRowEventArgs e)
        {
            DataGridViewRow dgvRow = elementDataGridView.CurrentRow;
            DeleteSelectecRow(dgvRow);
            PopulateDataGridView();
        }

        private void ChangeSelectedRow(DataGridViewRow dgvRow)
        {
            using (SqlConnection sqlCon = new SqlConnection(connectionString))
            {
                sqlCon.Open();
                SqlCommand sqlCmd = new SqlCommand("[dbo].[ElementAddOrEdit]", sqlCon) { CommandType = CommandType.StoredProcedure };

                // Insert
                if (dgvRow.Cells["elementIDDataGridViewTextBoxColumn"].Value == DBNull.Value)
                    sqlCmd.Parameters.AddWithValue("ElementID", "@@@");
                // Update  
                else
                    sqlCmd.Parameters.AddWithValue("ElementID", (dgvRow.Cells["elementIDDataGridViewTextBoxColumn"].Value));

                sqlCmd.Parameters.AddWithValue("Name", dgvRow.Cells["nameDataGridViewTextBoxColumn"].Value == DBNull.Value ?
                    "" : dgvRow.Cells["nameDataGridViewTextBoxColumn"].Value.ToString());
                sqlCmd.ExecuteNonQuery();
            }
        }

        private void DeleteSelectecRow(DataGridViewRow dgvRow)
        {
            try
            {
                if (dgvRow.Cells["elementIDDataGridViewTextBoxColumn"].Value != DBNull.Value)
                {
                    if (MessageBox.Show("Tem certeza que deseja deletar esse elemento?", "Atenção!", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        using (SqlConnection sqlCon = new SqlConnection(connectionString))
                        {
                            sqlCon.Open();
                            SqlCommand sqlCmd = new SqlCommand("[dbo].[ElementDeleteByID]", sqlCon) { CommandType = CommandType.StoredProcedure };
                            sqlCmd.Parameters.AddWithValue("@RoomID", dgvRow.Cells["elementIDDataGridViewTextBoxColumn"].Value);
                            sqlCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (SqlException)
            {
                MessageBox.Show("Você está tentando deletar um elemento que está sendo usado na classificação de ambientes, " +
                    "delete todas as referências desse elemento primeiro.", "Erro", MessageBoxButtons.OK);
            }
        }

        private void PopulateDataGridView()
        {
            using (SqlConnection sqlCon = new SqlConnection(connectionString))
            {
                sqlCon.Open();
                SqlDataAdapter sqlDa = new SqlDataAdapter("SELECT * FROM Element", sqlCon);
                DataTable dtbl = new DataTable();
                sqlDa.Fill(dtbl);
                elementDataGridView.DataSource = dtbl;
            }
        }
    }
}
