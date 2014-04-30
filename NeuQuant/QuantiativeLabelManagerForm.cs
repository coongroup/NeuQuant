using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly List<QuantiativeLabelControl> _activeLabelControls;

        public QuantiativeLabelManagerForm()
        {
            InitializeComponent();
            Reagents.Changed += Reagents_Changed;
            _activeLabelControls = new List<QuantiativeLabelControl>();
            siteListBox.Items.AddRange(Enum.GetNames(typeof(ModificationSites)));

            RefreshLists();
        }

        void Reagents_Changed(object sender, EventArgs e)
        {
            RefreshLists();
        }

        private void RefreshLists()
        {
            modListBox.DataSource = new BindingList<ChemicalFormulaModification>(Reagents.GetAllModifications().ToList());
            isotopologueListBox.DataSource = new BindingList<Isotopologue>(Reagents.GetAllIsotopologue().ToList());
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
            newControl.LabelComboBox.DataSource = new BindingList<ChemicalFormulaModification>(Reagents.GetAllModifications().ToList());
            newControl.SecondaryLabelComboBox.DataSource = new BindingList<ChemicalFormulaModification>(Reagents.GetAllModifications().ToList());
            AddNewQuantitativeLabel(flowLayoutPanel2, newControl);
            _channelCount++;
            _activeLabelControls.Add(newControl);
        }

        private void createChannelButton_Click(object sender, EventArgs e)
        {
            string modName = modNameBox.Text;
            bool isAmino = aminoAcidRadioButton.Checked;
            string formula = aminoAcidFormulaBox.Text;
            if (!isAmino)
            {
                formula = tagRadioButton.Text;
            }

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

        private void modListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateMods();
        }

        private void UpdateMods()
        {
            if (modListBox.SelectedItem != null)
            {
                var currentMod = Reagents.GetModification(modListBox.SelectedItem.ToString());
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
            var currentIso = Reagents.GetIsotopologue(selectedVal);
            flowLayoutPanel2.Controls.Clear();
            _channelCount = 1;
            foreach (Modification mod in currentIso.GetModifications())
            {
                var newControl = new QuantiativeLabelControl();
                newControl.NameTextBox.Text = "Channel " + _channelCount.ToString();
                newControl.LabelComboBox.DataSource = new BindingList<ChemicalFormulaModification>(Reagents.GetAllModifications().ToList());
                newControl.SecondaryLabelComboBox.DataSource = new BindingList<ChemicalFormulaModification>(Reagents.GetAllModifications().ToList());
                newControl.LabelComboBox.SelectedItem = mod.Name;
                newControl.removeButton.Click += removeButton_Click2;
                flowLayoutPanel2.Controls.Add(newControl);
                _channelCount++;
                _activeLabelControls.Add(newControl);
            }
        }

        private void modListBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != (char) Keys.Delete)
                return;

            for (int i = modListBox.SelectedIndices.Count - 1; i >= 0; i--)
            {
                var name = modListBox.Items[i].ToString();
                Reagents.RemoveModification(name);
                modListBox.Items.RemoveAt(modListBox.SelectedIndices[i]);
            }
            modListBox.DataSource = new BindingList<ChemicalFormulaModification>(Reagents.GetAllModifications().ToList());
            Reagents.Save();
        }
    }
}
