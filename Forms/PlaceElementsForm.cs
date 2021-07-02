using Autodesk.Revit.UI;
using System;
using System.Windows.Forms;
using CustomizacaoMoradias.Forms;

namespace CustomizacaoMoradias
{
    public partial class PlaceElementsForm : Form
    {
        private ExternalEvent m_ExEvent;
        public static PlaceElementsForm form;
        public static string filePath;
        public static RoofSelector roofSelector;

        public PlaceElementsForm(ExternalEvent exEvent, UserInputHandler handler)
        {
            InitializeComponent();
            fileTextBox.Text = Properties.Settings.Default.FileName;
            m_ExEvent = exEvent;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // we own both the event and the handler
            // we should dispose it before we are closed
            Properties.Settings.Default.FileName = fileTextBox.Text;
            Properties.Settings.Default.Save();
            m_ExEvent.Dispose();
            base.OnFormClosed(e);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            m_ExEvent.Raise();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();          
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            fileTextBox.Text = ElementPlacer.SelectFile();
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
