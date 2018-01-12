using KaplanMeierEstimator.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Excel = Microsoft.Office.Interop.Excel;

namespace KaplanMeierEstimator.IO
{
    public class ExcelWritter
    {
        const int TopOutput = 10;

        private readonly DirectoryInfo m_targetDir;
        private readonly IEnumerable<GeneResult> m_results;
        private readonly Func<GeneResult, double> m_measureSelector;

        private readonly Excel.Application m_excel = new Excel.Application();

        public ExcelWritter(DirectoryInfo targetDir, IEnumerable<GeneResult> results, Func<GeneResult, double> measureSelector)
        {
            m_targetDir = targetDir;
            m_results = results;
            m_measureSelector = measureSelector;
        }

        public void WriteExcelFiles()
        {
            object misValue = System.Reflection.Missing.Value;
            string numericFormat = "{0:D" + Math.Ceiling(Math.Log10(m_results.Count())) + "}";

            int i = 0;
            foreach (var result in m_results)
            {
                Excel.Workbook workbook = m_excel.Workbooks.Add(misValue);
                Excel.Worksheet worksheet = (Excel.Worksheet)workbook.Worksheets.get_Item(1);
                worksheet.Name = result.GeneId;

                WriteHeaders(worksheet);
                WriteData(worksheet, result);
                CreateChart(worksheet);
                WriteMetadata(worksheet, result);

                string fileName = string.Format(numericFormat, i++) + "_" + result.GeneId + "_" + string.Format("{0:G5}", m_measureSelector(result)) + ".xlsx";
                fileName = Path.Combine(m_targetDir.FullName, fileName);

                workbook.SaveAs(fileName, Excel.XlFileFormat.xlWorkbookDefault, misValue, misValue, misValue, misValue, Excel.XlSaveAsAccessMode.xlExclusive, misValue, misValue, misValue, misValue, misValue);
                workbook.Close(true, misValue, misValue);

                try
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
                }
                catch (Exception)
                {
                }

                if (i >= TopOutput)
                    break;
            }

            try
            {
                m_excel.Workbooks.Close();
                m_excel.Quit();
            }
            catch
            { }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        private void WriteHeaders(Excel.Worksheet worksheet)
        {
            worksheet.Cells[1, 1] = "T";
            worksheet.Cells[1, 2] = "GroupA (HIGH)";
            worksheet.Cells[1, 3] = "GroupB (LOW)";
        }
        private void WriteData(Excel.Worksheet worksheet, GeneResult result)
        {
            var groupAEvents = result.Estimate.GroupAEvents;
            var groupBEvents = result.Estimate.GroupBEvents;

            int currentTime = Math.Min(groupAEvents[0].Time, groupBEvents[0].Time);
            worksheet.Cells[2, 1] = (currentTime -1).ToString();
            worksheet.Cells[2, 2] = "1";
            worksheet.Cells[2, 3] = "1";

            int iA = 0, iB = 0, currentRow = 3;
            while (iA < groupAEvents.Count && iB < groupBEvents.Count)
            {
                currentTime = Math.Min(groupAEvents[iA].Time, groupBEvents[iB].Time);
                int failingA = (groupAEvents[iA].Time == currentTime ? groupAEvents[iA].NumberFailing : 0);
                int failingB = (groupBEvents[iB].Time == currentTime ? groupBEvents[iB].NumberFailing : 0);

                if (failingA + failingB != 0)
                {
                    worksheet.Cells[currentRow, 1] = currentTime.ToString();
                    worksheet.Cells[currentRow, 2] = worksheet.Cells[currentRow - 1, 2];
                    worksheet.Cells[currentRow, 3] = worksheet.Cells[currentRow - 1, 3];
                    currentRow++;

                    worksheet.Cells[currentRow, 1] = currentTime.ToString();
                    worksheet.Cells[currentRow, 2] = failingA == 0 ? worksheet.Cells[currentRow - 1, 2] : groupAEvents[iA].SurvivalProbability;
                    worksheet.Cells[currentRow, 3] = failingB == 0 ? worksheet.Cells[currentRow - 1, 3] : groupBEvents[iB].SurvivalProbability;
                    currentRow++;
                }

                if (groupAEvents[iA].Time == currentTime)
                    iA++;

                if (groupBEvents[iB].Time == currentTime)
                    iB++;
            }

            while (iA < groupAEvents.Count)
            {
                currentTime = groupAEvents[iA].Time;
                int failingA = groupAEvents[iA].NumberFailing;

                if (failingA != 0)
                {
                    worksheet.Cells[currentRow, 1] = currentTime.ToString();
                    worksheet.Cells[currentRow, 2] = worksheet.Cells[currentRow - 1, 2];
                    worksheet.Cells[currentRow, 3] = worksheet.Cells[currentRow - 1, 3];
                    currentRow++;

                    worksheet.Cells[currentRow, 1] = currentTime.ToString();
                    worksheet.Cells[currentRow, 2] = groupAEvents[iA].SurvivalProbability;
                    worksheet.Cells[currentRow, 3] = worksheet.Cells[currentRow - 1, 3];
                    currentRow++;
                }

                iA++;
            }

            while (iB < groupBEvents.Count)
            {
                currentTime = groupBEvents[iB].Time;
                int failingB = groupBEvents[iB].NumberFailing;

                if (failingB != 0)
                {
                    worksheet.Cells[currentRow, 1] = currentTime.ToString();
                    worksheet.Cells[currentRow, 2] = worksheet.Cells[currentRow - 1, 2];
                    worksheet.Cells[currentRow, 3] = worksheet.Cells[currentRow - 1, 3];
                    currentRow++;

                    worksheet.Cells[currentRow, 1] = currentTime.ToString();
                    worksheet.Cells[currentRow, 2] = worksheet.Cells[currentRow - 1, 2]; 
                    worksheet.Cells[currentRow, 3] = groupBEvents[iB].SurvivalProbability;
                    currentRow++;
                }

                iB++;
            }

            worksheet.ListObjects.Add(Excel.XlListObjectSourceType.xlSrcRange, worksheet.UsedRange, Type.Missing, Excel.XlYesNoGuess.xlYes, Type.Missing);
        }

        private void CreateChart(Excel.Worksheet worksheet)
        {
            Excel.Range usedRange = worksheet.UsedRange;

            Excel.ChartObjects xlCharts = (Excel.ChartObjects)worksheet.ChartObjects(Type.Missing);
            Excel.ChartObject myChart = xlCharts.Add(300, 80, 600, 350);
            Excel.Chart chartPage = myChart.Chart;

            chartPage.ChartType = Excel.XlChartType.xlXYScatterLines;
            chartPage.SetSourceData(usedRange, System.Reflection.Missing.Value);
        }

        private void WriteMetadata(Excel.Worksheet worksheet, GeneResult result)
        {
            worksheet.Cells[1, 7] = "LogPValue";
            worksheet.Cells[2, 7] = -Math.Log10(result.Estimate.PValue);

            worksheet.Cells[1, 8] = "FDR";
            worksheet.Cells[2, 8] = result.FDR;

            worksheet.ListObjects.Add(Excel.XlListObjectSourceType.xlSrcRange, worksheet.Range["G1:H2"], Type.Missing, Excel.XlYesNoGuess.xlYes, Type.Missing);
        }
    }
}
