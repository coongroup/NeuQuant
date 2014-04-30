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
