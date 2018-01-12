using KaplanMeierEstimator.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace KaplanMeierEstimator.IO.GeneReader
{
    public class CSVGeneReader
    {
        private class PatientMapping
        { 
            public int? BeforeColumn { get; set; }

            public int? AfterColumn { get; set; }
        }


        private readonly Dictionary<int, PatientMapping> m_patientMappings = new Dictionary<int, PatientMapping>();
        private readonly IEnumerable<string> _data;

        public CSVGeneReader(string fileName)
        {
            _data = File.ReadAllLines(fileName);

            string[] header = _data.First().Split(',');
            _data = _data.Skip(1);

            for (int column = 1; column < header.Length; ++column)
            {
                var columnParts = header[column].Split('_');
                Debug.Assert(columnParts.Length == 2);

                int patient = int.Parse(columnParts[0]);

                PatientMapping mapping;
                if (!m_patientMappings.TryGetValue(patient, out mapping))
                {
                    mapping = new PatientMapping();
                    m_patientMappings.Add(patient, mapping);
                }

                switch (columnParts[1])
                {
                    case "Before":
                        mapping.BeforeColumn = column;
                        break;

                    case "After":
                        mapping.AfterColumn = column;
                        break;

                    default:
                        throw new InvalidDataException("Unexpected column in the genes file");
                }
            }
        }

        public IEnumerable<IEnumerable<GeneExpression>> GetGenes()
        {
            return ReadToEnd().GroupBy(g => g.GeneId, (key, group) => group);
        }

        private IEnumerable<GeneExpression> ReadToEnd()
        {
            foreach (var line in _data)
            {
                var geneData = line.Split(',');
                var geneId = geneData[0];
                
                foreach (var geneMapping in m_patientMappings)
                {
                    var gene = new GeneExpression { GeneId= string.Intern(geneId), PatientId = geneMapping.Key };

                    double value;

                    if (geneMapping.Value.BeforeColumn.HasValue && double.TryParse(geneData[geneMapping.Value.BeforeColumn.Value], out value))
                        gene.Before = value;

                    if (geneMapping.Value.AfterColumn.HasValue && double.TryParse(geneData[geneMapping.Value.AfterColumn.Value], out value))
                        gene.After = value;


                    if (!double.IsNaN(gene.Before) || !double.IsNaN(gene.After))
                        yield return gene;
                }
            }
        }
    }
}
