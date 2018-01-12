using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaplanMeierEstimator.Common
{
    public class GeneResult
    {
        public string GeneId { get; set; }

        public KaplanMeierEstimate Estimate { get; set; }

        public int GroupSize { get; set; }

        public double FDR { get; set; }
    }
}
