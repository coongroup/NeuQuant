using CSMSL.IO.Thermo;
using CSMSL.Spectral;
using CSMSL;

namespace NeuQuant
{
    public class NeuQuantSpectrum : Spectrum, ISpectrum, ISpectrumTime
    {
        double ISpectrumTime.Time
        {
            get { return RetentionTime; }
        }

        public int ScanNumber { get;set;}
        public int ParentScanNumber { get; set; }
        public double RetentionTime {get;set;}
        public int MsnOrder {get;set;}
        public double Resolution {get;set;}
        public double InjectionTime {get;set;}
        public ThermoRawFile RawFile {get;set;}        

        public NeuQuantSpectrum(double[] mz, double[] intensities)
            : base(mz, intensities) { }

        public NeuQuantSpectrum(Spectrum spectrum)
            : base(spectrum) { }

        public NeuQuantSpectrum(byte[] bytes)
            : base(NeuQuantSpectrum.ConvertBytesToSpectrum(bytes, bytes.IsCompressed()))
        {
          
        }

        public static NeuQuantSpectrum Load(ThermoRawFile rawFile, int scannumber)
        {          
            var analyzer = rawFile.GetMzAnalyzer(scannumber);
            Spectrum spectrum = null;      

            if (analyzer == MZAnalyzerType.Orbitrap || analyzer == MZAnalyzerType.FTICR)
            {
                spectrum = rawFile.GetSNSpectrum(scannumber, 0);              
            }
            else
            {
                spectrum = rawFile.GetReadOnlyMZSpectrum(scannumber);
            }
            NeuQuantSpectrum nqSpectrum = new NeuQuantSpectrum(spectrum);
            nqSpectrum.RawFile = rawFile;
            nqSpectrum.ScanNumber = scannumber;
            nqSpectrum.Resolution = rawFile.GetResolution(scannumber);
            nqSpectrum.MsnOrder = rawFile.GetMsnOrder(scannumber);
            nqSpectrum.InjectionTime = rawFile.GetInjectionTime(scannumber);
            nqSpectrum.RetentionTime = rawFile.GetRetentionTime(scannumber);
            nqSpectrum.ParentScanNumber = rawFile.GetParentSpectrumNumber(scannumber);

            return nqSpectrum;
        }

        public new NeuQuantSpectrum Filter(double minIntensity, double maxIntensity = double.MaxValue)
        {
            var spectrum = base.Filter(minIntensity, maxIntensity);
            if (spectrum == null)
                return null;

            NeuQuantSpectrum nqSpectrum = new NeuQuantSpectrum(spectrum);
            nqSpectrum.RawFile = RawFile;
            nqSpectrum.ScanNumber = ScanNumber;
            nqSpectrum.Resolution = Resolution;
            nqSpectrum.MsnOrder = MsnOrder;
            nqSpectrum.InjectionTime = InjectionTime;
            nqSpectrum.RetentionTime = RetentionTime;
            nqSpectrum.ParentScanNumber = ParentScanNumber;
            return nqSpectrum;
        }

        public new NeuQuantSpectrum Extract(IRange<double> range, double systematicError = 0.0)
        {
            var spectrum = base.Extract(range);
            if (spectrum == null)
                return null;
            NeuQuantSpectrum nqSpectrum;

            if (systematicError != 0.0)
            {
                double[] masses = spectrum.GetMasses();
                for (int i = 0; i < masses.Length; i++)
                {
                    masses[i] -= systematicError;
                }
                nqSpectrum = new NeuQuantSpectrum(masses, spectrum.GetIntensities());
            }
            else
            {
                nqSpectrum = new NeuQuantSpectrum(spectrum);
            }

            nqSpectrum.RawFile = RawFile;
            nqSpectrum.ScanNumber = ScanNumber;
            nqSpectrum.Resolution = Resolution;
            nqSpectrum.MsnOrder = MsnOrder;
            nqSpectrum.InjectionTime = InjectionTime;
            nqSpectrum.RetentionTime = RetentionTime;
            nqSpectrum.ParentScanNumber = ParentScanNumber;
            return nqSpectrum;
        }
    }
}
