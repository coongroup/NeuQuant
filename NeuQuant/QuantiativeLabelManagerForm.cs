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
            Reagents.ModificationsChanged += ReagentsModificationsChanged;
            Reagents.IsotopologuesChanged += Reagents_IsotopologuesChanged;
            _activeLabelControls = new List<QuantiativeLabelControl>();
            siteListBox.Items.AddRange(Enum.GetNames(typeof(ModificationSites)));
            siteListBox.Items.RemoveAt(0);
            siteListBox.Items.Remove(ModificationSites.All);

            modListBox.DataSource = CurrentModifications;
            isotopologueListBox.DataSource = CurrentIsotopologues;

            RefreshModifications();
            RefreshIsotopologues();
        }

        internal static readonly BindingList<ChemicalFormulaModification> CurrentModifications = new BindingList<ChemicalFormulaModification>();
        internal static readonly BindingList<Isotopologue> CurrentIsotopologues = new BindingList<Isotopologue>();

        void Reagents_IsotopologuesChanged(object sender, EventArgs e)
        {
            RefreshIsotopologues();
        }

        void ReagentsModificationsChanged(object sender, EventArgs e)
        {
            RefreshModifications();
        }

        private void RefreshIsotopologues()
        {
            CurrentIsotopologues.Clear();
            foreach (var iso in Reagents.GetAllIsotopologue())
                CurrentIsotopologues.Add(iso);
        }

        private void RefreshModifications()
        {
            CurrentModifications.Clear();
            foreach (var mod in Reagents.GetAllModifications())
                CurrentModifications.Add(mod);
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
            newControl.LabelComboBox.DataSource = CurrentModifications;
            newControl.SecondaryLabelComboBox.DataSource = CurrentModifications;
            AddNewQuantitativeLabel(flowLayoutPanel2, newControl);
            _channelCount++;
            _activeLabelControls.Add(newControl);
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
            formulaBox.Text = mod.ChemicalFormula.ToString();
                
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
                newControl.NameTextBox.Text = "Channel " + _channelCount++;
                newControl.LabelComboBox.DataSource = CurrentModifications;
                newControl.SecondaryLabelComboBox.DataSource = CurrentModifications;
                newControl.LabelComboBox.SelectedItem = mod.Name;
                newControl.removeButton.Click += removeButton_Click2;
                flowLayoutPanel2.Controls.Add(newControl);
             
                _activeLabelControls.Add(newControl);
            }
        }
 

        private void modListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Delete)
                return;
           
           // var name = modListBox.SelectedItem.ToString();
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
    }
}
