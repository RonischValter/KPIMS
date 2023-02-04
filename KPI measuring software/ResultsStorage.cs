using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft;
using Microsoft.Office.Interop.Excel;
using OfficeOpenXml;
using OfficeOpenXml.Core.ExcelPackage;

namespace KPI_measuring_software
{
    internal class ResultsStorage
    {
        string path;
        public List<MeasurementSegment> listOfResults { get; set; }
        private int numberOfRepeats;
        public ResultsStorage(int numberOfRepeats)
        {
            listOfResults = new List<MeasurementSegment>();
            this.numberOfRepeats = numberOfRepeats;
            path = Directory.GetCurrentDirectory() + @"\results\Results.xlsx";
        }
        public void FillWorksheet(Worksheet ws)
        {
            //StreamWriter sw = new StreamWriter(".\\here.txt");
            for (int i = 0; i < listOfResults.Count; i++) //segment names
            {
                ws.Cells[i+2, 1].Value = listOfResults[i].segmentName;
            }
            for (int i = 0; i < listOfResults.Count; i++) //measured values
            {
                for (int j  = 0; j < listOfResults[i].successes; j++)
                {
                    //sw.WriteLine(i.ToString() + " " + j.ToString());
                    //sw.Flush();
                    if (listOfResults[i].measuredValueArraySize[j] == 0) //autofill
                    {
                        continue;
                    }
                    ws.Cells[i+2, j+2].Value = listOfResults[i].measuredValueArraySize[j].ToString();
                }
                ws.Cells[i + 2, numberOfRepeats + 2].Value = listOfResults[i].fails.ToString();

            }
            ws.Cells[1, numberOfRepeats + 2].Value = "Number of fails"; 
            //sw.Close();
        }
        public void WriteDataToTextFile()
        {
            StreamWriter sw = new StreamWriter(".\\debug\\" + "data" + ".txt");
            for (int i = 0; i < listOfResults.Count; i++)
            {
                sw.Write(listOfResults[i].segmentName + " ");
                for (int j = 0; j < numberOfRepeats; j++)
                {
                    sw.Write(listOfResults[i].measuredValueArraySize[j].ToString() + " ");
                }
                sw.WriteLine();
            }
            sw.Flush();
            sw.Close();
        }
        public void dump(string name)
        {

            WriteDataToTextFile();
            Microsoft.Office.Interop.Excel.Application excel = new();
            Workbook wb = excel.Workbooks.Open(path);
            try
            {
                //listOfResults.Add(new MeasureSegment("hi", 2));
                //listOfResults[0].Add(Status.Success, 10);

                wb.Sheets.Add();
                int i = wb.Sheets.Count;
                Worksheet ws = wb.Worksheets[1];
                FillWorksheet(ws);
                ws.Name = name;
                wb.Sheets[2].Move(wb.Sheets[1]);//move second sheet (now it is graphs and statistics) to first place
                ws.Move(wb.Sheets[2]);
            }
            finally
            {

                wb.Close();
            }
        }
        public void Add(Report report)
        {
            if (listOfResults.Count == 0)//empty list
            {
                listOfResults.Add(new MeasurementSegment(report.segmentName, numberOfRepeats));
                StoreResult(report, 0);
                return;
            }
            for (int i = 0; i < listOfResults.Count; i++)//search for correct list index
            {
                if (listOfResults[i].segmentName == report.segmentName)
                {
                    StoreResult(report, i);
                    return;
                }
            }
            //Measure segment not found
            listOfResults.Add(new MeasurementSegment(report.segmentName, numberOfRepeats));
            StoreResult(report, listOfResults.Count - 1);
            
        }
        private void StoreResult(Report report, int positionInList)
        {
            listOfResults[positionInList].Add(report.status, report.value);
        }

    }
    /// <summary>
    /// Stores results of measurements, number of successful tries and unsuccessful tries
    /// </summary>
    internal class MeasurementSegment
    {
        public string segmentName { get; }
        public int[] measuredValueArraySize { get; set; }
        public int successes;
        public int fails;
        public MeasurementSegment(string name, int size)
        {
            this.segmentName = name;
            measuredValueArraySize = new int[size];
            fails = 0;
            successes = 0;
        }
        public void Add(Status status, int result)
        {
            switch (status)
            {
                case Status.Success:
                    if (successes > measuredValueArraySize.Length)
                    {
                        return;
                    }
                    measuredValueArraySize[successes] = result;
                    successes++;
                    break;

                case Status.Fail:
                    fails++;
                    break;

                case Status.Debug:
                    throw new InvalidDataException("Error 7: Debug status not acceptable in this context");
                default:
                    throw new InvalidDataException("Error 8: Unknown status passed");
            }
        }
    }
    enum Status { Success, Fail, Debug }

    internal class Report
    {
        public string segmentName { get; }
        public int value { get; }
        public Status status { get; }
        public string message { get; }
        public Report(String segmentName, Status status, int value, string message) 
        { 
            this.segmentName = segmentName;
            this.value = value;
            this.status = status;
            this.message = message;
        }
    }
}
