using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSMSL.Analysis.ExperimentalDesign;
using CSMSL.Proteomics;
using NeuQuant.IO;

namespace NeuQuant
{
    public partial class NeuQuantFileGeneratorForm : Form
    {
        private NeuQuantForm neuQuantForm;

        public NeuQuantFileGeneratorForm(NeuQuantForm neuQuant)
        {
            neuQuantForm = neuQuant;
            InitializeComponent();
            PsmFileImporter importer = new PsmFileImporter();
            importer.removeButton.Visible = false;
            importer.Changed += importer_Changed;
            flowLayoutPanel1.Controls.Add(importer);
            
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.None;
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.CornflowerBlue;
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.Black;

            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.LightCyan;

            DataGridViewTextBoxColumn channelName = new DataGridViewTextBoxColumn();
            channelName.ReadOnly = true;
            channelName.HeaderText = "Channel";
            channelName.Name = "channel";
            channelName.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            channelName.SortMode = DataGridViewColumnSortMode.NotSortable;
            dataGridView1.Columns.Add(channelName);

            DataGridViewTextBoxColumn userName = new DataGridViewTextBoxColumn();
            userName.HeaderText = "Sample Name";
            userName.Name = "name";
            userName.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            userName.SortMode = DataGridViewColumnSortMode.NotSortable;
            dataGridView1.Columns.Add(userName);
            
            DataGridViewTextBoxColumn userDescription = new DataGridViewTextBoxColumn();
            userDescription.HeaderText = "Description (Optional)";
            userDescription.Name = "description";
            userDescription.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            userDescription.SortMode = DataGridViewColumnSortMode.NotSortable;
            dataGridView1.Columns.Add(userDescription);

            listBox1.DataSource = NeuQuantForm.CurrentExperiments;
            checkedListBox1.DataSource = NeuQuantForm.CurrentModifications;
            checkedListBox1.DisplayMember = "NameAndSites";
        }

        private void importer_Changed(object sender, EventArgs e)
        {
            PsmFileImporter importer = sender as PsmFileImporter;
            if (importer == null)
                return;

            if (!string.IsNullOrWhiteSpace(outputFileTB.Text))
                return;

            var files = importer.FileNames.Where(f => f.EndsWith(".csv")).ToList();
            if (files.Count == 0)
                return;

            string outputFileSuggestion = files[0].Replace(".csv", ".sqlite");
            outputFileTB.Text = outputFileSuggestion;
        }

        
        private void button1_Click(object sender, EventArgs e)
        {
            PsmFileImporter importer = new PsmFileImporter();
            importer.removeButton.Click += removeButton_Click;
            flowLayoutPanel1.Controls.Add(importer);
        }
       

        void removeButton_Click(object sender, EventArgs e)
        {
            Button b = sender as Button;
            flowLayoutPanel1.Controls.Remove(b.Parent.Parent);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                outputFileTB.Text = saveFileDialog1.FileName;
            }
        }

        public void GenerateFile()
        {
            string outputFile = outputFileTB.Text;

            ExperimentalSet experiment = listBox1.SelectedValue as ExperimentalSet;
            if (experiment == null)
                return;

            PsmFileImporter psmFileImporter = flowLayoutPanel1.Controls[0] as PsmFileImporter;
         
            // Get the psmfilereader from the control;
            var psmFile = psmFileImporter.GetPsmFile();
            
            // Add the quantitative experimental design 
            psmFile.SetExperimentalDesign(experiment);

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                var condition = row.Tag as ExperimentalCondition;
                string name = row.Cells["name"].Value.ToString();
                string description = row.Cells["Description"].Value.ToString();
                psmFile.SetChannel(name, description, condition);
            }

            // Add other fixed modifications
            foreach (var modification in checkedListBox1.CheckedItems.OfType<NeuQuantModification>())
            {
                psmFile.AddFixedModification(modification);
            }
          
            NeuQuantFile nqFile = null;
            Task t = Task.Factory.StartNew(() =>
            {
                nqFile = NeuQuantFile.LoadData(outputFile, psmFile);
            }).ContinueWith((t2) => neuQuantForm.LoadNeuQuantFile(nqFile), TaskScheduler.FromCurrentSynchronizationContext());
            neuQuantForm.SetStatusText("Generating File... (may take a few moments)");
            Close();
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            GenerateFile();
        }

        private void UpdateLabels()
        {
            ExperimentalSet experiment = listBox1.SelectedValue as ExperimentalSet;
            if (experiment == null)
                return;

            if (dataGridView1.ColumnCount == 0)
                return;

            dataGridView1.Rows.Clear();
            int i = 1;
            foreach (var condition in experiment)
            {
                int row = dataGridView1.Rows.Add(new[] {condition.Name, "Sample " + i++, string.Join(",", condition.Modifications)});
                dataGridView1.Rows[row].Tag = condition;
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            neuQuantForm.ShowLabelManager();
        }
        
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateLabels();
        }
  
    }
}
