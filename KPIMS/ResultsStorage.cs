
using Microsoft.Office.Interop.Excel;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace KPI_measuring_software
{
    internal class ResultsStorage
    {
        string excelPath;
        string failExcelPath;
        string filePath;
        string? error = null;
        Country country;

        public List<MeasurementSegment> listOfResults { get; set; }
        private int numberOfRepeats;
        public ResultsStorage(int numberOfRepeats, string launchPath, Country country)
        {
            listOfResults = new List<MeasurementSegment>();
            this.numberOfRepeats = numberOfRepeats;
            this.country = country;
            Directory.CreateDirectory(launchPath + "\\results");
            Directory.CreateDirectory(launchPath + "\\data");
            Directory.CreateDirectory(launchPath + "\\debug");
            filePath = launchPath;
        }
        public string GetDataPath()
        {
            if (error != null)
            {
                return error;
            }
            return excelPath;
        }
        public void FillWorksheet(Worksheet ws, List<DeviceInfo> deviceInfo)
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
                    if (listOfResults[i].measuredValueArray[j] == 0) //autofill
                    {
                        continue;
                    }
                    ws.Cells[i+2, j+2].Value = listOfResults[i].measuredValueArray[j].ToString();
                }
                ws.Cells[i + 2, numberOfRepeats + 2].Value = listOfResults[i].fails.ToString();

            }
            ws.Cells[1, numberOfRepeats + 2].Value = "Number of fails";

            //speed test
            //double? download = null;
            //try
            //{
            //    download = SpeedTest();
            //}
            //catch (Exception) { }
            //ws.Cells[listOfResults.Count + 3, 1].value = "Download speed";
            //if (download != null)
            //{
            //    ws.Cells[listOfResults.Count + 3, 2].value = download.ToString() +" MBps";
            //}
            //else
            //{
            //    ws.Cells[listOfResults.Count + 3, 2].value = "NaN";
            //}
            ws.Cells[listOfResults.Count + 4, 1].value = "Ping data";
            ws.Cells[listOfResults.Count + 4, 2].value = GetPingInfo() ;


            //fill device info
            for (int i = listOfResults.Count + 5; i < listOfResults.Count + 5 + deviceInfo.Count; i++)
            {
                ws.Cells[i, 1].value = deviceInfo[i - (listOfResults.Count + 5)].description;
                ws.Cells[i, 2].value = deviceInfo[i - (listOfResults.Count + 5)].value;
            }

            string ping = GetPingInfo();
            //sw.Close();
        }
        public string GetPingInfo()
        {
            if (country == Country.Serbia || country == Country.Bulgaria)
            {
                return "Cannot measure ping";
            }
            Console.WriteLine("Measuring ping");
            int pingAmount = 10;
            string command = $"ping -n {pingAmount} google.com";
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + command);
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;

            Process process = Process.Start(psi);
            string standartOutput = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            var lines = standartOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string output = "";
            for (int i = pingAmount+1; i < lines.Length; i++)
            {
                output += lines[i];
            }
            Console.WriteLine("Done");
            return output;
        }
        public static double SpeedTest()
        {
            Console.WriteLine("Starting Connection Speed Test");

            var watch = new Stopwatch();
            byte[] data;
            double download = 0;
            double upload = 0;
            using (var client = new WebClient())
            {
                watch.Start();
                data = client.DownloadData(
                    "http://ardownload.adobe.com/pub/adobe/reader/win/AcrobatDC/2001220041/AcroRdrDC2001220041_en_US.exe");
                watch.Stop();

                download = Math.Round((data.Length / watch.Elapsed.TotalSeconds) / (1000 * 1000), 2);
                Console.WriteLine("Done");

            }
            return download;
        }
        public void WriteDataToTextFile(List<DeviceInfo> deviceInfo)
        {
            StreamWriter sw = new StreamWriter(filePath + "\\debug\\" + "data-" + DateTime.Now.ToString("yyyy.MM.dd-hh.mm.ss") + ".txt");
            for (int i = 0; i < listOfResults.Count; i++)
            {
                sw.Write(listOfResults[i].segmentName + " ");
                for (int j = 0; j < numberOfRepeats; j++)
                {
                    sw.Write(listOfResults[i].measuredValueArray[j].ToString() + " ");
                }
                sw.WriteLine();
            }
            for (int i = 0; i < deviceInfo.Count; i++)
            {
                sw.WriteLine(deviceInfo[i].description + " - " + deviceInfo[i].value);
            }
            sw.Flush();
            sw.Close();
        }

        /// <summary>
        /// file name does not need to specify directory, only name of the .xlsx
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="sheetName"></param>
        /// <param name="debuglog"></param>
        public void dump(string fileName, string sheetName, StreamWriter debuglog, List<DeviceInfo> deviceInfo)
        {
            WriteDataToTextFile(deviceInfo);
            string workbookPath = filePath + "\\results\\" + fileName + ".xlsx";

            Microsoft.Office.Interop.Excel.Application excel = null;
            Workbook wb = null;
            bool newFile = false;

            try
            {
                excel = new();
                if (File.Exists(workbookPath))
                {
                    wb = excel.Workbooks.Open(workbookPath);
                }
                else
                {
                    wb = excel.Workbooks.Add();
                    newFile = true;
                }
                Worksheet ws = wb.Sheets.Add();
                debuglog.WriteLine("Sheet chosen");

                FillWorksheet(ws, deviceInfo);
                debuglog.WriteLine("Sheet filled");

                ws.Name = sheetName;
                debuglog.WriteLine("Sheet name changed");
                if (!newFile)
                {
                    wb.Save();
                }
                else
                {
                    wb.SaveAs2(workbookPath);
                }
                
                debuglog.WriteLine("Sheet save");

            }
            catch (Exception ex) { throw ex; }
            finally
            {
                if (wb != null)
                {
                    wb.Close();
                    Marshal.ReleaseComObject(wb);
                }
                if (excel != null)
                {
                    excel.Quit();
                    Marshal.ReleaseComObject(excel);
                }
                GC.Collect();
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
        public string segmentName { get; set; }
        public int[] measuredValueArray { get; set; }
        public int successes;
        public int fails;
        public MeasurementSegment(string name, int size)
        {
            this.segmentName = name;
            measuredValueArray = new int[size];
            fails = 0;
            successes = 0;
        }

        public void Add(Status status, int result)
        {
            switch (status)
            {
                case Status.Success:
                    if (successes >= measuredValueArray.Length)
                    {
                        return;
                    }
                    measuredValueArray[successes] = result;
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
