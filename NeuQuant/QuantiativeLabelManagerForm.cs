using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSMSL.Proteomics;
using WeifenLuo.WinFormsUI.Docking;

namespace NeuQuant
{
    public partial class QuantiativeLabelManagerForm : DockContent
    {
        public QuantiativeLabelManagerForm()
        {
            InitializeComponent();

            // Add two default, blank chanels
            AddNewQuantitativeLabel(flowLayoutPanel2, new QuantiativeLabelControl());
            AddNewQuantitativeLabel(flowLayoutPanel2, new QuantiativeLabelControl());

            // Default templates
            comboBox1.Items.Add("Duplex NueCode (+8)");
            comboBox1.Items.Add("Triplex NueCode (+8)");
            comboBox1.Items.Add("Duplex SILAC (+8)");
            comboBox1.Items.Add("Triplex SILAC (+4,+8)");
            comboBox1.Items.Add("Trypsin Duplex NueCode (+8)");

            checkedListBox1.DataSource = Enum.GetValues(typeof (ModificationSites));
            checkedListBox1.MultiColumn = true;
            checkedListBox1.ColumnWidth = 50;
            checkedListBox1.CheckOnClick = true;

            comboBox2.Items.AddRange(new[] {"Amino Acid", "Two Amino Acids", "Chemical Label"});
        }

        void removeButton_Click2(object sender, EventArgs e)
        {
            Button b = sender as Button;
            b.Parent.Parent.Parent.Controls.Remove(b.Parent.Parent);
        }

        private void AddNewQuantitativeLabel(Control panel, QuantiativeLabelControl control)
        {
            if (control == null)
                return;
            control.removeButton.Click += removeButton_Click2;
            panel.Controls.Add(control);
        }

        private void SetDefaultQuantitationLabels(Control control, string type)
        {
            if (string.IsNullOrEmpty(type))
                return;
            control.Controls.Clear();
            switch (type)
            {
                default:
                case "Duplex NueCode (+8)":
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Light", "+8", "Amino Acid", Reagents.K602));
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Heavy", "+8", "Amino Acid", Reagents.K080));
                    break;
                case "Triplex NueCode (+8)":
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Light", "+8", "Amino Acid", Reagents.K602));
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Medium", "+8", "Amino Acid", Reagents.K341));
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Heavy", "+8", "Amino Acid", Reagents.K080));
                    break;
                case "Duplex SILAC (+8)":
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Light", "+0"));
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Heavy", "+8", "Amino Acid", Reagents.K602));
                    break;
                case "Triplex SILAC (+4,+8)":
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Light", "+0"));
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Medium", "+4", "Amino Acid", Reagents.K040));
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Heavy", "+8", "Amino Acid", Reagents.K602));
                    break;
                case "Trypsin Duplex NueCode (+8)":
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Light", "+2", "Two Amino Acids", Reagents.K100, Reagents.R200));
                    AddNewQuantitativeLabel(control, new QuantiativeLabelControl("Light", "+2", "Two Amino Acids", Reagents.K002, Reagents.R002));
                    break;
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            AddNewQuantitativeLabel(flowLayoutPanel2, new QuantiativeLabelControl());
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            SetDefaultQuantitationLabels(flowLayoutPanel2, comboBox1.SelectedItem as string);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

    }
}
