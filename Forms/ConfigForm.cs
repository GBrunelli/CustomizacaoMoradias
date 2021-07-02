using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomizacaoMoradias.Forms
{
    public partial class ConfigForm : Form
    {
        public static ConfigForm form;

        public ConfigForm()
        {
            InitializeComponent();
            form = this;
        }

        public void ShowControl(Control control)
        {
            mainPanel.Controls.Clear();

            control.Dock = DockStyle.Fill;
            control.BringToFront();
            control.Focus();

            mainPanel.Controls.Add(control);
        }

        public void ClearControl()
        {
            mainPanel.Controls.Clear();
        }

        private void ClearButtons()
        {
            roomButton.BackColor = Color.White;
            elementButton.BackColor = Color.White;
            classifierButton.BackColor = Color.White;
            generalButton.BackColor = Color.White;
        }

        private void roomButton_Click(object sender, EventArgs e)
        {
            ClearButtons();
            roomButton.BackColor = SystemColors.Control;
            RoomConfigControl configControl = new RoomConfigControl();
            ShowControl(configControl);
        }

        private void elementButton_Click(object sender, EventArgs e)
        {
            ClearButtons();
            elementButton.BackColor = SystemColors.Control;
            ElementConfigControl configControl = new ElementConfigControl();
            ShowControl(configControl);
        }

        private void classifierButton_Click(object sender, EventArgs e)
        {
            ClearButtons();
            classifierButton.BackColor = SystemColors.Control;
            ClassifierConfigControl configControl = new ClassifierConfigControl();
            ShowControl(configControl);
        }

        private void generalButton_Click(object sender, EventArgs e)
        {
            ClearButtons();
            generalButton.BackColor = SystemColors.Control;
            GeneralConfigControl configControl = new GeneralConfigControl();
            ShowControl(configControl);
        }
    }
}
