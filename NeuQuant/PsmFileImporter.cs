using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using CSMSL.IO.Thermo;

namespace NeuQuant
{
    public partial class PsmFileImporter : UserControl
    {

        public event EventHandler Changed;

        private void OnChanged()
        {
            var handler = Changed;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public enum PSMFileType
        {
            OMSSA = 1,
            ProteomeDiscoverer = 2,
        };

        public string ExperimentName { get; private set; }
        public string RawFileDirectory { get; private set; }
        public PSMFileType PSMType { get; private set; }

        public IEnumerable<string> FileNames
        {
            get { return listBox1.Items.Cast<String>(); }
        }

        public bool IsValid { get { return string.IsNullOrEmpty(ExperimentName) || string.IsNullOrEmpty(RawFileDirectory); } }

        public PsmFileImporter()
        {
            InitializeComponent();
            ExperimentName = "";
            RawFileDirectory = "";
            comboBox1.DataSource = Enum.GetValues(typeof(PSMFileType));
        }

        private void PsmFileImporter_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                listBox1.Items.AddRange(files);
                string directory = Path.GetDirectoryName(files[0]);
                directoryBox.Text = directory;
                RawFileDirectory = directory;
                OnChanged();
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
               directoryBox.Text = directory;
               RawFileDirectory = directory;
               OnChanged();
           }
        }

        private void removeItemsButton_Click(object sender, EventArgs e)
        {
            for (int i = listBox1.SelectedIndices.Count - 1; i >= 0; i--)
            {
                listBox1.Items.RemoveAt(listBox1.SelectedIndices[i]);
            }
           
            OnChanged();
        }

        private void clearButton_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
            OnChanged();
        }

        private void nameBox_TextChanged(object sender, EventArgs e)
        {
            //this.ExperimentName = this.nameBox.Text;
        }

        private void directoryBox_TextChanged(object sender, EventArgs e)
        {
            RawFileDirectory = directoryBox.Text;
            OnChanged();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK) // Test result.
            {
                foreach (string filename in openFileDialog1.FileNames)
                {
                    listBox1.Items.Add(filename);
                }
               
                string directory = Path.GetDirectoryName(openFileDialog1.FileName);
                directoryBox.Text = directory;
                RawFileDirectory = directory;
                OnChanged();
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
               
                OnChanged();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            PSMType = (PSMFileType)comboBox1.SelectedValue;
            OnChanged();
        }

        private OmssaPeptideSpectralMatchFile GetOmssaPSMFile()
        {
            var csvFiles = FileNames.Where(x => x.EndsWith(".csv")).ToList();
            var csvFile = csvFiles[0];
            var psmFile = new OmssaPeptideSpectralMatchFile(csvFile);

            foreach(var xmlFile in FileNames.Where(x => x.EndsWith(".xml")))
            {
                psmFile.LoadUserMods(xmlFile);
            }

            return psmFile;
        }

        private ProteomeDiscovererPeptideSpectralMatchFile GetPDPsmFile()
        {
            var csvFiles = FileNames.Where(x => x.EndsWith(".csv")).ToList();
            var csvFile = csvFiles[0];
            var psmFile = new ProteomeDiscovererPeptideSpectralMatchFile(csvFile);

            var rawFiles = FileNames.Where(x => x.EndsWith(".raw")).ToList();
            var rawFile = rawFiles[0];
            psmFile.SetRawFile(new ThermoRawFile(rawFile));

            return psmFile;
        }

        public PeptideSpectralMatchFile GetPsmFile()
        {
            PeptideSpectralMatchFile psmFile;

            switch (PSMType)
            {
                default:
                case PSMFileType.OMSSA:
                    psmFile = GetOmssaPSMFile();
                    break;
                case PSMFileType.ProteomeDiscoverer:
                    psmFile = GetPDPsmFile();
                    break;
            }

            string directory = directoryBox.Text;
            psmFile.SetDataDirectory(directory);

            return psmFile;
        }
    }
}
