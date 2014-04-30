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
            this.LabelType.SelectedIndex = 0;
        }

        public QuantiativeLabelControl(string name, string nominalmass, string labeltype = "", Modification modification1 = null, Modification modification2 = null)
            :this()
        {

        }

        private void SetState()
        {
            
        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetState();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetState();
        }

        private void LabelType_SelectedValueChanged(object sender, EventArgs e)
        {
            if (this.LabelType.SelectedItem != null)
            {
                string selectedVal = this.LabelType.SelectedItem.ToString();
                switch (selectedVal)
                {
                    case ("Amino Acid"):
                        this.LabelComboBox.Visible = true;
                        this.SecondaryLabelComboBox.Visible = false;
                        break;
                    case ("Two Amino Acids"):
                        this.LabelComboBox.Visible = true;
                        this.SecondaryLabelComboBox.Visible = true;
                        break;

                    case ("Chemical Label"):
                        this.LabelComboBox.Visible = true;
                        this.SecondaryLabelComboBox.Visible = false;
                        break;
                    default:
                        break;

                }
            }
        }
    }
}
