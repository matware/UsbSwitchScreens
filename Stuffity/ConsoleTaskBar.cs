using System;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace MonitorSwitcher
{
    public class ConsoleTaskBar
    {
        ToolStripItem showHideItem;
        private NotifyIcon notifyIcon = new NotifyIcon();
        public ConsoleTaskBar()
        {
            
        }

        public bool ExitRequested { get; private set; } = false;

        public void Init(Dictionary<string,Action> menuItems, Action power = null)
        {
            var contextMenu = new ContextMenuStrip();

            if(menuItems != null && menuItems.Count > 0)
            {
                foreach(var key in menuItems.Keys)
                {
                    contextMenu.Items.Add(key, null, (s,e)=> { menuItems[key]();});
                }

                contextMenu.Items.Add("-");
            }

            showHideItem = contextMenu.Items.Add("Hide", null, ToggleClicked);

            contextMenu.Items.Add("Exit", null, (s, e) => {
                ExitRequested = true;
                Application.Exit();
            });
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
        private void ToggleClicked(object sender, EventArgs e)
        {
            Switch(!showing);
        }      

        private void Switch(bool newState)
        {
            showing = newState;
            SetConsoleWindowVisibility(showing);            
            showHideItem.Text = showing ? "Hide" : "Show";
        }

        public void HideConsole()
        {
            Switch(false);
        }

        public void ShowConsole()
        {
            Switch(true);
        }

        private void SetConsoleWindowVisibility(bool visible)
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
