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
        Playback_start_time_from_HeroChannelRail, Home_page_ready_from_EPG, Home_page_ready_from_recordings,
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
        bool Home_Page_ready_from_recordings = false;
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
        Controller controller;


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
        ImageProcessor imageProcessor;
        


        //private Process p;
        //private Thread process;

        public Main_Window()
        {
            InitializeComponent();
            HideAll();

            AllocConsole();
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
            debugLog.WriteLine(consoleOutput);
            Console.WriteLine(consoleOutput);
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

            var needle = imageProcessor.GetTemplate(Template.HomePagePlay);
            var loadingScreen = imageProcessor.GetTemplate(Template.LoginProcessingScreen);
            Stopwatch sw = Stopwatch.StartNew();
            var feed = new Queue<Shot>();
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;


            for (int i = 0; i < increment; i++)
            {
                controller.LogOut();
                controller.Login("leos.mitacek@cetin.cz", "Rotor_29");
                sw.Restart();
                int maxTimeToLoad = imageProcessor.GetMaxTimeOfAppearing(loadingScreen, new Shot(imageProcessor.BitmapScreenshot(chosenScreen), 
                                    (int)sw.ElapsedMilliseconds), loginProcessingScreenSensitivity, sw, ref feed, true, loginLoadingScreenTimeOutValue);
                if (maxTimeToLoad == -1)//loading screen not recognised
                {
                    if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), needle, homePagePlaySensitivity))//the app has already loaded
                    {
                        i--;
                        feed.Clear();
                        continue;
                    }

                    //error while loading
                    feed.Clear();
                    controller.PressOk();
                    sw.Restart();
                    maxTimeToLoad = imageProcessor.GetMaxTimeOfAppearing(loadingScreen, new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), loginProcessingScreenSensitivity, sw, ref feed, true, 20000);
                    if (maxTimeToLoad == -1)//loading screen not recognised
                    {
                        if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), needle, homePagePlaySensitivity))//the app has already loaded
                        {
                            i--;
                            feed.Clear();
                            continue;
                        }
                        controller.RecoverDevice();
                        return;
                    }
                }
                int maxTimeToFocus = imageProcessor.GetMaxTimeOfAppearing(needle, new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), playbackEmptyScreenSensitivity, sw, ref feed, true, loginTimeOutValue);////
                if (maxTimeToFocus == -1)
                {
                    controller.RecoverDevice();
                    return;
                }
                int x = imageProcessor.FindFrameAfterNeedleAppears(ref feed, needle, homePagePlaySensitivity);
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
            controller.GoToHomePage();


            if (!controller.GoToVOD())
            {
                WriteDebugInfo(new Report(segmentName, Status.Debug, 0, "Cannot find VOD"));
                return;
            }
            controller.PressOk();//try to find unpurchased item
            WaitXMilliseconds(3000);
            int fails = 0;
            while (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), imageProcessor.GetTemplate(Template.Continue), homePagePlaySensitivity))
            {
                if (fails > 8)
                {
                    WriteDebugInfo(new Report(segmentName, Status.Debug, 0, "Cannot find unpurchased item. Terminating " + segmentName));
                    return;
                }
                controller.VODNextUnpurchased(1);
                fails++;
            }//unpurchased item found

            Stopwatch sw = Stopwatch.StartNew();
            var feed = new Queue<Shot>();
            var suspects = new Queue<Shot>();
            var pauseButton = imageProcessor.GetTemplate(Template.StopButton);
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                controller.PutchaseThis();
                sw.Restart();

                int stage2 = imageProcessor.GetMaxTimeOfAppearing(pauseButton, new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds),
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

                    controller.VODNextUnpurchased(0);
                    continue;
                }

                int result = imageProcessor.FindFrameAfterNeedleAppears(ref feed, pauseButton, pausePlaySensitivity);
                Report r = new Report(segmentName, Status.Success, result, "");
                results.Add(r);
                WriteDebugInfo(r);
                controller.VODNextUnpurchased(0);
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

            Mat playbackPauseButton = imageProcessor.GetTemplate(Template.StopButton);
            Mat blackScreen = imageProcessor.GetTemplate(Template.PlaybackStage1);
            string stage1 = "_Stage1";
            string stage2 = "_Stage2";
            controller.GoToHomePage();
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Starting observation"));

            Queue<Shot> feed = new Queue<Shot>();
            Queue<Shot> EndStack = new Queue<Shot>(); //will contain areas around first picture
            Stopwatch sw = Stopwatch.StartNew();
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            int timeoutErrors = 0;
            for (int i = 0; i < increment; i++)
            {
                controller.StartHeroChannelRailFromHomePage();

                sw.Restart();

                Shot mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int prestage = imageProcessor.GetMaxTimeOfAppearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw,ref feed, true, playbackTimeoutValue);
                if (prestage == -1)
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Timeout error: Load time exceeded " + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Timeout error: Load time exceeded " + playbackTimeoutValue.ToString() + " seconds."));
                    timeoutErrors++;
                    if (timeoutErrors > 3)
                    {
                        controller.PressOk();
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

                    controller.GoToHomePage();
                    continue;
                }
                //black screen appeared, look for first picture
                mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage1 = imageProcessor.GetMaxTimeOfDissappearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
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

                    controller.GoToHomePage();
                    continue;
                }
                feed = imageProcessor.SeparateSuspects(ref feed, ref EndStack, endStage1);

                //first picture found, look for controls
                mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage2 = imageProcessor.GetMaxTimeOfAppearing(playbackPauseButton, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
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

                    controller.GoToHomePage();
                    continue;
                }
                //measure here

                int stage1Time = imageProcessor.FindFrameAfterNeedleDissappears(ref EndStack, blackScreen, firstPictureSensitivity);
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

                    controller.GoToHomePage();
                    continue;
                }
                while (feed.Count > 0)
                {
                    EndStack.Enqueue(feed.Dequeue());
                }
                int stage2Time = imageProcessor.FindFrameAfterNeedleAppears(ref EndStack, playbackPauseButton, this.playbackEmptyScreenSensitivity);
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
                    controller.GoToHomePage();
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

                    controller.GoToHomePage();
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

                    controller.GoToHomePage();
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

                controller.GoToHomePage();
                feed.Clear();
                EndStack.Clear();

            } //repeat measuring

            WriteDebugInfo(reportDone);

        } //repeat done
        private void RunEPG_To_HomePage(int repeats)
        {
            string segmentName = "Home page ready (from EPG)";
            WriteDebugInfo(new Report("System", Status.Fail, 0, segmentName + " in progress"));

            controller.SetToEPGNow();
            WaitXMilliseconds(10000);

            var homePagePlayButton = imageProcessor.GetTemplate(Template.HomePagePlay);
            Queue<Shot> feed = new Queue<Shot>();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                controller.SetToEPG();
                controller.PressHomePage();
                sw.Restart();

                int stage1End = imageProcessor.GetMaxTimeOfAppearing(homePagePlayButton, new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds),
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
                    continue;
                }
                int result = imageProcessor.FindFrameAfterNeedleAppears(ref feed, homePagePlayButton, homePagePlaySensitivity);
                if (result == -1)
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "Format error: Needle not in feed"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
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


            }
            //measure here

            WriteDebugInfo(reportDone);

        } //repeat done
        private void RunRecordings_To_HomePage(int repeats)
        {
            string segmentName = "Home page ready (from Recordings)";
            WriteDebugInfo(new Report("System", Status.Fail, 0, segmentName + " in progress"));

            var homePagePlayButton = imageProcessor.GetTemplate(Template.HomePagePlay);
            Queue<Shot> feed = new Queue<Shot>();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                controller.SetToRecordings();
                controller.PressHomePage();
                sw.Restart();



                int stage1End = imageProcessor.GetMaxTimeOfAppearing(homePagePlayButton, new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds),
                                                      homePagePlaySensitivity, sw, ref feed, true, homePageTimeOutValue);
                if (stage1End == -1)
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "Timeout error: Load time exceeded " + homePageTimeOutValue.ToString() + " seconds."));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    continue;
                }
                int result = imageProcessor.FindFrameAfterNeedleAppears(ref feed, homePagePlayButton, homePagePlaySensitivity);
                if (result == -1)
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "Format error: Needle not in feed"));

                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    continue;
                }
                WriteResults(new Report(segmentName, Status.Success, result, ""));

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
            //measure here

            WriteDebugInfo(reportDone);
        }
        private void RunPlayback_Start_From_Live(int repeats)
        {
            string segmentName = "Playback start time (LIVE from EPG)";
            string stage1 = "_Stage1";
            string stage2 = "_Stage2";
            var blackScreen = imageProcessor.GetTemplate(Template.PlaybackStage1);
            WriteDebugInfo(new Report("System", Status.Debug, 0, segmentName + " in progress"));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var playbackPauseButton = imageProcessor.GetTemplate(Template.StopButton);
            var feed = new Queue<Shot>();
            var EndStack = new Queue<Shot>();
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            controller.StartFirstChannel();

            for (int i = 0; i < increment; i++)
            {
                if (!controller.GoOneChannelUpNowLongWay(0))//try to go to next channel
                {
                    //cannot play another channel
                    WriteResults(new Report(segmentName, Status.Debug, 0, " process error: Cannot go one channel up"));
                    return;
                }


                sw.Restart();

                Shot mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int prestage = imageProcessor.GetMaxTimeOfAppearing(blackScreen, mostRecentShot, 0.85, sw, ref feed, true, playbackTimeoutValue);
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
                mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage1 = imageProcessor.GetMaxTimeOfDissappearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
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
                feed = imageProcessor.SeparateSuspects(ref feed, ref EndStack, endStage1);

                //first picture found, look for controls
                mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage2 = imageProcessor.GetMaxTimeOfAppearing(playbackPauseButton, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
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

                int stage1Time = imageProcessor.FindFrameAfterNeedleDissappears(ref EndStack, blackScreen, firstPictureSensitivity);
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
                int stage2Time = imageProcessor.FindFrameAfterNeedleAppears(ref EndStack, playbackPauseButton, this.playbackEmptyScreenSensitivity);
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
            var blackScreen = imageProcessor.GetTemplate(Template.PlaybackStage1);
            var playbackPauseButton = imageProcessor.GetTemplate(Template.StopButton);
            var feed = new Queue<Shot>();
            var EndStack = new Queue<Shot>();

            controller.GoToFirstChannelYesterday();

            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                if (!controller.GoOneChannelUp(VideoOrigin.Catchup, 0))
                {
                    WriteResults(new Report(segmentName + stage1, Status.Fail, 0, "Process error: cannot find next channel" + playbackTimeoutValue.ToString() + " seconds."));
                    WriteResults(new Report(segmentName + stage2, Status.Fail, 0, "Process error: cannot find next channel" + playbackTimeoutValue.ToString() + " seconds."));
                    return;
                }
                sw.Restart();
                Shot mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int prestage = imageProcessor.GetMaxTimeOfAppearing(blackScreen, mostRecentShot, 0.85, sw, ref feed, true, playbackTimeoutValue);
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
                mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage1 = imageProcessor.GetMaxTimeOfDissappearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
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
                feed = imageProcessor.SeparateSuspects(ref feed, ref EndStack, endStage1);

                //first picture found, look for controls
                mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage2 = imageProcessor.GetMaxTimeOfAppearing(playbackPauseButton, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
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

                int stage1Time = imageProcessor.FindFrameAfterNeedleDissappears(ref EndStack, blackScreen, firstPictureSensitivity);
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
                int stage2Time = imageProcessor.FindFrameAfterNeedleAppears(ref EndStack, playbackPauseButton, this.playbackEmptyScreenSensitivity);
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
                controller.StartFirstChannel();

                Stopwatch sw = new Stopwatch();
                Queue<Shot> feed = new Queue<Shot>();
                var EndStack = new Queue<Shot>();
                var playbackPauseButton = imageProcessor.GetTemplate(Template.StopButton);
                var blackScreen = imageProcessor.GetTemplate(Template.PlaybackStage1);
                sw.Start();
                int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
                for (int i = 0; i < increment; i++)
                {
                    controller.GoOneChannelUp(VideoOrigin.Live, 0);
                    controller._GoToPause();
                    WriteDebugInfo(new Report("System", Status.Debug, 0, "Imposing " + playPauseWaitTime / 1000 + " seconds wait time"));
                    WaitXMilliseconds(playPauseWaitTime);
                    controller._RestartPlayback();
                    sw.Restart();
                    Shot mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                    int prestage = imageProcessor.GetMaxTimeOfAppearing(blackScreen, mostRecentShot, 0.85, sw, ref feed, true, playbackTimeoutValue);
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
                    mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                    int endStage1 = imageProcessor.GetMaxTimeOfDissappearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
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
                    feed = imageProcessor.SeparateSuspects(ref feed, ref EndStack, endStage1);

                    //first picture found, look for controls
                    mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                    int endStage2 = imageProcessor.GetMaxTimeOfAppearing(playbackPauseButton, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
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

                    int stage1Time = imageProcessor.FindFrameAfterNeedleDissappears(ref EndStack, blackScreen, firstPictureSensitivity);
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
                    int stage2Time = imageProcessor.FindFrameAfterNeedleAppears(ref EndStack, playbackPauseButton, this.playbackEmptyScreenSensitivity);
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

                controller.ResetEPGToDefault();
                controller.GoToFirstChannelYesterday();
                

                Stopwatch sw = new Stopwatch();
                Queue<Shot> feed = new Queue<Shot>();
                var playbackStopButton = imageProcessor.GetTemplate(Template.StopButton);
                sw.Start();
                int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
                for (int i = 0; i < increment; i++)
                {
                    controller.GoOneChannelUp(VideoOrigin.Catchup, 0);
                    controller._GoToPause();
                    WriteDebugInfo(new Report("System", Status.Debug, 0, "Imposing " + playPauseWaitTime / 1000 + " seconds wait time"));
                    WaitXMilliseconds(playPauseWaitTime);
                    controller._RestartPlayback();
                    sw.Restart();

                    int endStage1 = imageProcessor.GetMaxTimeOfAppearing(playbackStopButton, new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);
                    if (endStage1 == -1)
                    {
                        WriteResults(new Report(segmentName, Status.Fail, 0, "Timeout error: Load time exceeded" + (playbackTimeoutValue / 1000).ToString() + " seconds."));
                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }
                        feed.Clear();

                        controller.GoOneChannelUp(VideoOrigin.Catchup, 0);
                        continue;
                    }
                    int result = imageProcessor.FindFrameAfterNeedleAppears(ref feed, playbackStopButton, playbackEmptyScreenSensitivity);
                    if (result == -1)
                    {
                        WriteResults(new Report(segmentName, Status.Fail, 0, "Format error: Needle not in feed"));

                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }

                        feed.Clear();
                        controller.GoToHomePage();
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
                    controller.GoOneChannelUp(VideoOrigin.Catchup, 0);
                    
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

            controller.StartFirstChannel();
            
            Stopwatch sw = new Stopwatch();
            sw.Start();

  
            var playbackPauseButton = imageProcessor.GetTemplate(Template.StopButton);
            var blackScreen = imageProcessor.GetTemplate(Template.PlaybackStage1);
            var feed = new Queue<Shot>();
            var EndStack = new Queue<Shot>();
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                controller.GoOneChannelUp(VideoOrigin.Live, 0);
                sw.Restart();
                Shot mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int prestage = imageProcessor.GetMaxTimeOfAppearing(blackScreen, mostRecentShot, 0.85, sw, ref feed, true, playbackTimeoutValue);
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
                mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage1 = imageProcessor.GetMaxTimeOfDissappearing(blackScreen, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
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
                feed = imageProcessor.SeparateSuspects(ref feed, ref EndStack, endStage1);

                //first picture found, look for controls
                mostRecentShot = new Shot(imageProcessor.BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage2 = imageProcessor.GetMaxTimeOfAppearing(playbackPauseButton, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
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

                int stage1Time = imageProcessor.FindFrameAfterNeedleDissappears(ref EndStack, blackScreen, firstPictureSensitivity);
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
                int stage2Time = imageProcessor.FindFrameAfterNeedleAppears(ref EndStack, playbackPauseButton, this.playbackEmptyScreenSensitivity);
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
                    case "Home_page_ready_from_recordings": Home_Page_ready_from_recordings= true; break;
                    default: throw new NotImplementedException();
                }
            }
            bool NG = false;
            if (string.Equals(textBox2.Text, "NG", StringComparison.OrdinalIgnoreCase))
            {
                NG = true;
                homePageTimeOutValue = 30000;
            }
            switch (ChosenDevice)
            {
                
                case Device.STB: controller = new STBController(IP, filePath, debugLog, imageProcessor, chosenCountyPrefix, chosenCountry, chosenScreen, NG);
                    break;
                case Device.AndroidTV: throw new NotImplementedException("Unknown device");
                default:
                    throw new NotImplementedException("Unknown device");
            }
            this.Hide();
            SetUpLogs();

            results = new ResultsStorage(iterations*cycles, filePath, chosenCountry);
            HideAll();

            #region options switch   
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Starting measurement"));
            controller.GoToHomePage();
 
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
                if (Home_Page_ready_from_recordings)
                {
                    RunRecordings_To_HomePage(iterations);
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
            results.dump(ChosenDevice.ToString() + "_" + chosenCountyPrefix, controller.GetAppVersion() + "_" + DateTime.Now.ToString("yyyy.MM.dd-hh.mm.ss"),
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
            imageProcessor = new ImageProcessor(debugLog, chosenCountyPrefix, filePath, chosenScreen);
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
                var bmp = imageProcessor.BitmapScreenshot(Screen.AllScreens[1]);
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