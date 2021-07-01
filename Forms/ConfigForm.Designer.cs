
namespace CustomizacaoMoradias.Forms
{
    partial class ConfigForm
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
            this.optionPanel = new System.Windows.Forms.Panel();
            this.generalButton = new System.Windows.Forms.Button();
            this.classifierButton = new System.Windows.Forms.Button();
            this.elementButton = new System.Windows.Forms.Button();
            this.roomButton = new System.Windows.Forms.Button();
            this.mainPanel = new System.Windows.Forms.Panel();
            this.optionPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // optionPanel
            // 
            this.optionPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.optionPanel.BackColor = System.Drawing.Color.White;
            this.optionPanel.Controls.Add(this.generalButton);
            this.optionPanel.Controls.Add(this.classifierButton);
            this.optionPanel.Controls.Add(this.elementButton);
            this.optionPanel.Controls.Add(this.roomButton);
            this.optionPanel.Location = new System.Drawing.Point(0, 0);
            this.optionPanel.Name = "optionPanel";
            this.optionPanel.Size = new System.Drawing.Size(168, 422);
            this.optionPanel.TabIndex = 0;
            // 
            // generalButton
            // 
            this.generalButton.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.generalButton.FlatAppearance.BorderSize = 0;
            this.generalButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.generalButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.generalButton.Location = new System.Drawing.Point(0, 90);
            this.generalButton.Name = "generalButton";
            this.generalButton.Size = new System.Drawing.Size(168, 23);
            this.generalButton.TabIndex = 2;
            this.generalButton.Text = "Geral";
            this.generalButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.generalButton.UseVisualStyleBackColor = true;
            this.generalButton.Click += new System.EventHandler(this.generalButton_Click);
            // 
            // classifierButton
            // 
            this.classifierButton.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.classifierButton.FlatAppearance.BorderSize = 0;
            this.classifierButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.classifierButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.classifierButton.Location = new System.Drawing.Point(0, 61);
            this.classifierButton.Name = "classifierButton";
            this.classifierButton.Size = new System.Drawing.Size(168, 23);
            this.classifierButton.TabIndex = 1;
            this.classifierButton.Text = "Classificação de ambientes";
            this.classifierButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.classifierButton.UseVisualStyleBackColor = true;
            this.classifierButton.Click += new System.EventHandler(this.classifierButton_Click);
            // 
            // elementButton
            // 
            this.elementButton.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.elementButton.FlatAppearance.BorderSize = 0;
            this.elementButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.elementButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.elementButton.Location = new System.Drawing.Point(0, 32);
            this.elementButton.Name = "elementButton";
            this.elementButton.Size = new System.Drawing.Size(168, 23);
            this.elementButton.TabIndex = 1;
            this.elementButton.Text = "Elementos";
            this.elementButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.elementButton.UseVisualStyleBackColor = true;
            this.elementButton.Click += new System.EventHandler(this.elementButton_Click);
            // 
            // roomButton
            // 
            this.roomButton.FlatAppearance.BorderColor = System.Drawing.Color.White;
            this.roomButton.FlatAppearance.BorderSize = 0;
            this.roomButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.roomButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.roomButton.Location = new System.Drawing.Point(0, 3);
            this.roomButton.Name = "roomButton";
            this.roomButton.Size = new System.Drawing.Size(168, 23);
            this.roomButton.TabIndex = 0;
            this.roomButton.Text = "Ambientes";
            this.roomButton.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.roomButton.UseVisualStyleBackColor = true;
            this.roomButton.Click += new System.EventHandler(this.roomButton_Click);
            // 
            // mainPanel
            // 
            this.mainPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mainPanel.Location = new System.Drawing.Point(174, 12);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(448, 403);
            this.mainPanel.TabIndex = 1;
            // 
            // ConfigForm
            // 
            this.AllowDrop = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoValidate = System.Windows.Forms.AutoValidate.Disable;
            this.ClientSize = new System.Drawing.Size(630, 423);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.optionPanel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Configurações";
            this.TopMost = true;
            this.optionPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel optionPanel;
        private System.Windows.Forms.Button generalButton;
        private System.Windows.Forms.Button classifierButton;
        private System.Windows.Forms.Button elementButton;
        private System.Windows.Forms.Button roomButton;
        private System.Windows.Forms.Panel mainPanel;
    }
}