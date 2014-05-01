using CSMSL.Proteomics;
using System;
using System.Windows.Forms;

namespace NeuQuant
{
    public partial class QuantiativeLabelControl : UserControl
    {
        public QuantiativeLabelControl()
        {
            InitializeComponent();
            LabelType.SelectedIndex = 0;
        }

        public string ChannelName { get { return NameTextBox.Text; } }

        public Modification Modification1
        {
            get { return LabelComboBox.SelectedItem as Modification; }
        }

        public Modification Modification2
        {
            get { return SecondaryLabelComboBox.SelectedItem as Modification; }
        }

        private void LabelType_SelectedValueChanged(object sender, EventArgs e)
        {
            if (LabelType.SelectedItem != null)
            {
                string selectedVal = LabelType.SelectedItem.ToString();
                switch (selectedVal)
                {
                    case ("Amino Acid"):
                        LabelComboBox.Visible = true;
                        SecondaryLabelComboBox.Visible = false;
                        break;
                    case ("Two Amino Acids"):
                        LabelComboBox.Visible = true;
                        SecondaryLabelComboBox.Visible = true;
                        break;
                    case ("Chemical Label"):
                        LabelComboBox.Visible = true;
                        SecondaryLabelComboBox.Visible = false;
                        break;
                    default:
                        break;

                }
            }
        }
    }
}
