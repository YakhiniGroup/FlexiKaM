using System.Collections.Generic;
using KaplanMeierEstimator.Common;

namespace KaplanMeierEstimator.SplitStrategies
{
    public interface ISplitStrategy
    {
        string Name { get; }

        void DoSplit(IEnumerable<GeneExpression> genes, out IEnumerable<Patient> groupA, out IEnumerable<Patient> groupB);
    }
}
