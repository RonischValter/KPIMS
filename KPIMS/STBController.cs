using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KPI_measuring_software
{
    internal abstract class Controller
    {
        protected Controller() { }
        public abstract void LogOut();
        public abstract void Login(string name, string password);
        public abstract void PressOk();
        public abstract void RecoverDevice();
        public abstract void GoToHomePage();
        public abstract bool GoToVOD();
        public abstract void VODNextUnpurchased(int recursion);
        public abstract void PutchaseThis();
        public abstract void StartHeroChannelRailFromHomePage();
        public abstract void SetToEPGNow();
        public abstract void SetToEPG();
        public abstract void StartFirstChannel();
        public abstract bool GoOneChannelUpNowLongWay(int recursion);
        public abstract void GoToFirstChannelYesterday();
        public abstract bool GoOneChannelUp(VideoOrigin videoOrigin, int recursion);
        public abstract void _GoToPause();
        public abstract void ResetEPGToDefault();
        public abstract void _RestartPlayback();
        public abstract bool ConnectToSTB();
        public abstract string GetAppVersion();
        public abstract void PressHomePage();
        public abstract void SetToRecordings();




    }
    internal class STBController : Controller
    {
        string IP;
        string filePath;
        string appVersion;
        string chosenCountyPrefix;
        Country chosenCountry;
        StreamWriter debugLog;
        Screen chosenScreen;
        ImageProcessor imageProcessor;

        double firstPictureSensitivity = 0.95;
        double homePagePlaySensitivity = 0.90;
        double startOverSensitivity = 0.95;
        double playbackEmptyScreenSensitivity = 0.85;
        double pausePlaySensitivity = 0.98;
        double playButtonSensitivity = 0.90;
        double nowSensitivity = 0.90;
        double watchLiveSensitivity = 0.70;
        double loginProcessingScreenSensitivity = 0.70;

        int playbackTimeoutValue = 7000;
        int homePageTimeOutValue = 7000;
        int loginLoadingScreenTimeOutValue = 60000;
        int playPauseWaitTime = 10000;
        int loadWaitTime = 3000;
        int loginTimeOutValue = 60000;
        int homePageButtonPressWaitTime = 7000;

        public STBController(string IP, string filePath, StreamWriter debugLog, ImageProcessor imageProcessor,
                            string chosenCountryPrefix, Country chosenCountry, Screen chosenScreen, bool NG) 
        {
            this.IP = IP;
            this.filePath = filePath;
            this.debugLog = debugLog;
            this.chosenCountyPrefix= chosenCountryPrefix;
            this.chosenCountry = chosenCountry;
            this.chosenScreen = chosenScreen;
            this.imageProcessor = imageProcessor;
            if(NG) { homePageButtonPressWaitTime = 20000; }
        }
        private void WaitXMilliseconds(int milliseconds)
        {
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Waiting " + milliseconds + " ms"));
            Thread.Sleep(milliseconds);
        }
        public override void PressHomePage()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            ShellSingleCommand(RemoteKey.HomePage);
            Console.WriteLine("HomePage process time = " + sw.ElapsedMilliseconds);
        }
        private void WriteDebugInfo(Report r)
        {
            debugLog.WriteLine(r.segmentName + ": " + r.message);
            debugLog.Flush();
            Console.WriteLine(r.segmentName + ": " + r.message);
        }
        public override void PressOk()
        {
            ShellSingleCommand(RemoteKey.Ok);
        }
        public override bool ConnectToSTB()
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
        public override string GetAppVersion()
        {
            string adbPath = @"C:\Users\vronisch\source\repos\KPIMS\KPIMS\bin\Debug\net6.0-windows\adb\adb.exe";
            string packageName = null;
            switch (chosenCountyPrefix)
            {
                case "CZ": packageName = "cz.o2.o2tv"; break;
                case "RS": packageName = "rs.tv.kal"; break;
                case "BG": packageName = "bg.yettel.tv"; break;
                case "HU": packageName = "hu.pgsm.tv"; break;
                default:
                    throw new NotImplementedException("GetAppVersion: package name unknown");
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

            Process.Start("CMD.exe", command).WaitForExit();
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Key " + k.ToString() + " pressed"));
        }
        private void NavigateGrid(string word, bool reset)
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
                //reset weird letters (ěščřžýáíé)
                ShellSingleCommand(RemoteKey.Right);
                ShellSingleCommand(RemoteKey.Left);
                bool letterInAlternativeGrid = false;

                if (row == 3)
                {
                    ShellSingleCommand(RemoteKey.Up);
                    row--;
                }
                while (col > 8)
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

        /// <summary>
        /// Goes from anywhere to EPG, now, first channel
        /// </summary>
        public override void ResetEPGToDefault()
        {

            SetToEPGNow();
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
            };
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
        public override void GoToHomePage()
        {
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                                imageProcessor.GetTemplate(Template.HomePagePlay),
                                homePagePlaySensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.HomePage });
                WaitXMilliseconds(homePageButtonPressWaitTime);
            }
            //ShellCommand(new RemoteKey[] {RemoteKey.Back,RemoteKey.Back,RemoteKey.Back,RemoteKey.Back,
            //                              RemoteKey.Back,RemoteKey.Back,RemoteKey.Back,RemoteKey.Up, RemoteKey.Up, RemoteKey.Up});
        }
        public override void LogOut()
        {
            GoToHomePage();
            _STBLogOut();
        }
        public override void Login(string name, string password)
        {
            ShellSingleCommand(RemoteKey.Ok);
            WaitXMilliseconds(5000);
            NavigateGrid("", true);
            NavigateGrid(password, true);
        }
        public void _STBLogOut()
        {
            ShellSingleCommand(RemoteKey.Up);
            var needle = imageProcessor.GetTemplate(Template.Menu);
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
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), 
                    imageProcessor.GetTemplate(Template.LogInButton), 0.90))
            {
                if (sw.ElapsedMilliseconds > 60000)
                {
                    WriteDebugInfo(new Report("System", Status.Debug, 0, "Log in button has not loaded in 60 seconds, restarting device"));
                    RecoverDevice();
                }
                Thread.Sleep(1000);
            }
        }
        public override void RecoverDevice()
        {
            string adb = filePath + "\\adb\\adb";

            Process connect = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath + "\\adb\\adb.exe",
                    Arguments = "reboot",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };
            connect.Start();
            WaitXMilliseconds(60000);
        }
        private bool _STBGoToVODMore()
        {
            if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), 
                imageProcessor.GetTemplate(Template.More), 0.95))
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
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), 
                    imageProcessor.GetTemplate(Template.More), 0.85))
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
        public override bool GoToVOD()
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
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), 
                    imageProcessor.GetTemplate(Template.StopButton), playButtonSensitivity))
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
            WaitXMilliseconds(5000);
            return true;
        }
        public override void _GoToPause()
        {
            bool needleFound = imageProcessor._WaitForNeedle(imageProcessor.GetTemplate(Template.StopButton));
            for (int i = 0; i < 2; i++)
            {
                if (needleFound)
                {
                    break;
                }
                ShellSingleCommand(RemoteKey.Ok);
                needleFound = imageProcessor._WaitForNeedle(imageProcessor.GetTemplate(Template.StopButton));
            }
            if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), 
                imageProcessor.GetTemplate(Template.PlayButton), playButtonSensitivity))
            {
                return;
            }
            if (needleFound)
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Ok });//stop playback
                WaitXMilliseconds(1000);

                int failMeasure = 0;
                while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), 
                        imageProcessor.GetTemplate(Template.PlayButton), playButtonSensitivity))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Ok });//stop playback
                    WaitXMilliseconds(1000);
                    if (failMeasure == 2) // failed 3 times
                    {
                        STBReturnFromPlaybackToEPG();
                        ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok, RemoteKey.Ok }); //start playback
                        _GoToPause();
                        return;
                    }
                    failMeasure++;
                }//didn't manage to stop playback

            }
            else//no pause button found -> try next channel
            {
                STBReturnFromPlaybackToEPG();

                if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                                imageProcessor.GetTemplate(Template.NowOutOfFocus),
                                playButtonSensitivity))
                {
                    _STBGoToEPGNow();
                    _STBStartNextChannel();
                }
                else
                {
                    GoOneChannelUp(VideoOrigin.Catchup, 0);
                }


                //program started but does not have regular controls
                STBReturnFromPlaybackToEPG();
                ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok }); //prepared on next channel
                _GoToPause();
            }
        }
        public void _STBStartNextChannel()
        {
            ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok, RemoteKey.Ok }); //prepared on next channel
        }
        public override void _RestartPlayback()
        {

            ShellCommand(new RemoteKey[] { RemoteKey.Ok }); //show menu
            WaitXMilliseconds(2000);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                                imageProcessor.GetTemplate(Template.PlayButton),
                                startOverSensitivity))
            {
                if (sw.ElapsedMilliseconds > 9000) //controlls dissappeared - re-open them
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
        public override void SetToEPG()
        {
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                    imageProcessor.GetTemplate(Template.NowOutOfFocus),
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

            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), novinky, 0.90))
            {
                if (fails > 5)//failed
                {
                    GoToHomePage();
                    GoToVOD();
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
        public override void VODNextUnpurchased(int recursion)
        {

            var novinky = CvInvoke.Imread(filePath + "\\templates\\CZnovinky.png");
            var homePage = imageProcessor.GetTemplate(Template.HomePagePlay);
            var continuee = imageProcessor.GetTemplate(Template.Continue);
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), novinky, 0.50))
            {
                ShellSingleCommand(RemoteKey.Back);
                WaitXMilliseconds(7000);
                if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), homePage, homePagePlaySensitivity))
                {
                    GoToVOD();
                    VODNextUnpurchased(recursion + 1);
                    while (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), continuee, homePagePlaySensitivity))
                    {
                        VODNextUnpurchased(1);
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

            if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), continuee, homePagePlaySensitivity))
            {
                VODNextUnpurchased(1);
            }
        }
        public override void PutchaseThis()
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
        public override void SetToEPGNow()
        {
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), 
                    imageProcessor.GetTemplate(Template.NowOutOfFocus), nowSensitivity))
            {
                ShellSingleCommand(RemoteKey.EPG);
                WaitXMilliseconds(500);
            }
            ShellSingleCommand(RemoteKey.EPG);

        }
        private void STBReturnFromPlaybackToEPG()
        {
            if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), imageProcessor.GetTemplate(Template.NowOutOfFocus), nowSensitivity) ||
                imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), imageProcessor.GetTemplate(Template.YesterdayOutOfFocus), nowSensitivity))
            {
                WriteDebugInfo(new Report("System", Status.Debug, 0, "Already in EPG"));
                return;
            }
            if (chosenCountyPrefix == "SK")
            {
                while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                        imageProcessor.GetTemplate(Template.NowOutOfFocus),
                        0.70) &&
                       !imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                        imageProcessor.GetTemplate(Template.YesterdayOutOfFocus),
                        0.70))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Back });
                    WaitXMilliseconds(loadWaitTime);
                }
                return;
            }
            ShellCommand(new RemoteKey[] { RemoteKey.EPG });
            WaitXMilliseconds(loadWaitTime);

            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                    imageProcessor.GetTemplate(Template.NowOutOfFocus),
                    startOverSensitivity) &&
                   !imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                    imageProcessor.GetTemplate(Template.YesterdayOutOfFocus),
                    nowSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
                WaitXMilliseconds(loadWaitTime);
            }
            if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                    imageProcessor.GetTemplate(Template.NowOutOfFocus),
                    startOverSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
                WaitXMilliseconds(500);
            }
        }
        private void _STBGoToEPGNow()
        {
            ShellCommand(new RemoteKey[] { RemoteKey.Back });
            var now = imageProcessor.GetTemplate(Template.Now);
            Thread.Sleep(500);
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), now, 0.85))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Right });
                Thread.Sleep(500);
            }
            ShellCommand(new RemoteKey[] { RemoteKey.Ok });//on now column
            ShellCommand(new RemoteKey[] { RemoteKey.One, RemoteKey.Ok });
            WaitXMilliseconds(1000);
        }
        private void _STBChannelUp() => ShellCommand(new RemoteKey[] { RemoteKey.ChannelUp });
        public override bool GoOneChannelUp(VideoOrigin o, int recursion)
        {
            if (recursion >= 5)
            {
                return false;
            }
            switch (o)
            {
                case VideoOrigin.Live:
                    _STBChannelUp();
                    return true;
                case VideoOrigin.Catchup:
                    _STBGoToYesterday();
                    var needle = imageProcessor.GetTemplate(Template.TVDetailPlayButton);
                    ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Ok });
                    WaitXMilliseconds(2000);
                    for (int i = 0; i < 5; i++)
                    {
                        if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), needle, homePagePlaySensitivity))
                        {
                            ShellCommand(new RemoteKey[] { RemoteKey.Ok });
                            return true;
                        }
                        ShellSingleCommand(RemoteKey.Ok);
                        WaitXMilliseconds(2000);
                    }
                    if (GoOneChannelUp(VideoOrigin.Catchup, recursion + 1))
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
            if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), 
                imageProcessor.GetTemplate(Template.YesterdayOutOfFocus), nowSensitivity))
            {
                return;
            }
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), 
                    imageProcessor.GetTemplate(Template.NowOutOfFocus), nowSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.EPG });
            }
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), 
                    imageProcessor.GetTemplate(Template.YesterdayOutOfFocus), nowSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Back, RemoteKey.Left, RemoteKey.Ok });
                WaitXMilliseconds(2000);
            }
        }
        public override void StartHeroChannelRailFromHomePage()
        {
            ShellCommand(new RemoteKey[] { RemoteKey.Down, RemoteKey.Right, RemoteKey.Right, RemoteKey.Ok }); //ready for ok
        }
        public override void StartFirstChannel()
        {
            ResetEPGToDefault();
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                    imageProcessor.GetTemplate(Template.StopButton),
                    playButtonSensitivity))
            {
                ShellCommand(new RemoteKey[] { RemoteKey.Ok });//chosen channal
                WaitXMilliseconds(3000);
            }
        }
        public override bool GoOneChannelUpNowLongWay(int recursionValue)
        {
            if (recursionValue > 5)//bigger problem somewhere
            {
                return false;
            }
            var needle = imageProcessor.GetTemplate(Template.NowOutOfFocus);
            while (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen), needle, nowSensitivity))
            {
                ShellSingleCommand(RemoteKey.EPG);
            }
            ShellSingleCommand(RemoteKey.Down);
            ShellSingleCommand(RemoteKey.Ok);
            WaitXMilliseconds(2000);
            for (int i = 0; i < 3; i++)//try to play
            {
                if (imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                                    imageProcessor.GetTemplate(Template.WatchLive),
                                    watchLiveSensitivity))
                {
                    ShellSingleCommand(RemoteKey.Ok);
                    return true;
                }
                ShellSingleCommand(RemoteKey.Ok);
                WaitXMilliseconds(1000);
            }
            if (GoOneChannelUpNowLongWay(recursionValue + 1))
            {
                return true;
            }
            return false;
        }
        public override void GoToFirstChannelYesterday()
        {
            ResetEPGToDefault();
            WaitXMilliseconds(loadWaitTime);
            _STBGoToYesterday();
            for (int i = 0; i < 3; i++)
            {
                if (!imageProcessor.FindPattern(imageProcessor.GetPrintscreenMat(chosenScreen),
                     imageProcessor.GetTemplate(Template.TVDetailPlayButton), playButtonSensitivity))
                {
                    ShellCommand(new RemoteKey[] { RemoteKey.Ok }); //start playback
                    WaitXMilliseconds(1000);
                }
                ShellSingleCommand(RemoteKey.Ok);
                WaitXMilliseconds(2000);
            }
        }

        public override void SetToRecordings()
        {
            GoToHomePage();
            ShellSingleCommand(RemoteKey.Up);
            WaitXMilliseconds(1500);
            ShellSingleCommand(RemoteKey.Right);
            WaitXMilliseconds(1500);
            ShellSingleCommand(RemoteKey.Right);
            WaitXMilliseconds(1500);
            ShellSingleCommand(RemoteKey.Ok);
            WaitXMilliseconds(5000);
        }
    }
}
