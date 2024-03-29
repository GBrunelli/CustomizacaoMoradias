﻿using System;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Form = System.Windows.Forms.Form;

namespace CustomizacaoMoradias.Forms
{
    public partial class BuildRoofForm : Form
    {
        public static BuildRoofForm form;
        public RoofSelector roofSelector;

        public static XYZ SlopeVector => form.roofSelector.SlopeVector;

        public static RoofDesign RoofDesign => form.roofSelector.RoofStyle;

        private readonly ExternalEvent exEvent;

        public BuildRoofForm(ExternalEvent exEvent)
        {
            InitializeComponent();
            this.exEvent = exEvent;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
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

        private void BuildRoofForm_Load(object sender, EventArgs e)
        {
            roofSelector = new RoofSelector { Dock = DockStyle.Fill };
            roofSelector.BringToFront();
            roofSelector.Focus();
            roofSelectorPanel.Controls.Add(roofSelector);
        }
    }
}
