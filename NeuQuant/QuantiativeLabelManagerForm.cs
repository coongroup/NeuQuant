using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using CSMSL;
using CSMSL.Proteomics;
using WeifenLuo.WinFormsUI.Docking;
using CSMSL.Chemistry;

namespace NeuQuant
{
    public partial class QuantiativeLabelManagerForm : DockContent
    {
        private int _channelCount = 1;

        public QuantiativeLabelManagerForm()
        {
            InitializeComponent();
            
            siteListBox.Items.AddRange(Enum.GetNames(typeof(ModificationSites)));
            siteListBox.Items.RemoveAt(0);
            siteListBox.Items.Remove(ModificationSites.All);

            modListBox.DataSource = NeuQuantForm.CurrentModifications;
            isotopologueListBox.DataSource = NeuQuantForm.CurrentIsotopologues;
        }
        
        void removeButton_Click2(object sender, EventArgs e)
        {
            var b = sender as Button;
            if (b == null)
                return;

            b.Parent.Parent.Parent.Controls.Remove(b.Parent.Parent);
        }

        private void AddNewQuantitativeLabel(Control panel, QuantiativeLabelControl control)
        {
            if (control == null)
                return;
            control.removeButton.Click += removeButton_Click2;
            panel.Controls.Add(control);
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            var newControl = new QuantiativeLabelControl();
            newControl.NameTextBox.Text = "Channel " + _channelCount;
            newControl.LabelComboBox.DataSource = NeuQuantForm.CurrentModifications; ;
            newControl.SecondaryLabelComboBox.DataSource = NeuQuantForm.CurrentModifications; ;
            AddNewQuantitativeLabel(flowLayoutPanel2, newControl);
            _channelCount++;
        }

        private void createChannelButton_Click(object sender, EventArgs e)
        {
            string modName = modNameBox.Text;
            bool isAmino = aminoAcidRadioButton.Checked;
            string formula = formulaBox.Text;
            var masterSites = ModificationSites.None;
            foreach (var item in siteListBox.CheckedItems)
            {
                var site = (ModificationSites)Enum.Parse(typeof(ModificationSites), item.ToString());
                masterSites |= site;
            }

            CreateNewMod(modName, formula, masterSites, isAmino);
        }

        private void CreateNewMod(string modName, string formula, ModificationSites sites, bool isAminoAcid)
        {
            //logic to handle adding a channel included here
            if (string.IsNullOrEmpty(modName))
            {
                MessageBox.Show("Specify a Valid Modification Name");
                return;
            }

            var newMod = new ChemicalFormulaModification(new ChemicalFormula(formula), modName, sites, isAminoAcid);

            Reagents.AddModification(newMod);
        }

        private void SaveIsotopologue(string name)
        {
            Isotopologue isotopologue = new Isotopologue(name);
            foreach (var control in flowLayoutPanel2.Controls.Cast<QuantiativeLabelControl>())
            {
                string channelName = control.ChannelName;
                Modification mod1 = control.Modification1;
                Modification mod2 = control.Modification2;
                if(mod1 != null)
                    isotopologue.AddModification(mod1);
            }
            Reagents.AddIsotopologue(isotopologue);
        }

        private void modListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateMods();
        }

        private void UpdateMods()
        {
            var mod = modListBox.SelectedValue as ChemicalFormulaModification;

            if (mod == null)
                return;

            aminoAcidRadioButton.Checked = mod.IsAminoAcid;
            formulaBox.Text = mod.ChemicalFormula.ToString(" ");
                
            for (int i = 0; i < siteListBox.Items.Count; i++)
            {
                siteListBox.SetItemChecked(i, false);
            }
            foreach (var modSite in mod.Sites.GetActiveSites())
            {
                for (int i = 0; i < siteListBox.Items.Count; i++)
                {
                    var currentSite =
                        (ModificationSites) Enum.Parse(typeof (ModificationSites), siteListBox.Items[i].ToString());
                    if (currentSite == modSite)
                    {
                        siteListBox.SetItemChecked(i, true);
                    }
                }
            }
            modNameBox.Text = mod.Name;
        }

        private void isotopologueListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateQuantitativeLabelControls();
        }

        private void UpdateQuantitativeLabelControls()
        {
            Isotopologue isotopologue = isotopologueListBox.SelectedValue as Isotopologue;

            if (isotopologue == null)
                return;
           
            flowLayoutPanel2.Controls.Clear();
            _channelCount = 1;
            foreach (var mod in isotopologue.GetModifications())
            {
                var newControl = new QuantiativeLabelControl();
                newControl.NameTextBox.Text = "Channel " + _channelCount++;
                newControl.LabelComboBox.DataSource = NeuQuantForm.CurrentModifications; 
                newControl.SecondaryLabelComboBox.DataSource = NeuQuantForm.CurrentModifications;
                newControl.LabelComboBox.SelectedItem = mod;
                newControl.removeButton.Click += removeButton_Click2;
                flowLayoutPanel2.Controls.Add(newControl);
            }
        }

        private void modListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete)
                return;
      
            var mod = modListBox.SelectedValue as ChemicalFormulaModification;

            if (mod == null)
                return;
                //Find associated Isotopolgues;

            if (mod.IsDefault)
            {
                MessageBox.Show(mod+" is a default modification and cannot be deleted", "Cannot Delete",MessageBoxButtons.OK,MessageBoxIcon.Information);
                return;
            }


            List<Isotopologue> isotopoglues = new List<Isotopologue>();

            foreach (Isotopologue iso in Reagents.GetAllIsotopologue())
            {
                if (iso.Contains(mod))
                {
                    isotopoglues.Add(iso);
                }
            }

            if (isotopoglues.Count > 0)
            {
                string dependingIso = string.Join(",", isotopoglues.Select(iso => iso.Name));
                DialogResult result = MessageBox.Show("The following isotopologues depend on this mod:\n(" + dependingIso
                                                        + ")\nDeleting will also remove these isotopologues!", "Delete the " + mod.Name + " Modification?", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                {
                    foreach(var iso in isotopoglues)
                    {
                        Reagents.RemoveIsotopologue(iso.Name);
                    }
                }
                else
                {
                    return;
                }
            }

            // Nothing depends on it, so remove it
            Reagents.RemoveModification(mod.Name);
                
            

        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveIsotopologue(textBox1.Text);
        }
    }
}
