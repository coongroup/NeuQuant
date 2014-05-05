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
            double minSN = (double) numericUpDown5.Value;
            double maxSN = (double) numericUpDown4.Value;
            double isoPercentError = (double) numericUpDown6.Value / 100;
            double lowSpacingPercent = (double) numericUpDown8.Value/100;
            double highSpacingPercent = (double) numericUpDown7.Value/100;
            //double minResolution = -10;
            //if (numericUpDown3.Enabled)
            //    minResolution = (double) numericUpDown3.Value;

            var processor = new Processor(nqFile, analysisName, numberOfisotopes, minRtBounds, maxRtBounds, minResolution, checkIsotopicDistribution: checkIsotopicDistribution, noiseBandCap: noiseBandCap,
                minSN: minSN, maxSN: maxSN, isotopicDistributionPercentError: isoPercentError, lowerSpacingPercent:lowSpacingPercent, upperSpacingPercent: highSpacingPercent);
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
            numericUpDown5.Value = (decimal) processor.MinimumSN;
            numericUpDown4.Value = (decimal)Math.Min(processor.MaximumSN, (double)numericUpDown4.Maximum);
            numericUpDown6.Value = (decimal) processor.IsotopicDistributionPercentError * 100;
            numericUpDown8.Value = (decimal) processor.LowerSpacingPercent*100;
            numericUpDown7.Value = (decimal) processor.UpperSpacingPercent*100;
          
            double minResolution = processor.MinimumResolution;
            
            checkedListBox1.DataSource = new BindingList<double>(processor.NqFile.GetUniqueResolutions().ToList());
            checkedListBox1.SetItemChecked(0, true);

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

        private void isotopicDistributionCheck_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown6.Enabled = isotopicDistributionCheck.Checked;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown7.Enabled = numericUpDown8.Enabled = checkBox1.Checked;
        }
    }
}
