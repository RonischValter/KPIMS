using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace KPI_measuring_software
{
    class MouseControl
    {
        public MouseControl() { }

        public Point GetCursor() { return WaitForClick(); }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out Point lpPoint);

        public static Point WaitForClick()
        {
            Point clickLocation;
            GetCursorPos(out clickLocation);
            Screen screen = Screen.FromPoint(clickLocation);

            while (!Control.MouseButtons.HasFlag(MouseButtons.Left))
            {
                Application.DoEvents();
                GetCursorPos(out clickLocation);
            }

            return clickLocation;
        }

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        public static void Click(Point point)
        {
            SetCursorPos(point.X, point.Y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, point.X, point.Y, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTUP, point.X, point.Y, 0, 0);
        }
    }
}
