using KaplanMeierEstimator.SplitStrategies;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KaplanMeierEstimator.Common
{
    public class StrategyRunner
    {
        private readonly ISplitStrategy m_splitStrategy;

        private const int MinGroupSize = 1;

        public StrategyRunner(ISplitStrategy strategy)
        {
            m_splitStrategy = strategy;
        }

        public IOrderedEnumerable<GeneResult> Run(List<IEnumerable<GeneExpression>> genes)
        {
            ConcurrentBag<GeneResult> results = new ConcurrentBag<GeneResult>();

            Parallel.ForEach(genes, geneGroup =>
            {
                IEnumerable<Patient> groupA;
                IEnumerable<Patient> groupB;

                m_splitStrategy.DoSplit(geneGroup, out groupA, out groupB);

                if (groupA.Count() < MinGroupSize || groupB.Count() < MinGroupSize)
                    return;

                KaplanMeierEstimate kmEstimate = new KaplanMeierEstimate(groupA, groupB);
                kmEstimate.RunEstimate();

                results.Add(new GeneResult { GeneId = geneGroup.First().GeneId, Estimate = kmEstimate, GroupSize = Math.Min(groupA.Count(), groupB.Count()) });
            });

            var sortedResults = results.OrderBy(result => result.Estimate.PValue);
            return sortedResults;
        }
    }
}
