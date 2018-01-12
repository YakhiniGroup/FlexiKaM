using KaplanMeierEstimator.Common;
using System;
using System.Collections.Generic;
using System.Linq;

using Excel = Microsoft.Office.Interop.Excel;

namespace KaplanMeierEstimator.IO.PatientReader
{
    public class ExcelPatientReader : PatientReader
    {
        private Excel.Application m_excel = new Excel.Application();
        private Excel.Workbook m_workbook;
        private Excel.Worksheet m_worksheet;
        private Excel.Range m_usedRange;
        private object[,] m_worksheetValues;

        public ExcelPatientReader(string excelFileName, string worksheetName, bool hasHeader)
        {
            _hasHeader = hasHeader;

            m_workbook = m_excel.Workbooks.Open(excelFileName);
            m_worksheet = m_workbook.Sheets[worksheetName] as Excel.Worksheet;

            m_usedRange = m_worksheet.UsedRange;

            m_worksheetValues = (object[,])m_usedRange.get_Value(Excel.XlRangeValueDataType.xlRangeValueDefault);

            if (_hasHeader)
            {
                Enumerable.Range(1, m_usedRange.Columns.Count).ToList().ForEach(
                    column => _indexMapping[m_worksheetValues[1, column].ToString()] = column
                );
            }
        }

        ~ExcelPatientReader()
        {
            try
            {
                m_workbook.Close(true, System.Reflection.Missing.Value, System.Reflection.Missing.Value);

                System.Runtime.InteropServices.Marshal.ReleaseComObject(m_worksheet);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(m_workbook);
            }
            catch (Exception)
            {
            }

            m_excel.Quit();
        }
        
        public override IEnumerable<Patient> ReadToEnd()
        {
            for (int row = (_hasHeader ? 2 : 1); row <= m_usedRange.Rows.Count; ++row)
            {
                var patient = new Patient();

                foreach (int index in m_indices)
                {
                    _parseActions[index](m_worksheetValues[row, index].ToString(), patient);
                }

                yield return patient;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
