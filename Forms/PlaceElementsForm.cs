using Autodesk.Revit.UI;
using System;
using System.Windows.Forms;
using System.Configuration;
using System.Threading;

namespace CustomizacaoMoradias
{
    public partial class PlaceElementsForm : System.Windows.Forms.Form
    {
        private ExternalEvent m_ExEvent;
        private UserInputHandler m_Handler;
        public static PlaceElementsForm form;
        public static string filePath;
        public static string levelName;
        public static string topLevelName;
        public static string roofType;
        public static string roofStyle;

        public PlaceElementsForm(ExternalEvent exEvent, UserInputHandler handler)
        {
            InitializeComponent();
            fileTextBox.Text = Properties.Settings.Default.FileName;
            levelNameTextBox.Text = Properties.Settings.Default.BaseLevelName;
            topLevelNametextBox.Text = Properties.Settings.Default.TopLevelName;
            m_ExEvent = exEvent;
            m_Handler = handler;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // we own both the event and the handler
            // we should dispose it before we are closed
            Properties.Settings.Default.FileName = fileTextBox.Text;
            Properties.Settings.Default.BaseLevelName = levelNameTextBox.Text;
            Properties.Settings.Default.TopLevelName = topLevelNametextBox.Text;
            Properties.Settings.Default.Save();
            m_ExEvent.Dispose();
            m_ExEvent = null;
            m_Handler = null;

            base.OnFormClosed(e);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            roofType = roofTypeComboBox.Text;
            roofStyle = RoofStyleComboBox.Text;
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

        private void levelNameTextBox_TextChanged(object sender, EventArgs e)
        {
            levelName = levelNameTextBox.Text;
        }

        private void fileTextBox_TextChanged(object sender, EventArgs e) 
        {
            filePath = fileTextBox.Text;
        }

        public static void CloseForm()
        {
            form.Close();
        }

        private void topLevelNametextBox_TextChanged(object sender, EventArgs e)
        {
            topLevelName = topLevelNametextBox.Text;
        }

        private void PlaceElementsForm_Load(object sender, EventArgs e)
        {

        }
    }
}
