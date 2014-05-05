using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NeuQuant.IO;
using NeuQuant.Processing;
using WeifenLuo.WinFormsUI.Docking;

namespace NeuQuant
{
    public partial class ProcessorForm : DockContent
    {
        public event EventHandler Analyze;

        private void OnAnalyze()
        {
            var handler = Analyze;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        public ProcessorForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OnAnalyze();
        }

        public Processor GetProcessor(NeuQuantFile nqFile)
        {
            string analysisName = textBox1.Text;
            int numberOfisotopes = (int) numericUpDown1.Value;
            bool noiseBandCap = noiseBandCapCB.Enabled && noiseBandCapCB.Checked;
            bool checkIsotopicDistribution = isotopicDistributionCheck.Enabled && isotopicDistributionCheck.Checked;

            double minResolution = 0;

            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                if (checkedListBox1.GetItemChecked(i))
                {
                    minResolution = (double)checkedListBox1.Items[i];
                    break;
                }
            }

            double minRtBounds = (double) numericUpDown2.Value;
            double maxRtBounds = (double) numericUpDown3.Value;
            //double minResolution = -10;
            //if (numericUpDown3.Enabled)
            //    minResolution = (double) numericUpDown3.Value;

            var processor = new Processor(nqFile, analysisName, numberOfisotopes, minRtBounds, maxRtBounds, minResolution, checkIsotopicDistribution: checkIsotopicDistribution, noiseBandCap: noiseBandCap);
            return processor;
        }
        
        public void SetProcessor(Processor processor)
        {
            textBox1.Text = processor.Name;
            numericUpDown1.Value = processor.NumberOfIsotopesToQuantify;
            noiseBandCapCB.Checked = processor.NoiseBandCap;
            isotopicDistributionCheck.Checked = processor.UseIsotopicDistribution;
            numericUpDown2.Value = (decimal)processor.MinimumRtDelta;
            numericUpDown3.Value = (decimal)processor.MaximumRtDelta;
        
            double minResolution = processor.MinimumResolution;
            
            checkedListBox1.DataSource = new BindingList<double>(processor.NqFile.GetUniqueResolutions().ToList());

            for (int i = 0; i < checkedListBox1.Items.Count; i++)
            {
                if (checkedListBox1.Items[i].Equals(minResolution))
                {
                    checkedListBox1.SetItemChecked(i,true);
                    break;
                }
            }
          
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
                for (int i = 0; i < checkedListBox1.Items.Count; ++i)
                    if (e.Index != i) checkedListBox1.SetItemChecked(i, false);
        }
    }
}
