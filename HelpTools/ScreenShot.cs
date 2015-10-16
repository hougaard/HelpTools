using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;

namespace HelpTools
{
    class Window
    {
        public string Title;
        public IntPtr Handle;

        public static List<Window> GetRunningWindows()
        {
            List<Window> l = new List<Window>();
            foreach (KeyValuePair<IntPtr, string> window in OpenWindowGetter.GetOpenWindows())
                l.Add(new Window { Title = window.Value, Handle = window.Key });
            return l;
        }
    }
    public class ScreenShot
    {
        private string Server { get; set; }
        private string Instance { get; set; }
        private string Company { get; set; }
        public ScreenShot()
        {
        }

        private Process NavProcess;
        public bool IsNAVRunning()
        {
            List<Window> Windows = Window.GetRunningWindows();
            int basecount = Windows.Count;
            foreach (var window in Windows)
            {
                if (window.Title.IndexOf("- Microsoft Dynamics NAV") == -1)
                    basecount--;
                else
                {
                    if (window.Title.IndexOf("Development Environment") != -1)
                        basecount--;
                }
            }
            if (basecount > 0)
            {
                if (basecount == 1)
                    return true;
                else
                {
                    throw new Exception("Too many Dynamics NAV instances found, cannot proceed");
                }
            }
            else
            {
                return false;
            }
        }
        public void StartNAV(string Server, string Instance, string Company)
        {
            this.Server = Server;
            this.Instance = Instance;
            this.Company = Company;

            string DynamicsURL = "dynamicsnav://{0}/{1}/{2}/";

            NavProcess = new Process();
            NavProcess.StartInfo = new ProcessStartInfo(String.Format(DynamicsURL, this.Server, this.Instance, this.Company));

            NavProcess.Start();
            int Timeout = 30;
            while (!IsNAVRunning())
            {
                Thread.Sleep(1000);
                Timeout--;
                if (Timeout == 0)
                    throw new Exception("Could not start Dynamics NAV within 30 seconds");
            }
        }
        public Bitmap GenerateScreenShot(int PageNo)
        {
            List<Window> baseline = Window.GetRunningWindows();

            string DynamicsURL = "dynamicsnav://{0}/{1}/{2}/runpage?page={3}";

            NavProcess.StartInfo = new ProcessStartInfo(String.Format(DynamicsURL, 
                                                             this.Server, 
                                                             this.Instance,
                                                             this.Company, 
                                                             PageNo));
            NavProcess.Kill();
            NavProcess.Start();

            IntPtr h = IsNAVPageOpen(baseline);
            
            var rect = new User32.Rect();
            User32.GetWindowRect(h, ref rect);
            int width = rect.right - rect.left;
            int height = rect.bottom - rect.top;
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics graphics = Graphics.FromImage(bmp);
            graphics.CopyFromScreen(rect.left, rect.top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
            //p.Kill();
            return bmp;
        }
        private static IntPtr IsNAVPageOpen(List<Window> baseline)
        {
            int timeout = 5;
            while (timeout > 0)
            {
                Thread.Sleep(1000);
                List<Window> Windows = Window.GetRunningWindows();
                if (Windows.Count > baseline.Count)
                {
                    foreach (var window in Windows)
                    {
                        bool found = false;
                        foreach (var basewindow in baseline)
                        {
                            if (basewindow.Title == window.Title)
                                found = true;

                        }
                        if (!found)
                        {
                            return window.Handle;
                        }
                    }
                }
                timeout--;
            }
            return (IntPtr)0;
        }
        /*
        List<Window> NewWindows = new List<Window>();
            int basecount = Windows.Count;
            int timeout = 5;
            while (timeout > 0)
            {
                Thread.Sleep(1000);
                Windows = Window.GetRunningWindows();
                foreach (var window in Windows)
                {
                    bool found = false;
                    foreach (var basewindow in baseline)
                    {
                        if (basewindow.Title == window.Title)
                            found = true;

                    }
                    if (!found)
                    {
                        NewWindows.Add(window);
                        break;
                    }
                }               
            }
            if (NewWindows.Count != 1)
                throw new Exception("More than one Dynamics window opened, failing");
            return NewWindows[0].Handle;
        }
         */

    }
    class User32
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);
    }
    /// <summary>Contains functionality to get all the open windows.</summary>
    public static class OpenWindowGetter
    {
        /// <summary>Returns a dictionary that contains the handle and title of all the open windows.</summary>
        /// <returns>A dictionary that contains the handle and title of all the open windows.</returns>
        public static IDictionary<IntPtr, string> GetOpenWindows()
        {
            IntPtr shellWindow = GetShellWindow();
            Dictionary<IntPtr, string> windows = new Dictionary<IntPtr, string>();

            EnumWindows(delegate(IntPtr hWnd, int lParam)
            {
                if (hWnd == shellWindow) return true;
                if (!IsWindowVisible(hWnd)) return true;

                int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

                StringBuilder builder = new StringBuilder(length);
                GetWindowText(hWnd, builder, length + 1);

                windows[hWnd] = builder.ToString();
                return true;

            }, 0);

            return windows;
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, int lParam);

        [DllImport("USER32.DLL")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("USER32.DLL")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("USER32.DLL")]
        private static extern IntPtr GetShellWindow();
    }
}
