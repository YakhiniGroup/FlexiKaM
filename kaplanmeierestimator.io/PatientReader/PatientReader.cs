using KaplanMeierEstimator.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KaplanMeierEstimator.IO.PatientReader
{
    public abstract class PatientReader
    {
        // Holds a mapping between the property name (in the header) to the index of the property
        protected IDictionary<string, int> _indexMapping = new Dictionary<string, int>();

        // Holds a mapping between an property index to the action to perform
        protected IDictionary<int, Action<string, Patient>> _parseActions = new Dictionary<int, Action<string, Patient>>();

        // Collection of relevant (active) indexes
        protected ICollection<int> m_indices = new HashSet<int>();

        protected bool _hasHeader;

        public void RegisterParser(string property, Action<string, Patient> parser)
        {
            int index;
            if (!_indexMapping.TryGetValue(property, out index))
            {
                throw new InvalidOperationException("No such property '{property}'");
            }

            RegisterParser(index, parser);
        }

        public void RegisterParser(int index, Action<string, Patient> parser)
        {
            if (m_indices.Contains(index))
            {
                throw new InvalidOperationException("Index '{index}' is added more than once");
            }

            m_indices.Add(index);
            _parseActions.Add(index, parser);
        }

        public abstract IEnumerable<Patient> ReadToEnd();
    }
}
