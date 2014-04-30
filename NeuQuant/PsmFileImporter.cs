using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;

namespace NeuQuant
{
    public partial class PsmFileImporter : UserControl
    {

        public enum PSMFileType
        {
            OMSSA = 1,
            ProteomeDiscoverer = 2,
        };

        public string ExperimentName { get; private set; }
        public string RawFileDirectory { get; private set; }
        public PSMFileType PSMType { get; private set; }
        public IEnumerable<string> FileNames { get; private set; }
        public bool IsValid { get { return string.IsNullOrEmpty(ExperimentName) || string.IsNullOrEmpty(RawFileDirectory); } }

        public PsmFileImporter()
        {
            InitializeComponent();
            this.ExperimentName = "";
            this.RawFileDirectory = "";
            this.comboBox1.DataSource = Enum.GetValues(typeof(PSMFileType));
        }

        private void PsmFileImporter_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                listBox1.Items.AddRange(files);
                string directory = Path.GetDirectoryName(files[0]);
                this.directoryBox.Text = directory;
                this.RawFileDirectory = directory;
                this.FileNames = listBox1.Items.Cast<String>().ToList();
            }
        }

        private void PsmFileImporter_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

        private void button2_Click(object sender, EventArgs e)
        {
           if (folderBrowserDialog1.ShowDialog() == DialogResult.OK) // Test result.
           {
               string directory = folderBrowserDialog1.SelectedPath;
               this.directoryBox.Text = directory;
               this.RawFileDirectory = directory;
           }
        }

        private void removeItemsButton_Click(object sender, EventArgs e)
        {
            for (int i = listBox1.SelectedIndices.Count - 1; i >= 0; i--)
            {
                listBox1.Items.RemoveAt(listBox1.SelectedIndices[i]);
            }
            this.FileNames = listBox1.Items.Cast<String>().ToList();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            this.listBox1.Items.Clear();
        }

        private void nameBox_TextChanged(object sender, EventArgs e)
        {
            this.ExperimentName = this.nameBox.Text;
        }

        private void directoryBox_TextChanged(object sender, EventArgs e)
        {
            this.RawFileDirectory = this.directoryBox.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK) // Test result.
            {
                foreach (string filename in openFileDialog1.FileNames)
                {
                    this.listBox1.Items.Add(filename);
                }
                this.FileNames = listBox1.Items.Cast<String>().ToList();
                string directory = Path.GetDirectoryName(openFileDialog1.FileName);
                this.directoryBox.Text = directory;
                this.RawFileDirectory = directory;
            }
        }

        private void listBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Back || e.KeyChar == (char)Keys.Delete)
            {
                for (int i = listBox1.SelectedIndices.Count - 1; i >= 0; i--)
                {
                    listBox1.Items.RemoveAt(listBox1.SelectedIndices[i]);
                }
                this.FileNames = listBox1.Items.Cast<String>().ToList();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.PSMType = (PSMFileType)this.comboBox1.SelectedValue;
        }
    }
}
