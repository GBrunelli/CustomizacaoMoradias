
namespace CustomizacaoMoradias.Forms
{
    partial class ClassifierConfigControl
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            this.element_roomDataGridView = new System.Windows.Forms.DataGridView();
            this.cbxElement = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.cbxRoom = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.txtScore = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.element_roomDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // element_roomDataGridView
            // 
            this.element_roomDataGridView.AllowUserToResizeColumns = false;
            this.element_roomDataGridView.AllowUserToResizeRows = false;
            this.element_roomDataGridView.BackgroundColor = System.Drawing.SystemColors.Control;
            this.element_roomDataGridView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.element_roomDataGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            this.element_roomDataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.element_roomDataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.cbxElement,
            this.cbxRoom,
            this.txtScore});
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.element_roomDataGridView.DefaultCellStyle = dataGridViewCellStyle1;
            this.element_roomDataGridView.GridColor = System.Drawing.SystemColors.ActiveBorder;
            this.element_roomDataGridView.Location = new System.Drawing.Point(3, 3);
            this.element_roomDataGridView.Name = "element_roomDataGridView";
            this.element_roomDataGridView.RowHeadersVisible = false;
            this.element_roomDataGridView.Size = new System.Drawing.Size(442, 397);
            this.element_roomDataGridView.TabIndex = 2;
            this.element_roomDataGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.element_roomDataGridView_CellValueChanged);
            // 
            // cbxElement
            // 
            this.cbxElement.DataPropertyName = "ElementID";
            this.cbxElement.HeaderText = "Element";
            this.cbxElement.Name = "cbxElement";
            this.cbxElement.Width = 180;
            // 
            // cbxRoom
            // 
            this.cbxRoom.DataPropertyName = "RoomID";
            this.cbxRoom.HeaderText = "Room";
            this.cbxRoom.Name = "cbxRoom";
            this.cbxRoom.Width = 180;
            // 
            // txtScore
            // 
            this.txtScore.DataPropertyName = "Score";
            this.txtScore.HeaderText = "Score";
            this.txtScore.Name = "txtScore";
            this.txtScore.Width = 60;
            // 
            // ClassifierConfigControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.element_roomDataGridView);
            this.Name = "ClassifierConfigControl";
            this.Size = new System.Drawing.Size(448, 403);
            this.Load += new System.EventHandler(this.ClassifierConfigControl_Load);
            ((System.ComponentModel.ISupportInitialize)(this.element_roomDataGridView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView element_roomDataGridView;
        private System.Windows.Forms.DataGridViewComboBoxColumn cbxElement;
        private System.Windows.Forms.DataGridViewComboBoxColumn cbxRoom;
        private System.Windows.Forms.DataGridViewTextBoxColumn txtScore;
    }
}
