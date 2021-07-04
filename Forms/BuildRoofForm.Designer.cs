
namespace CustomizacaoMoradias.Forms
{
    partial class BuildRoofForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.roofSelectorLabel = new System.Windows.Forms.Label();
            this.roofSelectorPanel = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(207, 13);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(75, 23);
            this.cancelButton.TabIndex = 5;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // okButton
            // 
            this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(126, 13);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 4;
            this.okButton.Text = "Ok";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.panel1.Controls.Add(this.cancelButton);
            this.panel1.Controls.Add(this.okButton);
            this.panel1.Location = new System.Drawing.Point(0, 223);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(295, 47);
            this.panel1.TabIndex = 16;
            // 
            // roofSelectorLabel
            // 
            this.roofSelectorLabel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.roofSelectorLabel.AutoSize = true;
            this.roofSelectorLabel.Location = new System.Drawing.Point(114, 9);
            this.roofSelectorLabel.Name = "roofSelectorLabel";
            this.roofSelectorLabel.Size = new System.Drawing.Size(72, 13);
            this.roofSelectorLabel.TabIndex = 18;
            this.roofSelectorLabel.Text = "Roof Selector";
            // 
            // roofSelectorPanel
            // 
            this.roofSelectorPanel.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.roofSelectorPanel.Location = new System.Drawing.Point(60, 25);
            this.roofSelectorPanel.Name = "roofSelectorPanel";
            this.roofSelectorPanel.Size = new System.Drawing.Size(175, 175);
            this.roofSelectorPanel.TabIndex = 17;
            // 
            // BuildRoofForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(294, 270);
            this.Controls.Add(this.roofSelectorLabel);
            this.Controls.Add(this.roofSelectorPanel);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "BuildRoofForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Build Roof";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.BuildRoofForm_Load);
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label roofSelectorLabel;
        private System.Windows.Forms.Panel roofSelectorPanel;
    }
}