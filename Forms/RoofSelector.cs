using System;
using System.Drawing;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Color = System.Drawing.Color;

namespace CustomizacaoMoradias.Forms
{
    public partial class RoofSelector : UserControl
    {

        public Color NotSelected = SystemColors.ControlDark;
        public Color Selected = Color.DeepSkyBlue;

        public enum EdgeState
        {
            EdgeVertical,
            EdgeHorizontal,
            MidVertical,
            MidHorizontal,
            AllEdges,
            None
        }

        private EdgeState _state;

        public EdgeState State
        {
            get => _state;

            set
            {
                _state = value;
                switch (_state)
                {
                    case EdgeState.EdgeVertical:
                        edgeBottom.BackColor = NotSelected;
                        edgeUp.BackColor = NotSelected;
                        edgeRight.BackColor = Selected;
                        edgeLeft.BackColor = Selected;
                        middleHorizontal.BackColor = NotSelected;
                        middleVertical.BackColor = NotSelected;
                        break;

                    case EdgeState.EdgeHorizontal:
                        edgeBottom.BackColor = Selected;
                        edgeUp.BackColor = Selected;
                        edgeRight.BackColor = NotSelected;
                        edgeLeft.BackColor = NotSelected;
                        middleHorizontal.BackColor = NotSelected;
                        middleVertical.BackColor = NotSelected;
                        break;

                    case EdgeState.MidVertical:
                        edgeBottom.BackColor = NotSelected;
                        edgeUp.BackColor = NotSelected;
                        edgeRight.BackColor = NotSelected;
                        edgeLeft.BackColor = NotSelected;
                        middleHorizontal.BackColor = NotSelected;
                        middleVertical.BackColor = Selected;
                        break;

                    case EdgeState.MidHorizontal:
                        edgeBottom.BackColor = NotSelected;
                        edgeUp.BackColor = NotSelected;
                        edgeRight.BackColor = NotSelected;
                        edgeLeft.BackColor = NotSelected;
                        middleHorizontal.BackColor = Selected;
                        middleVertical.BackColor = NotSelected;
                        break;

                    case EdgeState.AllEdges:
                        edgeBottom.BackColor = Selected;
                        edgeUp.BackColor = Selected;
                        edgeRight.BackColor = Selected;
                        edgeLeft.BackColor = Selected;
                        middleHorizontal.BackColor = NotSelected;
                        middleVertical.BackColor = NotSelected;
                        break;

                    case EdgeState.None:
                        edgeBottom.BackColor = NotSelected;
                        edgeUp.BackColor = NotSelected;
                        edgeRight.BackColor = NotSelected;
                        edgeLeft.BackColor = NotSelected;
                        middleHorizontal.BackColor = NotSelected;
                        middleVertical.BackColor = NotSelected;
                        break;
                }
            }
        }

        public static double GetSlopeByType(RoofDesign roofDesign)
        {
            switch (roofDesign)
            {
                case RoofDesign.Gable:
                    return 0.3;
                case RoofDesign.Hip:
                    return 0.3;
                case RoofDesign.HiddenButterfly:
                    return 0.05;
            }
            return 0.3;
        }

        public XYZ SlopeVector
        {
            get
            {
                switch (State)
                {
                    case EdgeState.EdgeVertical:
                        return XYZ.BasisX;
                    case EdgeState.EdgeHorizontal:
                        return XYZ.BasisY;
                    case EdgeState.MidHorizontal:
                        return XYZ.BasisX;
                    case EdgeState.MidVertical:
                        return XYZ.BasisY;
                    case EdgeState.AllEdges:
                        return XYZ.Zero;
                }
                return null;
            }
        }

        public RoofDesign RoofStyle
        {
            get
            {
                switch (State)
                {
                    case EdgeState.EdgeVertical:
                        return RoofDesign.Gable;
                    case EdgeState.EdgeHorizontal:
                        return RoofDesign.Gable;
                    case EdgeState.MidHorizontal:
                        return RoofDesign.HiddenButterfly;
                    case EdgeState.MidVertical:
                        return RoofDesign.HiddenButterfly;
                    case EdgeState.AllEdges:
                        return RoofDesign.Hip;
                }
                return RoofDesign.Hip;
            }
        }

        public RoofSelector()
        {
            InitializeComponent();
            State = EdgeState.None;
        }

        private void EdgeHorizontalClick()
        {
            switch (State)
            {
                case EdgeState.EdgeVertical:
                    State = EdgeState.AllEdges;
                    break;
                case EdgeState.EdgeHorizontal:
                    State = EdgeState.None;
                    break;
                case EdgeState.AllEdges:
                    State = EdgeState.EdgeVertical;
                    break;
                default:
                    State = EdgeState.EdgeHorizontal;
                    break;
            }
        }

        private void EdgeVerticalClick()
        {
            switch (State)
            {
                case EdgeState.EdgeVertical:
                    State = EdgeState.None;
                    break;
                case EdgeState.EdgeHorizontal:
                    State = EdgeState.AllEdges;
                    break;
                case EdgeState.AllEdges:
                    State = EdgeState.EdgeHorizontal;
                    break;
                default:
                    State = EdgeState.EdgeVertical;
                    break;
            }
        }

        private void edgeUp_Click(object sender, EventArgs e)
        {
            EdgeHorizontalClick();
        }

        private void edgeBottom_Click(object sender, EventArgs e)
        {
            EdgeHorizontalClick();
        }

        private void edgeLeft_Click(object sender, EventArgs e)
        {
            EdgeVerticalClick();
        }

        private void edgeRight_Click(object sender, EventArgs e)
        {
            EdgeVerticalClick();
        }

        private void middleVertical_Click(object sender, EventArgs e)
        {
            switch (State)
            {
                case EdgeState.MidVertical:
                    State = EdgeState.None;
                    break;
                default:
                    State = EdgeState.MidVertical;
                    break;
            }
        }

        private void middleHorizontal_Click(object sender, EventArgs e)
        {
            switch (State)
            {
                case EdgeState.MidHorizontal:
                    State = EdgeState.None;
                    break;
                default:
                    State = EdgeState.MidHorizontal;
                    break;
            }
        }
    }
}
