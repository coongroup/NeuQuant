using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSMSL.Analysis.ExperimentalDesign;
using WeifenLuo.WinFormsUI.Docking;

namespace NeuQuant
{
    public partial class NeuQuantFileGeneratorForm : Form
    {
        public NeuQuantFileGeneratorForm()
        {
            InitializeComponent();
            PsmFileImporter importer = new PsmFileImporter();
            importer.removeButton.Visible = false;
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
  
    }
}
