using Emgu.CV;
using Emgu.CV.Structure;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Net;
using System.Runtime.InteropServices;
using SpeedTest;
using SpeedTest.Net;
using System.Text.RegularExpressions;
using static System.Windows.Forms.AxHost;

namespace KPI_measuring_software
{
    enum Device {STB, AndroidTV } //in the future maybe Browser
    enum Country { Czechia, Bulgaria, Serbia, Slovakia, Hungary}
    enum RemoteKey { Up, Down, Left, Right, Ok, Back, ChannelUp, ChannelDown, One, Seven, EPG, HomePage, PowerButton }
    enum Transition { /*Login, Silent_Login,*/
        Playback_start_time_from_HeroChannelRail, Home_page_ready_from_EPG,
        Playback_start_time_LIVE_from_EPG, Playback_start_time_Catchup_from_EPG, Playback_resume_time,
        Zapper_start_time, Success_Login_Time, Purchase_Time }
    enum VideoOrigin { Live, Catchup}
    public enum Template { Now, NowOutOfFocus, YesterdayOutOfFocus, EPGStage1, SKBack, LoginProcessingScreen, 
                          Menu, HomePagePlay, PlayButton, StopButton, TVDetailPlayButton, WatchLive, 
                          PlaybackStage1, LogInButton, Continue, More}
    public partial class Main_Window : Form
    {
        bool Login = false;
        bool Silent_Login = false;
        bool Playback_start_time_from_HeroChannelRail = false;
        bool Home_page_ready_from_EPG = false;
        bool Playback_start_time_LIVE_from_EPG = false;
        bool Playback_start_time_Catchup_from_EPG = false;
        bool Zapper_start_time = false;
        bool Playback_resume_time = false;
        bool Success_Login_Time = false;
        bool Purchase_time = false;
        int imageNumber = 0;
        double firstPictureSensitivity = 0.95;
        double homePagePlaySensitivity = 0.90;
        double startOverSensitivity = 0.95;
        double playbackEmptyScreenSensitivity = 0.85;
        double pausePlaySensitivity = 0.98;
        double playButtonSensitivity = 0.90;
        double nowSensitivity = 0.90;
        double watchLiveSensitivity = 0.70;
        double loginProcessingScreenSensitivity = 0.70;
        string? IP = null;
        Report reportDone;
        public string filePath;
        MouseControl mouseControl;
        String loginName;
        String loginPassword;
        String appVersion;
        List<DeviceInfo> deviceInfo;
        Country chosenCountry;


        int playbackTimeoutValue = 7000;
        int homePageTimeOutValue = 7000;
        int loginLoadingScreenTimeOutValue = 60000;
        int playPauseWaitTime = 10000;
        int loadWaitTime = 3000;
        int loginTimeOutValue = 60000;
        int homePageButtonPressWaitTime = 7000;
        string chosenCountyPrefix;

        private Screen? chosenScreen = null;
        private Screen? temporaryScreen;
        private Device ChosenDevice;
        private Country country;
        //private StreamWriter resultsStream;
        private StreamWriter debugLog;
        private ResultsStorage results;
        System.Drawing.Point x;
        


        //private Process p;
        //private Thread process;

        public Main_Window()
        {
            InitializeComponent();
            HideAll();

            AllocConsole();
            mouseControl = new MouseControl();
            deviceInfo = new List<DeviceInfo>();


            foreach (Device e in (Device[]) Enum.GetValues(typeof(Device))) //Enivronments
            {
                TestEnvironmentOptions.Items.Add(e);

            }
            TestEnvironmentOptions.SelectedIndex = 0;
            foreach (Country c in (Country[])Enum.GetValues(typeof(Country))) //Languages
            {
                LanguageDropdown.Items.Add(c);

            }
            LanguageDropdown.SelectedIndex = 0;
            foreach (Transition t in (Transition[])Enum.GetValues(typeof(Transition))) // transitions
            {
                Options.Items.Add(t);
                //Console.WriteLine("private void Run{0}() \n {1}TestProgressLabel.Text = \"{0} in progress\"; {2}", t.ToString(), "{", "}");
            }
            EnvironmentSetUp();
            for (int i = 0; i < Options.Items.Count; i++)
            {
                Options.SetItemChecked(i, true);           
            }
            
            SetFilePath();
            
            //SpeedTest();
            ChooseEnvironmentLabel.Text = "Choose device";

            string adb = filePath + "\\adb\\adb";
            Process.Start("CMD.exe", "/C \"" + adb + "\" kill-server").WaitForExit();
            //Process.Start("CMD.exe", "/C \"" + adb + "\" devices").WaitForExit();
            

            Directory.CreateDirectory(filePath + "\\debug");
            Directory.CreateDirectory(filePath + "\\data"); 
            reportDone = new Report("System", Status.Debug, 0, "Done");

            SetUpLogs();


#if DEBUG
            TestButton.Visible = true;
            TakeScreenshotButton.Visible = true;
#endif


        }

        public void SetFilePath()
        {
            try
            {
                StreamReader sr = new StreamReader("filepath.txt");
                this.filePath = sr.ReadLine();
                sr.Close();
            }
            catch (Exception) //filepath not yet set
            {
                using (FolderBrowserDialog openFileDialog = new FolderBrowserDialog())
                {
                    openFileDialog.Description = "Choose path to store results";
                    openFileDialog.InitialDirectory = "c:\\";
                    


                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        //Get the path of specified file
                        filePath = openFileDialog.SelectedPath;


                        StreamWriter sw = new StreamWriter("filepath.txt"); //flag that filepath has been set - stores filepath
                        sw.Write(filePath);
                        sw.Flush();
                        sw.Close();
                    }
                }

            }
        }

        private void StoreDeviceDetails()
        {
            for (int i = 1; i <= 6; i++)
            {
                try
                {
                    // Get the label and textbox controls with the corresponding index
                    Label label = this.Controls.Find("label" + i.ToString(), true).FirstOrDefault() as Label;
                    TextBox textBox = this.Controls.Find("textBox" + i.ToString(), true).FirstOrDefault() as TextBox;

                    // Set the Visible property of the controls to true
                    deviceInfo.Add(new DeviceInfo(label.Text, textBox.Text));
                    //neukládá se device info
                }
                catch (Exception)
                { }

            }
        }

        internal sealed class NativeMethods
        {
            [DllImport("kernel32.dll")]
            public static extern bool AllocConsole();

            [DllImport("kernel32.dll")]
            public static extern bool FreeConsole();
        }

        private void SetUpLogs()
        {
            NativeMethods.AllocConsole();
            Directory.CreateDirectory(filePath + "\\debug");
            debugLog = new StreamWriter(filePath +"\\debug\\-" + chosenCountyPrefix + "-" + DateTime.Now.ToString("yyyy.MM.dd-hh.mm.ss") + ".txt");
        }
        private void CloseLogs()
        {
            debugLog.Flush();
            debugLog.Close();
            NativeMethods.FreeConsole();
        }
        public Bitmap BitmapScreenshot(Screen s)
        {
            //define bitmap image for current display
            Bitmap screenshot = new Bitmap(s.Bounds.Width, s.Bounds.Height, PixelFormat.Format32bppRgb);

            //prepare the screenshot
            Graphics memoryGraphics = Graphics.FromImage(screenshot);
            memoryGraphics.CopyFromScreen(s.Bounds.X, s.Bounds.Y, 0, 0,
                                          s.Bounds.Size, CopyPixelOperation.SourceCopy);
            //screenshot.Save("file.png", ImageFormat.Png);
            //RGBtoBGR(screenshot);
            return screenshot;

        }

