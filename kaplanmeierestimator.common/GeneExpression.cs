using System;

namespace KaplanMeierEstimator.Common
{
    /// <summary>
    ///  Holds the gene expression before and after a procedure, referring to a specific patient
    /// </summary>
    public class GeneExpression
    {
        public string GeneId { get; set; }

        public int PatientId { get; set; }

        public double Before { get; set; } = double.NaN;

        public double After { get; set; } = double.NaN;

        public double AbsoluteDifference
        {
            get
            {
                if (double.IsNaN(Before) || double.IsNaN(After))
                {
                    return double.NaN;
                }

                return Math.Abs(Before - After);
            }
        }
    }
}
