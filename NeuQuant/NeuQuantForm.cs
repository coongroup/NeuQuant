using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NeuQuant.Processing;
using WeifenLuo.WinFormsUI.Docking;
using NeuQuant.IO;
using CSMSL.IO.Thermo;
using ZedGraph;
using CSMSL.Spectral;
using CSMSL.Proteomics;
using CSMSL.Util.Collections;
using CSMSL;
using CSMSL.Chemistry;

namespace NeuQuant
{
    public partial class NeuQuantForm : Form
    {

        #region Statics

        public static readonly string ProgramVersion = string.Format("NeuQuant {0}-bit (v{1})", IntPtr.Size * 8, GetRunningVersion());

        public static Version GetRunningVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        }

        #endregion

        #region Child Forms

        private TextBoxForm _logForm;
        private GraphForm _msSpectrumForm;
        private GraphForm _xicForm;
        private GraphForm _spacingForm;
        private DGVForm _psmsForm;
        private DGVForm _peptidesForm;
        private NeuQuantFileGeneratorForm _nqFileGeneratorForm;
        private QuantiativeLabelManagerForm _labelManagerForm;

        #endregion

        private NeuQuantFile _currentNQFile = null;
        private Processor _currentProcessor = null;
                
        public NeuQuantForm()
        {
            InitializeComponent();
            NeuQuantFile.OnProgess += OnProgress;
            NeuQuantFile.OnMessage += OnMessage;
        }

        protected override void OnLoad(EventArgs e)
        {
            Text = ProgramVersion;

            _logForm = new TextBoxForm();
            _logForm.Text = "Log";
            _logForm.TextBox.ReadOnly = true;
            _logForm.Show(dockPanel1, DockState.DockRightAutoHide);
            RegisterForm(_logForm);

            _msSpectrumForm = new GraphForm();
            _msSpectrumForm.Text = "MS Spectrum";
            _msSpectrumForm.Show(dockPanel1, DockState.Document);
            _msSpectrumForm.GraphControl.PreviewKeyDown +=GraphControl_PreviewKeyDown;
            _msSpectrumForm.GraphControl.GraphPane.XAxis.Scale.Min = 0;
            _msSpectrumForm.GraphControl.GraphPane.XAxis.Scale.Max = 2000;
            _msSpectrumForm.GraphControl.GraphPane.XAxis.Title.Text = "m/z";
            _msSpectrumForm.GraphControl.GraphPane.YAxis.Scale.Min = 0;
            _msSpectrumForm.GraphControl.GraphPane.YAxis.Title.Text = "S/N";
            _msSpectrumForm.GraphControl.GraphPane.Title.Text = "Mass Spectrum";
            _msSpectrumForm.GraphControl.GraphPane.XAxis.MajorTic.IsOutside = true;
            _msSpectrumForm.GraphControl.GraphPane.XAxis.MajorTic.IsInside = false;
            _msSpectrumForm.GraphControl.GraphPane.XAxis.MinorTic.IsOutside = true;
            _msSpectrumForm.GraphControl.GraphPane.XAxis.MinorTic.IsInside = false;
            RefreshGraph(_msSpectrumForm.GraphControl);
            RegisterForm(_msSpectrumForm);

            _xicForm = new GraphForm();
            _xicForm.Text = "eXtracted Ion Chromatograms";
            _xicForm.Show(dockPanel1, DockState.Document);
            //_xicForm.DockPanel = dockPanel1;
            _xicForm.GraphControl.GraphPane.XAxis.Scale.Min = 0;
            _xicForm.GraphControl.GraphPane.XAxis.Scale.Max = 2000;
            _xicForm.GraphControl.GraphPane.XAxis.Title.Text = "Retention Time (min)";
            _xicForm.GraphControl.GraphPane.YAxis.Scale.Min = 0;
            _xicForm.GraphControl.GraphPane.YAxis.Title.Text = "S/N";
            _xicForm.GraphControl.GraphPane.Title.Text = "XIC";
            _xicForm.GraphControl.MouseMove += MoveVerticalLine;
            _xicForm.GraphControl.MouseClick += ToggleCurveVisibility;
            _xicForm.GraphControl.MouseClick += RetentionTimePlotClick;
            _xicForm.GraphControl.PreviewKeyDown += GraphControl_PreviewKeyDown;
            _xicForm.GraphControl.GraphPane.XAxis.MajorTic.IsOutside = true;
            _xicForm.GraphControl.GraphPane.XAxis.MajorTic.IsInside = false;
            _xicForm.GraphControl.GraphPane.XAxis.MinorTic.IsOutside = true;
            _xicForm.GraphControl.GraphPane.XAxis.MinorTic.IsInside = false;
            _xicForm.GraphControl.GraphPane.YAxis.MajorTic.IsOutside = true;
            _xicForm.GraphControl.GraphPane.YAxis.MajorTic.IsInside = false;
            _xicForm.GraphControl.GraphPane.YAxis.MinorTic.IsOutside = true;
            _xicForm.GraphControl.GraphPane.YAxis.MinorTic.IsInside = false;
            _xicForm.GraphControl.ContextMenuBuilder += GraphControl_ContextMenuBuilder;
            

            RefreshGraph(_xicForm.GraphControl);
            RegisterForm(_xicForm);

            _spacingForm = new GraphForm();
            _spacingForm.Text = "Peak Spacing";
            _spacingForm.GraphControl.MouseMove += MoveVerticalLine;
            _spacingForm.GraphControl.MouseClick += ToggleCurveVisibility;
            _spacingForm.GraphControl.MouseClick += RetentionTimePlotClick;
            _spacingForm.GraphControl.PreviewKeyDown += GraphControl_PreviewKeyDown;
            _spacingForm.Show(dockPanel1, DockState.Document);
            RefreshGraph(_spacingForm.GraphControl);
            RegisterForm(_spacingForm);

            _peptidesForm = new DGVForm("Peptides");
            _peptidesForm.Show(dockPanel1, DockState.DockBottom);
            _peptidesForm.DataGridView.RowEnter += PeptideRowEnter;
            RegisterForm(_peptidesForm);

            _nqFileGeneratorForm = new NeuQuantFileGeneratorForm();
            //_nqFileGeneratorForm.Show();
            //_nqFileGeneratorForm.Show(dockPanel1, DockState.Document);
            //RegisterForm(_nqFileGeneratorForm);

            _labelManagerForm = new QuantiativeLabelManagerForm();
            _labelManagerForm.DockPanel = dockPanel1;
            RegisterForm(_labelManagerForm);

            // Link this two forms together so their events are reflected in both
            _spacingForm.LinkForms(_xicForm);

            LogMessage(" == " + ProgramVersion + " == ", false);

            LoadNeuQuantFile(@"E:\Desktop\NeuQuant\2plex NeuCode Charger\19February2014_duplex_480K_1to1.sqlite");

            base.OnLoad(e);
        }

