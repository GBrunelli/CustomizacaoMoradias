namespace CustomizacaoMoradias
{
    partial class PlaceElementsForm
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
            this.fileLabel = new System.Windows.Forms.Label();
            this.fileTextBox = new System.Windows.Forms.TextBox();
            this.openButton = new System.Windows.Forms.Button();
            this.levelNameLabel = new System.Windows.Forms.Label();
            this.levelNameTextBox = new System.Windows.Forms.TextBox();
            this.cancelButton = new System.Windows.Forms.Button();
            this.okButton = new System.Windows.Forms.Button();
            this.topLevelNameLabel = new System.Windows.Forms.Label();
            this.topLevelNametextBox = new System.Windows.Forms.TextBox();
            this.roofTypeComboBox = new System.Windows.Forms.ComboBox();
            this.roofTypeLabel = new System.Windows.Forms.Label();
            this.RoofStyleComboBox = new System.Windows.Forms.ComboBox();
            this.roofStyleLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // fileLabel
            // 
            this.fileLabel.AutoSize = true;
            this.fileLabel.Location = new System.Drawing.Point(12, 9);
            this.fileLabel.Name = "fileLabel";
            this.fileLabel.Size = new System.Drawing.Size(23, 13);
            this.fileLabel.TabIndex = 6;
            this.fileLabel.Text = "File";
            // 
            // fileTextBox
            // 
            this.fileTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.fileTextBox.Location = new System.Drawing.Point(15, 25);
            this.fileTextBox.Name = "fileTextBox";
            this.fileTextBox.Size = new System.Drawing.Size(341, 20);
            this.fileTextBox.TabIndex = 0;
            this.fileTextBox.TextChanged += new System.EventHandler(this.fileTextBox_TextChanged);
            // 
            // openButton
            // 
            this.openButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.openButton.AutoSize = true;
            this.openButton.Location = new System.Drawing.Point(362, 23);
            this.openButton.Name = "openButton";
            this.openButton.Size = new System.Drawing.Size(52, 23);
            this.openButton.TabIndex = 1;
            this.openButton.Text = "Open";
            this.openButton.UseVisualStyleBackColor = true;
            this.openButton.Click += new System.EventHandler(this.openButton_Click);
            // 
            // levelNameLabel
            // 
            this.levelNameLabel.AutoSize = true;
            this.levelNameLabel.Location = new System.Drawing.Point(12, 48);
            this.levelNameLabel.Name = "levelNameLabel";
            this.levelNameLabel.Size = new System.Drawing.Size(91, 13);
            this.levelNameLabel.TabIndex = 7;
            this.levelNameLabel.Text = "Base Level Name";
            // 
            // levelNameTextBox
            // 
            this.levelNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.levelNameTextBox.Location = new System.Drawing.Point(15, 64);
            this.levelNameTextBox.Name = "levelNameTextBox";
            this.levelNameTextBox.Size = new System.Drawing.Size(399, 20);
            this.levelNameTextBox.TabIndex = 2;
            this.levelNameTextBox.TextChanged += new System.EventHandler(this.levelNameTextBox_TextChanged);
            // 
            // cancelButton
            // 
            this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(343, 221);
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
            this.okButton.Location = new System.Drawing.Point(262, 221);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(75, 23);
            this.okButton.TabIndex = 4;
            this.okButton.Text = "Ok";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // topLevelNameLabel
            // 
            this.topLevelNameLabel.AutoSize = true;
            this.topLevelNameLabel.Location = new System.Drawing.Point(12, 87);
            this.topLevelNameLabel.Name = "topLevelNameLabel";
            this.topLevelNameLabel.Size = new System.Drawing.Size(86, 13);
            this.topLevelNameLabel.TabIndex = 8;
            this.topLevelNameLabel.Text = "Top Level Name";
            // 
            // topLevelNametextBox
            // 
            this.topLevelNametextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.topLevelNametextBox.Location = new System.Drawing.Point(15, 103);
            this.topLevelNametextBox.Name = "topLevelNametextBox";
            this.topLevelNametextBox.Size = new System.Drawing.Size(399, 20);
            this.topLevelNametextBox.TabIndex = 3;
            this.topLevelNametextBox.TextChanged += new System.EventHandler(this.topLevelNametextBox_TextChanged);
            // 
            // roofTypeComboBox
            // 
            this.roofTypeComboBox.FormattingEnabled = true;
            this.roofTypeComboBox.IntegralHeight = false;
            this.roofTypeComboBox.Items.AddRange(new object[] {
            "(0, 0, 0)",
            "(1, 0, 0)",
            "(0, 1, 0) "});
            this.roofTypeComboBox.Location = new System.Drawing.Point(15, 186);
            this.roofTypeComboBox.Name = "roofTypeComboBox";
            this.roofTypeComboBox.Size = new System.Drawing.Size(121, 21);
            this.roofTypeComboBox.TabIndex = 9;
            // 
            // roofTypeLabel
            // 
            this.roofTypeLabel.AutoSize = true;
            this.roofTypeLabel.Location = new System.Drawing.Point(12, 168);
            this.roofTypeLabel.Name = "roofTypeLabel";
            this.roofTypeLabel.Size = new System.Drawing.Size(57, 13);
            this.roofTypeLabel.TabIndex = 10;
            this.roofTypeLabel.Text = "Roof Type";
            // 
            // RoofStyleComboBox
            // 
            this.RoofStyleComboBox.FormattingEnabled = true;
            this.RoofStyleComboBox.Items.AddRange(new object[] {
            "Hip Roof",
            "Gable Roof",
            "Hidden Roof"});
            this.RoofStyleComboBox.Location = new System.Drawing.Point(15, 144);
            this.RoofStyleComboBox.Name = "RoofStyleComboBox";
            this.RoofStyleComboBox.Size = new System.Drawing.Size(121, 21);
            this.RoofStyleComboBox.TabIndex = 11;
            // 
            // roofStyleLabel
            // 
            this.roofStyleLabel.AutoSize = true;
            this.roofStyleLabel.Location = new System.Drawing.Point(12, 126);
            this.roofStyleLabel.Name = "roofStyleLabel";
            this.roofStyleLabel.Size = new System.Drawing.Size(56, 13);
            this.roofStyleLabel.TabIndex = 12;
            this.roofStyleLabel.Text = "Roof Style";
            // 
            // PlaceElementsForm
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(430, 256);
            this.Controls.Add(this.roofStyleLabel);
            this.Controls.Add(this.RoofStyleComboBox);
            this.Controls.Add(this.roofTypeLabel);
            this.Controls.Add(this.roofTypeComboBox);
            this.Controls.Add(this.topLevelNametextBox);
            this.Controls.Add(this.topLevelNameLabel);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.levelNameTextBox);
            this.Controls.Add(this.levelNameLabel);
            this.Controls.Add(this.openButton);
            this.Controls.Add(this.fileTextBox);
            this.Controls.Add(this.fileLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PlaceElementsForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Place Elements";
            this.Load += new System.EventHandler(this.PlaceElementsForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label fileLabel;
        private System.Windows.Forms.TextBox fileTextBox;
        private System.Windows.Forms.Button openButton;
        private System.Windows.Forms.Label levelNameLabel;
        private System.Windows.Forms.TextBox levelNameTextBox;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Label topLevelNameLabel;
        private System.Windows.Forms.TextBox topLevelNametextBox;
        private System.Windows.Forms.ComboBox roofTypeComboBox;
        private System.Windows.Forms.Label roofTypeLabel;
        private System.Windows.Forms.ComboBox RoofStyleComboBox;
        private System.Windows.Forms.Label roofStyleLabel;
    }
}