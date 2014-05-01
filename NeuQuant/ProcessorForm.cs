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
            bool isFusion = isFusionCB.Enabled && isFusionCB.Checked;
            double rtBounds = (double) numericUpDown2.Value;
            double minResolution = -10;
            if (numericUpDown3.Enabled)
                minResolution = (double) numericUpDown3.Value;

            var processor = new Processor(nqFile, analysisName, numberOfisotopes, rtBounds, rtBounds, minResolution, checkIsotopicDistribution: checkIsotopicDistribution, noiseBandCap: noiseBandCap);
            return processor;
        }

        private void isFusionCB_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown3.Enabled = !isFusionCB.Checked;
        }
    }
}
