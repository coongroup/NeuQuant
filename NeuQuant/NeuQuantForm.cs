using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CSMSL.Analysis.ExperimentalDesign;
using CSMSL.IO;
using CSMSL.IO.Thermo;
using NeuQuant.Processing;
using WeifenLuo.WinFormsUI.Docking;
using NeuQuant.IO;
using ZedGraph;
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

        public static Color[] MasterColors = {Color.CornflowerBlue, Color.Sienna, Color.YellowGreen};

        public static readonly BindingList<NeuQuantModification> CurrentModifications = new BindingList<NeuQuantModification>();
        public static readonly BindingList<ExperimentalSet> CurrentExperiments = new BindingList<ExperimentalSet>();
        public static readonly SortableBindingList<NeuQuantPeptide> LoadedPeptides = new SortableBindingList<NeuQuantPeptide>();

        #endregion

        #region Child Forms

        private TextBoxForm _logForm;
        private GraphForm _msSpectrumForm;
        private GraphForm _xicForm;
        private GraphForm _spacingForm;
        private GraphForm _histrogramForm;
      
        private DGVForm _peptidesForm;
        private NeuQuantFileGeneratorForm _nqFileGeneratorForm;
        private QuantiativeLabelManagerForm _labelManagerForm;
        private TreeViewForm _analysesForm;
        private ProcessorForm _processorForm;

        #endregion

        #region Class Variables

        private NeuQuantFile _currentNQFile = null;
        private Processor _currentProcessor = null;
        private long _currentAnalysisID = -1;

        #endregion

        #region Constructor / Intialization

        public NeuQuantForm()
        {
            InitializeComponent();
            
            Reagents.ModificationsChanged += (sender, e) => RefreshModifications();
            Reagents.ExperimentsChanged += (sender, e) => RefreshExperiments();
            NeuQuantFile.OnProgess += OnProgress;
            NeuQuantFile.OnMessage += OnMessage;

            RefreshExperiments();
            RefreshModifications();
        }

        private void RefreshExperiments()
        {
            CurrentExperiments.RaiseListChangedEvents = false;
            CurrentExperiments.Clear();
            foreach (var experiment in Reagents.GetAllExperiments())
                CurrentExperiments.Add(experiment);
            CurrentExperiments.RaiseListChangedEvents = true;
            CurrentExperiments.ResetBindings();
        }

        private void RefreshModifications()
        {
            CurrentModifications.RaiseListChangedEvents = false;
            CurrentModifications.Clear();
            foreach (var mod in Reagents.GetAllModifications())
                CurrentModifications.Add(mod);
            CurrentModifications.RaiseListChangedEvents = true;
            CurrentModifications.ResetBindings();
        }
        
        protected override void OnLoad(EventArgs e)
        {
            Text = ProgramVersion;

            _logForm = new TextBoxForm();
            _logForm.Text = "Log";
            _logForm.TextBox.ReadOnly = true;
            _logForm.Show(dockPanel1, DockState.DockBottom);
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
            _msSpectrumForm.Hide();

            _spacingForm = new GraphForm();
            _spacingForm.Text = "Peak Spacing";
            _spacingForm.GraphControl.MouseMove += MoveVerticalLine;
            _spacingForm.GraphControl.MouseClick += ToggleCurveVisibility;
            _spacingForm.GraphControl.MouseClick += RetentionTimePlotClick;
            _spacingForm.GraphControl.PreviewKeyDown += GraphControl_PreviewKeyDown;
            _spacingForm.Show(dockPanel1, DockState.Document);
            RefreshGraph(_spacingForm.GraphControl);
            RegisterForm(_spacingForm);
            _spacingForm.Hide();

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
            RefreshGraph(_xicForm.GraphControl);
            RegisterForm(_xicForm);
            _xicForm.Hide();

            _histrogramForm = new GraphForm();
            _histrogramForm.Text = "Histrogram";
            _histrogramForm.GraphControl.MouseMove += MoveVerticalLine;
            _histrogramForm.Show(dockPanel1, DockState.Document);
            RefreshGraph(_histrogramForm.GraphControl);
            RegisterForm(_histrogramForm);
            _histrogramForm.Hide();
            
            _nqFileGeneratorForm = new NeuQuantFileGeneratorForm(this);
            //_nqFileGeneratorForm.Show();
            //_nqFileGeneratorForm.Show(dockPanel1, DockState.Document);
            //RegisterForm(_nqFileGeneratorForm);

            _labelManagerForm = new QuantiativeLabelManagerForm();
            _labelManagerForm.DockPanel = dockPanel1;
            RegisterForm(_labelManagerForm);
            
            _analysesForm = new TreeViewForm("Analyses");
            _analysesForm.Show(dockPanel1, DockState.DockRight);
            _analysesForm.TreeView.NodeMouseClick += TreeView_NodeMouseClick;
            RegisterForm(_analysesForm);
            _analysesForm.Hide();
          
            _processorForm = new ProcessorForm();
            _processorForm.Show(dockPanel1, DockState.DockRight);
            _processorForm.Analyze += _processorForm_Analyze;
            RegisterForm(_processorForm);
            _processorForm.Hide();

            _peptidesForm = new DGVForm("Peptides");
            _peptidesForm.Show(dockPanel1, DockState.DockRight);
            _peptidesForm.DataGridView.RowEnter += PeptideRowEnter;
            _peptidesForm.DataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.LightBlue;
            
            _peptidesForm.DataGridView.AutoGenerateColumns = false;
            _peptidesForm.DataGridView.AllowUserToAddRows = false;
            _peptidesForm.DataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _peptidesForm.DataGridView.DataBindingComplete += DataGridView_DataBindingComplete;
            
            DataGridViewTextBoxColumn sequenceColumn = new DataGridViewTextBoxColumn();
            sequenceColumn.DataPropertyName = "Sequence";
            sequenceColumn.HeaderText = "Sequence";
            _peptidesForm.DataGridView.Columns.Add(sequenceColumn);

            DataGridViewTextBoxColumn psmColumn = new DataGridViewTextBoxColumn();
            psmColumn.DataPropertyName = "NumberOfPeptideSpectrumMatches";
            psmColumn.HeaderText = "# PSMs";
            _peptidesForm.DataGridView.Columns.Add(psmColumn);

            DataGridViewTextBoxColumn channelsColumn = new DataGridViewTextBoxColumn();
            channelsColumn.DataPropertyName = "NumberOfChannels";
            channelsColumn.HeaderText = "# Quantitative Channels";
            _peptidesForm.DataGridView.Columns.Add(channelsColumn);
            
            _peptidesForm.DataGridView.DataSource = LoadedPeptides;
 
            RegisterForm(_peptidesForm);
            _peptidesForm.Hide();

            // Link this two forms together so their events are reflected in both
            _spacingForm.LinkForms(_xicForm);

            LogMessage(" == " + ProgramVersion + " == ", false);
            

            base.OnLoad(e);
        }

        void DataGridView_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            var dgv = sender as DataGridView;
            if (dgv == null)
                return;

            var dgvForm = dgv.Parent as DGVForm;
            if (dgvForm == null)
                return;

            int rows = dgv.Rows.Count;

            dgvForm.AppendTitle("(n=" + rows + ")");
        }
       
        private void _processorForm_Analyze(object sender, EventArgs e)
        {
            ProcessorForm processorForm = sender as ProcessorForm;
            if (processorForm == null)
                return;

            if (_currentNQFile == null)
            {
                LogMessage("No Data is loaded to analyze!");
                return;
            }

            _currentProcessor = processorForm.GetProcessor(_currentNQFile);

            AnalyzeFile();
        }

        #endregion

        #region File Loading

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
                _currentProcessor = null;
                SetStatusText("Loading File...");
                LogMessage("Attempting to load NeuQuant File " + file.FilePath);

                if (_currentNQFile != null)
                {
                    LogMessage("Saving old NeuQuant File " + _currentNQFile.FilePath + " first...");

                    _currentNQFile.Dispose();
                   
                    ClearGraph(_xicForm.GraphControl);
                    ClearGraph(_spacingForm.GraphControl);
                    ClearGraph(_histrogramForm.GraphControl);
                    ClearGraph(_msSpectrumForm.GraphControl);
                  
                    //TODO add stuff to save and close the old file
                }
               

                _currentNQFile = file;              
                _currentNQFile.Open();

                if (!_currentNQFile.TryGetLastProcessor(out _currentProcessor))
                {
                    _currentProcessor = _processorForm.GetProcessor(_currentNQFile);
                }

                _currentProcessor.Open();

                LogMessage("Loaded NeuQuant File " + _currentNQFile.FilePath);
               
            }).ContinueWith((t2) =>
            {
                if (_currentNQFile == null)
                    return;
               
                Text = ProgramVersion + " - " + _currentNQFile.FilePath;
                _processorForm.SetProcessor(_currentProcessor);
                LoadAnalyses(_currentNQFile, _analysesForm, true);
                LoadPeptides(_currentNQFile);
                _peptidesForm.Show();
                _analysesForm.Show();
                _processorForm.Show();
                _msSpectrumForm.Show();
                _spacingForm.Show();
                _xicForm.Show();
                _peptidesForm.Show();
                SetStatusText("Ready");
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        
        #endregion

        #region Feedback

        void OnMessage(object sender, MessageEventArgs e)
        {
            LogMessage(e.Message, true);
        }

        void OnProgress(object sender, ProgressEventArgs e)
        {
            SetProgress(e.Percent);
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

            if (percent < 0)
            {
                progressBar.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                progressBar.Style = ProgressBarStyle.Continuous;
                progressBar.Value = (int) (percent*progressBar.Maximum);
            }
        }

        public void LogMessage(string message, bool timeStamped = true)
        {
            if (_logForm != null)
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
                message = string.Format("[{0}]\t{1}", DateTime.Now.ToLongTimeString(), message);
            }

            richTextBox.AppendText(message);
            richTextBox.AppendText("\r\n");
            richTextBox.ScrollToCaret();
        }

        #endregion
        
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

        private void DisplayResults(NeuQuantFile nqFile, long analysisID, ZedGraphControl control)
        {
            if (nqFile == null || control == null)
                return;

            ClearGraph(control);

            var quants = nqFile.GetQuantitation(analysisID).ToList();
            int colorI = 0;
            List<NeuQuantSample> samples = nqFile.GetSamples().ToList();
            for (int i = 0; i < samples.Count - 1; i++)
            {
                NeuQuantSample sample1 = samples[i];
                for (int j = i + 1; j < samples.Count; j++)
                {
                    NeuQuantSample sample2 = samples[j];

                    var color = MasterColors[colorI++];

                    List<double> log2ratios = new List<double>();
                    foreach (var quant in quants)
                    {
                        double quant1 = quant.Item2[sample1];
                        double quant2 = quant.Item2[sample2];

                        double ratio = quant1/quant2;
                        double log2ratio = Math.Log(ratio, 2);

                        // Prevent oddities
                        if (double.IsNaN(log2ratio) || double.IsInfinity(log2ratio))
                            continue;

                        log2ratios.Add(log2ratio);
                    }

                    int count = log2ratios.Count;
                    if (count == 0)
                        return;

                    double mean = log2ratios.Average();
                    double median = log2ratios.Median();
                    double stdev = log2ratios.StdDev();

                    if (stdev == 0)
                    {
                        // Something is wrong,, break out
                        LogMessage("Error getting results!");
                        SetStatusText("Error");
                        return;
                    }

                    PointPairList points = new PointPairList();
                    int numberOfBins = 100;
                    double min, max, stepSize;
                    int[] bins = log2ratios.Histogram(numberOfBins, out min, out max, out stepSize);
                    for (int k = 0; k < numberOfBins; k++)
                    {
                        points.Add(min + k*stepSize, bins[k]);
                    }

                    LineItem line = control.GraphPane.AddCurve(string.Format("({0}/{1}) N={2} u={3:f3} m={4:f3} stdev={5:f3}", sample1.Name, sample2, count, Math.Pow(2, mean), Math.Pow(2, median),  stdev), points, color, SymbolType.None);
                    line.Line.Width = 2.5f;
                }
            }

            control.AxisChange();

            control.GraphPane.YAxis.Title.Text = "Number of Peptides";
            control.GraphPane.XAxis.Title.Text = string.Format("Log2(Ratio)");

           
            RefreshGraph(control);
        }

        private void PlotSpacing(ZedGraphControl control, NeuQuantFeatureSet featureSet, double deltaRT = 0.75)
        {
            if (_currentNQFile == null || featureSet == null || _currentProcessor == null)
                return;

            if (!featureSet.Peptide.ContainsQuantitativeChannel)
                return;

            if (featureSet.Spectra.Count == 0)
                return;

            double minRT = featureSet.MinimumRetentionTime;
            double maxRT = featureSet.MaximumRetentionTime;
            int charge = featureSet.ChargeState;
         
            ClearGraph(control);
            
            var isotopologues = featureSet.Peptide.QuantifiableChannels.Values;
            control.GraphPane.Title.Text = "Spacing for "+featureSet.Peptide;
            
            int colorI = 0;
            double[] expectedSpacings = Isotopologue.GetExpectedSpacings(isotopologues, charge);

            SymbolType[] symbols = {SymbolType.Circle, SymbolType.Square, SymbolType.Triangle};

            double minSpacing = expectedSpacings.Min();

            bool useMDa = minSpacing < 1;
            double factor = useMDa ? 1000 : 1;

            double biggestSpacing = 0;

            double minTolerance = 0.4;
            double maxTolerance = 1.15;

            int index = 0;
            double totalSpacing = 0;
            foreach (double spacings in expectedSpacings)
            {
                var color = MasterColors[colorI++];
                totalSpacing += spacings;
                for (int isotope = 0; isotope < 1; isotope++)
                {
                    double[] spacingsArray = new double[featureSet.NumberOfFeatures];
                    double[] times = new double[featureSet.NumberOfFeatures];
                    int i = 0;
                    foreach (var feature in featureSet)
                    {
                        double spacing = 0;
                        Peptide iso1 = isotopologues[0];
                        Peptide iso2 = isotopologues[index + 1];

                        var peak1 = feature.GetChannelpeak(iso1, isotope);
                        var peak2 = feature.GetChannelpeak(iso2, isotope);
                        
                        if(peak1 != null && peak2 != null)
                            spacing = peak2.X - peak1.X;

                        times[i] = feature.RetentionTime;
                        spacingsArray[i] = spacing * factor;
                        if (spacingsArray[i] > biggestSpacing)
                        {
                            biggestSpacing = spacingsArray[i];
                        }
                        i++;
                    }
                    var pointPairList = new PointPairList(times, spacingsArray);
                    LineItem item = control.GraphPane.AddCurve("Isotope " + isotope, pointPairList, color, symbols[isotope]);
                    item.Line.IsVisible = false;
                    item.Symbol.Size = 10f;
                    item.Symbol.Border.Width = 2f;

                    double expectedSpacing = totalSpacing*factor;

                    LineObj expectedLine = new LineObj(color, 0, expectedSpacing, 1, expectedSpacing);
                    expectedLine.Location.CoordinateFrame = CoordType.XChartFractionYScale;
                    expectedLine.Line.Width = 1.5F;
                    expectedLine.Line.Style = DashStyle.Dot;
                    control.GraphPane.GraphObjList.Add(expectedLine);

                    BoxObj boxObj = new BoxObj(0, expectedSpacing * maxTolerance, 1, expectedSpacing * minTolerance, Color.Empty, Color.FromArgb(50, color));
                    boxObj.Location.CoordinateFrame = CoordType.XChartFractionYScale;
                    boxObj.ZOrder = ZOrder.F_BehindGrid;
                    boxObj.IsClippedToChartRect = true;
                    control.GraphPane.GraphObjList.Add(boxObj);
                }

                index++;
            }

            control.GraphPane.XAxis.Title.Text = "Retention Time (min)";
            control.GraphPane.YAxis.Title.Text = "Peak Spacing (" + (useMDa ? "mDa)" : "Da)");

            biggestSpacing = Math.Max(biggestSpacing, totalSpacing * factor * maxTolerance);

            control.GraphPane.YAxis.Scale.Min = -2;
            control.GraphPane.YAxis.Scale.Max = biggestSpacing * 1.1;
            
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

            if (featureSet.Spectra.Count == 0)
                return;
            
            double minRT = featureSet.MinimumRetentionTime;
            double maxRT = featureSet.MaximumRetentionTime;
            int charge = featureSet.ChargeState;
            double bestRT = featureSet.BestPSM.RetentionTime;

            ClearGraph(control);
            

            var isotopologues = featureSet.Peptide.QuantifiableChannels.Values;
            control.GraphPane.Title.Text = "";
            int colorI = 0;
            foreach (var isotopologue in isotopologues)
            {
                double totalTIC = 0;
                Color color = MasterColors[colorI++];
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
  
            LineItem item = control.GraphPane.AddStick("Spectrum Number: " + spectrum.ScanNumber, pointPairList, Color.Black);
            item.Symbol.IsVisible = false;
            var isotopologues = feature.Peptide.QuantifiableChannels.Values;
            List<Color> colors = new List<Color>();
            colors.Add(Color.Black);
            int colorI = 0;
            foreach (var iso in isotopologues)
            {
                colors.Add(MasterColors[colorI++]);
            }
          
            item.Line.GradientFill = new Fill(colors.ToArray());
            item.Line.GradientFill.Type = FillType.GradientByZ;
            item.Line.GradientFill.RangeMax = colors.Count - 1;
            item.Line.Width = 2F;
            int c = 0;
           
            foreach (var isotopologue in isotopologues)
            {
                c++;
                string message = string.Join(",", isotopologue.GetUniqueModifications<CSMSL.Proteomics.Modification>());
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
        
        #region Graph Handlers
        
        private void ToggleCurveVisibility(object sender, MouseEventArgs e)
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

        private void RetentionTimePlotClick(object sender, MouseEventArgs e)
        {
            ZedGraphControl graphControl = sender as ZedGraphControl;
            if (graphControl == null)
                return;

            GraphForm parentForm = graphControl.ParentForm as GraphForm;
            if (parentForm == null)
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

            _msSpectrumForm.GraphControl.GraphPane.XAxis.Scale.Min = spectrum.FirstMz - 0.25;
            _msSpectrumForm.GraphControl.GraphPane.XAxis.Scale.Max = spectrum.LastMZ + 0.25;
            _msSpectrumForm.GraphControl.GraphPane.YAxis.Scale.MaxAuto = true;
            RefreshGraph(_msSpectrumForm.GraphControl, true);

            foreach (var graph in parentForm.GraphControls)
            {
                // Clear the old one if it is there            
                int i = graph.GraphPane.GraphObjList.IndexOfTag("ms1Line");
                if (i >= 0)
                    graph.GraphPane.GraphObjList.RemoveAt(i);

                LineObj verticalLine = new LineObj(spectrum.RetentionTime, 0, spectrum.RetentionTime, 1);
                verticalLine.Line.Color = Color.Green;
                verticalLine.Location.CoordinateFrame = CoordType.XScaleYChartFraction;
                verticalLine.ZOrder = ZOrder.D_BehindAxis;
                verticalLine.Tag = "ms1Line";
                graph.GraphPane.GraphObjList.Add(verticalLine);

                graph.Refresh();
            }
        }

        private void MoveVerticalLine(object sender, MouseEventArgs e)
        {
            const string tagName = "verticalLine";
            ZedGraphControl graphControl = sender as ZedGraphControl;
            if (graphControl == null)
                return;

            GraphForm parentForm = graphControl.ParentForm as GraphForm;
            if (parentForm == null)
                return;
            
            double x, y;
            graphControl.GraphPane.ReverseTransform(e.Location, out x, out y);

            if (x < graphControl.GraphPane.XAxis.Scale.Min ||
                x > graphControl.GraphPane.XAxis.Scale.Max ||
                y < graphControl.GraphPane.YAxis.Scale.Min ||
                y > graphControl.GraphPane.YAxis.Scale.Max
                )
            {
                // hide the old line if it is there       
                foreach (var graph in parentForm.GraphControls)
                {
                    int i = graph.GraphPane.GraphObjList.IndexOfTag(tagName);
                    if (i >= 0)
                    {
                        graph.GraphPane.GraphObjList[i].IsVisible = false;
                        graph.Refresh();
                    }
                }
            }
            else
            {
                foreach (var graph in parentForm.GraphControls)
                {

                    int i = graph.GraphPane.GraphObjList.IndexOfTag(tagName);
                    if (i >= 0)
                    {
                        graph.GraphPane.GraphObjList[i].IsVisible = true;
                        graph.GraphPane.GraphObjList[i].Location.X = x;
                    }
                    else
                    {
                        LineObj verticalLine = new LineObj(x, 0, x, 1);
                        verticalLine.Line.Color = Color.DarkCyan;
                        verticalLine.Location.CoordinateFrame = CoordType.XScaleYChartFraction;
                        verticalLine.ZOrder = ZOrder.D_BehindAxis;
                        verticalLine.Tag = tagName;

                        graph.GraphPane.GraphObjList.Add(verticalLine);
                    }

                    graph.Refresh();
                }
            }
        }

        private void GraphControl_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
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

        private void PeptideRowEnter(object sender, DataGridViewCellEventArgs e)
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

            if (_currentProcessor == null)
                return;

            try
            {
                List<NeuQuantFeatureSet> featureSets = _currentProcessor.ExtractFeatureSets(peptide).ToList();

                NeuQuantFeatureSet featureSet = featureSets[0];

                _currentProcessor.FindPeaks(featureSet);
               
                PlotXIC(_xicForm.GraphControl, featureSet);
                PlotSpacing(_spacingForm.GraphControl, featureSet);
            }
            catch (Exception e1)
            {
                LogMessage("ERROR! But safe to ignore");
                LogMessage(e1.Message);
            }
        }
        
        private void LoadAnalyses(NeuQuantFile nqFile, TreeViewForm treeForm, bool clear = false)
        {
            if (nqFile == null)
                return;

            List<NeuQuantAnalysis> analyses = nqFile.GetAnalyses().ToList();

            if (clear)
                treeForm.TreeView.Nodes.Clear();

            foreach (NeuQuantAnalysis analysis in analyses)
            {
                TreeNode node = treeForm.TreeView.Nodes[analysis.Name];
                if(node == null)
                    node = treeForm.TreeView.Nodes.Add(analysis.Name, analysis.Name);

                foreach (KeyValuePair<DateTime, long> kvp in analysis.Analyses)
                {
                    TreeNode leafNode = node.Nodes[kvp.Key.ToString()];
                    if (leafNode == null)
                        leafNode = node.Nodes.Add(kvp.Key.ToString(), kvp.Key.ToLocalTime().ToLongTimeString());
                    leafNode.Tag = kvp.Value;
                }
            }
        }
        
        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeView tree = sender as TreeView;
            if (tree == null)
                return;

            TreeNode node = e.Node;
            if (node.Tag is long)
            {
                _currentAnalysisID = (long)node.Tag;
                _currentProcessor = _currentNQFile.GetProcessor(_currentAnalysisID);
                _processorForm.SetProcessor(_currentProcessor);
                DisplayResults(_currentNQFile, _currentAnalysisID, _histrogramForm.GraphControl);
                _histrogramForm.Show();
            }
        }

        private void LoadPeptides(NeuQuantFile nqFile)
        {
            LoadedPeptides.Clear();
            LoadedPeptides.RaiseListChangedEvents = false;
            foreach (var peptide in nqFile.GetPeptides().Where(p => p.ContainsQuantitativeChannel))
            {
                LoadedPeptides.Add(peptide);
            }
            LoadedPeptides.RaiseListChangedEvents = true;
            LoadedPeptides.ResetBindings();
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
            _nqFileGeneratorForm = new NeuQuantFileGeneratorForm(this);
            _nqFileGeneratorForm.Show();
        }

        private void manageLabelsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _labelManagerForm.Show();
        }
        
        private void AnalyzeFile()
        {
            Task t = Task.Factory.StartNew(() =>
            {
                Processor processor = _currentProcessor;
                processor.Progress += OnProgress;
                processor.Message += OnMessage;

                processor.Open();
                processor.SaveAnalysis();
                processor.GetPeptides();
                processor.FilterPeptides();
                processor.ExtractFeatureSets();
                processor.CalculateSystematicError();
                processor.FindPeaks();
                processor.QuantifyPeptides();

                processor.Progress -= OnProgress;
                processor.Message -= OnMessage;
            }).ContinueWith((t2) => LoadAnalyses(_currentNQFile, _analysesForm), TaskScheduler.FromCurrentSynchronizationContext());
        }
        
        private void restoreModsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Reagents.RestoreDefaults();
        }

        public void ShowLabelManager()
        {
            _labelManagerForm.Show();
        }

        private void loadFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                LoadNeuQuantFile(openFileDialog1.FileName);
            }
        }

        private void peptideQuantitationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_currentAnalysisID < 0 || _currentProcessor == null)
                return;

            if (saveFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            string filePath = saveFileDialog1.FileName;

            LogMessage("Exporting data to " + filePath);

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                List<NeuQuantSample> samples = _currentProcessor.NqFile.GetSamples().ToList();
                writer.Write("Peptide");
                foreach (var sample in samples)
                {
                    writer.Write(',');
                    writer.Write(sample.Name);
                }
                writer.WriteLine();

                var quants = _currentProcessor.NqFile.GetQuantitation(_currentAnalysisID).ToList();

                foreach (var quant in quants)
                {
                    string sequence = quant.Item1;
                    Dictionary<NeuQuantSample, double> data = quant.Item2;
                    writer.Write(sequence);
                    foreach (var sample in samples)
                    {
                        writer.Write(',');
                        double value = 0;
                        data.TryGetValue(sample, out value);
                        writer.Write(value);
                    }
                    writer.WriteLine();
                }
            }
        }

        private void File_DropDownOpening(object sender, EventArgs e)
        {
            exportDataToolStripMenuItem.Enabled = (_currentAnalysisID >= 0);
        }
   
    }
}
