using Autodesk.Revit.UI;
using System;
using System.Windows.Forms;

namespace CustomizacaoMoradias
{
    public partial class PlaceElementsForm : System.Windows.Forms.Form
    {

        private ExternalEvent m_ExEvent;
        private UserInputHandler m_Handler;
        public static string filePath;
        public static string levelName;
        public static PlaceElementsForm form;

        public PlaceElementsForm(ExternalEvent exEvent, UserInputHandler handler)
        {
            InitializeComponent();
            m_ExEvent = exEvent;
            m_Handler = handler;   
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // we own both the event and the handler
            // we should dispose it before we are closed
            m_ExEvent.Dispose();
            m_ExEvent = null;
            m_Handler = null;

            // do not forget to call the base class
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
            fileTextBox.Text = PlaceElementsUtil.SelectFile();
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
    }
}