        private void WriteResults(Report report)
        {
            WriteDebugInfo(report);
            // start work
            if (report.status != Status.Debug)//not debug info
            {
                results.Add(report);
            }

            //ConsoleLog.WriteLine("Playback_From_HeroChannelRail: stage1= " + stage1Time + "ms, stage2: " + stage2Time)
        }
        private void WriteDebugInfo(Report report) 
        {

            debugLog.WriteLine(report.segmentName + ": " + report.message);
            if (report.status == Status.Success)
            {
                debugLog.WriteLine(report.segmentName + " measured time: " + report.value.ToString() + " ms");
                Console.WriteLine(report.segmentName + " measured time: " + report.value.ToString() + " ms");

            }
            debugLog.Flush();
            Console.WriteLine(report.segmentName + ": " + report.message);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        private void WaitXMilliseconds(int waitTimeInMilliseconds)
        {
            Stopwatch sw = Stopwatch.StartNew();
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Waiting " + waitTimeInMilliseconds + " ms"));

            while (sw.ElapsedMilliseconds < waitTimeInMilliseconds)
            {
                Thread.Sleep(1000);
            }
        }
        public bool CheckForStalledProgress(ref int i, ref int lastSuccesses, int totalSuccesses, int totalFailures, int increment)
        {
            if (i == increment - 1)//every increment check results
            {
                if (lastSuccesses == totalSuccesses) //no change in last 10 tries
                {
                    return true;
                }
                else //still finding good results, do one more increment
                {
                    if (totalSuccesses * 2 < totalFailures)
                    {
                        return true;
                    }
                    i = i - 10;
                    lastSuccesses = totalSuccesses;
                }
            }
            return false;
        }


        #region STB control
        private bool ConnectToSTB()
        {
            if (IP == null)
            {
                throw new ArgumentNullException("Error 3: IP adress is null");
            }
            string adb = filePath + "\\adb\\adb";

            Process connect = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath + "\\adb\\adb.exe",
                    Arguments = "connect " + IP,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            Process killServer = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath + "\\adb\\adb.exe",
                    Arguments = "kill-server",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            connect.Start(); //p = Process.Start("CMD.exe", "/C \"" + adb + "\" connect " + IP);
            string consoleOutput = connect.StandardOutput.ReadToEnd();
            WriteDebugInfo(new Report("System", Status.Debug, 0, consoleOutput));
            var response = consoleOutput.Split(' ');

            for (int j = 0; j < 2; j++)
            {
                if (response[0].Equals("connected") || response[0].Equals("already"))
                {
                    debugLog.WriteLine(consoleOutput);
                    return true;
                }
                debugLog.WriteLine(consoleOutput);
                killServer.Start();
                killServer.WaitForExit();
                connect.Start();
            }
            return false;


            //Process.Start("\\CMD.exe", "/C adb connect " + IP).WaitForExit();

        }
        static void SpeedTest()
        {

        }
        private string GetAppVersion()
        {
            string adbPath = @"C:\Users\vronisch\source\repos\KPIMS\KPIMS\bin\Debug\net6.0-windows\adb\adb.exe";
            string packageName = null;
            switch (chosenCountyPrefix)
            {
                case "CZ": packageName = "cz.o2.o2tv"; break;
                case "RS": packageName = "rs.tv.kal"; break;
                case "BG": packageName = "bg.yettel.tv"; break;
                case "HU": packageName = "hu.pgsm.tv"; break;
                default: throw new NotImplementedException("GetAppVersion: package name unknown");
                    break;
            }

            
            string command = $"{adbPath} shell dumpsys package {packageName} | findstr /i \"versionName\"";

            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + command);
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;

            Process process = Process.Start(psi);
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            string versionName = output.Trim().Replace("\r", "");
            appVersion = "UnknownVersion";
            for (int i = 0; i < versionName.Split("\\n").Length; i++)
            {
                var version = versionName.Split("\\n")[0].Split("=")[1].Split(".");
                int ver = int.Parse(version[0]);
                int subVer = int.Parse(version[1]);
                if (ver >= 2 && subVer >= 14)
                {
                    string s = versionName.Split("\\n")[0].Split("=")[1].Replace("\n", "").Trim();
                    return Regex.Replace(s, @"[^\d.]", "");

                }
            }
            return "Unknown-APP-Version";

        }
        private void ShellSingleCommand(RemoteKey k)
        {
            string command = "/C \"" + filePath + "\\adb\\adb\" shell input keyevent " + Translate(k);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Process.Start("CMD.exe", command).WaitForExit();
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Key " + k.ToString() + " pressed"));
        }
        public void NavigateGrid(string word, bool reset)
        {
            int row = 2;
            int col = 4; // starting position is "g"
            char[][] grid = new char[][] {
                new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' },
                new char[] { 'q', 'w', 'e', 'r', 't', 'z', 'u', 'i', 'o', 'p' },
                new char[] { 'a', 's', 'd', 'f', 'g', 'h', 'j', 'k', 'l', '.' },
                new char[] { 'y', 'x', 'c', 'v', 'b', 'n', 'm', '-', ' ', ' ' }
            };

            char[][] alternativeGrid = new char[][] {
                new char[] { '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' },
                new char[] { '\'', '~', '!', '@', '#', '$', '%', '^', '&', '*' },
                new char[] { '+', '-', '_', '{', '}', '|', '\'', '.', '/', '?' },
                new char[] { '[', ']', '=', '(', ')', '0', '<', '>', '0', '0' },
            };


            foreach (char c in word)
            {
                //reset weird letters (ìšèøžýáíé)
                ShellSingleCommand(RemoteKey.Right); 
                ShellSingleCommand(RemoteKey.Left);
                bool letterInAlternativeGrid = false;

                if (row == 3)
                {
                    ShellSingleCommand(RemoteKey.Up);
                    row--;
                }
                while(col > 8)
                {
                    ShellSingleCommand(RemoteKey.Left);
                    col--;
                }
                //dont go to bottom right unless neccessary
                if (c == '@')
                {
                    GridGoTo(ref row, ref col, 4, 0); 
                    ShellSingleCommand(RemoteKey.Ok);
                    ShellSingleCommand(RemoteKey.Up);
                    ShellSingleCommand(RemoteKey.Up);
                    row -= 2;
                    continue;
                }

                char normalisedLetter = c;
                if (char.IsUpper(c)) //press shift
                {
                    GridGoTo(ref row, ref col, 3, 8);
                    ShellSingleCommand(RemoteKey.Ok);
                    ShellSingleCommand(RemoteKey.Up);
                    row--;
                    normalisedLetter = char.ToLower(c);
                }

                int[] target = FindLetter(grid, normalisedLetter);
                if (target[0] == -1) //weird char
                {
                    target = FindLetter(alternativeGrid, normalisedLetter);
                    GridGoTo(ref row, ref col, 6, 9);
                    ShellSingleCommand(RemoteKey.Ok);
                    row = 0;
                    col = 0;
                    letterInAlternativeGrid = true;
                }
                WriteDebugInfo(new Report("System", Status.Debug, 0, "Looking for letter " + normalisedLetter + " on coordinates " +
                                        target[0] + " " + target[1]));

                if (target[0] == -1)
                {
                    throw new ArgumentException("Error 12: Unknow symbol " + c);
                }
                int targetRow = target[0];
                int targetCol = target[1];
                while (row != targetRow)
                {
                    if (row < targetRow)
                    {
                        ShellSingleCommand(RemoteKey.Down);
                        row++;
                    }
                    else
                    {
                        ShellSingleCommand(RemoteKey.Up);
                        row--;
                    }
                }
                while (col != targetCol)
                {
                    if (col < targetCol)
                    {
                        ShellSingleCommand(RemoteKey.Right);
                        col++;
                    }
                    else
                    {
                        ShellSingleCommand(RemoteKey.Left);
                        col--;
                    }
                }
                ShellSingleCommand(RemoteKey.Ok);
                if (letterInAlternativeGrid)
                {
                    row = 0;
                    col = 0;
                }
            }
            if (!reset)
            {
                return;
            }
            //confirm and reset if reset true
            while (col != 8)
            {
                if (col < 8)
                {
                    ShellSingleCommand(RemoteKey.Right);
                    col++;
                }
                if (col > 8)
                {
                    ShellSingleCommand(RemoteKey.Left);
                    col--;
                }
            }
            while (row != 2)
            {
                if (row < 2)
                {
                    ShellSingleCommand(RemoteKey.Down);
                    row++;
                }
                if (row > 2)
                {
                    ShellSingleCommand(RemoteKey.Up);
                    row--;
                }
            }
            for (int i = 0; i < 5; i++)
            {
                ShellSingleCommand(RemoteKey.Down);
            }
            ShellSingleCommand(RemoteKey.Ok);

        }
        public void GridGoTo(ref int currentRow, ref int currentCol, int row, int col)
        {
            while (currentCol != col)
            {
                if (currentCol < col)
                {
                    ShellSingleCommand(RemoteKey.Right);
                    currentCol++;
                }
                else
                {
                    ShellSingleCommand(RemoteKey.Left);
                    currentCol--;
                }
            }
            while (currentRow != row)
            {
                if (currentRow < row)
                {
                    ShellSingleCommand(RemoteKey.Down);
                    currentRow++;
                }
                else
                {
                    ShellSingleCommand(RemoteKey.Up);
                    currentRow--;
                }
            }
        }

        private int[] FindLetter(char[][] grid, char letter)
        {
            for (int i = 0; i < grid.Length; i++)
            {
                for (int j = 0; j < grid[i].Length; j++)
                {
                    if (grid[i][j] == letter)
                    {
                        return new int[] { i, j };
                    }
                }
            }
            return new int[] { -1, -1 };
        }

