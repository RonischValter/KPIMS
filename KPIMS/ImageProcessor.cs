using Emgu.CV.Structure;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;

namespace KPI_measuring_software
{
    internal class ImageProcessor
    {
        double playButtonSensitivity = 0.90;
        StreamWriter debugLog;
        string chosenCountryPrefix;
        string filePath;
        Screen chosenScreen;
        public ImageProcessor(StreamWriter debugLog, string chosenCountryPrefix, string filePath, Screen chosenScreen) 
        {
            this.debugLog = debugLog;
            this.chosenCountryPrefix = chosenCountryPrefix;
            this.filePath = filePath;
            this.chosenScreen = chosenScreen;
        }

        private void WriteDebugInfo(Report r)
        {
            debugLog.WriteLine(r.segmentName + ": " + r.message);
            debugLog.Flush();
            Console.WriteLine(r.segmentName + ": " + r.message);
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

        public Mat GetTemplate(Template t)
        {
            string s;
            switch (t)
            {
                case Template.Now:
                    s = chosenCountryPrefix + "now";
                    break;
                case Template.NowOutOfFocus:
                    s = chosenCountryPrefix + "nowOutOfFocus";
                    break;
                case Template.YesterdayOutOfFocus:
                    s = chosenCountryPrefix + "yesterdayOutOfFocus";
                    break;
                case Template.EPGStage1:
                    s = chosenCountryPrefix + "EPG_stage1";
                    break;
                case Template.HomePagePlay:
                    s = chosenCountryPrefix + "HomePagePlay";
                    break;
                case Template.PlayButton:
                    s = chosenCountryPrefix + "playButton";
                    break;
                case Template.StopButton:
                    s = chosenCountryPrefix + "stopButton";
                    break;
                case Template.TVDetailPlayButton:
                    s = chosenCountryPrefix + "TVDetailPlayButton";
                    break;
                case Template.WatchLive:
                    s = chosenCountryPrefix + "watchLive";
                    break;
                case Template.SKBack:
                    s = chosenCountryPrefix + "back";
                    break;
                case Template.PlaybackStage1:
                    s = chosenCountryPrefix + "PlaybackStage1";
                    break;
                case Template.LoginProcessingScreen:
                    s = chosenCountryPrefix + "LoginScreen";
                    break;
                case Template.Menu:
                    s = chosenCountryPrefix + "menu";
                    break;
                case Template.LogInButton:
                    s = chosenCountryPrefix + "loginButton";
                    break;
                case Template.More:
                    s = chosenCountryPrefix + "more";
                    break;
                case Template.Continue:
                    s = chosenCountryPrefix + "continue";
                    break;
                default: throw new NotImplementedException("Error 11: Unknown template");
            }
            return CvInvoke.Imread(filePath + "\\templates\\" + s + ".png");
        }
        public Mat GetPrintscreenMat(Screen chosenScreen)
        {
            return BitmapScreenshot(chosenScreen).ToImage<Bgr, byte>().Mat;
        }
        public bool _WaitForNeedle(Mat needle)
        {
            Stopwatch sw = Stopwatch.StartNew();
            int failedAttempts = 0;
            while (!FindPattern(GetPrintscreenMat(chosenScreen), needle, playButtonSensitivity))
            {
                WriteDebugInfo(new Report("System", Status.Debug, 0, "Waiting for needle"));

                if (sw.ElapsedMilliseconds > 10000)
                {
                    WriteDebugInfo(new Report("System", Status.Debug, 0, "Playback resume failed: Pause button not found"));
                    return false;
                }
            }
            return true;
        }

        public bool FindPattern(Mat stack, Mat needle, double sensitivity)
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
        public int GetMaxTimeOfAppearing(Mat needle,
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
        public int GetMaxTimeOfDissappearing(Mat needle,
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
                WriteDebugInfo(new Report("System", Status.Debug, 0, "shot time = " + lastShot.time));

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
            WriteDebugInfo(new Report("System", Status.Debug, 0, "Returning time: " + susShot.time + ", feed min time: " + feed.Peek().time));
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
        public int FindFrame(ref Queue<Shot> feed, Mat needle, double sensitivity)
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
        public int FindFrameAfterNeedleDissappears(ref Queue<Shot> feed, Mat needle, double sensitivity)
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
        public int FindFrameAfterNeedleAppears(ref Queue<Shot> feed, Mat needle, double sensitivity)
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

        public Queue<Shot> SeparateSuspects(ref Queue<Shot> from, ref Queue<Shot> to, int maxTimeValue)
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
            while (from.Count > 0)
            {
                copy.Enqueue(from.Dequeue());
            }
            return copy;

        }
    }
}
