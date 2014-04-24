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
    public partial class GraphForm : DockContent, IDockContent
    {
        public GraphForm()
        {
            InitializeComponent();
            LinkedGraphForms = new List<GraphForm>();
        }

        public List<GraphForm> LinkedGraphForms;

        public void LinkForms(GraphForm form)
        {
            LinkedGraphForms.Add(form);
            form.LinkedGraphForms.Add(this);
        }
    }
}