        private void STBSKGoToDefault()
        {
            while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                    GetTemplate(Template.SKBack), startOverSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Back });
            }
        }
        /// <summary>
        /// Goes from anywhere to EPG, now, first channel
        /// </summary>
        public void STBResetEPGToDefault()
        {
            if (chosenCountyPrefix == "SK")
            {
                STBSKGoToDefault();
                ShellCommand(new RemoteKey[] { RemoteKey.Ok, RemoteKey.Right, RemoteKey.Ok, RemoteKey.One, RemoteKey.Ok, RemoteKey.Back });
                while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                    GetTemplate(Template.Now), startOverSensitivity))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Right });
                }
                ShellCommand(new RemoteKey[] { RemoteKey.Ok });

                return;
            }
            STBSetToEPGNow();
            if (chosenCountry == Country.Hungary)
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Seven, RemoteKey.Ok });
                WaitXMilliseconds(2000);
                return;
            }
            ShellCommand(new RemoteKey[] { RemoteKey.One, RemoteKey.Ok });
            WaitXMilliseconds(2000);
        }
        private string Translate(RemoteKey k)
        {
            return k switch
            {
                RemoteKey.Up => "19",
                RemoteKey.Down => "20",
                RemoteKey.Left => "21",
                RemoteKey.Right => "22",
                RemoteKey.Ok => "23",
                RemoteKey.Back => "4",
                RemoteKey.ChannelUp => "166",
                RemoteKey.ChannelDown => "167",
                RemoteKey.One => "8",
                RemoteKey.Seven => "14",
                RemoteKey.EPG => "172",
                RemoteKey.HomePage => "3",
                RemoteKey.PowerButton => "26",
                _ => throw new NotImplementedException(),
            } ;
        }
        private void ShellCommand(RemoteKey[] cmd)
        {

            for (int i = 0; i < cmd.Length; i++)
            {
                string command = "/C \"" + filePath + "\\adb\\adb\" shell input keyevent " + Translate(cmd[i]);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Process.Start("CMD.exe", command).WaitForExit();
                WriteDebugInfo(new Report("System", Status.Debug, 0, "Key " + cmd[i].ToString() + " pressed"));
            }
        }
        public void STBResetEPGToNow()
        {
            for (int i = 0; i < 2; i++)
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
            }
        }
        private void STBGoToHomePage()
        {

            if (chosenCountyPrefix == "SK")
            {
                STBSKGoToDefault();
                ShellCommand(new RemoteKey[] { RemoteKey.Ok, RemoteKey.Down });
                return;
            }
            while (!FindPattern(GetPrintscreenMat(), 
                                GetTemplate(Template.HomePagePlay), 
                                homePagePlaySensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.HomePage });
                WaitXMilliseconds(homePageButtonPressWaitTime);
            }
            //ShellCommand(new RemoteKey[] {RemoteKey.Back,RemoteKey.Back,RemoteKey.Back,RemoteKey.Back,
            //                              RemoteKey.Back,RemoteKey.Back,RemoteKey.Back,RemoteKey.Up, RemoteKey.Up, RemoteKey.Up});
        }
        private void STBLogOut()
        {
            STBGoToHomePage();
            _STBLogOut();
        }
        private void STBLogin(string password)
        {
            ShellSingleCommand(RemoteKey.Ok);
            WaitXMilliseconds(5000);
            NavigateGrid("", true);
            NavigateGrid(password, true);
        }
        public void _STBLogOut()
        {
            ShellSingleCommand(RemoteKey.Up);
            var needle = GetTemplate(Template.Menu);
            for (int i = 0; i < 8; i++)
            {
                ShellSingleCommand(RemoteKey.Right);
                WaitXMilliseconds(1500);
                
            }
            ShellSingleCommand(RemoteKey.Ok);
            WaitXMilliseconds(1000);
            ShellSingleCommand(RemoteKey.Left);
            WaitXMilliseconds(1000);
            for (int i = 0; i < 12; i++)
            {
                ShellSingleCommand(RemoteKey.Down);
            }
            ShellSingleCommand(RemoteKey.Ok);
            WaitXMilliseconds(2000);
            ShellSingleCommand(RemoteKey.Right);
            WaitXMilliseconds(2000);
            ShellSingleCommand(RemoteKey.Ok);
            WaitXMilliseconds(10000);
            Stopwatch sw = Stopwatch.StartNew();
            while (!FindPattern(GetPrintscreenMat(), GetTemplate(Template.LogInButton), 0.90))
            {
                if (sw.ElapsedMilliseconds > 60000)
                {
                    WriteDebugInfo(new Report("System", Status.Debug, 0, "Log in button has not loaded in 60 seconds, restarting device"));
                    STBRecover();
                }
                Thread.Sleep(1000);
            }
        }
        public void STBRecover()
        {
                
        }
        private bool _STBGoToVODMore()
        {
            if (FindPattern(GetPrintscreenMat(), GetTemplate(Template.More), 0.95))
            {
                ShellSingleCommand(RemoteKey.Ok);
                WaitXMilliseconds(1000);
                return true;
            }
            for (int i = 0; i < 2; i++)
            {
                ShellSingleCommand(RemoteKey.Down);
                WaitXMilliseconds(1000);
            }
            int tries = 0;
            while (!FindPattern(GetPrintscreenMat(), GetTemplate(Template.More), 0.85))
            {
                if (tries > 20)
                {
                    return false;
                }
                ShellSingleCommand(RemoteKey.Right);
                tries++;
            }
            ShellSingleCommand(RemoteKey.Ok);
            return true;
        }
        private bool STBGoToVOD()
        {
            ShellSingleCommand(RemoteKey.Up);
            WaitXMilliseconds(1500);
            for (int i = 0; i < 6; i++)
            {
                ShellSingleCommand(RemoteKey.Right);
                WaitXMilliseconds(1000);
            }
            ShellSingleCommand(RemoteKey.Ok);
            WaitXMilliseconds(1000);
            //ensure that there is at least one purchased item
            int fails = 0;
            bool criticalFailiure = false;
            while (!FindPattern(GetPrintscreenMat(), GetTemplate(Template.StopButton), playButtonSensitivity))
            {
                if (fails > 10)
                {
                    if (criticalFailiure)//did not manage to open first thing in VOD
                    {
                        return false;
                    }
                    ShellSingleCommand(RemoteKey.Up);
                    WaitXMilliseconds(1000);
                    ShellSingleCommand(RemoteKey.Left);
                    WaitXMilliseconds(1000);
                    fails = 0;
                }
                ShellSingleCommand(RemoteKey.Ok);
                WaitXMilliseconds(500);
                fails++;
            }
            for (int i = 0; i < 3; i++)
            {
                ShellSingleCommand(RemoteKey.Back);
                WaitXMilliseconds(2000);
            }
            bool success = _STBGoToVODMore();
            fails = 0;
            while (!success)
            {
                WriteDebugInfo(new Report("System", Status.Debug, 0, "VOD not found, reseting search for VOD"));
                for (int i = 0; i < 10; i++)
                {
                    ShellSingleCommand(RemoteKey.Up);
                }
                ShellSingleCommand(RemoteKey.Down);
                fails++;
                if (fails > 5)
                {
                    return false;
                }

                success = _STBGoToVODMore();

            }
            WaitXMilliseconds(2000);
            return true;
        }
        private void _STBGoToPause()
        {
            bool needleFound = _WaitForNeedle(GetTemplate(Template.StopButton));
            if (FindPattern(GetPrintscreenMat(), GetTemplate(Template.PlayButton), playButtonSensitivity))
            {
                return;
            }
            if (needleFound)
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Ok });//stop playback
                WaitXMilliseconds(1000);
                
                int failMeasure = 0;
                while (!FindPattern(GetPrintscreenMat(), GetTemplate(Template.PlayButton), playButtonSensitivity))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Ok });//stop playback
                    WaitXMilliseconds(1000);
                    if (failMeasure == 2) // failed 3 times
                    {
                        STBReturnFromPlaybackToEPG();
                        ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok, RemoteKey.Ok }); //start playback
                        _STBGoToPause();
                        return;
                    }
                    failMeasure++;
                }//didn't manage to stop playback

            }
            else//no pause button found -> try next channel
            {
                STBReturnFromPlaybackToEPG(); 

                if (FindPattern(GetPrintscreenMat(),
                                GetTemplate(Template.NowOutOfFocus),
                                playButtonSensitivity))
                {
                    _STBGoToEPGNow();
                    _STBStartNextChannel();
                }
                else
                {
                    STBGoOneChannelUp(VideoOrigin.Catchup, 0);
                }


                //program started but does not have regular controls
                STBReturnFromPlaybackToEPG();
                ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok }); //prepared on next channel
                _STBGoToPause();
            }
        }
        public void _STBStartNextChannel()
        {
            ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok, RemoteKey.Ok }); //prepared on next channel
        }
        private void _STBRestartPlayback(ref Stopwatch sw)
        {

            ShellCommand(new RemoteKey[] { RemoteKey.Ok }); //show menu
            WaitXMilliseconds(2000);
            sw.Restart();
            while (!FindPattern(GetPrintscreenMat(),
                                GetTemplate(Template.PlayButton),
                                startOverSensitivity))
            {
                if (sw.ElapsedMilliseconds > 9000) //controlls dissappeared - reopen them
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Ok });
                    WaitXMilliseconds(1500);
                }
            }
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Restarting playback"));
            ShellCommand(new RemoteKey[] { RemoteKey.Ok }); //Press play
            sw.Restart();

        }

        /// <summary>
        /// From home page to STB
        /// </summary>
        private void STBSetToEPG()
        {
            if (chosenCountyPrefix == "SK")
            {
                STBSKGoToDefault();
                ShellCommand(new RemoteKey[] { RemoteKey.Ok, RemoteKey.Right, RemoteKey.Ok });
                return;
            }
            while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                    GetTemplate(Template.NowOutOfFocus),
                    startOverSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
                WaitXMilliseconds(2000);
            }

            //STBReset();
            //_STBGoToEPG();
        }
        public void _STBReturnToVOD()
        {
            var novinky = CvInvoke.Imread(filePath + "\\templates\\CZnovinky.png");
            int fails = 0;

            ShellSingleCommand(RemoteKey.Back);
            ShellSingleCommand(RemoteKey.Back);
            WaitXMilliseconds(2000);

            while (!FindPattern(GetPrintscreenMat(), novinky, 0.90))
            {
                if (fails > 5)//failed
                {
                    STBGoToHomePage();
                    STBGoToVOD();
                    return;
                }
                ShellSingleCommand(RemoteKey.Back);
                WaitXMilliseconds(7000);
                fails++;
            }
        }
        /// <summary>
        /// back until in videoteka, then go to next
        /// </summary>
        public void STBVODNextUnpurchased(int recursion)
        {

            var novinky = CvInvoke.Imread(filePath + "\\templates\\CZnovinky.png");
            var homePage = GetTemplate(Template.HomePagePlay);
            var continuee = GetTemplate(Template.Continue);
            while (!FindPattern(GetPrintscreenMat(), novinky, 0.70))
            {
                ShellSingleCommand(RemoteKey.Back);
                WaitXMilliseconds(7000);
                if (FindPattern(GetPrintscreenMat(), homePage, homePagePlaySensitivity))
                {
                    STBGoToVOD();
                    STBVODNextUnpurchased(recursion + 1);
                    while (FindPattern(GetPrintscreenMat(), continuee, homePagePlaySensitivity))
                    {
                        STBVODNextUnpurchased(1);
                    }
                    return;
                }
            }
            if (recursion == 0)
            {
                ShellSingleCommand(RemoteKey.Right);
                WaitXMilliseconds(1000);
                ShellSingleCommand(RemoteKey.Ok);
                WaitXMilliseconds(1000);
            }
            else
            {
                ShellSingleCommand(RemoteKey.Down);
                WaitXMilliseconds(1000);
                ShellSingleCommand(RemoteKey.Ok);
                WaitXMilliseconds(1000);
            }

            if (FindPattern(GetPrintscreenMat(), continuee, homePagePlaySensitivity))
            {
                STBVODNextUnpurchased(0);
            }
        }
        public void STBPutchaseThis()
        {
            ShellSingleCommand(RemoteKey.Ok);
            WaitXMilliseconds(1000);
            ShellSingleCommand(RemoteKey.Up);
            WaitXMilliseconds(1000);
            ShellSingleCommand(RemoteKey.Left);
            WaitXMilliseconds(1000);
            for (int i = 0; i < 3; i++)
            {
                ShellSingleCommand(RemoteKey.Ok);
                WaitXMilliseconds(1000);
            }
            ShellSingleCommand(RemoteKey.Ok);
        }
        private void STBSetToEPGNow()
        {
            if (chosenCountyPrefix == "SK")
            { ///co s tím? Default a pak dohledat???? 

                STBSKGoToDefault();
                ShellCommand(new RemoteKey[] { RemoteKey.Ok, RemoteKey.Right, RemoteKey.Ok, RemoteKey.Back });
                while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                        GetTemplate(Template.Now), 0.70))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Right });
                    WaitXMilliseconds(1000);
                }
                ShellCommand(new RemoteKey[] { RemoteKey.Ok ,RemoteKey.One, RemoteKey.Ok });
                return;
            }
            while (!FindPattern(GetPrintscreenMat(), GetTemplate(Template.NowOutOfFocus), nowSensitivity))
            {
                ShellSingleCommand(RemoteKey.EPG);
                WaitXMilliseconds(500);
            }
            ShellSingleCommand(RemoteKey.EPG);
            
        } 

        private void STBReturnFromPlaybackToEPG()
        {
            if (FindPattern(GetPrintscreenMat(), GetTemplate(Template.NowOutOfFocus), nowSensitivity) ||
                FindPattern(GetPrintscreenMat(), GetTemplate(Template.YesterdayOutOfFocus), nowSensitivity))
            {
                WriteDebugInfo(new Report("System", Status.Debug, 0, "Already in EPG"));
                return;
            }
            if (chosenCountyPrefix == "SK")
            {
                while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                        GetTemplate(Template.NowOutOfFocus),
                        0.70) &&
                       !FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                        GetTemplate(Template.YesterdayOutOfFocus),
                        0.70))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Back });
                    WaitXMilliseconds(loadWaitTime);
                }
                return;
            }
            ShellCommand(new RemoteKey[] { RemoteKey.EPG });
            WaitXMilliseconds(loadWaitTime);

            while (!FindPattern(GetPrintscreenMat(),
                    GetTemplate(Template.NowOutOfFocus),
                    startOverSensitivity) &&
                   !FindPattern(GetPrintscreenMat(),
                    GetTemplate(Template.YesterdayOutOfFocus),
                    nowSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
                WaitXMilliseconds(loadWaitTime);
            }
            if (FindPattern(GetPrintscreenMat(),
                    GetTemplate(Template.NowOutOfFocus),
                    startOverSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
                WaitXMilliseconds(500);
            }
        }
        private void _STBGoToEPGNow()
        {
            ShellCommand(new RemoteKey[] { RemoteKey.Back });
            var now = GetTemplate(Template.Now);
            Thread.Sleep(500);
            while (!FindPattern(GetPrintscreenMat(), now, 0.85))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Right });
                Thread.Sleep(500);
            }
            ShellCommand(new RemoteKey[] { RemoteKey.Ok });//on now column
            ShellCommand(new RemoteKey[] { RemoteKey.One, RemoteKey.Ok });
            WaitXMilliseconds(1000);
        }
        private void _STBChannelUp() => ShellCommand(new RemoteKey[] { RemoteKey.ChannelUp });


        private bool STBGoOneChannelUp(VideoOrigin o, int recursion)
        {
            if (recursion >= 5)
            {
                return false;
            }
            switch (o)
            {
                case VideoOrigin.Live: _STBChannelUp();
                    WaitXMilliseconds(1000);
                    return true;
                    break;
                case VideoOrigin.Catchup:
                    _STBGoToYesterday();
                    var needle = GetTemplate(Template.TVDetailPlayButton);
                    ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok});
                    WaitXMilliseconds(2000);
                    for (int i = 0; i < 5; i++)
                    {
                        if (FindPattern(GetPrintscreenMat(), needle, homePagePlaySensitivity))
                        {
                            ShellCommand(new RemoteKey[] { RemoteKey.Ok });
                            return true;
                        }
                        ShellSingleCommand(RemoteKey.Ok);
                        WaitXMilliseconds(2000);
                    }
                    if (STBGoOneChannelUp(VideoOrigin.Catchup, recursion + 1))
                    {
                        return true;
                    }
                    return false;
                default: throw new NotImplementedException("Error 6: Video Origin not implemented");
                    
            }
        }
        private void _STBGoToYesterday()
        {
            ShellCommand(new RemoteKey[] { RemoteKey.EPG });
            WaitXMilliseconds(2000);
            if (FindPattern(GetPrintscreenMat(), GetTemplate(Template.YesterdayOutOfFocus), nowSensitivity))
            {
                return;
            }
            while (!FindPattern(GetPrintscreenMat(), GetTemplate(Template.NowOutOfFocus), nowSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
            }
            while (!FindPattern(GetPrintscreenMat(), GetTemplate(Template.YesterdayOutOfFocus), nowSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Back, RemoteKey.Left, RemoteKey.Ok });
                WaitXMilliseconds(2000);
            }
        }

        private void STBStartHeroChannelRailFromHomePage()
        {
            ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Right, RemoteKey.Right, RemoteKey.Ok }); //ready for ok
        }
        private void STBStartFirstChannel()
        {
            STBResetEPGToDefault();
            while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                    GetTemplate(Template.StopButton),
                    playButtonSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Ok });//chosen channal
                WaitXMilliseconds(3000);
            }
        }
        private bool STBGoOneChannelUpNowLongWay(int recursionValue)
        {
            if (recursionValue > 5)//bigger problem somewhere
            {
                return false;
            }
            var needle = GetTemplate(Template.NowOutOfFocus);
            while (!FindPattern(GetPrintscreenMat(), needle, nowSensitivity))
            {
                ShellSingleCommand(RemoteKey.EPG);
            }
            ShellSingleCommand(RemoteKey.Down);
            ShellSingleCommand(RemoteKey.Ok);
            WaitXMilliseconds(2000);
            for (int i = 0; i < 3; i++)//try to play
            {
                if (FindPattern(GetPrintscreenMat(),
                                    GetTemplate(Template.WatchLive),
                                    watchLiveSensitivity))
                {
                    ShellSingleCommand(RemoteKey.Ok);
                    return true;
                }
                ShellSingleCommand(RemoteKey.Ok);
                WaitXMilliseconds(1000);
            }
            if (STBGoOneChannelUpNowLongWay(recursionValue + 1))
            {
                return true;
            }
            return false;
        }
        private void STBGoToFirstChannelYesterday()
        {
            STBResetEPGToDefault();
            WaitXMilliseconds(loadWaitTime);
            _STBGoToYesterday();
            for (int i = 0; i < 3; i++)
            {
                if (!FindPattern(GetPrintscreenMat(), GetTemplate(Template.TVDetailPlayButton), playButtonSensitivity))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Ok }); //start playback
                    WaitXMilliseconds(1000);
                }
                ShellSingleCommand(RemoteKey.Ok);
                WaitXMilliseconds(2000);
            }
        }
        //private void SafeShellCommand() { //todo
        #endregion

        #region SetUps
        private void DeviceDetailsSetUp()
        {
            HideAll();
            for (int i = 1; i <= 6; i++)
            {
                // Get the label and textbox controls with the corresponding index
                Label label = this.Controls.Find("label" + i.ToString(), true).FirstOrDefault() as Label;
                TextBox textBox = this.Controls.Find("textBox" + i.ToString(), true).FirstOrDefault() as TextBox;

                // Set the Visible property of the controls to true
                if (label != null) label.Visible = true;
                if (textBox != null) textBox.Visible = true;
            }
            deviceDetailsConfirmButton.Visible = true;
        }

        public void ScreenSetUp()
        {
            HideAll();

            SetVisible(screenShowBox);
            SetVisible(ChooseThisScreenButton);
            SetVisible(ScreenChooseNextButton);
            SetVisible(ScreenShowPreviousButton);

            ScreenShowPreviousButton.Enabled = false;
            if (Screen.AllScreens.Length < 2)//only one screen
            {
                ScreenChooseNextButton.Enabled = false;
            }

            if (chosenScreen == null)
            {
                temporaryScreen = Screen.AllScreens[0];
            }
            screenShowBox.Image = BitmapScreenshot(temporaryScreen);

        }
        private void SetVisible(Control c)   { c.Visible= true;} //done

        private void SetInvisible(Control c) { c.Visible= false; } //done
        private void EnvironmentSetUp()
        {
            HideAll();
            SetVisible(ChooseEnvironmentLabel);
            SetVisible(ChooseEnvironmentOkButton);
            SetVisible(TestEnvironmentOptions);
            ChooseEnvironmentLabel.Text = "Choose environment";
            ChooseEnvironmentLabel.ForeColor = Color.Empty;
        }
        private void CountrySetUp()
        {
            HideAll();
            SetVisible(ChooseCountryLabel);
            SetVisible(LanguageDropdown);
            SetVisible(ChooseCountryOkButton);
            ChooseEnvironmentLanguageLabel.Text = "Choose environment";
            ChooseEnvironmentLabel.ForeColor = Color.Empty;

        }


        private void OptionsSetUp()
        {
            HideAll();
            SetVisible(Options);
            SetVisible(OptionsConfirmButton);
            SetVisible(OptionsIterationValueTextBox);
            SetVisible(cycleValueLabel);
            SetVisible(cycleValueTextBox);
            SetVisible(iterationValueLabel);

        }

        private void HideAll()
        {
            foreach (Control c in this.Controls)
            {
                c.Visible = false;
            }
        }

        private void STBIPSetUp()
        {
            HideAll();
            SetVisible(STBIPInputOkButton);
            SetVisible(STBIPLabel);
            SetVisible(STBIPInputBox);
            if (File.Exists(filePath + "\\data\\STBIP.txt"))
            {
                using(StreamReader sr = new StreamReader(filePath + "\\data\\STBIP.txt"))
                {
                    STBIPInputBox.Text = sr.ReadToEnd();
                }
            }
        }
        #endregion

        #region Feed processing
        public Mat GetTemplate(Template t)
        {
            string s;
            switch (t)
            {
                case Template.Now: 
                    s = chosenCountyPrefix + "now";
                    break;
                case Template.NowOutOfFocus:
                    s = chosenCountyPrefix + "nowOutOfFocus";
                    break;
                case Template.YesterdayOutOfFocus:
                    s = chosenCountyPrefix + "yesterdayOutOfFocus";
                    break;
                case Template.EPGStage1:
                    s = chosenCountyPrefix + "EPG_stage1";
                    break;
                case Template.HomePagePlay:
                    s = chosenCountyPrefix + "HomePagePlay";
                    break;
                case Template.PlayButton:
                    s = chosenCountyPrefix + "playButton";
                    break;
                case Template.StopButton:
                    s = chosenCountyPrefix + "stopButton";
                    break;
                case Template.TVDetailPlayButton:
                    s = chosenCountyPrefix + "TVDetailPlayButton";
                    break;
                case Template.WatchLive:
                    s = chosenCountyPrefix + "watchLive";
                    break;
                case Template.SKBack:
                    s = chosenCountyPrefix + "back";
                    break;
                case Template.PlaybackStage1:
                    s = chosenCountyPrefix + "PlaybackStage1";
                    break;
                case Template.LoginProcessingScreen:
                    s = chosenCountyPrefix + "LoginScreen";
                    break;
                case Template.Menu:
                    s = chosenCountyPrefix + "menu";
                    break;
                case Template.LogInButton:
                    s = chosenCountyPrefix + "loginButton";
                    break;
                case Template.More:
                    s = chosenCountyPrefix + "more";
                    break;
                case Template.Continue:
                    s = chosenCountyPrefix + "continue";
                    break;
                default: throw new NotImplementedException("Error 11: Unknown template");
            }
            return CvInvoke.Imread(filePath + "\\templates\\" + s + ".png");
        }
        public Mat GetPrintscreenMat()
        {
            return BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat;
        }
        private bool _WaitForNeedle(Mat needle)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int failedAttempts = 0;
            while (!FindPattern(GetPrintscreenMat(), needle, playButtonSensitivity))
            {
                WriteDebugInfo(new Report("System", Status.Debug, 0, "Waiting for needle"));

                if (sw.ElapsedMilliseconds > 10000)
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Ok });//stop playback
                    sw.Restart();
                    failedAttempts++;
                }
                if (failedAttempts > 2)
                {
                    WriteDebugInfo(new Report("System", Status.Debug, 0, "Playback resume failed: Pause button not found"));
                    return false;
                }
            }
            return true;
        }

        private bool FindPattern(Mat stack, Mat needle, double sensitivity)
        {
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Looking for pattern"));

            Mat result = new Mat();
            //CvInvoke.Imshow("stack", stack);
            //CvInvoke.Imshow("needle", needle);
            CvInvoke.MatchTemplate(stack, needle, result, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed);
            //CvInvoke.Imshow("Raw result", result);
            CvInvoke.Threshold(result, result, sensitivity, 1, Emgu.CV.CvEnum.ThresholdType.ToZero);
            //CvInvoke.Imshow("filtered result", result);
            var resultImage = result.ToImage<Gray, byte>();
            //CvInvoke.Imshow("matches", resultImage);
            //hi
            //int matches = 0;
            for (int i = 0; i < result.Cols; i++) //search for matches
            {
                for (int j = 0; j < result.Rows; j++)
                {
                    if (resultImage[j, i].Intensity > sensitivity)
                    {
                        WriteDebugInfo(new Report("System", Status.Debug, 0, "Pattern Found"));
                        return true;
                    }
                }
            }
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Pattern Not Found"));
            return false;
        }
        /// <summary>
        /// Queue can be empty, start the stopwatch before passing in, timeoutValue is only important,
        /// if timeout is enabled. On time out returns "-1".
        /// </summary>
        /// <param name="needle"></param>
        /// <param name="sensitivity"></param>
        /// <param name="sw"></param>
        /// <param name="feed"></param>
        /// <returns></returns>
        ///
        private int GetMaxTimeOfAppearing(Mat needle,
                                          Shot mostRecentImageInput,
                                          double sensitivity,
                                          Stopwatch sw,
                                          ref Queue<Shot> feed,
                                          bool timeoutEnabled,
                                          int timeoutValue)
            
        {
            WriteDebugInfo(new Report("System", Status.Debug, 0, "shot time = " + mostRecentImageInput.time));
            bool matchFound = false;
            Shot susShot = mostRecentImageInput;
            feed.Enqueue(mostRecentImageInput);
            if (!sw.IsRunning)
            {
                sw.Start();
            }
            var process = new Thread(
                    () =>
                    {
                        
                        matchFound = FindPattern(susShot.image, needle, sensitivity);
                    }
                    );
            process.Start();
            if (!sw.IsRunning) //measuring stage 1
            {
                sw.Start();
            }


            Shot lastShot;

            while (!matchFound)//still working
            {
                if (timeoutEnabled && sw.ElapsedMilliseconds > timeoutValue)
                {
                    return -1;
                }
                lastShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                feed.Enqueue(lastShot);

                WriteDebugInfo(new Report("System", Status.Debug, 0, "shot time = " + lastShot.time));
                
                if (!process.IsAlive)
                {
                    if (matchFound)//ended in meantime
                    {
                        return susShot.time;
                    }
                    //not found
                    while (feed.Peek().time <= susShot.time)
                    {
                        WriteDebugInfo(new Report("System", Status.Debug, 0, "removing trash: " + feed.Peek().time));
                        feed.Dequeue();
                    }
                    susShot = lastShot;
                    process = new Thread(
                        () =>
                        {
                            matchFound = FindPattern(susShot.image, needle, sensitivity); ;
                        }
                        );
                    process.Start();
                }
                //thread alive - keep working
            }
            //match found
            return susShot.time;

        }
        /// <summary>
        /// Queue can be empty, start the stopwatch before passing in, timeoutValue is only important,
        /// if timeout is enabled. On time out returns "-1".
        /// </summary>
        /// <param name="needle"></param>
        /// <param name="mostRecentImageInput"></param>
        /// <param name="sensitivity"></param>
        /// <param name="sw"></param>
        /// <param name="feed"></param>
        /// <param name="timeouEnabled"></param>
        /// <param name="timeoutValue"></param>
        /// <returns></returns>
        private int GetMaxTimeOfDissappearing(Mat needle,
                                              Shot mostRecentImageInput,
                                              double sensitivity,
                                              Stopwatch sw,
                                              ref Queue<Shot> feed,
                                              bool timeouEnabled,
                                              int timeoutValue)
        {
            bool matchLost = false;
            Shot susShot = mostRecentImageInput;
            
            

            if (!sw.IsRunning)
            {
                sw.Start();
            }
            var process = new Thread(
                    () =>
                    {

                        matchLost = !FindPattern(susShot.image, needle, sensitivity);
                    }
                    );
            process.Start();

            Shot lastShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds); ;
            
            while (!matchLost)//still working
            {
                if (timeouEnabled && sw.ElapsedMilliseconds > timeoutValue)
                {
                    return -1;
                }
                //lastShot = new Shot(BitmapScreenshot(screen), (int)sw.ElapsedMilliseconds);
                feed.Enqueue(lastShot);
                WriteDebugInfo(new Report("System", Status.Debug, 0, "shot time = "+ lastShot.time));

                if (!process.IsAlive)
                {

                    if (matchLost)//ended in meantime
                    {
                        return susShot.time;
                    }
                    //not found
                    WriteDebugInfo(new Report("System", Status.Debug, 0, "susshot: " + susShot.time + ", top of feed: " + feed.Peek().time));

                    while (feed.Peek().time <= susShot.time)
                    {
                        WriteDebugInfo(new Report("System", Status.Debug, 0, "removing trash: " + feed.Peek().time));
                        feed.Dequeue();
                    }
                    //WriteToConsole("Thread Dead");
                    susShot = lastShot;
                    process = new Thread(
                        () =>
                        {
                            matchLost = !FindPattern(susShot.image, needle, sensitivity); 
                        }
                        );
                    process.Start();
                }
                else
                {
                    //WriteToConsole("Thread Alive");
                }
                //thread alive - keep working
                lastShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);


            }
            //match found
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Returning time: " + susShot.time +", feed min time: " + feed.Peek().time));
            return susShot.time;

        }
        /// <summary>
        /// Returns timestamp of the firt picture, that has needle in it. Leaves the picture at the top of the Queue
        /// </summary>
        /// <param name="feed"></param>
        /// <param name="needle"></param>
        /// <param name="sensitivity"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private int FindFrame(ref Queue<Shot> feed, Mat needle, double sensitivity)
        {
            Shot suspect;
            while (feed.Count > 0)
            {
                suspect = feed.Peek();
                if (FindPattern(suspect.image, needle, sensitivity))
                {
                    return suspect.time;
                }
                feed.Dequeue();
            }
            throw new Exception("Error 1: Empty feed, or feed without expected result passed into function");
        } // obsolete
        private int FindEndOfFirstPicture(ref Queue<Shot> feed, double sensitivity)
        {
            while (feed.Count >= 2)
            {
                if (!FindPattern(feed.Dequeue().image, feed.Peek().image, sensitivity))
                {
                    return feed.Peek().time;
                }
            }
            return feed.Peek().time;
        }

        /// <summary>
        /// Returns timestamp of the first image after needle disappears. Leaves the picture at the top of the Queue
        /// </summary>
        /// <param name="feed"></param>
        /// <param name="needle"></param>
        /// <param name="sensitivity"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private int FindFrameAfterNeedleDissappears(ref Queue<Shot> feed, Mat needle, double sensitivity)
        {
            Shot suspect;
            while (feed.Count > 0)
            {
                suspect = feed.Peek();
                if (!FindPattern(suspect.image, needle, sensitivity))
                {
                    return suspect.time;
                }
                feed.Dequeue();
            }
            throw new Exception("Error 2: Empty feed, or feed without expected result passed into function");
        }
        private int FindFrameAfterNeedleAppears(ref Queue<Shot> feed, Mat needle, double sensitivity)
        {
            Shot suspect;
            while (feed.Count > 0)
            {
                suspect = feed.Peek();
                if (FindPattern(suspect.image, needle, sensitivity))
                {
                    return suspect.time;
                }
                feed.Dequeue();
            }
            return -1;
        }

        private Queue<Shot> SeparateSuspects(ref Queue<Shot> from, ref Queue<Shot> to, int maxTimeValue)
        {
            Queue<Shot> copy = new Queue<Shot>();
            while (from.Peek().time <= maxTimeValue)
            {
                if (from.Count == 1) //match on last picture
                {
                    to.Enqueue(from.Peek());
                    copy.Enqueue(from.Peek());
                    return copy;

                }
                Shot first = from.Dequeue();
                to.Enqueue(first);
                copy.Enqueue(first);
            }
            while (from.Count>0)
            {
                copy.Enqueue(from.Dequeue());
            }
            return copy;

        }
