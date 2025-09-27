using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class MiniControlIsland : Form
{
    const int HOTKEY_ID = 0x1000;
    const uint MOD_CONTROL = 0x0002;
    const uint VK_SPACE = 0x20;
    const int WM_HOTKEY = 0x0312;

    NotifyIcon trayIcon;

    ListBox lst;
    ComboBox cmbSort;
    System.Windows.Forms.Timer refreshTimer;
    Process[] cachedProcesses = new Process[0];

    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int nLeftRect,int nTopRect,int nRightRect,int nBottomRect,int nWidthEllipse,int nHeightEllipse);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);
    delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    const int SW_RESTORE = 9;

    public MiniControlIsland()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(620, 360);
        TopMost = true;
        BackColor = Color.FromArgb(30, 0, 80);
        Opacity = 0.95;
        ShowInTaskbar = false;
        Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));

        lst = new ListBox() { Dock = DockStyle.Fill, Font = new Font("SF-Pro-Display", 10), BorderStyle = BorderStyle.None};
        Controls.Add(lst);

        Panel top = new Panel() { Height = 36, Dock = DockStyle.Top, Padding = new Padding(8) };
        Label lbl = new Label() { Text = "MCI - Mini Control Island", AutoSize = true, ForeColor = Color.White, Font = new Font("SF-Pro-Display", 10, FontStyle.Bold), Location = new Point(8, 8) };
        cmbSort = new ComboBox() { Width = 200, Location = new Point(180, 4), DropDownStyle = ComboBoxStyle.DropDownList};
        cmbSort.Items.AddRange(new string[] { "Important", "Storage", "Name" });
        cmbSort.SelectedIndex = 0;
        cmbSort.SelectedIndexChanged += (s, e) => RefreshProcesses();
        top.Controls.Add(lbl); top.Controls.Add(cmbSort);
        Controls.Add(top);

        refreshTimer = new System.Windows.Forms.Timer() { Interval = 2000 };
        refreshTimer.Tick += (s, e) => RefreshProcesses();
        refreshTimer.Start();

        KeyPreview = true;
        KeyDown += MiniControlIsland_KeyDown;

        var ok = RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL, VK_SPACE);
        if (!ok) MessageBox.Show("Hotkey error");

        Visible = false;
        RefreshProcesses();

        trayIcon = new NotifyIcon();
        trayIcon.Icon = SystemIcons.Application;
        trayIcon.Visible = true;
        trayIcon.Text = "MCI";

        var menu = new ContextMenuStrip();
        menu.Items.Insert(0, new ToolStripLabel("MCI by 1NT4K"));
        menu.Items.Add("Settings", null, (s, e) => { var SettingsWindow = new SettingsWindow { Owner = this }; SettingsWindow.ShowDialog(); });
        menu.Items.Add("∑ author", null, (s, e) => OpenGithub());
        menu.Items.Insert(3, new ToolStripLabel("––––––––––––"));
        menu.Items.Add("✓ Open", null, (s, e) => { Visible = true; RefreshProcesses(); });
        menu.Items.Add("⟳ Restart", null, (s, e) => RestartApp());
        menu.Items.Add("✕ Close", null, (s, e) => Application.Exit());
        trayIcon.ContextMenuStrip = menu;

        
    }

    public class SettingsWindow : Form
    {

        [DllImport("gdi32.dll")]
        static extern IntPtr CreateRoundRectRgn(
            int nLeftRect, int nTopRect, int nRightRect, int nBottomRect,
            int nWidthEllipse, int nHeightEllipse);

        public SettingsWindow()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(620, 360);
            TopMost = true;
            BackColor = Color.FromArgb(40, 40, 40);
            Opacity = 0.98;
            ShowInTaskbar = false;
            Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 20, 20));


            Label lbl = new Label()
            {
                Text = "⚙ Settings",
                ForeColor = Color.White,
                Font = new Font("SF-Pro-Display", 12, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true
            };
            Controls.Add(lbl);
            Button btnDark = new Button()
            {
                Text = "Dark Theme",
                Location = new Point(20, 60),
                Width = 150,
                BackColor = Color.MediumPurple,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDark.Click += (s, e) => Owner.BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(btnDark);

            
            Button btnPurple = new Button()
            {
                Text = "Purple Theme",
                Location = new Point(20, 110),
                Width = 150,
                BackColor = Color.MediumPurple,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnPurple.Click += (s, e) => Owner.BackColor = Color.FromArgb(30, 0, 25);
            Controls.Add(btnPurple);

            Button btnBlue = new Button()
            {
                Text = "Blue Theme",
                Location = new Point(20, 160),
                Width = 150,
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnBlue.Click += (s, e) => Owner.BackColor = Color.FromArgb(80, 120, 200);
            Controls.Add(btnBlue);



            Button closeBtn = new Button()
            {
                Text = "✕",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Popup,
                Size = new Size(30, 30),
                Location = new Point(this.Width - 40, 10)
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.Click += (s, e) => this.Close();
            Controls.Add(closeBtn);

            Shown += (s, e) =>
            {
                if (Owner != null)
                { Location = new Point(Owner.Left + 0, Owner.Top + 0); }
            };
            
        }
    }

    void    OpenGithub()
        {
            try
            {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Antos1812/Mini_Island",
                UseShellExecute = true
            });
            }
            catch (Exception ex)
            {
            MessageBox.Show("Error: Cannot open - " + ex.Message);
            }
        }

    // void SettingsColor()
    // {
    //     try
    //     {
    //         FormBorderStyle = FormBorderStyle.None;
    //         StartPosition = FormStartPosition.CenterScreen;
    //         Size = new Size(620, 360);
    //         TopMost = true;
    //         BackColor = Color.FromArgb(30, 0, 30);
    //         Opacity = 0.95;

    //         Panel top = new Panel() { Height = 36, Dock = DockStyle.Top, Padding = new Padding(8) };
    //         Label lbl = new Label() { Text = "MCI - Mini Control Island", AutoSize = true, ForeColor = Color.White, Font = new Font("SF-Pro-Display", 10, FontStyle.Bold), Location = new Point(8, 8) };
    //         cmbSort = new ComboBox() { Width = 200, Location = new Point(180, 4), DropDownStyle = ComboBoxStyle.DropDownList };

    //     }
    //     catch (Exception)
    //     {
    //         MessageBox.Show("Cannot load Settings (￣_,￣ )");
    //     }
    // }


    // void DarkMode()
    // {
    //     try
    //     {
    //         BackColor = Color.FromArgb(80, 120, 200);
    //     }
    //     catch (Exception)
    //     {
    //         MessageBox.Show("Cannot change to dark (￣_,￣ )");
    //     }
    // }

    void RestartApp()
    {
        try
        {
            Process.Start(Application.ExecutablePath);

            Application.Exit();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Cannot restart CRY BOUT IT :" + ex.Message);
        }
    }
    

    private void MiniControlIsland_KeyDown(object? sender, KeyEventArgs e)
    {
        int idx = -1;
        if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9) idx = (int)(e.KeyCode - Keys.D1);
        if (e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9) idx = (int)(e.KeyCode - Keys.NumPad1);
        if (idx >= 0 && idx < lst.Items.Count)
        {
            var pct = cachedProcesses[idx];
            if (e.Control)
            {
                try { pct.Kill(); } catch { }
                RefreshProcesses();
            }
            else if (e.Alt)
            {
                MessageBox.Show($"Settings {pct.ProcessName}");
            }
            else
            {
                OpenOrFocusProcess(pct);
            }
        }
    }

    protected override void WndProc(ref Message m)
    {
        if(m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            Visible = !Visible;
            if (Visible)
            {
                RefreshProcesses();
                Activate();
                Focus();
            }
        }
        base.WndProc(ref m);
    }

    void RefreshProcesses()
    {
        var allProcesses = Process.GetProcesses();
        var trayProcesses = new List<Process>();

        foreach (var p in allProcesses)
        {
            try
            {
                IntPtr hwnd = GetTopLevelWindowForProcess(p.Id);
                if (hwnd != IntPtr.Zero)
                {
                    if (!IsWindowOnTaskbar(hwnd))
                    {
                        trayProcesses.Add(p);
                    }
                }
            }
            catch { }
        }

        if (cmbSort.SelectedIndex == 1)
            trayProcesses = trayProcesses.OrderByDescending(p => { try { return p.WorkingSet64; } catch { return 0; } }).ToList();
        if (cmbSort.SelectedIndex == 2)
            trayProcesses = trayProcesses.OrderBy(p => p.ProcessName).ToList();

        cachedProcesses = trayProcesses.ToArray();
        lst.Items.Clear();
        for (int i = 0; i < trayProcesses.Count && i < 20; i++)
            lst.Items.Add($"{i + 1}. {trayProcesses[i].ProcessName}");
    }




    [DllImport("user32.dll")]
