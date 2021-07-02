using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;

namespace CustomizacaoMoradias.Forms
{
    public partial class ClassifierConfigControl : UserControl
    {

        private readonly static string CONNECTION_STRING = Properties.Settings.Default.PropertiesDatabaseConnectionString;
        public ClassifierConfigControl()
        {
            InitializeComponent();
        }

        private void PopulateComboBox(string table, 
            DataGridViewComboBoxColumn comboBoxColumn, 
            string valueMember, 
            string displayMember)
        {
            using (SqlConnection sqlCon = new SqlConnection(CONNECTION_STRING))
            {
                sqlCon.Open();
                SqlDataAdapter sqlDa = new SqlDataAdapter($"SELECT * FROM {table}", sqlCon);
                DataTable dtbl = new DataTable();
                sqlDa.Fill(dtbl);
                comboBoxColumn.ValueMember = valueMember;
                comboBoxColumn.DisplayMember = displayMember;
                DataRow topItem = dtbl.NewRow();
                topItem[0] = 0;
                topItem[1] = "Selecionar";
                dtbl.Rows.InsertAt(topItem, 0);
                comboBoxColumn.DataSource = dtbl;
            }
        }

        private void PopulateDataGridView()
        {
            using (SqlConnection sqlCon = new SqlConnection(CONNECTION_STRING))
            {
                sqlCon.Open();
                SqlDataAdapter sqlDa = new SqlDataAdapter("SELECT * FROM Element_Room", sqlCon);
                DataTable dtbl = new DataTable();
                sqlDa.Fill(dtbl);
                element_roomDataGridView.DataSource = dtbl;
            }
        }

        private void ClassifierConfigControl_Load(object sender, EventArgs e)
        {
            PopulateComboBox("Element", cbxElement, "ElementID", "Name");
            PopulateComboBox("Room", cbxRoom, "RoomID", "Name");
            PopulateDataGridView();
        }

        private void element_roomDataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (element_roomDataGridView.CurrentRow != null)
            {
                DataGridViewRow dgvRow = element_roomDataGridView.CurrentRow;
                bool elementID = dgvRow.Cells["cbxElement"].Value != DBNull.Value;
                bool roomID = dgvRow.Cells["cbxRoom"].Value != DBNull.Value;
                bool score = dgvRow.Cells["txtScore"].Value != DBNull.Value;

                if(elementID && roomID && score)
                {
                    if (dgvRow.Cells["txtScore"].Value.ToString().Trim() == "0")                 
                        DeleteSelectedRow(dgvRow);
                    else
                        ChangeSelectedRow(dgvRow);

                    PopulateDataGridView();
                }
            }
        }

        public void DeleteSelectedRow(DataGridViewRow dgvRow)
        {
            if (MessageBox.Show("Tem certeza que deseja deletar essa relação?", "Atenção!", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                using (SqlConnection sqlCon = new SqlConnection(CONNECTION_STRING))
                {
                    sqlCon.Open();
                    SqlCommand sqlCmd = new SqlCommand("[dbo].[Element_RoomDelete]", sqlCon) { CommandType = CommandType.StoredProcedure };
                    sqlCmd.Parameters.AddWithValue("@ElementID", dgvRow.Cells["cbxElement"].Value.ToString());
                    sqlCmd.Parameters.AddWithValue("@RoomID", Convert.ToInt32(dgvRow.Cells["cbxRoom"].Value.ToString()));
                    sqlCmd.ExecuteNonQuery();
                }
            }
        }

        private void ChangeSelectedRow(DataGridViewRow dgvRow)
        {
            using (SqlConnection sqlCon = new SqlConnection(CONNECTION_STRING))
            {
                sqlCon.Open();
                SqlCommand sqlCmd = new SqlCommand("[dbo].[Element_RoomAddOrEdit]", sqlCon) { CommandType = CommandType.StoredProcedure };
                sqlCmd.Parameters.AddWithValue("@ElementID", dgvRow.Cells["cbxElement"].Value.ToString());
                sqlCmd.Parameters.AddWithValue("@RoomID", Convert.ToInt32(dgvRow.Cells["cbxRoom"].Value.ToString()));
                sqlCmd.Parameters.AddWithValue("@Score", Convert.ToInt32(dgvRow.Cells["txtScore"].Value.ToString()));
                sqlCmd.ExecuteNonQuery();
            }
        }
    }
}