#endregion

        #region Run Option
        private void RunLogin() //later
        {
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Login not implemented"));
        }
        private void RunSilent_Login()//later
        {
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Silent_Login not implemented"));
        }
        private void RunSuccess_Login(int repeats)
        {
            string segmentName = "Success login time";
            WriteDebugInfo(new Report("System", Status.Debug, 0, segmentName + " in progress"));

            var needle = GetTemplate(Template.HomePagePlay);
            var loadingScreen = GetTemplate(Template.LoginProcessingScreen);
            Stopwatch sw = Stopwatch.StartNew();
            var feed = new Queue<Shot>();
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;


            for (int i = 0; i < increment; i++)
            {
                STBLogOut();
                STBLogin("Rotor_29");
                sw.Restart();
                int maxTimeToLoad = GetMaxTimeOfAppearing(loadingScreen, new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), loginProcessingScreenSensitivity, sw, ref feed, true, loginLoadingScreenTimeOutValue);
                if (maxTimeToLoad == -1)//loading screen not recognised
                {
                    if (FindPattern(GetPrintscreenMat(), needle, homePagePlaySensitivity))//the app has already loaded
                    {
                        i--;
                        feed.Clear();
                        continue;
                    }

                    //error while loading
                    feed.Clear();
                    ShellSingleCommand(RemoteKey.Ok);//try to launch again
                    sw.Restart();
                    maxTimeToLoad = GetMaxTimeOfAppearing(loadingScreen, new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), loginProcessingScreenSensitivity, sw, ref feed, true, 20000);
                    if (maxTimeToLoad == -1)//loading screen not recognised
                    {
                        if (FindPattern(GetPrintscreenMat(), needle, homePagePlaySensitivity))//the app has already loaded
                        {
                            i--;
                            feed.Clear();
                            continue;
                        }
                        STBRecover();
                        return;
                    }
                }
                int maxTimeToFocus = GetMaxTimeOfAppearing(needle, new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), playbackEmptyScreenSensitivity, sw, ref feed, true, loginTimeOutValue);////
                if (maxTimeToFocus == -1)
                {
                    STBRecover();
                    return;
                }
                int x = FindFrameAfterNeedleAppears(ref feed, needle, homePagePlaySensitivity);
                if (x == -1) //needle not found
                {
                    totalFailures++;
                    feed.Clear();
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        return;
                    }
                    continue;
                }
                results.Add(new Report(segmentName, Status.Success, x, ""));
                totalSuccesses++;
                if (totalSuccesses == repeats)
                {
                    break;
                }
                if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                {
                    break;
                }
                feed.Clear();
            }
        }
        private void RunPurchase_Time(int repeats)
        {
            string segmentName = "Purchase_Time";
            WriteDebugInfo(new Report(segmentName, Status.Debug, 0, "in progress"));
            STBGoToHomePage();


            if (!STBGoToVOD())
            {
                WriteDebugInfo(new Report(segmentName, Status.Debug, 0, "Cannot find VOD"));
                return;
            }
            ShellSingleCommand(RemoteKey.Ok);//try to find unpurchased item
            WaitXMilliseconds(3000);
            int fails = 0;
            while (FindPattern(GetPrintscreenMat(), GetTemplate(Template.Continue), homePagePlaySensitivity))
            {
                if (fails > 8)
                {
                    WriteDebugInfo(new Report(segmentName, Status.Debug, 0, "Cannot find unpurchased item. Terminating " + segmentName));
                    return;
                }
                STBVODNextUnpurchased(0);
                fails++;
            }//unpurchased item found

            Stopwatch sw = Stopwatch.StartNew();
            var feed = new Queue<Shot>();
            var suspects = new Queue<Shot>();
            var pauseButton = GetTemplate(Template.StopButton);
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                STBPutchaseThis();
                sw.Restart();

                int stage2 = GetMaxTimeOfAppearing(pauseButton, new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds),
                                      firstPictureSensitivity, sw, ref feed, true, playbackTimeoutValue);
                if (stage2 == -1)
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();

                    STBVODNextUnpurchased(0);
                    continue;
                }

                int result = FindFrameAfterNeedleAppears(ref feed, pauseButton, pausePlaySensitivity);
                Report r = new Report(segmentName, Status.Success, result, "");
                results.Add(r);
                WriteDebugInfo(r);
                STBVODNextUnpurchased(0);
                totalSuccesses++;
                if (repeats == totalSuccesses)
                {
                    break;
                }
                feed.Clear();
            }
            WriteDebugInfo(reportDone);
        }

        private void RunPlayback_From_HeroChannelRail(int repeats)
        {
            string segmentName = "Playback start time (from HeroChannelRail)";
            WriteDebugInfo(new Report("System", Status.Debug, 0, segmentName + " in progress"));

            Mat playbackPauseButton = GetTemplate(Template.StopButton);
            Mat blackScreen = GetTemplate(Template.PlaybackStage1);
            string stage1 = "_Stage1";
            string stage2 = "_Stage2";
            STBGoToHomePage();
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Starting observation"));

            Queue<Shot> feed = new Queue<Shot>();
            Queue<Shot> EndStack = new Queue<Shot>(); //will contain areas around first picture
            Stopwatch sw = Stopwatch.StartNew();
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            int timeoutErrors = 0;
            for (int i = 0; i < increment; i++)
            {
                STBStartHeroChannelRailFromHomePage();

                sw.Restart();

                Shot mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int prestage = GetMaxTimeOfAppearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw,ref feed, true, playbackTimeoutValue);
                if (prestage == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded " + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded " + playbackTimeoutValue.ToString() + " seconds."));
                    timeoutErrors++;
                    if (timeoutErrors > 3)
                    {
                        ShellSingleCommand(RemoteKey.Ok);
                        WaitXMilliseconds(10000);
                        timeoutErrors= 0;   
                    }
                    

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();

                    STBGoToHomePage();
                    continue;
                }
                //black screen appeared, look for first picture
                mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage1 = GetMaxTimeOfDissappearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
                if (prestage == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded " + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded " + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();

                    STBGoToHomePage();
                    continue;
                }
                feed = SeparateSuspects(ref feed, ref EndStack, endStage1);

                //first picture found, look for controls
                mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage2 = GetMaxTimeOfAppearing(playbackPauseButton, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
                if (endStage2 == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress( ref i, ref lastSuccesses,  totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();

                    STBGoToHomePage();
                    continue;
                }
                //measure here

                int stage1Time = FindFrameAfterNeedleDissappears(ref EndStack, blackScreen, firstPictureSensitivity);
                if (stage1Time == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Black screen not in feed"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Black screen not in feed"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();

                    STBGoToHomePage();
                    continue;
                }
                while (feed.Count > 0)
                {
                    EndStack.Enqueue(feed.Dequeue());
                }
                int stage2Time = FindFrameAfterNeedleAppears(ref EndStack, playbackPauseButton, this.playbackEmptyScreenSensitivity);
                if (stage2Time == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Pause Button not in feed"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Pause Button not in feed"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    STBGoToHomePage();
                    continue;
                }
                if (stage2Time < stage1Time)//start was black
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Stage 1 ended after Stage 2"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Stage 1 ended after stage 2"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();

                    STBGoToHomePage();
                    continue;
                }
                if (stage2Time < 1000)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Suspected error: Stage 2 ended too fast"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Suspected error: Stage 2 ended too fast"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();

                    STBGoToHomePage();
                    continue;
                }
                WriteResults(new Report(segmentName + stage1, Status.Success, stage1Time, ""));
                WriteResults(new Report(segmentName + stage2, Status.Success, stage2Time, ""));


                totalSuccesses++;
                if (totalSuccesses == repeats)
                {
                    break;
                }
                if(CheckForStalledProgress(ref i, ref lastSuccesses,  totalSuccesses,  totalFailures, increment))
                {
                    break;
                }

                STBGoToHomePage();
                feed.Clear();
                EndStack.Clear();

            } //repeat measuring

            WriteDebugInfo(reportDone);

        } //repeat done
        private void RunEPG_To_HomePage(int repeats)
        {
            string segmentName = "Home page ready (from EPG)";
            WriteDebugInfo(new Report("System", Status.Fail, 0, segmentName + " in progress"));

            STBSetToEPGNow();
            WaitXMilliseconds(10000);

            var homePagePlayButton = GetTemplate(Template.HomePagePlay);
            Queue<Shot> feed = new Queue<Shot>();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                if (chosenCountyPrefix == "SK")
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Back, RemoteKey.Back });

                }
                else 
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.HomePage });
                }
                sw.Restart();

                int stage1End = GetMaxTimeOfAppearing(homePagePlayButton, new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds),
                                                      homePagePlaySensitivity, sw, ref feed, true, homePageTimeOutValue);
                if (stage1End == -1)
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "Timeout error: Load time exceeded " + homePageTimeOutValue.ToString() + " seconds."));

                    totalFailures++;
                    if(CheckForStalledProgress(ref i,ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    STBSetToEPG();
                    continue;
                }
                int result = FindFrameAfterNeedleAppears(ref feed, homePagePlayButton, homePagePlaySensitivity);
                if (result == -1)
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "Format error: Needle not in feed"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    STBGoToHomePage();
                    continue;
                }
                WriteResults(new Report(segmentName, Status.Success, result, ""));
                
                totalSuccesses++;
                if (totalSuccesses == repeats)
                {
                    break;
                }
                if(CheckForStalledProgress(ref i, ref   lastSuccesses, totalSuccesses, totalFailures, increment))
                {
                    break;
                }


                feed.Clear();
                STBSetToEPG();


            }
            //measure here

            WriteDebugInfo(reportDone);

        } //repeat done

        private void RunPlayback_Start_From_Live(int repeats)
        {
            string segmentName = "Playback start time (LIVE from EPG)";
            string stage1 = "_Stage1";
            string stage2 = "_Stage2";
            var blackScreen = GetTemplate(Template.PlaybackStage1);
            WriteDebugInfo(new Report("System", Status.Debug, 0, segmentName + " in progress"));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var playbackPauseButton = GetTemplate(Template.StopButton);
            var feed = new Queue<Shot>();
            var EndStack = new Queue<Shot>();
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            STBStartFirstChannel();

            for (int i = 0; i < increment; i++)
            {
                if (!STBGoOneChannelUpNowLongWay(0))//try to go to next channel
                {
                    //cannot play another channel
                    WriteResults(new Report(segmentName, Status.Debug, 0, " process error: Cannot go one channel up"));
                    return;
                }


                sw.Restart();

                Shot mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int prestage = GetMaxTimeOfAppearing(blackScreen, mostRecentShot, 0.85, sw, ref feed, true, playbackTimeoutValue);
                if (prestage == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                //black screen appeared, look for first picture
                mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage1 = GetMaxTimeOfDissappearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
                if (prestage == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                feed = SeparateSuspects(ref feed, ref EndStack, endStage1);

                //first picture found, look for controls
                mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage2 = GetMaxTimeOfAppearing(playbackPauseButton, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
                if (endStage2 == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                //measure here

                int stage1Time = FindFrameAfterNeedleDissappears(ref EndStack, blackScreen, firstPictureSensitivity);
                if (stage1Time == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Black screen not in feed"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Black screen not in feed"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                while (feed.Count > 0)
                {
                    EndStack.Enqueue(feed.Dequeue());
                }
                int stage2Time = FindFrameAfterNeedleAppears(ref EndStack, playbackPauseButton, this.playbackEmptyScreenSensitivity);
                if (stage2Time == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Pause Button not in feed"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Pause Button not in feed"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                if (stage2Time < stage1Time)//start was black
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Stage 1 ended after Stage 2"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Stage 1 ended after stage 2"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                if (stage2Time < 1000)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Suspected error: Stage 2 ended too fast"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Suspected error: Stage 2 ended too fast"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                WriteResults(new Report(segmentName + stage1, Status.Success, stage1Time, ""));
                WriteResults(new Report(segmentName + stage2, Status.Success, stage2Time, ""));


                totalSuccesses++;
                if (totalSuccesses == repeats)
                {
                    break;
                }
                if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                {
                    break;
                }

                feed.Clear();
                EndStack.Clear();

            } //repeat measuring

            WriteDebugInfo(reportDone);
        }
        
        /// <summary>
        /// appliable only in EPG. Returns EPG to section yesterday during catchup sessions
        /// </summary>
        private void RunPlayback_Start_From_Catchup(int repeats)
        {
            string segmentName = "Playback start time (Catchup from EPG)";
            string stage1 = "_Stage1";
            string stage2 = "_Stage2";
            WriteDebugInfo(new Report("System", Status.Debug, 0, segmentName + " in progress"));


            Stopwatch sw = new Stopwatch();
            sw.Start();
            var blackScreen = GetTemplate(Template.PlaybackStage1);
            var playbackPauseButton = GetTemplate(Template.StopButton);
            var feed = new Queue<Shot>();
            var EndStack = new Queue<Shot>();
            ShellCommand(new RemoteKey[] { RemoteKey.Ok }); //start playback

            STBGoToFirstChannelYesterday();

            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                if (!STBGoOneChannelUp(VideoOrigin.Catchup, 0))
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Process error: cannot find next channel" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Process error: cannot find next channel" + playbackTimeoutValue.ToString() + " seconds."));
                    return;
                }
                sw.Restart();
                Shot mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int prestage = GetMaxTimeOfAppearing(blackScreen, mostRecentShot, 0.85, sw, ref feed, true, playbackTimeoutValue);
                if (prestage == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                //black screen appeared, look for first picture
                mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage1 = GetMaxTimeOfDissappearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
                if (prestage == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                feed = SeparateSuspects(ref feed, ref EndStack, endStage1);

                //first picture found, look for controls
                mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage2 = GetMaxTimeOfAppearing(playbackPauseButton, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
                if (endStage2 == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                //measure here

                int stage1Time = FindFrameAfterNeedleDissappears(ref EndStack, blackScreen, firstPictureSensitivity);
                if (stage1Time == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Black screen not in feed"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Black screen not in feed"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                while (feed.Count > 0)
                {
                    EndStack.Enqueue(feed.Dequeue());
                }
                int stage2Time = FindFrameAfterNeedleAppears(ref EndStack, playbackPauseButton, this.playbackEmptyScreenSensitivity);
                if (stage2Time == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Pause Button not in feed"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Pause Button not in feed"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                if (stage2Time < stage1Time)//start was black
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Stage 1 ended after Stage 2"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Stage 1 ended after stage 2"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                if (stage2Time < 1000)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Suspected error: Stage 2 ended too fast"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Suspected error: Stage 2 ended too fast"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                WriteResults(new Report(segmentName + stage1, Status.Success, stage1Time, ""));
                WriteResults(new Report(segmentName + stage2, Status.Success, stage2Time, ""));


                totalSuccesses++;
                if (totalSuccesses == repeats)
                {
                    break;
                }
                if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                {
                    break;
                }

                feed.Clear();
                EndStack.Clear();

            } //repeat measuring

            WriteDebugInfo(reportDone);

        }

        /// <summary>
        /// Go to TVOption before starting this method
        /// </summary>
        /// <param name="origin"></param>

        private void RunPlayback_Resume_Time(VideoOrigin origin, int repeats)
        {
            if (origin == VideoOrigin.Live)
            {
                string segmentName = "Playback resume time (LIVE)";
                string stage1 = "_Stage1";
                string stage2 = "_Stage2";
                WriteDebugInfo(new Report("System", Status.Debug, 0, segmentName + " in progress"));
                STBStartFirstChannel();

                Stopwatch sw = new Stopwatch();
                Queue<Shot> feed = new Queue<Shot>();
                var EndStack = new Queue<Shot>();
                var playbackPauseButton = GetTemplate(Template.StopButton);
                var blackScreen = GetTemplate(Template.PlaybackStage1);
                sw.Start();
                int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
                for (int i = 0; i < increment; i++)
                {
                    STBGoOneChannelUp(VideoOrigin.Live, 0);
                    _STBGoToPause();
                    WriteDebugInfo(new Report("System", Status.Debug, 0, "Imposing " + playPauseWaitTime / 1000 + " seconds wait time"));
                    WaitXMilliseconds(playPauseWaitTime);
                    _STBRestartPlayback(ref sw);
                    sw.Restart();
                    Shot mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                    int prestage = GetMaxTimeOfAppearing(blackScreen, mostRecentShot, 0.85, sw, ref feed, true, playbackTimeoutValue);
                    if (prestage == -1)
                    {
                        WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                        WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }

                        feed.Clear();
                        EndStack.Clear();
                        continue;
                    }
                    //black screen appeared, look for first picture
                    mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                    int endStage1 = GetMaxTimeOfDissappearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
                    if (prestage == -1)
                    {
                        WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                        WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }

                        feed.Clear();
                        EndStack.Clear();
                        continue;
                    }
                    feed = SeparateSuspects(ref feed, ref EndStack, endStage1);

                    //first picture found, look for controls
                    mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                    int endStage2 = GetMaxTimeOfAppearing(playbackPauseButton, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
                    if (endStage2 == -1)
                    {
                        WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                        WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }

                        feed.Clear();
                        EndStack.Clear();
                        continue;
                    }
                    //measure here

                    int stage1Time = FindFrameAfterNeedleDissappears(ref EndStack, blackScreen, firstPictureSensitivity);
                    if (stage1Time == -1)
                    {
                        WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Black screen not in feed"));
                        WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Black screen not in feed"));

                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }

                        feed.Clear();
                        EndStack.Clear();
                        continue;
                    }
                    while (feed.Count > 0)
                    {
                        EndStack.Enqueue(feed.Dequeue());
                    }
                    int stage2Time = FindFrameAfterNeedleAppears(ref EndStack, playbackPauseButton, this.playbackEmptyScreenSensitivity);
                    if (stage2Time == -1)
                    {
                        WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Pause Button not in feed"));
                        WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Pause Button not in feed"));

                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }

                        feed.Clear();
                        EndStack.Clear();
                        continue;
                    }
                    if (stage2Time < stage1Time)//start was black
                    {
                        WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Stage 1 ended after Stage 2"));
                        WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Stage 1 ended after stage 2"));

                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }

                        feed.Clear();
                        EndStack.Clear();
                        continue;
                    }
                    if (stage2Time < 1000)
                    {
                        WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Suspected error: Stage 2 ended too fast"));
                        WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Suspected error: Stage 2 ended too fast"));

                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }

                        feed.Clear();
                        EndStack.Clear();
                        continue;
                    }
                    WriteResults(new Report(segmentName + stage1, Status.Success, stage1Time, ""));
                    WriteResults(new Report(segmentName + stage2, Status.Success, stage2Time, ""));


                    totalSuccesses++;
                    if (totalSuccesses == repeats)
                    {
                        break;
                    }
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();

                } //repeat measuring

                WriteDebugInfo(reportDone);
                //in report indicate it is from live
                return;
            }
            if (origin == VideoOrigin.Catchup)
            {
                string segmentName = "Playback resume time (Catchup)";
                WriteDebugInfo(new Report("System", Status.Debug, 0, segmentName + " in progress"));
                STBResetEPGToDefault();
                STBGoToFirstChannelYesterday();
                

                Stopwatch sw = new Stopwatch();
                Queue<Shot> feed = new Queue<Shot>();
                var playbackStopButton = GetTemplate(Template.StopButton);
                sw.Start();
                int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
                for (int i = 0; i < increment; i++)
                {
                    STBGoOneChannelUp(VideoOrigin.Catchup, 0);
                    _STBGoToPause();
                    WriteDebugInfo(new Report("System", Status.Debug, 0, "Imposing " + playPauseWaitTime / 1000 + " seconds wait time"));
                    WaitXMilliseconds(playPauseWaitTime);
                    _STBRestartPlayback(ref sw);

                    int endStage1 = GetMaxTimeOfAppearing(playbackStopButton, new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);
                    if (endStage1 == -1)
                    {
                        WriteResults(new Report(segmentName, Status.Fail, 0, "Timeout error: Load time exceeded" + (playbackTimeoutValue / 1000).ToString() + " seconds."));
                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }
                        feed.Clear();

                        STBGoOneChannelUp(VideoOrigin.Catchup, 0);
                        continue;
                    }
                    int result = FindFrameAfterNeedleAppears(ref feed, playbackStopButton, playbackEmptyScreenSensitivity);
                    if (result == -1)
                    {
                        WriteResults(new Report(segmentName, Status.Fail, 0, "Format error: Needle not in feed"));

                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }

                        feed.Clear();
                        STBGoToHomePage();
                        continue;
                    }
                    WriteResults(new Report(segmentName, Status.Success, result, ""));
                    feed.Clear();
                    totalSuccesses++;
                    if (totalSuccesses == repeats)
                    {
                        break;
                    }
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }
                    STBGoOneChannelUp(VideoOrigin.Catchup, 0);
                    
                }


                WriteDebugInfo(reportDone);
                return;
                
            }

        }
        
        private void RunZapping(int repeats)
        {
            string segmentName = "Zapper start time";
            string stage1 = "_Stage1";
            string stage2 = "_Stage2";

            WriteDebugInfo(new Report("System", Status.Debug, 0, segmentName + " in progress"));

            STBStartFirstChannel();
            
            Stopwatch sw = new Stopwatch();
            sw.Start();

  
            var playbackPauseButton = GetTemplate(Template.StopButton);
            var blackScreen = GetTemplate(Template.PlaybackStage1);
            var feed = new Queue<Shot>();
            var EndStack = new Queue<Shot>();
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                ShellCommand(new RemoteKey[] { RemoteKey.ChannelUp });
                sw.Restart();
                Shot mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int prestage = GetMaxTimeOfAppearing(blackScreen, mostRecentShot, 0.85, sw, ref feed, true, playbackTimeoutValue);
                if (prestage == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                //black screen appeared, look for first picture
                mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage1 = GetMaxTimeOfDissappearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
                if (prestage == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                feed = SeparateSuspects(ref feed, ref EndStack, endStage1);

                //first picture found, look for controls
                mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage2 = GetMaxTimeOfAppearing(playbackPauseButton, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
                if (endStage2 == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                //measure here

                int stage1Time = FindFrameAfterNeedleDissappears(ref EndStack, blackScreen, firstPictureSensitivity);
                if (stage1Time == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Black screen not in feed"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Black screen not in feed"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                while (feed.Count > 0)
                {
                    EndStack.Enqueue(feed.Dequeue());
                }
                int stage2Time = FindFrameAfterNeedleAppears(ref EndStack, playbackPauseButton, this.playbackEmptyScreenSensitivity);
                if (stage2Time == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Pause Button not in feed"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Pause Button not in feed"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                if (stage2Time < stage1Time)//start was black
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Format error: Stage 1 ended after Stage 2"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Format error: Stage 1 ended after stage 2"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                if (stage2Time < 1000)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Suspected error: Stage 2 ended too fast"));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Suspected error: Stage 2 ended too fast"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    EndStack.Clear();
                    continue;
                }
                WriteResults(new Report(segmentName + stage1, Status.Success, stage1Time, ""));
                WriteResults(new Report(segmentName + stage2, Status.Success, stage2Time, ""));


                totalSuccesses++;
                if (totalSuccesses == repeats)
                {
                    break;
                }
                if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                {
                    break;
                }

                feed.Clear();
                EndStack.Clear();

            } //repeat measuring

            WriteDebugInfo(reportDone);
        }
        #endregion

        #region BUTTON_CLICK

        private void STBIPInputOkButton_Click(object sender, EventArgs e)
        {
            if (IPAddress.TryParse(STBIPInputBox.Text, out var address))
            {
                IP = address.ToString();
                if (!ConnectToSTB())
                {
                    STBIPLabel.Text = "Cannot connect to " + IP;
                    STBIPLabel.ForeColor = Color.Red;
                    return;
                } //connected
                DeviceDetailsSetUp();
                Directory.CreateDirectory(filePath + "\\data");
                using(StreamWriter sw = new StreamWriter(filePath + "\\data\\STBIP.txt"))
                {
                    sw.Write(IP);
                    sw.Flush();
                    sw.Close();
                }
                STBIPLabel.Text = "Input STB IP";
                STBIPLabel.ForeColor = Color.Empty;
            }
            else
            {
                STBIPLabel.Text = "Invalid IP format";
                STBIPLabel.ForeColor = Color.Red;
            }
        }

        private void ChooseCountryOkButton_Click(object sender, EventArgs e)
        {
            if (LanguageDropdown.SelectedItem == null) //no option chosen - Country
            {
                ChooseCountryLabel.Text = "No country selected"; //Error field
                ChooseCountryLabel.ForeColor = System.Drawing.Color.Red;
                return;
            } //country

            country = (Country)LanguageDropdown.SelectedItem;
            chosenCountry = country;
            switch (country)
            {
                case Country.Czechia: chosenCountyPrefix = "CZ";
                    break;
                case Country.Bulgaria: chosenCountyPrefix = "BG";
                    break;
                case Country.Serbia: chosenCountyPrefix = "RS";
                    break;
                case Country.Slovakia: chosenCountyPrefix = "SK";
                    break;
                case Country.Hungary: chosenCountyPrefix = "HU";
                    break;
                default: throw new NotImplementedException("Error 4: Unknown BU");
            }
            ChooseEnvironmentLabel.Text = "Choose country";
            ChooseEnvironmentLabel.ForeColor = Color.Empty;

            ScreenSetUp();
            
        }

        private void ChooseEnvironmentOkButton_Click(object sender, EventArgs e)
        {

            if (TestEnvironmentOptions.SelectedItem == null) //no option chosen
            {
                ChooseEnvironmentLabel.Text = "No option selected"; //Error field
                ChooseEnvironmentLabel.ForeColor = Color.Red;
                return;
            }
            ChosenDevice = (Device)TestEnvironmentOptions.SelectedItem;
            ChooseEnvironmentLabel.Text = "Choose Environment";
            ChooseEnvironmentLabel.ForeColor = Color.Empty;
            if (ChosenDevice == Device.STB) //STB setup
            {
                STBIPSetUp();
                return;
            }
            DeviceDetailsSetUp();
             //country option
            return;


        }

        private void OptionsConfirmButton_Click(object sender, EventArgs e)
        {

            if (Options.CheckedItems.Count == 0)
            {
                return;
            }
            int iterations, cycles;
            if (!int.TryParse(OptionsIterationValueTextBox.Text, out iterations) || iterations < 1)
            {
                iterationValueLabel.Text = "Invalid number of iterations";
                iterationValueLabel.ForeColor = Color.Red;
                return;
            }
            if (!int.TryParse(cycleValueTextBox.Text, out cycles) || cycles < 1)
            {
                cycleValueLabel.Text = "Invalid number of cycles";
                cycleValueLabel.ForeColor = Color.Red;
                return;
            }
            cycleValueLabel.Text = "Required number of cycles";
            cycleValueLabel.ForeColor = Color.Empty;
            iterationValueLabel.Text = "Required amount of iterations per cycle";
            iterationValueLabel.ForeColor = Color.Empty;

            foreach (var item in Options.CheckedItems)
            {
                switch (item.ToString())
                {
                    //case "Login": Login = true; break;
                    //case "Silent_Login": Silent_Login = true; break;
                    case "Playback_start_time_from_HeroChannelRail": Playback_start_time_from_HeroChannelRail = true; break;
                    case "Home_page_ready_from_EPG": Home_page_ready_from_EPG = true; break;
                    case "Playback_start_time_LIVE_from_EPG": Playback_start_time_LIVE_from_EPG = true; break;
                    case "Playback_start_time_Catchup_from_EPG": Playback_start_time_Catchup_from_EPG = true; break;
                    case "Playback_resume_time": Playback_resume_time = true; break;
                    case "Zapper_start_time": Zapper_start_time = true; break;
                    case "Success_Login_Time": Success_Login_Time= true; break;
                    case "Purchase_Time": Purchase_time= true; break;
                    default: throw new NotImplementedException();
                }
            }
            this.Hide();
            SetUpLogs();

            results = new ResultsStorage(iterations*cycles, filePath, chosenCountry);
            HideAll();

            #region options switch   
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Starting measurement"));
            STBGoToHomePage();
 
            for (int i = 1; i < cycles + 1; i++) //cycle through chosen measures
            {
                WriteDebugInfo(new Report("System", Status.Debug, 0, "Starting cycle " + i));

                if (Login)
                {
                    RunLogin(); //later
                }
                if (Silent_Login)
                {
                    RunSilent_Login(); //later
                }
                if (Playback_start_time_from_HeroChannelRail)
                {
                    RunPlayback_From_HeroChannelRail(iterations);
                }
                if (Home_page_ready_from_EPG)
                {
                    RunEPG_To_HomePage(iterations);
                }
                if (Playback_start_time_LIVE_from_EPG)
                {
                    RunPlayback_Start_From_Live(iterations);
                }
                if (Playback_resume_time) // from live
                {
                    RunPlayback_Resume_Time(VideoOrigin.Live, iterations);
                }
                if (Playback_start_time_Catchup_from_EPG)
                {
                    RunPlayback_Start_From_Catchup(iterations);
                }
                if (Playback_resume_time) //from catch-up
                {
                    RunPlayback_Resume_Time(VideoOrigin.Catchup, iterations);
                }
                if (Zapper_start_time)
                {
                    RunZapping(iterations);
                }
                if(Purchase_time) 
                {
                    RunPurchase_Time(iterations);
                }
            }
            if (Success_Login_Time)
            {
                for (int i = 0; i < cycles; i++)
                {

                    RunSuccess_Login(iterations);
                }
            }


            #endregion
            results.dump(ChosenDevice.ToString() + "_" + chosenCountyPrefix, GetAppVersion() + "_" + DateTime.Now.ToString("yyyy.MM.dd-hh.mm.ss"),
                debugLog, deviceInfo);
            ChooseCountryLabel.Text = results.GetDataPath();
            ChooseCountryLabel.Visible = true;
            CloseLogs();
            this.Show();

        }
        private void ChooseThisScreenButton_Click(object sender, EventArgs e)
        {
            chosenScreen = temporaryScreen;
            temporaryScreen = null;
            OptionsSetUp();
            //screenShowBox.Image = BitmapScreenshot(screen);

        } //done

        private void ScreenChooseNextButton_Click(object sender, EventArgs e)
        {
            Screen[] screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i] == temporaryScreen)
                {
                    if (screens.Length > i + 1)
                    {
                        temporaryScreen = screens[i + 1];
                        screenShowBox.Image = BitmapScreenshot(temporaryScreen);
                        ScreenShowPreviousButton.Enabled = true;
                        if (Screen.AllScreens.Length == i + 2)
                        {
                            ScreenChooseNextButton.Enabled = false;
                        }
                        return;
                    }
                }
            }
        }
        private void deviceDetailsConfirmButton_Click(object sender, EventArgs e)
        {
            if (textBox2.Text == "NG")
            {
                homePageButtonPressWaitTime = 30000;
            }
            StoreDeviceDetails();
            CountrySetUp();
        }

        private void ScreenShowPreviousButton_Click(object sender, EventArgs e)
        {
            Screen[] screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i] == temporaryScreen)
                {
                    if (i > 0)
                    {
                        temporaryScreen = screens[i - 1];
                        screenShowBox.Image = BitmapScreenshot(temporaryScreen);
                        ScreenChooseNextButton.Enabled = true;
                        if (i - 1 == 0)
                        {
                            ScreenShowPreviousButton.Enabled = false;
                        }
                        return;
                    }
                }
            }
        }
        private void TakeScreenshotButton_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < 60; i++)
            {
                var bmp = BitmapScreenshot(Screen.AllScreens[1]);
                bmp.Save(filePath + "\\templates3\\" + chosenCountyPrefix + i.ToString() + ".png");
            }
        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            
            

            //chosenScreen = Screen.AllScreens[1];
            ChosenDevice = Device.STB;
            //IP = "192.168.1.156"; //O2 ZTE STB
            IP = "10.0.0.27"; //O2 NG STB
            //IP = "10.20.40.74"; //HU STB - connect to TLN RS
            //IP = "192.168.1.151"; //RS STB - connect to Yettel
            //IP = "10.20.40.75"; // BG STB - connect to TLN RS
            //IP = "10.0.1.40";//SK

            NativeMethods.AllocConsole();
            SetUpLogs();

            //ConnectToSTB();

            chosenCountyPrefix = "CZ";
            ////NavigateGrid("leos.mitacek@cetin.cz", true);
            //NavigateGrid("", true);
            //NavigateGrid("Rotor_29", true);
            //ShellSingleCommand(RemoteKey.Ok);
            //ShellSingleCommand(RemoteKey.Ok);
            int repeats = 1;
            results = new ResultsStorage(repeats, filePath, Country.Czechia);
            results.dump("STB_CZ", "now", debugLog, deviceInfo);
            //RunPurchase_Time(repeats);

            //RunPlayback_From_HeroChannelRail(repeats);
            //RunEPG_To_HomePage(repeats);
            //RunEPG_Live(repeats);
            //RunEPG_CatchUp(repeats); //success
            //RunPlay_Pause(VideoOrigin.Live, repeats);
            //RunPlay_Pause(VideoOrigin.Catchup, repeats);
            //RunZapping(repeats);
            //FillResults(ref results); //measured results
            results.dump(ChosenDevice.ToString() + "_" + chosenCountyPrefix, DateTime.Now.ToString("yyyy.MM.dd-hh.mm.ss"),
                debugLog, deviceInfo);
            CloseLogs();

        }
        
        #endregion
        private void FillResults(ref ResultsStorage r)
        {
            StreamReader sr = new StreamReader(filePath + "\\debug\\data.txt");
            for (int i = 0; i < 7; i++)
            {
                var str = sr.ReadLine().Split(" ");
                for (int j = 1; j < 6; j++)
                {
                    int value;
                    int.TryParse(str[j], out value);
                    r.Add(new Report(str[0], Status.Success, value, ""));
                }
            }
            sr.Close();
        }

    }
}

namespace x { }