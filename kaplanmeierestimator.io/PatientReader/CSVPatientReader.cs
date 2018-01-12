using KaplanMeierEstimator.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KaplanMeierEstimator.IO.PatientReader
{
    public class CSVPatientReader : PatientReader
    {
        private readonly IEnumerable<string> _data;

        public CSVPatientReader(string csvFileName, bool hasHeader)
        {
            _hasHeader = hasHeader;

            _data = File.ReadLines(csvFileName);

            if (_hasHeader)
            {
                _data.First().Split(',').Select(x => x.Trim()).Select((column, index) => _indexMapping[column] = index);
                _data = _data.Skip(1);
            }
        }

        public override IEnumerable<Patient> ReadToEnd()
        {
            foreach(string line in _data)
            {
                var split = line.Split(',').Select(x => x.Trim()).ToArray();
                var patient = new Patient();

                foreach (int index in m_indices)
                {
                    _parseActions[index](split[index], patient);
                }

                yield return patient;
            }
        }
    }
}
