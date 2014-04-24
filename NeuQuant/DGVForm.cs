using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;

namespace NeuQuant
{
    public partial class DGVForm : DockContent
    {
        public string BaseName { get; private set; }

        public DGVForm(string baseName)
        {
            BaseName = baseName;
            InitializeComponent();
            Text = baseName;
        }

        public void AppendTitle(string title)
        {
            Text = BaseName + " " + title;
        }
    }
}
