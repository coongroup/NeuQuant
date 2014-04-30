using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace NeuQuant
{
    public partial class NeuQuantFileGeneratorForm : Form
    {
        public NeuQuantFileGeneratorForm()
        {
            InitializeComponent();
            PsmFileImporter importer = new PsmFileImporter();
            //importer.removeButton.Visible = false;
            flowLayoutPanel1.Controls.Add(importer);
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
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.outputFileBox.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            //Add all logic here to another method that the event handler calls
            //File.DirectoryExists
            //string.IsNullOrEmpty
            if (!this.outputFileBox.Text.Equals(""))
            {
                foreach (PsmFileImporter importer in this.flowLayoutPanel1.Controls)
                {
                    if (importer.IsValid)
                    {
                        //get rid of nesting, throw exceptions where necessary and continue with code execution
                        //pull out any mod files
                        List<string> xmlFiles = importer.FileNames.Where(x => x.EndsWith(".xml")).ToList();
                        if (importer.PSMType == PsmFileImporter.PSMFileType.OMSSA)
                        {
                            List<string> csvFiles = importer.FileNames.Where(x => x.EndsWith(".csv")).ToList();
                            foreach (string csv in csvFiles)
                            {
                                OmssaPeptideSpectralMatchFile omssaFile = new OmssaPeptideSpectralMatchFile(csv);
                                if (xmlFiles.Count > 0)
                                {
                                    omssaFile.LoadUserMods(xmlFiles[0]);
                                }
                                omssaFile.SetDataDirectory(importer.RawFileDirectory);
                                //need to combine label manager and file generator form.
                            }
                        }
                        if (importer.PSMType == PsmFileImporter.PSMFileType.ProteomeDiscoverer)
                        {

                        }
                    }
                    else
                    {
                        MessageBox.Show("Missing Fields");
                    }
                }
            }
            else
            {
                MessageBox.Show("Specify Valid Output Directory.");
            }
        }
    }
}
