using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using MathNet.Numerics.Distributions;

namespace KaplanMeierEstimator.Common
{
    /// <summary>
    /// Performs the Kaplan Meier algorithm over 2 groups, and computes the statistic significance for the groups behaving similarly over time.
    /// </summary>
    public class KaplanMeierEstimate
    {
        /// <summary>
        /// Group time event
        /// </summary>
        public class KaplanMeierStatus
        {
            public int Time { get; set; }

            public int NumberAtRisk { get; set; }

            public int NumberFailing { get; set; }

            public double SurvivalProbability { get; set; }
        }

        private class JoinedEvent
        {
            public int Time { get; set; }
            public int AtRiskA { get; set; }
            public int AtRiskB { get; set; }
            public int FailingA { get; set; }
            public int FailingB { get; set; }
        }

        private readonly IEnumerable<Patient> m_groupA;
        private readonly IEnumerable<Patient> m_groupB;

        private List<JoinedEvent> MergedEvents { get; set; } = new List<JoinedEvent>();

        public IReadOnlyList<KaplanMeierStatus> GroupAEvents { get; private set; }

        public IReadOnlyList<KaplanMeierStatus> GroupBEvents { get; private set; }


        public int TotalFailingA { get; private set; }

        public int TotalFailingB { get; private set; }

        public double PValue { get; private set; }
        
        public KaplanMeierEstimate(IEnumerable<Patient> groupA, IEnumerable<Patient> groupB)
        {
            if (groupA == null || groupB == null)
            {
                throw new ArgumentNullException(groupA == null ? "groupA" : "groupB");
            }

            m_groupA = groupA;
            m_groupB = groupB;
        }

        /// <summary>
        /// Performs the algorithm
        /// </summary>
        public void RunEstimate()
        {
            GroupAEvents = RunGroup(m_groupA);
            GroupBEvents = RunGroup(m_groupB);

            TotalFailingA = GroupAEvents.Sum(e => e.NumberFailing);
            TotalFailingB = GroupBEvents.Sum(e => e.NumberFailing);

            MergeEvents();
            ComputePValue();
        }

        /// <summary>
        /// Converts the given <paramref name="patients"/> to a list of KM events holding the failure and censoring events as well as computing the
        /// survival probability for each point in time
        /// </summary>
        /// <param name="patients">The patient collection to convert</param>
        /// <returns></returns>
        private static IReadOnlyList<KaplanMeierStatus> RunGroup(IEnumerable<Patient> patients)
        {
            List<KaplanMeierStatus> retVal = new List<KaplanMeierStatus>();
            int atRisk = patients.Count();
            double prevSurvivalProbability = 1.0;

            var orderedGroup = patients.GroupBy(x => x.CensorEventTime).OrderBy(p => p.Key);
            foreach (var patientGroup in orderedGroup)
            {
                int died = patientGroup.Count(patient => patient.CensorEvent == EventFreeSurvival.Death);
                double survivalProbability = prevSurvivalProbability * (1 - died / (double)(atRisk));

                retVal.Add(
                    new KaplanMeierStatus
                    {
                        Time = patientGroup.Key,
                        NumberAtRisk = atRisk,
                        NumberFailing = died,
                        SurvivalProbability = survivalProbability
                    });

                prevSurvivalProbability = survivalProbability;
                atRisk -= patientGroup.Count();
            }

            return retVal;
        }

        /// <summary>
        /// Joins the two groups events into a single collection to allow further processing
        /// </summary>
        private void MergeEvents()
        {
            int iA = 0, iB = 0;            
            while (iA < GroupAEvents.Count && iB < GroupBEvents.Count)
            {
                int currentTime = Math.Min(GroupAEvents[iA].Time, GroupBEvents[iB].Time);
                int failingA = (GroupAEvents[iA].Time == currentTime ? GroupAEvents[iA].NumberFailing : 0);
                int failingB = (GroupBEvents[iB].Time == currentTime ? GroupBEvents[iB].NumberFailing : 0);

                if (failingA + failingB != 0)
                {
                    MergedEvents.Add(new JoinedEvent
                    {
                        Time = currentTime,
                        AtRiskA = GroupAEvents[iA].NumberAtRisk,
                        AtRiskB = GroupBEvents[iB].NumberAtRisk,
                        FailingA = failingA,
                        FailingB = failingB
                    });
                }

                if (GroupAEvents[iA].Time == currentTime)
                    iA++;

                if (GroupBEvents[iB].Time == currentTime)
                    iB++;
            }

            int aAtRisk = GroupAEvents[GroupAEvents.Count - 1].NumberAtRisk - GroupAEvents[GroupAEvents.Count - 1].NumberFailing;
            int bAtRisk = GroupBEvents[GroupBEvents.Count - 1].NumberAtRisk - GroupBEvents[GroupBEvents.Count - 1].NumberFailing;

            while (iA < GroupAEvents.Count)
            {
                int currentTime = GroupAEvents[iA].Time;
                MergedEvents.Add(new JoinedEvent
                {
                    Time = currentTime,
                    AtRiskA = GroupAEvents[iA].NumberAtRisk,
                    AtRiskB = bAtRisk,
                    FailingA = GroupAEvents[iA].NumberFailing,
                    FailingB = 0
                });

                iA++;
            }

            while (iB < GroupBEvents.Count)
            {
                int currentTime = GroupBEvents[iB].Time;
                MergedEvents.Add(new JoinedEvent
                {
                    Time = currentTime,
                    AtRiskA = aAtRisk,
                    AtRiskB = GroupBEvents[iB].NumberAtRisk,
                    FailingA = 0,
                    FailingB = GroupBEvents[iB].NumberFailing
                });

                iB++;
            }
        }

        /// <summary>
        /// Computes the statistical significance of the observation
        /// </summary>
        private void ComputePValue()
        {
            double sumEA = 0, sumEB = 0;

            // Compute the expected number of failures for each group, should have they been taken together
            foreach (var kmEvent in MergedEvents.OrderBy(x=> x.Time))
            {
                double totalFailing = kmEvent.FailingA + kmEvent.FailingB;
                double totalAtRisk = kmEvent.AtRiskA + kmEvent.AtRiskB;

                double eA = (totalFailing / totalAtRisk) * kmEvent.AtRiskA;
                double eB = (totalFailing / totalAtRisk) * kmEvent.AtRiskB;

                sumEA += eA;
                sumEB += eB;
            }

            Debug.Assert(!(sumEA == 0) || sumEB == 0); // (sumEA == 0) ==> (sumEB == 0)

            double statistic = 0;
            if (sumEA != 0 && sumEB != 0)
            {
                // The test statistic is the deviation from the expected for both groups
                statistic = (Math.Pow(TotalFailingA - sumEA, 2) / sumEA) + (Math.Pow(TotalFailingB - sumEB, 2) / sumEB);
            }

            // The PValue is computed using the Chi-Square statistic, with degrees of freedom =1
            PValue = 1 - ChiSquared.CDF(1, statistic);
        }
    }
}
