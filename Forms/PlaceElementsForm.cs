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

        private void RetriveUserInput()
        {
            fileTextBox.Text = Properties.Settings.Default.FileName;
            levelNameTextBox.Text = Properties.Settings.Default.BaseLevelName;
            topLevelNametextBox.Text = Properties.Settings.Default.TopLevelName;
        }

        public PlaceElementsForm(ExternalEvent exEvent, UserInputHandler handler)
        {
            InitializeComponent();
            Thread thread = new Thread(new ThreadStart(RetriveUserInput));
            thread.Start();
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

            // do not forget to call the base class
            base.OnFormClosed(e);
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            roofType = roofTypeComboBox.Text;
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
    }
}
