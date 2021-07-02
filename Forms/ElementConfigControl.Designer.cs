
namespace CustomizacaoMoradias.Forms
{
    partial class ElementConfigControl
    {
        /// <summary> 
        /// Variável de designer necessária.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Limpar os recursos que estão sendo usados.
        /// </summary>
        /// <param name="disposing">true se for necessário descartar os recursos gerenciados; caso contrário, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código gerado pelo Designer de Componentes

        /// <summary> 
        /// Método necessário para suporte ao Designer - não modifique 
        /// o conteúdo deste método com o editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.propertiesDatabaseDataSet = new CustomizacaoMoradias.PropertiesDatabaseDataSet();
            this.elementDataGridView = new System.Windows.Forms.DataGridView();
            this.elementIDDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.nameDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.elementBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.elementTableAdapter = new CustomizacaoMoradias.PropertiesDatabaseDataSetTableAdapters.ElementTableAdapter();
            ((System.ComponentModel.ISupportInitialize)(this.propertiesDatabaseDataSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.elementDataGridView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.elementBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // propertiesDatabaseDataSet
            // 
            this.propertiesDatabaseDataSet.DataSetName = "PropertiesDatabaseDataSet";
            this.propertiesDatabaseDataSet.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // elementDataGridView
            // 
            this.elementDataGridView.AllowUserToResizeColumns = false;
            this.elementDataGridView.AllowUserToResizeRows = false;
            this.elementDataGridView.AutoGenerateColumns = false;
            this.elementDataGridView.BackgroundColor = System.Drawing.SystemColors.Control;
            this.elementDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.elementDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            this.elementDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.elementDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.elementIDDataGridViewTextBoxColumn,
            this.nameDataGridViewTextBoxColumn});
            this.elementDataGridView.DataSource = this.elementBindingSource;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.elementDataGridView.DefaultCellStyle = dataGridViewCellStyle1;
            this.elementDataGridView.GridColor = System.Drawing.SystemColors.ActiveBorder;
            this.elementDataGridView.Location = new System.Drawing.Point(3, 3);
            this.elementDataGridView.Name = "elementDataGridView";
            this.elementDataGridView.RowHeadersVisible = false;
            this.elementDataGridView.Size = new System.Drawing.Size(442, 397);
            this.elementDataGridView.TabIndex = 2;
            this.elementDataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.elementDataGridView_CellClick);
            this.elementDataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.elementDataGridView_CellValueChanged);
            this.elementDataGridView.UserDeletedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.elementDataGridView_UserDeletedRow);
            // 
            // elementIDDataGridViewTextBoxColumn
            // 
            this.elementIDDataGridViewTextBoxColumn.DataPropertyName = "ElementID";
            this.elementIDDataGridViewTextBoxColumn.HeaderText = "ElementID";
            this.elementIDDataGridViewTextBoxColumn.Name = "elementIDDataGridViewTextBoxColumn";
            // 
            // nameDataGridViewTextBoxColumn
            // 
            this.nameDataGridViewTextBoxColumn.DataPropertyName = "Name";
            this.nameDataGridViewTextBoxColumn.HeaderText = "Name";
            this.nameDataGridViewTextBoxColumn.Name = "nameDataGridViewTextBoxColumn";
            this.nameDataGridViewTextBoxColumn.Width = 320;
            // 
            // elementBindingSource
            // 
            this.elementBindingSource.DataMember = "Element";
            this.elementBindingSource.DataSource = this.propertiesDatabaseDataSet;
            // 
            // elementTableAdapter
            // 
            this.elementTableAdapter.ClearBeforeFill = true;
            // 
            // ElementConfigControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.elementDataGridView);
            this.Name = "ElementConfigControl";
            this.Size = new System.Drawing.Size(448, 403);
            ((System.ComponentModel.ISupportInitialize)(this.propertiesDatabaseDataSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.elementDataGridView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.elementBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private PropertiesDatabaseDataSet propertiesDatabaseDataSet;
        private System.Windows.Forms.DataGridView elementDataGridView;
        private System.Windows.Forms.BindingSource elementBindingSource;
        private PropertiesDatabaseDataSetTableAdapters.ElementTableAdapter elementTableAdapter;
        private System.Windows.Forms.DataGridViewTextBoxColumn elementIDDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn nameDataGridViewTextBoxColumn;
    }
}