        void GraphControl_ContextMenuBuilder(ZedGraphControl sender, ContextMenuStrip menuStrip, Point mousePt, ZedGraphControl.ContextMenuObjectState objState)
        {
            ToolStripMenuItem item = new ToolStripMenuItem("Show Smoothed");
            item.CheckOnClick = true;
            menuStrip.Items.Add(item);
           
        }
        
        void OnMessage(object sender, MessageEventArgs e)
        {
            LogMessage(e.Message, true);
        }

        void OnProgress(object sender, ProgressEventArgs e)
        {
            SetProgress(e.Percent);
        }

        public void LoadNeuQuantFile(string filePath)
        {
            LoadNeuQuantFile(new NeuQuantFile(filePath));
        }

        public void LoadNeuQuantFile(NeuQuantFile file)
        {
            if (file == null)
                return;

            Task t = Task.Factory.StartNew(() =>
            {
                SetStatusText("Loading File...");
                LogMessage("Attempting to load NeuQuant File " + file.FilePath);

                if (_currentNQFile != null)
                {
                    LogMessage("Saving old NeuQuant File " + _currentNQFile.FilePath + " first...");
                    //TODO add stuff to save and close the old file
                }

                _currentNQFile = file;              
                _currentNQFile.Open();

                Processor processor = null;
                _currentProcessor = _currentNQFile.TryGetLastProcessor(out processor) ? processor : new Processor(_currentNQFile);
                
                _currentProcessor.Open();

                LogMessage("Loaded NeuQuant File " + _currentNQFile.FilePath);
               
            }).ContinueWith((t2) =>
            {               
                Text = ProgramVersion + " - " + _currentNQFile.FilePath;
                DisplayPeptides(_currentNQFile, _peptidesForm);
                SetStatusText("Ready");


            }, TaskScheduler.FromCurrentSynchronizationContext());
            
        }

