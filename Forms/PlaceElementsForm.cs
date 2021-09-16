using System;
using System.Windows.Forms;
using Autodesk.Revit.UI;
using CustomizacaoMoradias.Forms;
using CustomizacaoMoradias.Source.Builder;

namespace CustomizacaoMoradias
{
    public partial class PlaceElementsForm : Form
    {
        private readonly ExternalEvent exEvent;
        public static PlaceElementsForm form;
        public static string filePath;
        public static RoofSelector roofSelector;

        public PlaceElementsForm(ExternalEvent exEvent)
        {
            InitializeComponent();
            fileTextBox.Text = Properties.Settings.Default.FileName;
            this.exEvent = exEvent;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // we own both the event and the handler
            // we should dispose it before we are closed
            Properties.Settings.Default.FileName = fileTextBox.Text;
            Properties.Settings.Default.Save();
            exEvent.Dispose();
            base.OnFormClosed(e);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            exEvent.Raise();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            fileTextBox.Text = HouseBuilder.SelectFile();
            filePath = fileTextBox.Text;
        }

        private void fileTextBox_TextChanged(object sender, EventArgs e)
        {
            filePath = fileTextBox.Text;
        }

        public static void CloseForm()
        {
            form.Close();
        }

        private void PlaceElementsForm_Load(object sender, EventArgs e)
        {
            roofSelector = new RoofSelector { Dock = DockStyle.Fill };
            roofSelector.BringToFront();
            roofSelector.Focus();
            roofSelectorPanel.Controls.Add(roofSelector);
        }
    }
}
