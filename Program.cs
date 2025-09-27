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

    ListBox lst;
    ComboBox cmbSort;
    System.Windows.Forms.Timer refreshTimer;
    Process[] cachedProcesses = new Process[0];

    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int nLeftRect,int nTopRect,int nRightRect,int nBottomRect,int nWidthEllipse,int nHeightEllipse);
    const int SW_RESTORE = 9;

    public MiniControlIsland()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(620, 360);
        TopMost = true;
        BackColor = Color.FromArgb(30, 30, 30);
        Opacity = 0.95;
        ShowInTaskbar = false;
        Region = Region.FromHrgn(CreateRoundRectRgn(0,0,Width,Height,20,20));

        lst = new ListBox() { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.None };
        Controls.Add(lst);

        Panel top = new Panel() { Height = 36, Dock = DockStyle.Top, Padding = new Padding(8) };
        Label lbl = new Label() { Text = "Mini Control Island", AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(8,8) };
        cmbSort = new ComboBox() { Width = 200, Location = new Point(180,4), DropDownStyle = ComboBoxStyle.DropDownList };
        cmbSort.Items.AddRange(new string[]{"Najważniejsze","Pamięć","Nazwa"});
        cmbSort.SelectedIndex = 0;
        cmbSort.SelectedIndexChanged += (s,e)=> RefreshProcesses();
        top.Controls.Add(lbl); top.Controls.Add(cmbSort);
        Controls.Add(top);

        refreshTimer = new System.Windows.Forms.Timer() { Interval = 2000 };
        refreshTimer.Tick += (s,e) => RefreshProcesses();
        refreshTimer.Start();

        KeyPreview = true;
        KeyDown += MiniControlIsland_KeyDown;

        var ok = RegisterHotKey(Handle, HOTKEY_ID, MOD_CONTROL, VK_SPACE);
        if(!ok) MessageBox.Show("Hotkey error");

        Visible = false;
        RefreshProcesses();
    }

    private void MiniControlIsland_KeyDown(object? sender, KeyEventArgs e)
    {
        int idx = -1;
        if(e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9) idx = (int)(e.KeyCode - Keys.D1);
        if(e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9) idx = (int)(e.KeyCode - Keys.NumPad1);
        if(idx >= 0 && idx < lst.Items.Count)
        {
            var pct = cachedProcesses[idx];
            if(e.Control)
            {
                try { pct.Kill(); } catch {}
                RefreshProcesses();
            }
            else if(e.Alt)
            {
                MessageBox.Show($"Ustawienia {pct.ProcessName}");
            }
            else
            {
                try { ShowWindow(pct.MainWindowHandle, SW_RESTORE); SetForegroundWindow(pct.MainWindowHandle); } catch {}
                Visible = false;
            }
        }
    }

    protected override void WndProc(ref Message m)
    {
        if(m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
        {
            Visible = !Visible;
            if(Visible) RefreshProcesses();
        }
        base.WndProc(ref m);
    }

    void RefreshProcesses()
    {
        var all = Process.GetProcesses().Where(p=>!string.IsNullOrEmpty(p.MainWindowTitle)).ToArray();
        if(cmbSort.SelectedIndex==1) all = all.OrderByDescending(p=>{ try{return p.WorkingSet64;}catch{return 0;} }).ToArray();
        if(cmbSort.SelectedIndex==2) all = all.OrderBy(p=>p.ProcessName).ToArray();
        cachedProcesses = all;
        lst.Items.Clear();
        for(int i=0;i<all.Length && i<9;i++) lst.Items.Add($"{i+1}. {all[i].ProcessName}");
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