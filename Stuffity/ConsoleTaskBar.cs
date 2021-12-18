using System;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MonitorSwitcher
{
    public class ConsoleTaskBar
    {
        private NotifyIcon notifyIcon = new NotifyIcon();
        public ConsoleTaskBar()
        {
            
        }

        public void Init()
        {
            var contextMenu = new ContextMenuStrip();

            contextMenu.Items.Add("Hide", null, ToggleClicked);
            contextMenu.Items.Add("Exit", null, (s, e) => { Application.Exit(); });
            notifyIcon.ContextMenuStrip = contextMenu;

            notifyIcon.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            notifyIcon.Visible = true;
            notifyIcon.Text = Application.ProductName;
        }


        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private static bool showing = true;
        static private void ToggleClicked(object sender, EventArgs e)
        {
            showing = !showing;
            SetConsoleWindowVisibility(showing);
            var tsi = sender as ToolStripItem;
            tsi.Text = showing ? "Hide" : "Show";
        }

        public static void SetConsoleWindowVisibility(bool visible)
        {
            IntPtr hWnd = FindWindow(null, Console.Title);
            if (hWnd != IntPtr.Zero)
            {
                if (visible) ShowWindow(hWnd, 1); //1 = SW_SHOWNORMAL           
                else ShowWindow(hWnd, 0); //0 = SW_HIDE               
            }
        }
    }
}
