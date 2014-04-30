using System;
using System.Collections.Generic;
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
        private readonly List<QuantiativeLabelControl> _activeLabelControls;

        public QuantiativeLabelManagerForm()
        {
            InitializeComponent();
            _activeLabelControls = new List<QuantiativeLabelControl>();
            modListBox.DataSource = new BindingSource(Reagents.Modifications.Keys, null);
            siteListBox.Items.AddRange(Enum.GetNames(typeof(ModificationSites)));
            isotopologueListBox.DataSource = new BindingSource(Reagents.Isotopologues.Keys, null);
        }

        void removeButton_Click2(object sender, EventArgs e)
        {
            var b = sender as Button;
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
            newControl.NameTextBox.Text = "Channel " + _channelCount.ToString();
            newControl.LabelComboBox.DataSource = new BindingSource(Reagents.Modifications.Keys, null);
            newControl.SecondaryLabelComboBox.DataSource = new BindingSource(Reagents.Modifications.Keys, null);
            AddNewQuantitativeLabel(flowLayoutPanel2, newControl);
            _channelCount++;
            _activeLabelControls.Add(newControl);
        }

        private void createChannelButton_Click(object sender, EventArgs e)
        {
            CreateNewMod();
            //modname, chemicalformula, mod sites. check all of these too.
        }

        private void CreateNewMod()
        {
            //logic to handle adding a channel included here
            string modName = modNameBox.Text;
            ChemicalFormula currentFormula = null;
            if (string.IsNullOrEmpty(modName))
            {
                MessageBox.Show("Specify a Valid Modification Name");
                return;
            }
            try
            {
                if (aminoAcidRadioButton.Checked)
                {
                    currentFormula = new ChemicalFormula(this.aminoAcidFormulaBox.Text);
                }
                if (tagRadioButton.Checked)
                {
                    currentFormula = new ChemicalFormula(this.tagRadioButton.Text);
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show("Invalid Chemical Formula");
                return;
            }
            if (siteListBox.CheckedItems.Count == 0)
            {
                MessageBox.Show("Select Site(s) of Modification");
                return;
            }
            var masterSites = ModificationSites.None;
            foreach (var item in siteListBox.CheckedItems)
            {
                var site = (ModificationSites)Enum.Parse(typeof(ModificationSites), item.ToString());
                masterSites |= site;
            }
            var newMod = new ChemicalFormulaModification(currentFormula, modName, masterSites, aminoAcidRadioButton.Checked);
            if (Reagents.Modifications.ContainsKey(modName))
            {
                Reagents.Modifications[modName] = newMod;
            }
            else
            {
                Reagents.Modifications.Add(modName, newMod);
            }
            modListBox.DataSource = new BindingSource(Reagents.Modifications.Keys, null);
            foreach (var control in _activeLabelControls)
            {
                control.LabelComboBox.DataSource = new BindingSource(Reagents.Modifications.Keys, null);
            }
            UpdateQuantitativeLabelControls();
            Reagents.WriteXmlOutput();
        }

        private void modListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateMods();
        }

        private void UpdateMods()
        {
            if (modListBox.SelectedItem != null)
            {
                var currentMod = Reagents.Modifications[modListBox.SelectedItem.ToString()];
                aminoAcidRadioButton.Checked = currentMod.IsAminoAcid;
                if (currentMod.IsAminoAcid)
                {
                    aminoAcidFormulaBox.Text = currentMod.ChemicalFormula.ToString();
                    chemTagFormBox.Text = "";
                }
                else
                {
                    chemTagFormBox.Text = currentMod.ChemicalFormula.ToString();
                    aminoAcidFormulaBox.Text = "";
                }

                for (int i = 0; i < siteListBox.Items.Count; i++)
                {
                    siteListBox.SetItemChecked(i, false);
                }
                foreach (var modSite in currentMod.Sites.GetActiveSites())
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
            }
        }

        private void isotopologueListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateQuantitativeLabelControls();
        }

        private void UpdateQuantitativeLabelControls()
        {
            if (isotopologueListBox.SelectedItem == null)
            {
                return;
            }
            var selectedVal = isotopologueListBox.SelectedItem.ToString();
            var currentIso = Reagents.Isotopologues[selectedVal];
            flowLayoutPanel2.Controls.Clear();
            _channelCount = 1;
            foreach (Modification mod in currentIso.GetModifications())
            {
                var newControl = new QuantiativeLabelControl();
                newControl.NameTextBox.Text = "Channel " + _channelCount.ToString();
                newControl.LabelComboBox.DataSource = new BindingSource(Reagents.Modifications.Keys, null);
                newControl.SecondaryLabelComboBox.DataSource = new BindingSource(Reagents.Modifications.Keys, null);
                newControl.LabelComboBox.SelectedItem = mod.Name;
                newControl.removeButton.Click += removeButton_Click2;
                flowLayoutPanel2.Controls.Add(newControl);
                _channelCount++;
                _activeLabelControls.Add(newControl);
            }
        }

        private void modListBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            
            if (e.KeyChar == (char) Keys.Back || e.KeyChar == (char) Keys.Delete)
                for (int i = modListBox.SelectedIndices.Count - 1; i >= 0; i--)
                {
                    var name = modListBox.Items[i].ToString();
                    Reagents.Modifications.Remove(name);
                    modListBox.Items.RemoveAt(modListBox.SelectedIndices[i]);
                }
            modListBox.DataSource = new BindingSource(Reagents.Modifications.Keys,null);
            Reagents.WriteXmlOutput();
        }
    }
}
