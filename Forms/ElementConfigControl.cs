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
        private bool inDataSource = false;

        private string lastID;

        private readonly static string CONNECTION_STRING = Properties.Settings.Default.PropertiesDatabaseConnectionString;

        public ElementConfigControl()
        {
            InitializeComponent();
            PopulateDataGridView();
        }

        private void elementDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (inDataSource) return;

            if (elementDataGridView.CurrentRow != null)
            {
                DataGridViewRow dgvRow = elementDataGridView.CurrentRow;
                bool id = dgvRow.Cells["elementIDDataGridViewTextBoxColumn"].Value != DBNull.Value;
                bool name = dgvRow.Cells["nameDataGridViewTextBoxColumn"].Value != DBNull.Value;              
             
                if (!(id || name))
                {
                    DeleteSelectecRow(dgvRow);
                    PopulateDataGridView();
                }
                else if(id && name)
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
            using (SqlConnection sqlCon = new SqlConnection(CONNECTION_STRING))
            {
                sqlCon.Open();
                SqlCommand sqlCmd = new SqlCommand("[dbo].[ElementAddOrEdit]", sqlCon) { CommandType = CommandType.StoredProcedure };
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
                if (MessageBox.Show("Tem certeza que deseja deletar esse elemento?", "Atenção!", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    using (SqlConnection sqlCon = new SqlConnection(CONNECTION_STRING))
                    {
                        sqlCon.Open();
                        SqlCommand sqlCmd = new SqlCommand("[dbo].[ElementDeleteByID]", sqlCon) { CommandType = CommandType.StoredProcedure };
                        sqlCmd.Parameters.AddWithValue("@ElementID", lastID);
                        sqlCmd.ExecuteNonQuery();
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
            try
            {
                using (SqlConnection sqlCon = new SqlConnection(CONNECTION_STRING))
                {
                    sqlCon.Open();
                    SqlDataAdapter sqlDa = new SqlDataAdapter("SELECT * FROM Element", sqlCon);
                    DataTable dtbl = new DataTable();
                    sqlDa.Fill(dtbl);
                    inDataSource = true;
                    elementDataGridView.DataSource = dtbl;
                    inDataSource = false;
                }
            }
            catch (Exception e) {
                MessageBox.Show(e.Message, "Erro", MessageBoxButtons.OK);
            }
        }

        private void elementDataGridView_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (elementDataGridView.CurrentRow != null)
            {
                DataGridViewRow dgvRow = elementDataGridView.CurrentRow;
                if (dgvRow.Cells["elementIDDataGridViewTextBoxColumn"].Value != DBNull.Value)
                {
                    lastID = dgvRow.Cells["elementIDDataGridViewTextBoxColumn"].Value.ToString();
                }
            }
        }
    }
}
