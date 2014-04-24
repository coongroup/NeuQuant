using CSMSL.Proteomics;
using System;
using System.Windows.Forms;

namespace NeuQuant
{
    public partial class QuantiativeLabelControl : UserControl
    {
        private string _oldComboBox1Value;

        public QuantiativeLabelControl()
        {
            InitializeComponent();
            _oldComboBox1Value = "+0";
            NominalMass.Items.Add("+0");
            NominalMass.Items.Add("+4");
            NominalMass.Items.Add("+8");
            NominalMass.SelectedIndex = 0;
            LabelType.SelectedIndex = 0;
            SetState();
        }

        public QuantiativeLabelControl(string name, string nominalmass, string labeltype = "", Modification modification1 = null, Modification modification2 = null)
            :this()
        {
            NameTextBox.Text = name;
            NominalMass.SelectedItem = nominalmass;
            LabelType.SelectedItem = labeltype;
            SetState();
            if(modification1 != null)
                Label.SelectedItem = modification1;
            if (modification2 != null)
                SecondaryLabel.SelectedItem = modification2;
        }

        private void SetState()
        {
            string nominalMass = NominalMass.SelectedItem as string;
            string labelType = LabelType.SelectedItem as string;
            if (nominalMass != _oldComboBox1Value)
            {
                Label.Items.Clear();
                SecondaryLabel.Items.Clear();
                _oldComboBox1Value = nominalMass;
            }
            Label.Items.Clear();
            SecondaryLabel.Items.Clear();
            LabelType.Visible = false;
            Label.Visible = false;
            SecondaryLabel.Visible = false;
            switch (nominalMass)
            {
                default:
                case "+0":
                    break;
                case "+4":
                    LabelType.Visible = true;
                    Label.Visible = true;
                    switch (labelType)
                    {
                        default:
                        case "Amino Acid":
                            Label.Items.Add(Reagents.K040);
                            break;
                    }
                    break;
                case "+8":
                    LabelType.Visible = true;
                    Label.Visible = true;
                    switch (labelType)
                    {
                        default:
                        case "Amino Acid":
                            Label.Items.Add(Reagents.K080);
                            Label.Items.Add(Reagents.K341);
                            Label.Items.Add(Reagents.K440);
                            Label.Items.Add(Reagents.K422);
                            Label.Items.Add(Reagents.K521);
                            Label.Items.Add(Reagents.K602);
                            break;
                        case "Two Amino Acids":
                            SecondaryLabel.Visible = true;
                            SecondaryLabel.Items.Add(Reagents.R002);
                            SecondaryLabel.Items.Add(Reagents.R200);
                            goto case "Amino Acid";
                        case "Chemical Label":
                            Label.Items.Add("Temp");
                            break;
                    }
                    break;
            }

        }



        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetState();
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetState();
        }
    }
}