        public void SetStatusText(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetStatusText), message);
                return;
            }

            statusLabel.Text = message;            
        }

        public void SetProgress(double percent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<double>(SetProgress), percent);
                return;
            }
                     
            progressBar.Value = (int)(percent * progressBar.Maximum);       
        }

        public void LogMessage(string message, bool timeStamped = true)
        {
            if(_logForm != null)
                LogMessage(message, _logForm.TextBox, timeStamped);
        }

        public void LogMessage(string message, RichTextBox richTextBox, bool timeStamped = true)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string, RichTextBox, bool>(LogMessage), message, richTextBox, timeStamped);
                return;
            }
        
            if (timeStamped)
            {
                message =  string.Format("[{0}]\t{1}", DateTime.Now.ToLongTimeString(),message);
            }

            richTextBox.AppendText(message);
            richTextBox.AppendText("\r\n");
            richTextBox.ScrollToCaret();
        }

        #region Graphing

        public void ClearGraph(ZedGraphControl control)
        {
            control.GraphPane.CurveList.Clear();
            control.GraphPane.GraphObjList.Clear();           
        }

        public void RefreshGraph(ZedGraphControl control, bool changeAxis = true)
        {
            if (changeAxis)
                control.AxisChange();
            control.Invalidate();
        }

        private void PlotSpacing(ZedGraphControl control, NeuQuantFeatureSet featureSet, double deltaRT = 0.75)
        {
            if (_currentNQFile == null || featureSet == null)
                return;

            if (!featureSet.Peptide.ContainsIsotopologue)
                return;

            double minRT = featureSet.MinimumRetentionTime;
            double maxRT = featureSet.MaximumRetentionTime;
            int charge = featureSet.ChargeState;
            double bestRT = featureSet.BestPSM.RetentionTime;
            
            ClearGraph(control);

            Color[] colors = new Color[] { Color.CornflowerBlue, Color.Sienna, Color.DarkBlue};

            var isotopologues = featureSet.Peptide.QuantifiableChannels.Values;
            control.GraphPane.Title.Text = "";
            int colorI = 0;

            double[] expectedSpacing = Isotopologue.GetExpectedSpacings(isotopologues, charge);

            for (int isotope = 0; isotope < 1; isotope++)
            {
                var color = colors[isotope];
                double isotopeMass = isotope*Constants.C13C12Difference;
                double[] spacings = new double[featureSet.NumberOfFeatures];
                double[] times = new double[featureSet.NumberOfFeatures];
                int i = 0;
                foreach (var spectrum in featureSet.Select(fs => fs.Spectrum))
                {
                    double previousMZ = -1;
                    double spacing = 0;

                    foreach (var isotopologue in isotopologues)
                    {
                        double mass = isotopologue.MonoisotopicMass + isotopeMass;
                        double mz = Mass.MzFromMass(mass, charge);
                        var range = new MzRange(mz, Tolerance.FromPPM(15));
                        var peak = spectrum.GetClosestPeak(range);
                        if (peak == null) continue;
                        if (previousMZ < 0)
                        {
                            previousMZ = peak.MZ;
                        }
                        else
                        {
                            spacing = peak.MZ - previousMZ;
                            previousMZ = peak.MZ;
                        }
                    }
                    
                    times[i] = spectrum.RetentionTime;
                    spacings[i] = spacing*1000;
                    i++;
                }
                var pointPairList = new PointPairList(times, spacings);
                LineItem item = control.GraphPane.AddCurve("Spacing for isotope " + isotope, pointPairList, color, SymbolType.Circle);
                item.Line.IsVisible = false;
                item.Symbol.Size = 10f;
                item.Symbol.Border.Width = 2f;
                
                var pointPairList2 = new PointPairList(new[] { minRT, maxRT }, new[] { expectedSpacing[0] * 1000, expectedSpacing[0] * 1000 });

                LineObj expectedLine = new LineObj(color, 0, expectedSpacing[0]*1000, 1, expectedSpacing[0]*1000);
                expectedLine.Location.CoordinateFrame = CoordType.XChartFractionYScale;
                expectedLine.Line.Width = 1.5F;
                expectedLine.Line.Style = DashStyle.Dot;
                control.GraphPane.GraphObjList.Add(expectedLine);

                BoxObj boxObj = new BoxObj(0, expectedSpacing[0]*1000*1.15, 1, expectedSpacing[0]*1000*0.4, Color.Empty, Color.FromArgb(50, color));
                boxObj.Location.CoordinateFrame = CoordType.XChartFractionYScale;
                boxObj.ZOrder = ZOrder.F_BehindGrid;
                boxObj.IsClippedToChartRect = true;
                control.GraphPane.GraphObjList.Add(boxObj);
            }
            control.GraphPane.XAxis.Title.Text = "Retention Time (min)";
            control.GraphPane.YAxis.Title.Text = "Peak Spacing (mDa)";
           
            control.GraphPane.YAxis.Scale.Min = -2;
            control.GraphPane.YAxis.Scale.MaxAuto = true;
            control.GraphPane.XAxis.Scale.Min = minRT;
            control.GraphPane.XAxis.Scale.Max = maxRT;

            // Hide the y=0 axis line
            control.GraphPane.YAxis.MajorGrid.IsZeroLine = false;
            
            // Store the current peptide
            control.Tag = featureSet;

            RefreshGraph(control);
        }

        private void PlotXIC(ZedGraphControl control, NeuQuantFeatureSet featureSet, double deltaRT = 0.75)
        {
            if (_currentNQFile == null)
                return;
            
            double minRT = featureSet.MinimumRetentionTime;
            double maxRT = featureSet.MaximumRetentionTime;
            int charge = featureSet.ChargeState;
            double bestRT = featureSet.BestPSM.RetentionTime;

            ClearGraph(control);

            Color[] colors = new Color[] { Color.CornflowerBlue, Color.Sienna};

            var isotopologues = featureSet.Peptide.QuantifiableChannels.Values;
            control.GraphPane.Title.Text = "";
            int colorI = 0;
            foreach (var isotopologue in isotopologues)
            {
                double totalTIC = 0;
                Color color = colors[colorI++];
                double[] totalIntensities = new double[featureSet.NumberOfFeatures];
                double[] times = new double[featureSet.NumberOfFeatures];

                int i = 0;
                foreach (var feature in featureSet)
                {
                    double featureIntensity = feature.GetChannelIntensity(isotopologue, false);
                    totalIntensities[i] = featureIntensity;
                    totalTIC += featureIntensity;
                    times[i] = feature.RetentionTime;
                    i++;
                }

                DoubleRange rtBounds =  featureSet.GetBounds(isotopologue);

                BoxObj boxObj = new BoxObj(rtBounds.Minimum, 0, rtBounds.Width, 1, Color.Empty, Color.FromArgb(50, color));
                //boxObj.Border.Style = DashStyle.Dot;
                boxObj.Location.CoordinateFrame = CoordType.XScaleYChartFraction;
                boxObj.IsClippedToChartRect = true;
                boxObj.ZOrder = ZOrder.D_BehindAxis;
                control.GraphPane.GraphObjList.Add(boxObj);


                //for (int isotope = 0; isotope < 3; isotope++)
                //{
                //    double mz = isotopologue.ToMz(charge, isotope);
                //    var range = new MzRange(mz, Tolerance.FromPPM(10));

                //    var chrom = spectra.GetClosetsPeakChromatogram(range).Smooth(SmoothingType.BoxCar, 3);
                    
                //    double[] intensities = chrom.GetIntensities();
                //    times = chrom.GetTimes();
                //    totalTIC += chrom.TotalIonCurrent;
                //    var pointPairList = new PointPairList(times, intensities);
                //    for (int i = 0; i < chrom.Count; i++)
                //        totalIntensities[i] += intensities[i];
                //    //LineItem item = control.GraphPane.AddCurve(isotopologue + " m/z " + range.ToString("F4"), pointPairList, color);
                //    //item.Line.DashOn = 2.5F * isotope;
                //    //item.Line.DashOff = 2.5F * isotope;
                //    //item.Line.Style = DashStyle.Custom;
                //    //item.Symbol.IsVisible = false;
                //    //item.Line.IsOptimizedDraw = true;
                //    //item.Line.Width = 1.5f;
                //}

                //var chrom2 = new Chromatogram(times, totalIntensities);
                //var apex = chrom2.FindNearestApex(bestRT, 2);

                //TextObj apexObj = new TextObj(apex.Time.ToString("F2"), apex.Time, apex.Intensity);
                //control.GraphPane.GraphObjList.Add(apexObj);

                //DoubleRange width = chrom2.GetPeakWidth(apex.Time, 0.1, 2);

                //BoxObj boxObj = new BoxObj(width.Minimum, 0, width.Width, 1, Color.Empty, Color.FromArgb(25, color));
                ////boxObj.Border.Style = DashStyle.Dot;
                //boxObj.Location.CoordinateFrame = CoordType.XScaleYChartFraction;
                //boxObj.IsClippedToChartRect = true;
                //boxObj.ZOrder = ZOrder.D_BehindAxis;
                //control.GraphPane.GraphObjList.Add(boxObj);

                var pointPairList2 = new PointPairList(times, totalIntensities);
                LineItem item2 = control.GraphPane.AddCurve(isotopologue.ToString(), pointPairList2, color,SymbolType.Circle);
                item2.Line.Width = 2.5f;

                control.GraphPane.Title.Text += isotopologue + " " + totalTIC.ToString("G5") + " ";
            }
            control.GraphPane.Title.Text = "";
            control.GraphPane.XAxis.Scale.Min = minRT;
            control.GraphPane.XAxis.Scale.Max = maxRT;
            control.AxisChange();

            foreach (var psm in featureSet.PSMs)
            {
                double x = psm.RetentionTime;

                //var text = new TextObj(psm.MatchScore.ToString("G3"), x, 0, CoordType.XScaleYChartFraction, AlignH.Center, AlignV.Top);
                
                LineObj verticalLine = new LineObj(Color.LightSlateGray, x, 0, x, control.GraphPane.YAxis.Scale.Max);

                if (psm == featureSet.BestPSM)
                {
                    verticalLine.Line.Color = Color.Black;
                }

                verticalLine.ZOrder = ZOrder.E_BehindCurves;
                verticalLine.IsClippedToChartRect = true;
                verticalLine.Line.Style = DashStyle.Dash;
              
          
                control.GraphPane.GraphObjList.Add(verticalLine);
            }

            // Store the current peptide
            control.Tag = featureSet;

            RefreshGraph(control);
        }

        public PointPairList PlotSpectrum(ZedGraphControl control, PeptideSpectrumMatch psm)
        {
            if (_currentNQFile == null)
                return null;

            var spectrum = _currentNQFile.GetSpectrum(psm);

            if (spectrum == null)
                return null;

            ClearGraph(control);          

            var pointPairList = new PointPairList(spectrum.GetMasses(), spectrum.GetIntensities());

            control.GraphPane.XAxis.Scale.Min = pointPairList[0].X - 10;
            control.GraphPane.XAxis.Scale.Max = pointPairList[pointPairList.Count - 1].X + 10;

            LineItem item = control.GraphPane.AddStick("Spectrum Number: " + spectrum.ScanNumber, pointPairList, Color.Black);
            item.Symbol.IsVisible = false;

            RefreshGraph(control, true);

            return pointPairList;
        }

        public PointPairList PlotSpectrum(ZedGraphControl control, NeuQuantSpectrum spectrum)
        {
            ClearGraph(control);

            var pointPairList = new PointPairList(spectrum.GetMasses(), spectrum.GetIntensities());

            control.GraphPane.XAxis.Scale.Min = pointPairList[0].X - 10;
            control.GraphPane.XAxis.Scale.Max = pointPairList[pointPairList.Count - 1].X + 10;

            LineItem item = control.GraphPane.AddStick("Spectrum Number: "+spectrum.ScanNumber, pointPairList, Color.Black);
            item.Symbol.IsVisible = false;

            RefreshGraph(control, true);

            return pointPairList;
        }

        private NeuQuantSpectrum PlotPrecursorSpectrum(ZedGraphControl control, NeuQuantFeature feature)
        {
            if (_currentNQFile == null)
                return null;
            
            var spectrum = feature.Spectrum;

            if (spectrum == null)
                return null;

            ClearGraph(control);

            double[] masses = spectrum.GetMasses();
            double[] intensities = spectrum.GetIntensities();

            var pointPairList = new PointPairList(masses, intensities);

            var psm = feature.ParentSet.BestPSM;

            double minMZ = spectrum.FirstMz;
            double maxMZ = spectrum.LastMZ;

            control.GraphPane.XAxis.Scale.Min = minMZ;
            control.GraphPane.XAxis.Scale.Max = maxMZ;
            control.GraphPane.YAxis.Scale.MaxAuto = true;

            LineItem item = control.GraphPane.AddStick("Spectrum Number: " + spectrum.ScanNumber, pointPairList, Color.Black);
            item.Symbol.IsVisible = false;

            Color[] colors = new Color[] { Color.CornflowerBlue, Color.Sienna };
            item.Line.GradientFill = new Fill(Color.Black, Color.CornflowerBlue, Color.Sienna);
            item.Line.GradientFill.Type = FillType.GradientByZ;
            item.Line.GradientFill.RangeMax = 2;
            item.Line.Width = 2F;
            int c = 0;
            var isotopologues = feature.Peptide.QuantifiableChannels.Values;
            foreach (var isotopologue in isotopologues)
            {
                c++;
                string message = string.Join(",", isotopologue.GetUniqueModifications<Modification>());
                for (int i = 0; i < 3; i++)
                {
                    double mz = isotopologue.ToMz(psm.Charge, i);
                    
                    PointPair bestPoint = null;
                    double bestSpacing = double.MaxValue;
                    foreach (var pointpair in pointPairList)
                    {
                        double spacing = Math.Abs(pointpair.X - mz);
                        if (spacing < bestSpacing)
                        {
                            bestSpacing = spacing;
                            bestPoint = pointpair;
                        }
                    }

                    double ppm = Tolerance.GetTolerance(mz + bestSpacing, mz, ToleranceType.PPM);

                    if (ppm < 10)
                    {
                        bestPoint.ColorValue = c;
                    }
                }
            }
         
            RefreshGraph(control);

            return spectrum;
        }

        #endregion
        
        void PeptideRowEnter(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv == null)
                return;

            DataGridViewRow row = dgv.Rows[e.RowIndex];
            if (row == null)
                return;

            NeuQuantPeptide peptide = row.DataBoundItem as NeuQuantPeptide;
            if (peptide == null)
                return;

            if (!peptide.ContainsQuantitativeChannel)
            {
                ClearGraph(_xicForm.GraphControl);
                RefreshGraph(_xicForm.GraphControl);
                ClearGraph(_spacingForm.GraphControl);
                RefreshGraph(_spacingForm.GraphControl);
                return;
            }

            List<NeuQuantFeatureSet> featureSets = _currentProcessor.ExtractFeatureSets(peptide, 3).ToList();

            NeuQuantFeatureSet featureSet = featureSets[0];

            featureSet.FindPeaks(Tolerance.FromPPM(10), 3);
            featureSet.FindElutionProfile(3);

            PlotXIC(_xicForm.GraphControl, featureSet);
            PlotSpacing(_spacingForm.GraphControl, featureSet);
        }

        #region Graph Handlers
        
        void ToggleCurveVisibility(object sender, MouseEventArgs e)
        {
            ZedGraphControl graphControl = sender as ZedGraphControl;
            if (graphControl == null)
                return;

            int legend = 0;
            if (!graphControl.GraphPane.Legend.FindPoint(e.Location, graphControl.GraphPane, graphControl.GraphPane.CalcScaleFactor(), out legend)) 
                return;

            var curve = graphControl.GraphPane.CurveList[legend];
            curve.Label.FontSpec = new FontSpec(graphControl.GraphPane.Legend.FontSpec);
            if (curve.IsVisible = !curve.IsVisible)
            {
                curve.Label.FontSpec.FontColor = Color.Black;
            }
            else
            {
                curve.Label.FontSpec.FontColor = Color.Gray;
            }

            graphControl.Refresh();
        }

        void RetentionTimePlotClick(object sender, MouseEventArgs e)
        {
            ZedGraphControl graphControl = sender as ZedGraphControl;
            if (graphControl == null)
                return;

            NeuQuantFeatureSet featureSet = graphControl.Tag as NeuQuantFeatureSet;
            if (featureSet == null)
                return;

            double retentionTime, y;
            graphControl.GraphPane.ReverseTransform(e.Location, out retentionTime, out y);
           
            if (retentionTime < graphControl.GraphPane.XAxis.Scale.Min ||
               retentionTime > graphControl.GraphPane.XAxis.Scale.Max ||
               y < graphControl.GraphPane.YAxis.Scale.Min ||
               y > graphControl.GraphPane.YAxis.Scale.Max
               )
            {               
                return;
            }

            var feature = featureSet.GetFeature(retentionTime);

            var spectrum = PlotPrecursorSpectrum(_msSpectrumForm.GraphControl, feature);
            if (spectrum == null)
                return;

            // Clear the old one if it is there            
            int i = graphControl.GraphPane.GraphObjList.IndexOfTag("ms1Line");
            if (i >= 0)
                graphControl.GraphPane.GraphObjList.RemoveAt(i);
            
            LineObj verticalLine = new LineObj(spectrum.RetentionTime, 0, spectrum.RetentionTime, 1);
            verticalLine.Line.Color = Color.Green;
            verticalLine.Location.CoordinateFrame = CoordType.XScaleYChartFraction;
            verticalLine.ZOrder = ZOrder.D_BehindAxis;
            verticalLine.Tag = "ms1Line";
            graphControl.GraphPane.GraphObjList.Add(verticalLine);

            GraphForm parentForm = graphControl.ParentForm as GraphForm;
            if (parentForm != null)
            {
                foreach (GraphForm linkedForm in parentForm.LinkedGraphForms)
                {
                    i = linkedForm.GraphControl.GraphPane.GraphObjList.IndexOfTag("ms1Line");
                    if (i >= 0)
                        linkedForm.GraphControl.GraphPane.GraphObjList.RemoveAt(i);

                    linkedForm.GraphControl.GraphPane.GraphObjList.Add(verticalLine);
                    linkedForm.GraphControl.Refresh();
                }
            }

            graphControl.Refresh();
        } 

        void MoveVerticalLine(object sender, MouseEventArgs e)
        {
            ZedGraphControl graphControl = sender as ZedGraphControl;
            if (graphControl == null)
                return;

            double x,y;
            graphControl.GraphPane.ReverseTransform(e.Location, out x, out y);

            // Clear the old one if it is there            
            int i = graphControl.GraphPane.GraphObjList.IndexOfTag("verticalLine");
            if(i >= 0)
                graphControl.GraphPane.GraphObjList.RemoveAt(i);

            GraphForm parentForm = graphControl.ParentForm as GraphForm;

            if (parentForm != null && parentForm.LinkedGraphForms.Count > 0)
            {
                foreach (GraphForm linkedForm in parentForm.LinkedGraphForms)
                {
                    i = linkedForm.GraphControl.GraphPane.GraphObjList.IndexOfTag("verticalLine");
                    if (i >= 0)
                        linkedForm.GraphControl.GraphPane.GraphObjList.RemoveAt(i);
                    linkedForm.GraphControl.Refresh();
                }
            }


            if (x < graphControl.GraphPane.XAxis.Scale.Min ||
                x > graphControl.GraphPane.XAxis.Scale.Max ||
                y < graphControl.GraphPane.YAxis.Scale.Min ||
                y > graphControl.GraphPane.YAxis.Scale.Max
                )
            {
               
            }
            else
            {
                
                LineObj verticalLine = new LineObj(x, 0, x, 1);
                verticalLine.Line.Color = Color.DarkCyan;
                verticalLine.Location.CoordinateFrame = CoordType.XScaleYChartFraction;
                verticalLine.ZOrder = ZOrder.D_BehindAxis;
                verticalLine.Tag = "verticalline";
          
                if (parentForm != null && parentForm.LinkedGraphForms.Count > 0)
                {
                    foreach (GraphForm linkedForm in parentForm.LinkedGraphForms)
                    {
                        linkedForm.GraphControl.GraphPane.GraphObjList.Add(verticalLine);
                        linkedForm.GraphControl.Refresh();
                    }
                }

                graphControl.GraphPane.GraphObjList.Add(verticalLine);
            }

            graphControl.Refresh();
        }

        void GraphControl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (_currentNQFile == null)
                return;

            ZedGraphControl graphControl = sender as ZedGraphControl;
            if (graphControl == null)
                return;

            NeuQuantFeatureSet featureSet = graphControl.Tag as NeuQuantFeatureSet;
            if (featureSet == null)
                return;
            
            int i = graphControl.GraphPane.GraphObjList.IndexOfTag("ms1Line");
            if (i < 0)
                return;

            double rt = graphControl.GraphPane.GraphObjList[i].Location.X;
         
            int featureIndex = featureSet.GetFeatureIndex(rt);

            if (e.KeyCode == Keys.Right)
            {
                featureIndex = Math.Min(featureSet.NumberOfFeatures - 1, featureIndex + 1);
            }          
            else if (e.KeyCode == Keys.Left)
            {
                featureIndex = Math.Max(0, featureIndex - 1);
            }

            var spectrum = PlotPrecursorSpectrum(_msSpectrumForm.GraphControl, featureSet[featureIndex]);

            if (spectrum == null)
                return;

            // Clear the old one if it is there            
            i = graphControl.GraphPane.GraphObjList.IndexOfTag("ms1Line");
            if (i >= 0)
                graphControl.GraphPane.GraphObjList.RemoveAt(i);

            LineObj verticalLine = new LineObj(spectrum.RetentionTime, 0, spectrum.RetentionTime, 1);
            verticalLine.Line.Color = Color.Green;
            verticalLine.Location.CoordinateFrame = CoordType.XScaleYChartFraction;
            verticalLine.ZOrder = ZOrder.D_BehindAxis;
            verticalLine.Tag = "ms1Line";
            graphControl.GraphPane.GraphObjList.Add(verticalLine);

            GraphForm parentForm = graphControl.ParentForm as GraphForm;
            if (parentForm != null)
            {
                foreach (GraphForm linkedForm in parentForm.LinkedGraphForms)
                {
                    i = linkedForm.GraphControl.GraphPane.GraphObjList.IndexOfTag("ms1Line");
                    if (i >= 0)
                        linkedForm.GraphControl.GraphPane.GraphObjList.RemoveAt(i);

                    linkedForm.GraphControl.GraphPane.GraphObjList.Add(verticalLine);
                    linkedForm.GraphControl.Refresh();
                }
            }

            graphControl.Refresh();
        }

        #endregion
        
        private void Create()
        {
            Task t = Task.Factory.StartNew(() =>
            {
                OmssaPeptideSpectralMatchFile psmFile = new OmssaPeptideSpectralMatchFile(@"E:\Desktop\NeuQuant\2plex NeuCode Charger\19February2014_duplex_480K_1to1_ITMS_CID_psms.csv");
                psmFile.SetDataDirectory(@"E:\Desktop\NeuQuant\2plex NeuCode Charger");
                psmFile.AddFixedModification(Reagents.K8Plex2);
                psmFile.AddFixedModification(new Modification("C2H3NO", "CAM", ModificationSites.C));
                psmFile.LoadUserMods(@"E:\Desktop\NeuQuant\AverageLys8-119.xml");

                NeuQuantFile.LoadData(@"E:\Desktop\NeuQuant\2plex NeuCode Charger\19February2014_duplex_480K_1to1.sqlite", psmFile);
            }).ContinueWith((t2) => LoadNeuQuantFile(@"E:\Desktop\NeuQuant\2plex NeuCode Charger\19February2014_duplex_480K_1to1.sqlite"), TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void DisplayPeptides(NeuQuantFile nqFile, DGVForm form)
        {
            SortableBindingList<NeuQuantPeptide> peptides = new SortableBindingList<NeuQuantPeptide>(nqFile.GetPeptides());
            form.DataGridView.AutoGenerateColumns = false;

            DataGridViewTextBoxColumn sequenceColumn = new DataGridViewTextBoxColumn();
            sequenceColumn.DataPropertyName = "Sequence";
            sequenceColumn.HeaderText = "Sequence";
            form.DataGridView.Columns.Add(sequenceColumn);

            DataGridViewTextBoxColumn psmColumn = new DataGridViewTextBoxColumn();
            psmColumn.DataPropertyName = "NumberOfPeptideSpectrumMatches";
            psmColumn.HeaderText = "# PSMs";
            form.DataGridView.Columns.Add(psmColumn);


            DataGridViewTextBoxColumn channelsColumn = new DataGridViewTextBoxColumn();
            channelsColumn.DataPropertyName = "NumberOfChannels";
            channelsColumn.HeaderText = "# Quantitative Channels";
            form.DataGridView.Columns.Add(channelsColumn);

            form.DataGridView.DataSource = peptides;
            int count = peptides.Count;
            form.AppendTitle("(" + count + ")");
            LogMessage("Loaded " + count + " peptides.", true);
        }

        private void DisplayPsms(NeuQuantFile nqFile, DGVForm form)
        {          
            SortableBindingList<PeptideSpectrumMatch> psms = new SortableBindingList<PeptideSpectrumMatch>(nqFile.GetPsms());
            form.DataGridView.DataSource = psms;
            int count = psms.Count;
            form.AppendTitle("(" + count + ")");
            LogMessage("Loaded " + count + " psms.", true);
        }

        void psmFile_Message(object sender, MessageEventArgs e)
        {
            LogMessage(e.Message, true);
        }
        
        private void RegisterForm(Form form)
        {            
            var item = new ToolStripMenuItem();
            item.Tag = form;
            item.Text = form.Text;
            item.Click += windowFormClick;
            windowMenuItem.DropDownItems.Add(item);
        }
                
        private void windowFormClick(object sender, EventArgs e)
        {
            var menuItem = sender as ToolStripMenuItem;
            if (menuItem == null)
                return;

            var dockingForm = menuItem.Tag as DockContent;
            if (dockingForm != null)
            {
                dockingForm.Show(dockPanel1);
                return;
            }

            var form = menuItem.Tag as Form;
            if (form == null)
                return;

            form.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void NeuQuantForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

        private void NeuQuantForm_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                LoadNeuQuantFile(files[0]);
            }
        }

        private void processDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _nqFileGeneratorForm = new NeuQuantFileGeneratorForm();
            _nqFileGeneratorForm.Show();
        }

        private void manageLabelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _labelManagerForm.Show();
        }

        private void createTempToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Create();
        }

        private void AnalyzeFile()
        {
            Task t = Task.Factory.StartNew(() =>
            {
                Processor processor = new Processor(_currentNQFile, 3, 0.75, 0.75, 480000, checkIsotopicDistribution: true);
                processor.Progress += OnProgress;
                processor.Message += OnMessage;

                processor.Open();
                processor.GetPeptides();
                processor.FilterPeptides();
                processor.ExtractFeatureSets();
                processor.FindPeaks();
                processor.QuantifyPeaks();

                processor.Progress -= OnProgress;
                processor.Message -= OnMessage;
            });
        }

        private void goToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AnalyzeFile();
        }
   
    }
}
