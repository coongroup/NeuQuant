using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuQuant
{
    public partial class PsmFileImporter : UserControl
    {
        public PsmFileImporter()
        {
            InitializeComponent();
        }

        private void PsmFileImporter_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
                listBox1.Items.AddRange(files);
            }
        }

        private void PsmFileImporter_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }


    }
}
