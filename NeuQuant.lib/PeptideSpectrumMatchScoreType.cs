namespace NeuQuant
{
    /// <summary>
    /// The type of score assigned to the peptide spectrum match.
    /// The sign of the value represents which direction to compare scores
    /// (Positive = higher is better, Negative = lower is better)
    /// </summary>
    public enum PeptideSpectrumMatchScoreType
    {
        Unknown = 0,
        OmssaEValue = -1,
        XCorr = 1
    }
}
