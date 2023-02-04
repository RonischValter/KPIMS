using Emgu.CV;
using Emgu.CV.DepthAI;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing.Design;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml.Schema;
using System.Xml.XPath;
using static System.Net.Mime.MediaTypeNames;

namespace KPI_measuring_software
{
    enum Device {STB } //in the future maybe Browser
    enum Country { Czechia, Bulgaria, Serbia, Slovakia, Hungary}
    enum RemoteKey { Up, Down, Left, Right, Ok, Back, ChannelUp, ChannelDown, One, EPG, HomePage }
    enum Transition { Login, Silent_Login, Play_From_HeroChannelRail, EPG_To_HomePage, 
                      EPG_Live, EPG_CatchUp, Play_Pause, Zapping}
    enum VideoOrigin { Live, Catchup}
    public partial class main_window : Form
    {
        bool Login = false;
        bool Silent_Login = false;
        bool Play_From_HeroChannelRail = false;
        bool EPG_To_HomePage = false;
        bool EPG_Live = false;
        bool EPG_CatchUp = false;
        bool Zapping = false;
        bool Play_Pause = false;
        int imageNumber = 0;
        int rollTOLeft = 7;
        double firstPictureSensitivity = 0.95;
        double homePagePlaySensitivity = 0.95;
        double startOverSensitivity = 0.95;
        double playbackEmptyScreenSensitivity = 0.90;
        double pausePlaySensitivity = 0.98;
        double playButtonSensitivity = 0.95;
        double nowSensitivity = 0.90;
        double watchLiveSensitivity = 0.70;
        string? IP = null;
        Report reportDone;


        int playbackTimeoutValue = 7000;
        int homePageTimeOutValue = 7000;
        int playPauseWaitTime = 10000;
        int loadWaitTime = 3000;
        int homePageButtonPressWaitTime = 7000;
        string chosenCountyPrefix;

        private Screen? chosenScreen = null;
        private Screen? temporaryScreen;
        private Device ChosenDevice;
        private Country country;
        //private StreamWriter resultsStream;
        private StreamWriter debugLog;
        private ResultsStorage results;
        


        //private Process p;
        //private Thread process;

