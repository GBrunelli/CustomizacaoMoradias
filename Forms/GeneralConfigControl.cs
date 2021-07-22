using System;
using System.Windows.Forms;

namespace CustomizacaoMoradias.Forms
{
    public partial class GeneralConfigControl : UserControl
    {
        private readonly Properties.Settings settings = Properties.Settings.Default;

        public GeneralConfigControl()
        {
            InitializeComponent();
            ResetTextBoxes();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            try
            {
                settings.BaseLevelName = baseLevelTextBox.Text;
                settings.TopLevelName  = topLevelTextBox.Text;
                settings.FloorName     = floorTypeTextBox.Text;
                settings.CeilingName   = ceilingTypeTextBox.Text;
                settings.WallTypeName  = wallTypeTextBox.Text;
                settings.Scale         = float.Parse(scaleTextBox.Text);
                settings.Save();
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Erro!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            ResetTextBoxes();
        }

        private void ResetTextBoxes()
        {
            baseLevelTextBox.Text   = settings.BaseLevelName;
            topLevelTextBox.Text    = settings.TopLevelName;
            floorTypeTextBox.Text   = settings.FloorName;
            ceilingTypeTextBox.Text = settings.CeilingName;
            wallTypeTextBox.Text    = settings.WallTypeName;
            scaleTextBox.Text       = settings.Scale.ToString();
        }
    }
}