static extern int GetWindowLong(IntPtr hWnd, int nIndex);
const int GWL_EXSTYLE = -20;
const int WS_EX_APPWINDOW = 0x00040000;
const int WS_EX_TOOLWINDOW = 0x00000080;

    bool IsWindowOnTaskbar(IntPtr hWnd)
    {
        int exStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        return (exStyle & WS_EX_APPWINDOW) != 0;  // If it has WS_EX_APPWINDOW, it’s on taskbar
    }




    void OpenOrFocusProcess(Process p)
    {
        try
        {
            IntPtr h = GetTopLevelWindowForProcess(p.Id);
            if (h != IntPtr.Zero)
            {
                ShowWindow(h, SW_RESTORE);
                SetForegroundWindow(h);
                Visible = false;
                return;
            }

            if (p.MainWindowHandle != IntPtr.Zero)
            {
                try { ShowWindow(p.MainWindowHandle, SW_RESTORE); SetForegroundWindow(p.MainWindowHandle); Visible = false; return; } catch { }

            }
            string path = null;
            try { path = p.MainModule.FileName; } catch { }
            if (!string.IsNullOrEmpty(path))
            {
                try { Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }); Visible = false; return; } catch { }
            }
        }
        catch { }
        MessageBox.Show("Cannot open process" + p.ProcessName);
    }

    IntPtr GetTopLevelWindowForProcess(int pid)
    {
        IntPtr found = IntPtr.Zero;
        EnumWindows((hWnd, lParam) =>
        {
            GetWindowThreadProcessId(hWnd, out uint proc);
            if (proc == pid && IsWindowVisible(hWnd))
            {
                found = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        UnregisterHotKey(Handle, HOTKEY_ID);
        base.OnFormClosing(e);
    }

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.Run(new MiniControlIsland());
    }
}