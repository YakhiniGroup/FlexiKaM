using KaplanMeierEstimator.Common;
using KaplanMeierEstimator.IO;
using KaplanMeierEstimator.IO.GeneReader;
using KaplanMeierEstimator.IO.PatientReader;
using KaplanMeierEstimator.SplitStrategies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KaplanMeierEstimator.Console
{
    class Program
    {
        static string GeneFileName = @"PathologicalDatabaseGenes.csv";
        static string PatientFileName = @"PathologicalDatabasePatients.xlsx";
        static string PatientSheetName = @"Sheet1";
        static string Time = @"diff";
        static int Percentile = 0;

        static void SetParams()
        {
            string temp;
            System.Console.WriteLine("Insert gene expression file path (or enter \"example\" for running with the sample file):");
            temp = System.Console.ReadLine();
            if (temp != "example")
            {
                GeneFileName = temp;
            }

            System.Console.WriteLine("Insert survival file path (or enter \"example\" for running with the sample file):");
            temp = System.Console.ReadLine();
            if (temp != "example")
            {
                PatientFileName = temp;
            }

            System.Console.WriteLine("Would you like to examine the expression before treatment (enter \"before\"), after treatment (enter \"after\") or the differential expression (enter \"diff\")?");
            Time = System.Console.ReadLine();

            System.Console.WriteLine("By what percentile would you like to divide the patients to groups? Please insert a round number from 0 to 100.");
            Percentile = int.Parse(System.Console.ReadLine());
        }

        static void Main(string[] args)
        {
            System.Console.WriteLine("Welcome to the Kaplan-Meier estimator program!");
            SetParams();

            var patientReader = new ExcelPatientReader(Path.Combine(Directory.GetCurrentDirectory(), PatientFileName), PatientSheetName, hasHeader: true);

            patientReader.RegisterParser(@"Patientnummer", (value, patient) => patient.Id = int.Parse(value));
            patientReader.RegisterParser(@"EventFreeSurvival_event (metastasis or death)", (value, patient) => patient.CensorEvent = (EventFreeSurvival)Enum.Parse(typeof(EventFreeSurvival), value));
            patientReader.RegisterParser(@"reg_t_fu_months", (value, patient) => patient.CensorEventTime = (int)Math.Floor(double.Parse(value)));

            var geneReader = new CSVGeneReader(Path.Combine(Directory.GetCurrentDirectory(), GeneFileName));

            var patients = patientReader.ReadToEnd().ToDictionary(patient => patient.Id);
            var genes = geneReader.GetGenes().ToList();

            ISplitStrategy[] splitStrategies = null;
            if (Time == "before")
            {
                splitStrategies = new ISplitStrategy[] { new T0_TopNPercentSplitStrategy(Percentile, patients) };
            }

            if (Time == "after")
            {
                splitStrategies = new ISplitStrategy[] { new T2_TopNPercentSplitStrategy(Percentile, patients) };
            }

            if (Time == "diff")
            {
                splitStrategies = new ISplitStrategy[] { new T0T2Diff_TopNPercentSplitStrategy(Percentile, patients) };
            }

            string currentDir = Directory.GetCurrentDirectory();
            foreach (var strategy in splitStrategies)
            {
                if (Directory.Exists(Path.Combine(currentDir, strategy.Name)))
                    Directory.Delete(Path.Combine(currentDir, strategy.Name), true);

                DirectoryInfo targetDir = Directory.CreateDirectory(Path.Combine(currentDir, strategy.Name));
                if (!targetDir.Exists)
                    targetDir.Create();

                StrategyRunner runner = new StrategyRunner(strategy);
                var orderedResults = runner.Run(genes).ToArray();
                int resultsCount = orderedResults.Length;

                for (int index = 0; index < resultsCount; index++)
                {
                    var result = orderedResults[index];
                    result.FDR = (result.Estimate.PValue * resultsCount) / (index + 1);
                }

                File.WriteAllText(Path.Combine(targetDir.FullName, "Results.csv"), "Illumina ID, P-Value, -Log(P-Value), FDR, GroupSize" + Environment.NewLine);
                File.AppendAllLines(Path.Combine(targetDir.FullName, "Results.csv"), orderedResults.Select((result, index) => string.Format("{0}, {1:E}, {2}, {3}, {4}", result.GeneId, result.Estimate.PValue, -Math.Log10(result.Estimate.PValue), (result.Estimate.PValue * resultsCount) / (index + 1), result.GroupSize)));

                var logRankDir = targetDir.CreateSubdirectory("Results_LogRank");
                ExcelWritter excelOutput = new ExcelWritter(logRankDir, orderedResults, result => -Math.Log10(result.Estimate.PValue));
                excelOutput.WriteExcelFiles();

                orderedResults = orderedResults.OrderBy(result => result.FDR).ToArray();

                var fdrRankDir = targetDir.CreateSubdirectory("Results_FDR");
                excelOutput = new ExcelWritter(fdrRankDir, orderedResults, result => result.FDR);
                excelOutput.WriteExcelFiles();


                System.Console.WriteLine("Output is ready at {0}", targetDir.FullName);
                System.Console.ReadLine();
            }
        }

        static void Main2(string[] args)
        {
            var groupA = new List<Patient>
            {
                new Patient { CensorEventTime = 2, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 3, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 5, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 5, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 5, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 5, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 8, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 8, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 8, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 9, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 10, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 10, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 11, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 11, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 12, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 12, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 13, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 13, CensorEvent = EventFreeSurvival.Censored },
            };

            var groupB = new List<Patient>
            {
                new Patient { CensorEventTime = 1, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 3, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 4, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 6, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 6, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 6, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 8, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 8, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 9, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 10, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 10, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 12, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 12, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 13, CensorEvent = EventFreeSurvival.Death },
                new Patient { CensorEventTime = 13, CensorEvent = EventFreeSurvival.Censored },
                new Patient { CensorEventTime = 13, CensorEvent = EventFreeSurvival.Censored },
            };

            KaplanMeierEstimate estimate = new KaplanMeierEstimate(groupA, groupB);
            estimate.RunEstimate();


            CSVGeneReader reader = new CSVGeneReader(@"C:\Users\sashaa\Source\Workspaces\KaplanMeierEstimator\SampleFiles\PathologicalDatabaseGenes.csv");
            var x = reader.GetGenes();

        }
    }
}
