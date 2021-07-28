using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Globalization;

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
                try
                {
                    DataGridViewRow dgvRow = elementDataGridView.CurrentRow;
                    bool id = dgvRow.Cells["elementIDDataGridViewTextBoxColumn"].Value != DBNull.Value;
                    bool name = dgvRow.Cells["nameDataGridViewTextBoxColumn"].Value != DBNull.Value;

                    if (!(id || name))
                    {
                        DeleteSelectecRow(dgvRow);
                        PopulateDataGridView();
                    }
                    else if (id && name)
                    {
                        ChangeSelectedRow(dgvRow);
                        PopulateDataGridView();
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Erro!", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private float[] GetVectorComponents(string vector)
        {
            CultureInfo provider = new CultureInfo("en-US");
            string[] tokens = vector.Replace('(', ' ').Replace(')', ' ').Split(',');
            if (tokens.Length != 2) throw new FormatException("Vetor formatato incorretamente");
            float[] components = new float[2];
            components[0] = float.Parse(tokens[0], provider);
            components[1] = float.Parse(tokens[1], provider);
            return components;
        }

        private void ChangeSelectedRow(DataGridViewRow dgvRow)
        {
            using (SqlConnection sqlCon = new SqlConnection(CONNECTION_STRING))
            {
                sqlCon.Open();
                SqlCommand sqlCmd = new SqlCommand("[dbo].[ElementAddOrEdit]", sqlCon) { CommandType = CommandType.StoredProcedure };
                float[] components = GetVectorComponents(dgvRow.Cells["Offset"].Value.ToString());
                sqlCmd.Parameters.AddWithValue("ElementID", dgvRow.Cells["elementIDDataGridViewTextBoxColumn"].Value);
                sqlCmd.Parameters.AddWithValue("Name", dgvRow.Cells["nameDataGridViewTextBoxColumn"].Value.ToString());
                sqlCmd.Parameters.AddWithValue("OffsetX", components[0]); 
                sqlCmd.Parameters.AddWithValue("OffsetY", components[1]);
                sqlCmd.ExecuteNonQuery();
            }
        }

        private void DeleteSelectecRow(DataGridViewRow dgvRow)
        {
            try
            {
                if (MessageBox.Show("Tem certeza que deseja deletar esse elemento?", "Atenção!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
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
                    "delete todas as referências desse elemento primeiro.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    dtbl.Columns.Add("Offset", typeof(string));

                    foreach(DataRow row in dtbl.Rows)
                    {
                        double x = Math.Round((double)row["OffsetX"], 2); 
                        double y = Math.Round((double)row["OffsetY"], 2);
                        row["Offset"] = $"({Convert.ToString(x, new CultureInfo("en-US"))}, {Convert.ToString(y, new CultureInfo("en-US"))})";
                    }
                    inDataSource = true;
                    elementDataGridView.DataSource = dtbl;
                    inDataSource = false;
                }
            }
            catch (Exception e) {
                MessageBox.Show(e.Message, "Erro!", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
