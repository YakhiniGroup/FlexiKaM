using System;
using System.Collections.Generic;
using System.Linq;
using KaplanMeierEstimator.Common;

namespace KaplanMeierEstimator.SplitStrategies
{
    public class T0_TopNPercentSplitStrategy : ISplitStrategy
    {
        private readonly int m_percent;
        private readonly IDictionary<int, Patient> m_patients;

        public T0_TopNPercentSplitStrategy(int percent, IDictionary<int, Patient> patients)
        {
            if (percent <= 0 || percent > 50)
            {
                throw new ArgumentOutOfRangeException("percent");
            }

            if (patients == null)
            {
                throw new ArgumentNullException("patients");
            }

            m_percent = percent;
            m_patients = patients;
        }

        public void DoSplit(IEnumerable<GeneExpression> genes, out IEnumerable<Patient> groupA, out IEnumerable<Patient> groupB)
        {
            var relevantGenes = genes.Where(gene => !double.IsNaN(gene.Before));
            relevantGenes = relevantGenes.Where(gene => m_patients.ContainsKey(gene.PatientId));
            var orderedGenes = relevantGenes.OrderBy(gene => gene.Before);

            int groupSize = (int)(orderedGenes.Count() * (m_percent / 100.0));

            var groupAGenes = orderedGenes.Take(groupSize);
            var groupBGenes = orderedGenes.Skip(orderedGenes.Count() - groupSize);

            groupA = groupAGenes.Select(gene => m_patients[gene.PatientId]).ToList();
            groupB = groupBGenes.Select(gene => m_patients[gene.PatientId]).ToList();
        }

        public string Name
        {
            get
            {
                return "T0_Top" + m_percent;
            }
        }
    }
}