        public main_window()
        {
            InitializeComponent();
            HideAll();
            AllocConsole();
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
            
            ChooseEnvironmentLabel.Text = "Choose device";
            Directory.CreateDirectory("debug");
            reportDone = new Report("System", Status.Debug, 0, "Done");


#if DEBUG
            TestButton.Visible = true;
            TakeScreenshotButton.Visible = true;
#endif


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
            debugLog = new StreamWriter(".\\debug\\-" + DateTime.Now.ToString("yyyy.MM.dd-hh.mm.ss") + ".txt");
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
        private void STBReboot() 
        {
            Process.Start("CMD.exe", "/C adb reboot").WaitForExit();
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();
        private void ConnectToSTB()
        {
            if (IP == null)
            {
                throw new ArgumentNullException("Error 3: IP adress is null");
            }
            Process.Start("CMD.exe", "/C adb connect " + IP).WaitForExit();
            WaitXMilliseconds(1000);
            Process.Start("CMD.exe", "/C adb connect " + IP).WaitForExit();

        }

        #region STB control
        private void WaitXMilliseconds(int waitTimeInMilliseconds)
        {
            Stopwatch sw = Stopwatch.StartNew();
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Waiting " + waitTimeInMilliseconds + " ms"));

            while (sw.ElapsedMilliseconds < waitTimeInMilliseconds)
            {
                Thread.Sleep(1000);
            }
        }

        private void STBSKGoToDefault()
        {
            while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                    CvInvoke.Imread(".\\templates\\SKBack.png"), startOverSensitivity))
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
                    CvInvoke.Imread(".\\templates\\SKnow.png"), startOverSensitivity))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Right });
                }
                ShellCommand(new RemoteKey[] { RemoteKey.Ok });

                return;
            }
            STBSetToEPGNow();
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
                RemoteKey.EPG => "172",
                RemoteKey.HomePage => "3",
                _ => throw new NotImplementedException(),
            };
        }
        private void ShellCommand(RemoteKey[] cmd)
        {

            for (int i = 0; i < cmd.Length; i++)
            {
                string command = "/C adb shell input keyevent " + Translate(cmd[i]);
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Process.Start("CMD.exe", command).WaitForExit();
                WriteDebugInfo(new Report("System", Status.Debug, 0, "Key " + cmd[i].ToString() + " pressed"));
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
            while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat, 
                                CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "HomePagePlay.png"), 
                                playButtonSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.HomePage });
                WaitXMilliseconds(homePageButtonPressWaitTime);

            }
            //ShellCommand(new RemoteKey[] {RemoteKey.Back,RemoteKey.Back,RemoteKey.Back,RemoteKey.Back,
            //                              RemoteKey.Back,RemoteKey.Back,RemoteKey.Back,RemoteKey.Up, RemoteKey.Up, RemoteKey.Up});
        }

        private void _STBGoToPause()
        {
            bool needleFound = _WaitForNeedle(CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "stopButton.png"));
            if (needleFound)
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Ok });//stop playback
            }
            else//no pause button found
            {
                if (FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                    CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "nowOutOfFocus.png"),
                    playButtonSensitivity))//cannot access program from EPG
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok, RemoteKey.Ok }); //start playback
                    _STBGoToPause();
                }
                //program started but does not have regular controls
                STBReturnFromPlaybackToEPG();
                ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok }); //prepared on next channel
                _STBGoToPause();
            }
        }
        private void _STBRestartPlayback(ref Stopwatch sw)
        {
            sw.Restart();
            ShellCommand(new RemoteKey[] { RemoteKey.Ok }); //show menu
            while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                                CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "playButton.png"),
                                startOverSensitivity))
            {
                if (sw.ElapsedMilliseconds > 9000) //controlls dissappeared - reopen them
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Ok });
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
                    CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "nowOutOfFocus.png"),
                    startOverSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
            }

            //STBReset();
            //_STBGoToEPG();
        }
        private void STBSetToEPGNow()
        {
            if (chosenCountyPrefix == "SK")
            { ///co s tím? Default a pak dohledat???? 

                STBSKGoToDefault();
                ShellCommand(new RemoteKey[] { RemoteKey.Ok, RemoteKey.Right, RemoteKey.Ok, RemoteKey.Back });
                while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                        CvInvoke.Imread(".\\templates\\SKnow.png"), 0.70))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Right });
                    WaitXMilliseconds(1000);
                }
                ShellCommand(new RemoteKey[] { RemoteKey.Ok ,RemoteKey.One, RemoteKey.Ok });
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
            }
        }

        private void STBReturnFromPlaybackToEPG()
        {
            if (FindPattern(GetPrintscreenMat(), GetTemplate("nowOutOfFocus"), nowSensitivity) ||
                FindPattern(GetPrintscreenMat(), GetTemplate("yesterdayOutOfFocus"), nowSensitivity))
            {
                WriteDebugInfo(new Report("System", Status.Debug, 0, "Already in EPG"));
                return;
            }
            if (chosenCountyPrefix == "SK")
            {
                while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                        CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "nowOutOfFocus.png"),
                        0.70) &&
                       !FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                        CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "yesterdayOutOfFocus.png"),
                        0.70))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Back });
                    WaitXMilliseconds(loadWaitTime);
                }
                return;
            }
            ShellCommand(new RemoteKey[] { RemoteKey.EPG });
            WaitXMilliseconds(loadWaitTime);

            while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                    CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "nowOutOfFocus.png"),
                    startOverSensitivity) &&
                   !FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                    CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "yesterdayOutOfFocus.png"),
                    nowSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
                WaitXMilliseconds(loadWaitTime);
            }

            /*int backPressedTimes = 0;
            while (!FindPattern(BitmapScreenshot(screen).ToImage<Bgr, byte>().Mat,
                    CvInvoke.Imread(".\\templates\\" + languagePrefix + "nowOutOfFocus.png"),
                    startOverSensitivity) &&
                   !FindPattern(BitmapScreenshot(screen).ToImage<Bgr, byte>().Mat,
                    CvInvoke.Imread(".\\templates\\" + languagePrefix + "yesterdayOutOfFocus.png"),
                    nowSensitivity)) //go to EPG - EPG is either in now or yesterday
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Back });
                backPressedTimes++;
                WaitXMilliseconds(1500);
                if (backPressedTimes > 6)
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Up, RemoteKey.Right, RemoteKey.Ok, RemoteKey.Down });

                }
            }
            while (FindPattern(BitmapScreenshot(screen).ToImage<Bgr, byte>().Mat,
                    CvInvoke.Imread(".\\templates\\" + languagePrefix + "yesterdayOutOfFocus.png"),
                    nowSensitivity)) //check, if the EPG is not on "yesterday". If yes, move right
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Right });//chosen channal
                WaitXMilliseconds(1000);
            }*/

        }
        private void _STBGoToEPGNow()
        {
            ShellCommand(new RemoteKey[] { RemoteKey.Back });
            var now = CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "now.png");
            Thread.Sleep(500);
            while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat, now, 0.85))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Right });
                Thread.Sleep(500);
            }
            ShellCommand(new RemoteKey[] { RemoteKey.Ok });//on now column
            ShellCommand(new RemoteKey[] { RemoteKey.One, RemoteKey.Ok });
            WaitXMilliseconds(1000);
        }
        private void _STBChannelUp() => ShellCommand(new RemoteKey[] { RemoteKey.ChannelUp });


        private void STBGoOneChannelUp(VideoOrigin o)
        {
            switch (o)
            {
                case VideoOrigin.Live: _STBChannelUp();
                    WaitXMilliseconds(1000);
                    break;
                case VideoOrigin.Catchup:
                    _STBGoToYesterday();
                    ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok});
                    WaitXMilliseconds(1000);
                    ShellCommand(new RemoteKey[] { RemoteKey.Ok });

                    break;
                default: throw new NotImplementedException("Error 6: Video Origin not implemented");
                    
            }
        }
        private void _STBGoToYesterday()
        {
            ShellCommand(new RemoteKey[] { RemoteKey.EPG });
            WaitXMilliseconds(2000);
            if (FindPattern(GetPrintscreenMat(), GetTemplate("yesterdayOutOfFocus"), nowSensitivity))
            {
                return;
            }
            while (!FindPattern(GetPrintscreenMat(), GetTemplate("nowOutOfFocus"), nowSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
            }
            while (!FindPattern(GetPrintscreenMat(), GetTemplate("yesterdayOutOfFocus"), nowSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Back, RemoteKey.Left, RemoteKey.Ok });
                WaitXMilliseconds(2000);
            }
        }

        private void STBStartHeroChannelRailFromHomePage()
        {
            ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Right, RemoteKey.Right, RemoteKey.Ok }); //ready for ok
        }
        //private void SafeShellCommand() { //todo
        #endregion  

        #region SetUps
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
            SetVisible(OptionsRepeatValueTextBox);
            SetVisible(repeatValueLabel);

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
            if (File.Exists("STBIP.txt"))
            {
                using(StreamReader sr = new StreamReader("STBIP.txt"))
                {
                    STBIPInputBox.Text = sr.ReadLine();
                }
            }
        }
        #endregion

        #region Feed processing
        public Mat GetTemplate(string s)
        {

            return CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + s + ".png");
        }
        public Mat GetPrintscreenMat()
        {
            return BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat;
        }
        private bool _WaitForNeedle(Mat needle)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int failedAttempts = 0;
            while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat, needle, playButtonSensitivity))
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
                    WriteDebugInfo(new Report("System", Status.Debug, 0, "Pause_Play failed: Pause button not found"));
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

            //int matches = 0;
            for (int i = 0; i < result.Rows; i++) //search for matches
            {
                for (int j = 0; j < result.Cols; j++)
                {
                    if (resultImage[i, j].Intensity > sensitivity)
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
            throw new Exception("Error 2: Empty feed, or feed without expected result passed into function");
        }

        private Queue<Shot> SeparateSuspects(Queue<Shot> from, ref Queue<Shot> to, int maxTimeValue)
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
        private void RunPlayback_From_HeroChannelRail(int repeats)
        {
            string segmentName = "Playback_From_HeroChannelRail";
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Play_From_HeroChannelRail in progress"));

            Mat playbackPauseButton = CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "stopButton.png");
            Mat playButton = CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "HomePagePlay.png");

            STBGoToHomePage();
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Starting observation"));

            Queue<Shot> feed = new Queue<Shot>();
            Queue<Shot> EndStack = new Queue<Shot>(); //will contain areas around stage 1 and 2 End
            Stopwatch sw = Stopwatch.StartNew();
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;

            for (int i = 0; i < increment; i++)
            {
                STBStartHeroChannelRailFromHomePage();

                sw.Restart();

                Shot mostRecentShot = new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds);
                int endStage1 = GetMaxTimeOfAppearing(playbackPauseButton, mostRecentShot, playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);////
                if (endStage1 == -1)
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds."));
                    
                    totalFailures++;
                    if (CheckForStalledProgress( ref i, ref lastSuccesses,  totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    STBGoToHomePage();
                    continue;
                }
                //measure here
                                
                int stage1Time = FindFrameAfterNeedleAppears(ref feed, playbackPauseButton, this.playbackEmptyScreenSensitivity);
                WriteResults(new Report(segmentName, Status.Success, stage1Time, ""));

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
            } //repeat measuring
            
            WriteDebugInfo(reportDone);

        } //repeat done
        private void RunEPG_To_HomePage(int repeats)
        {
            string segmentName = "EPG_To_HomePage";
            WriteDebugInfo(new Report("System", Status.Fail, 0, "EPG_To_HomePage in progress"));

            STBSetToEPGNow();

            var homePagePlayButton = CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "HomePagePlay.png");
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
                    WriteResults(new Report(segmentName, Status.Fail, 0, "Timeout error: Load time exceeded" + homePageTimeOutValue.ToString() + " seconds."));

                    totalFailures++;
                    if(CheckForStalledProgress(ref i,ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }

                    feed.Clear();
                    STBSetToEPG();
                }
                int result = FindFrameAfterNeedleAppears(ref feed, homePagePlayButton, homePagePlaySensitivity);
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
        private void RunEPG_Live(int repeats)
        {
            string segmentName = "EPG_Live";
            WriteDebugInfo(new Report("System", Status.Debug, 0, "EPG_Live in progress"));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var playbackPauseButton = CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "stopButton.png");
            var feed = new Queue<Shot>();

            STBResetEPGToDefault();
            while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat,
                    playbackPauseButton,
                    playButtonSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Ok });//chosen channal
                WaitXMilliseconds(1000);
            }
            STBSetToEPGNow();
            sw.Restart();

            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok});//chosen channal
                sw.Restart();
                while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat, 
                                    CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "watchLive.png"), 
                                    watchLiveSensitivity)) //wait for TVOptions
                {
                    if (sw.ElapsedMilliseconds > playbackTimeoutValue)//timeout - try new TVOption
                    {
                        WriteResults(new Report(segmentName, Status.Fail, 0, "EPG_Live timeout error: watch live not found"));
                        feed.Clear();
                        STBReturnFromPlaybackToEPG(); //go to EPG now
                        ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok });//chosen channal
                        WaitXMilliseconds(1000);
                        continue;
                    }
                }             
                //measure here
                ShellCommand(new RemoteKey[] { RemoteKey.Ok });
                sw.Restart();

                int stage1End = GetMaxTimeOfAppearing(playbackPauseButton, new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);
                if (stage1End == -1)
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "EPG_Live: measurement error: Stop button not found before timeout"));
                    feed.Clear();
                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }
                    STBReturnFromPlaybackToEPG(); //go to EPG now
                    continue;
                }

                WriteDebugInfo(new Report(segmentName, Status.Debug, 0,"Stage 1 ended: " + stage1End.ToString() + " ms"));

                int result = FindFrameAfterNeedleAppears(ref feed, playbackPauseButton, playbackEmptyScreenSensitivity);
                if (result < 1000) // zahoï šmejdy
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "EPG_Live: measurement error: Stage 1 end time = " + result.ToString() + "ms is too short"));
                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }
                    feed.Clear();
                    STBReturnFromPlaybackToEPG();
                    continue;
                }
                
                WriteResults(new Report(segmentName, Status.Success, result, ""));
                totalSuccesses++;
                if (totalSuccesses == repeats)
                {
                    break;
                }
                if (CheckForStalledProgress(    ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                {
                    break;
                }
                feed.Clear();
                STBReturnFromPlaybackToEPG(); //go to EPG now
            }
            
            WriteDebugInfo(reportDone);
        }
        /// <summary>
        /// appliable only in EPG. Returns EPG to section yesterday during catchup sessions
        /// </summary>

        private void RunEPG_CatchUp(int repeats)
        {
            string segmentName = "EPG_CatchUp";
            WriteDebugInfo(new Report("System", Status.Debug, 0, "EPG_CatchUp in progress"));

            STBResetEPGToDefault();
            WaitXMilliseconds(loadWaitTime);
            _STBGoToYesterday();
            for (int i = 0; i < 3; i++)
            {
                if (!FindPattern(GetPrintscreenMat(), GetTemplate("TVDetailPlayButton"), playButtonSensitivity))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Ok }); //start playback
                    WaitXMilliseconds(1000);
                }
            }

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var needle = CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "stopButton.png");
            var feed = new Queue<Shot>();
            ShellCommand(new RemoteKey[] { RemoteKey.Ok }); //start playback

            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                sw.Restart();
                int Stage1 = GetMaxTimeOfAppearing(needle, new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);
                if (Stage1 == -1)
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "Timeout error: Load time exceeded " + (playbackTimeoutValue / 1000).ToString() + " seconds."));
                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }
                    feed.Clear();
                    STBGoOneChannelUp(VideoOrigin.Catchup);
                    continue;
                }
                //int stage1 = GetMaxTimeOfDissappearing(needle, new Shot(BitmapScreenshot(screen), (int)sw.ElapsedMilliseconds), playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);
                //if (stage1 == -1)
                //{
                //    WriteResults("Timeout error: Load time exceeded" + playbackTimeoutValue.ToString() + " seconds.");
                //    return;
                //}
                int result = FindFrameAfterNeedleAppears(ref feed, needle, playbackEmptyScreenSensitivity);
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
                STBGoOneChannelUp(VideoOrigin.Catchup);
            }

            WriteDebugInfo(reportDone);

        }

        /// <summary>
        /// Go to TVOption before starting this method
        /// </summary>
        /// <param name="origin"></param>

        private void RunPlay_Pause(VideoOrigin origin, int repeats)
        {
            if (origin == VideoOrigin.Live)
            {
                string segmentName = "Play_Pause_Live";
                WriteDebugInfo(new Report("System", Status.Debug, 0, "Play_Pause_Live in progress"));

                STBResetEPGToDefault();
                ShellCommand(new RemoteKey[] { RemoteKey.Ok, RemoteKey.Ok }); //start first channel
                WaitXMilliseconds(loadWaitTime);
                STBReturnFromPlaybackToEPG();
                ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok, RemoteKey.Ok }); //second channel running

                Stopwatch sw = new Stopwatch();
                Queue<Shot> feed = new Queue<Shot>();
                var needle = CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "stopButton.png");
                sw.Start();
                int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
                for (int i = 0; i < increment; i++)
                {
                    _STBGoToPause();
                    WriteDebugInfo(new Report("System", Status.Debug, 0, "Imposing " + playPauseWaitTime / 1000 + " seconds wait time"));
                    WaitXMilliseconds(playPauseWaitTime);

                    //start the feed
                    
                    _STBRestartPlayback(ref sw);


                    int endStage1 = GetMaxTimeOfAppearing(needle, new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), playbackEmptyScreenSensitivity, sw, ref feed, true, playbackTimeoutValue);
                    if (endStage1 == -1 || endStage1 < 1000)
                    {
                        if (endStage1 == -1)
                        {
                            WriteResults(new Report(segmentName, Status.Fail, 0, "Timeout error: Load time exceeded" + (playbackTimeoutValue / 1000).ToString() + " seconds."));
                        }
                        else
                        {
                            WriteResults(new Report(segmentName, Status.Fail, 0, "Measurement error: Load time under 1 second"));
                        }
                        totalFailures++;
                        if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                        {
                            break;
                        }
                        feed.Clear();
                        
                        STBGoOneChannelUp(VideoOrigin.Catchup);
                        continue;
                    }
                    int result = FindFrameAfterNeedleAppears(ref feed, needle, playbackEmptyScreenSensitivity);
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
                    STBGoOneChannelUp(VideoOrigin.Live);

                }
                
                WriteDebugInfo(reportDone);
                //in report indicate it is from live
                return;
            }
            if (origin == VideoOrigin.Catchup)
            {
                string segmentName = "Play_Pause_CatchUp";
                WriteDebugInfo(new Report("System", Status.Debug, 0, "Play_Pause_CatchUp in progress"));
                STBResetEPGToDefault();
                _STBGoToYesterday();
                ShellCommand(new RemoteKey[] { RemoteKey.Ok, RemoteKey.Ok }); //first channel play

                Stopwatch sw = new Stopwatch();
                Queue<Shot> feed = new Queue<Shot>();
                var playbackStopButton = CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "stopButton.png");
                sw.Start();
                int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
                for (int i = 0; i < increment; i++)
                {
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

                        STBGoOneChannelUp(VideoOrigin.Catchup);
                        continue;
                    }
                    int result = FindFrameAfterNeedleAppears(ref feed, playbackStopButton, playbackEmptyScreenSensitivity);
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
                    STBGoOneChannelUp(VideoOrigin.Catchup);
                    
                }


                WriteDebugInfo(reportDone);
                return;
                
            }

        }
        
        private void RunZapping(int repeats)
        {
            string segmentName = "Zapping";
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Zapping in progress"));

            STBResetEPGToDefault();
            ShellCommand(new RemoteKey[] {RemoteKey.Ok});//program chosen
            
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (!FindPattern(BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat, CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "watchLive.png"), startOverSensitivity))
            {
                int fails = 0;
                if (sw.ElapsedMilliseconds > playbackTimeoutValue)
                {
                    if (fails >= 5)
                    {
                        WriteDebugInfo(new Report("Zapping", Status.Debug, 0, "Failed to initialize"));
                        return;
                    }
                    STBReturnFromPlaybackToEPG();
                    ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok });//play - setup done
                    sw.Restart();
                    fails++;
                }
            }//channel options visible
            
            ShellCommand(new RemoteKey[] {RemoteKey.Ok });//play - setup done
                                                          //measure here
            var needle = CvInvoke.Imread(".\\templates\\" + chosenCountyPrefix + "stopButton.png");
            var feed = new Queue<Shot>();
            int pauseButtonMaxExpirationTime = 1500;
            int totalSuccesses = 0, totalFailures = 0, lastSuccesses = 0, increment = 10;
            for (int i = 0; i < increment; i++)
            {
                ShellCommand(new RemoteKey[] { RemoteKey.ChannelUp });
                sw.Restart();
                int prestageEndTime = GetMaxTimeOfDissappearing(needle, new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), playButtonSensitivity, sw, ref feed, true, pauseButtonMaxExpirationTime);
                if (prestageEndTime == -1)
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "Timeout error: Pause button took more than " + pauseButtonMaxExpirationTime + " milliseconds to dissappear"));
                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }
                    feed.Clear();
                    continue;
                }
                while (feed.Peek().time < prestageEndTime)
                {
                    feed.Dequeue();
                }

                int stage1End = GetMaxTimeOfAppearing(needle, new Shot(BitmapScreenshot(chosenScreen), (int)sw.ElapsedMilliseconds), playButtonSensitivity, sw, ref feed, true, playbackTimeoutValue);
                if (stage1End == -1)
                {
                    WriteResults(new Report(segmentName, Status.Fail, 0, "Timeout error: Load time exceeded " + (playbackTimeoutValue/1000).ToString() + " seconds"));
                    totalFailures++;
                    if (CheckForStalledProgress(ref i, ref lastSuccesses, totalSuccesses, totalFailures, increment))
                    {
                        break;
                    }
                    feed.Clear();
                    continue;
                }

                int result = FindFrameAfterNeedleAppears(ref feed, needle, playbackEmptyScreenSensitivity);
                
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
            WriteDebugInfo(reportDone);
        }
        #endregion

        #region BUTTON_CLICK
        private void TakeScreenshotButton_Click(object sender, EventArgs e)
        {
            var bmp = BitmapScreenshot(Screen.AllScreens[1]);
            bmp.Save(".\\templates\\" + chosenCountyPrefix + ".png");
        }

        private void STBIPInputOkButton_Click(object sender, EventArgs e)
        {
            if (IPAddress.TryParse(STBIPInputBox.Text, out var address))
            {
                IP = address.ToString();
                CountrySetUp();
                using(StreamWriter sw = new StreamWriter("STBIP.txt"))
                {
                    sw.WriteLine(IP);
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
            CountrySetUp(); //country option
            return;


        }

        private void OptionsConfirmButton_Click(object sender, EventArgs e)
        {

            if (Options.CheckedItems.Count == 0)
            {
                return;
            }
            int repeats;
            if (!int.TryParse(OptionsRepeatValueTextBox.Text, out repeats))
            {
                repeatValueLabel.Text = "Invalid number of measurements";
                repeatValueLabel.ForeColor = Color.Red;
            }
            repeatValueLabel.Text = "Required amount of measurements";
            repeatValueLabel.ForeColor = Color.Empty;

            foreach (var item in Options.CheckedItems)
            {
                switch (item.ToString())
                {
                    case "Login": Login = true; break;
                    case "Silent_Login": Silent_Login = true; break;
                    case "Play_From_HeroChannelRail": Play_From_HeroChannelRail = true; break;
                    case "EPG_To_HomePage": EPG_To_HomePage = true; break;
                    case "EPG_Live": EPG_Live = true; break;
                    case "EPG_CatchUp": EPG_CatchUp = true; break;
                    case "Play_Pause": Play_Pause = true; break;
                    case "Zapping": Zapping = true; break;
                    default: throw new NotImplementedException();
                }
            }
            results = new ResultsStorage(repeats);           
            SetUpLogs();
            if (ChosenDevice == Device.STB)
            {
                ConnectToSTB();
            }
            results = new ResultsStorage(repeats);
            HideAll();
            #region options switch   
            this.Hide();


            if (Login)
            {
                RunLogin(); //later
            }
            if (Silent_Login)
            {
                RunSilent_Login(); //later
            }
            if (Play_From_HeroChannelRail)
            {
                RunPlayback_From_HeroChannelRail(repeats);
            }
            if (EPG_To_HomePage)
            {
                RunEPG_To_HomePage(repeats);
            }
            if (EPG_Live)
            {
                RunEPG_Live(repeats);
            }
            if (Play_Pause) // from live
            {
                RunPlay_Pause(VideoOrigin.Live, repeats);
            }
            if (EPG_CatchUp)
            {
                RunEPG_CatchUp(repeats);
            }
            if (Play_Pause) //from catch-up
            {
                RunPlay_Pause(VideoOrigin.Catchup, repeats);
            }
            if (Zapping)
            {
                RunZapping(repeats);
            }
            #endregion
            results.dump(ChosenDevice.ToString() + "_" + chosenCountyPrefix + "_" + DateTime.Now.ToString("yyyy.MM.dd-hh.mm.ss"));
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
        private void TestButton_Click(object sender, EventArgs e)
        {
            //Mat needle = CvInvoke.Imread(".\\templates\\playback_stage1.png");
            //Mat n  = CvInvoke.Imread(".\\templates\\playback_stage1.png");
            //if (FindPattern(n, needle, 0.95))
            //{
            //    WriteToConsole("match");
            //}
            chosenScreen = Screen.AllScreens[1];
            ChosenDevice = Device.STB;
            IP = "10.0.0.8"; //O2 ZTE STB
            //IP = "10.0.0.109"; //O2 NG STB
            //IP = "10.20.40.74"; //HU STB - connect to TLN RS
            //IP = "192.168.1.151"; //RS STB - connect to Yettel
            //IP = "10.20.40.75"; // BG STB - connect to TLN RS
            //IP = "10.0.1.40";//SK

            NativeMethods.AllocConsole();
            SetUpLogs();

            ConnectToSTB();

            chosenCountyPrefix = "CZ";
            int repeats = 5;
            results = new ResultsStorage(repeats);


            //RunPlayback_From_HeroChannelRail(repeats);
            //RunEPG_To_HomePage(repeats);
            //RunEPG_Live(repeats);
            //RunEPG_CatchUp(repeats); //success
            //RunPlay_Pause(VideoOrigin.Live, repeats);
            //RunPlay_Pause(VideoOrigin.Catchup, repeats);
            //RunZapping(repeats);
            FillResults(ref results); //measured results
            results.dump(ChosenDevice.ToString() + "_" + chosenCountyPrefix + "_" + DateTime.Now.ToString("yyyy.MM.dd-hh.mm.ss"));
            CloseLogs();

        }
        
        #endregion
        private void FillResults(ref ResultsStorage r)
        {
            StreamReader sr = new StreamReader(".\\debug\\data.txt");
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