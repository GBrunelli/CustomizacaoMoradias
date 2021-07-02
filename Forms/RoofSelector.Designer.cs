
namespace CustomizacaoMoradias.Forms
{
    partial class RoofSelector
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
            this.edgeUp = new System.Windows.Forms.Panel();
            this.edgeLeft = new System.Windows.Forms.Panel();
            this.edgeRight = new System.Windows.Forms.Panel();
            this.middleVertical = new System.Windows.Forms.Panel();
            this.middleHorizontal = new System.Windows.Forms.Panel();
            this.edgeBottom = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // edgeUp
            // 
            this.edgeUp.BackColor = System.Drawing.SystemColors.ControlDark;
            this.edgeUp.Location = new System.Drawing.Point(13, 3);
            this.edgeUp.Name = "edgeUp";
            this.edgeUp.Size = new System.Drawing.Size(150, 10);
            this.edgeUp.TabIndex = 0;
            this.edgeUp.Click += new System.EventHandler(this.edgeUp_Click);
            // 
            // edgeLeft
            // 
            this.edgeLeft.BackColor = System.Drawing.SystemColors.ControlDark;
            this.edgeLeft.Location = new System.Drawing.Point(3, 13);
            this.edgeLeft.Name = "edgeLeft";
            this.edgeLeft.Size = new System.Drawing.Size(10, 150);
            this.edgeLeft.TabIndex = 2;
            this.edgeLeft.Click += new System.EventHandler(this.edgeLeft_Click);
            // 
            // edgeRight
            // 
            this.edgeRight.BackColor = System.Drawing.SystemColors.ControlDark;
            this.edgeRight.Location = new System.Drawing.Point(163, 13);
            this.edgeRight.Name = "edgeRight";
            this.edgeRight.Size = new System.Drawing.Size(10, 150);
            this.edgeRight.TabIndex = 3;
            this.edgeRight.Click += new System.EventHandler(this.edgeRight_Click);
            // 
            // middleVertical
            // 
            this.middleVertical.BackColor = System.Drawing.SystemColors.ControlDark;
            this.middleVertical.Location = new System.Drawing.Point(85, 13);
            this.middleVertical.Name = "middleVertical";
            this.middleVertical.Size = new System.Drawing.Size(5, 150);
            this.middleVertical.TabIndex = 4;
            this.middleVertical.Click += new System.EventHandler(this.middleVertical_Click);
            // 
            // middleHorizontal
            // 
            this.middleHorizontal.BackColor = System.Drawing.SystemColors.ControlDark;
            this.middleHorizontal.Location = new System.Drawing.Point(13, 88);
            this.middleHorizontal.Name = "middleHorizontal";
            this.middleHorizontal.Size = new System.Drawing.Size(150, 5);
            this.middleHorizontal.TabIndex = 1;
            this.middleHorizontal.Click += new System.EventHandler(this.middleHorizontal_Click);
            // 
            // edgeBottom
            // 
            this.edgeBottom.BackColor = System.Drawing.SystemColors.ControlDark;
            this.edgeBottom.Location = new System.Drawing.Point(13, 163);
            this.edgeBottom.Name = "edgeBottom";
            this.edgeBottom.Size = new System.Drawing.Size(150, 10);
            this.edgeBottom.TabIndex = 1;
            this.edgeBottom.Click += new System.EventHandler(this.edgeBottom_Click);
            // 
            // RoofSelector
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.middleHorizontal);
            this.Controls.Add(this.middleVertical);
            this.Controls.Add(this.edgeRight);
            this.Controls.Add(this.edgeLeft);
            this.Controls.Add(this.edgeBottom);
            this.Controls.Add(this.edgeUp);
            this.Name = "RoofSelector";
            this.Size = new System.Drawing.Size(175, 175);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel edgeUp;
        private System.Windows.Forms.Panel edgeLeft;
        private System.Windows.Forms.Panel edgeRight;
        private System.Windows.Forms.Panel middleVertical;
        private System.Windows.Forms.Panel middleHorizontal;
        private System.Windows.Forms.Panel edgeBottom;
    }
}
