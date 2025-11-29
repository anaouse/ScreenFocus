using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace ScreenFocus;

public partial class Form1 : Form
{
    public class FocusRecord
    {
        public int elapsedMinutes { get; set; }
        public long timestamp { get; set; }
    }
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;

    private System.Windows.Forms.Timer workTimer;
    private System.Windows.Forms.Timer minusTimer;
    private Random random;

    // 用来控制菜单项的状态（比如点了Start就禁用Start，启用Stop）
    private ToolStripMenuItem startMenuItem;
    private ToolStripMenuItem stopMenuItem;

    // status
    private int nextInterval = 0;
    private int elapseMilli = 0;
    private int wholeMilli = 0;
    private long currentSessionTimestamp = 0;

    // exe位置
    private string baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "");
    private string icon_path = "";

    public Form1()
    {
        this.icon_path = Path.Combine(this.baseDirectory, "emo_face.ico");
        InitializeComponent();
        InitializeGrid();
        InitializeLogic(); 
        InitializeTray();
        ReloadRecords();
    }

    private void InitializeLogic()
    {
        workTimer = new System.Windows.Forms.Timer();
        minusTimer = new System.Windows.Forms.Timer();
        random = new Random();
        workTimer.Tick += WorkTimer_Tick;
        minusTimer.Interval = 60000; // 每分钟更新一次剩余时间
        minusTimer.Tick += (s, e) => {
            elapseMilli += minusTimer.Interval;
            int remaining = nextInterval - elapseMilli;
            TimeSpan remainingTime = TimeSpan.FromMilliseconds(remaining);
            string remainingString = $"{nextInterval / 60000} minutes, remain: " + remainingTime.ToString("mm\\:ss");
            trayIcon.Text = remainingString;
        };
    }

    private void WorkTimer_Tick(object? sender, EventArgs e)
    {
        workTimer.Stop();
        minusTimer.Stop();
        DialogResult result = MessageBox.Show(
            "Time out \n\n do you need focus on screen?", 
            "ScreenFocus Reminder", 
            MessageBoxButtons.YesNo, 
            MessageBoxIcon.Question, 
            MessageBoxDefaultButton.Button1, 
            MessageBoxOptions.DefaultDesktopOnly 
        );
        wholeMilli += nextInterval;     

        if (result == DialogResult.Yes)
        {
            StartFocusCycle();
        }
        else
        {
            StopFocusCycle();
        }
    }

    private void StartFocusCycle()
    {
        if (wholeMilli == 0)
        {
            currentSessionTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
        nextInterval = random.Next(17*60000, 23*60000);// 17-23分钟更新一次
        elapseMilli = 0;
        workTimer.Interval = nextInterval;
        workTimer.Start();
        minusTimer.Start();
        UpdateStatus(true, $"{nextInterval / 60000} minutes");
    }

    private void StopFocusCycle()
    {
        workTimer.Stop();
        minusTimer.Stop();
        UpdateStatus(false, $"ScreenFocus - stop - whole time: {wholeMilli / 60000}");
    }

    private void CleanFocusCycle()
    {
        StopFocusCycle();
        nextInterval = 0;
        elapseMilli = 0;
        wholeMilli = 0;
        currentSessionTimestamp = 0;
        
        trayIcon.Text = "ScreenFocus - waiting";
    }

    private void SaveData(int minutes, long startTime)
    {
        // 和exe同文件夹
        string filePath = Path.Combine(baseDirectory, "records.json");

        List<FocusRecord> records = new List<FocusRecord>();

        try
        {
            if (File.Exists(filePath))
            {
                string jsonContent = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(jsonContent))
                {
                    var existingData = JsonSerializer.Deserialize<List<FocusRecord>>(jsonContent);
                    if (existingData != null)
                    {
                        records = existingData;
                    }
                }
            }
            var newRecord = new FocusRecord
            {
                elapsedMinutes = minutes,
                timestamp = startTime
            };

            records.Add(newRecord);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string newJsonContent = JsonSerializer.Serialize(records, options);
            File.WriteAllText(filePath, newJsonContent);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save data: {ex.Message}");
        }
    }
    
    private void UpdateStatus(bool isRunning, string text)
    {
        startMenuItem.Enabled = !isRunning;
        stopMenuItem.Enabled = isRunning;
        trayIcon.Text = text;
    }
    
    private void InitializeTray()
    {
        trayMenu = new ContextMenuStrip();
        trayIcon = new NotifyIcon();

        trayMenu.Items.Add("show homepage", null, (sender, e) => {
            this.Show();
            this.Activate();
        });
        trayMenu.Items.Add(new ToolStripSeparator());

        startMenuItem = new ToolStripMenuItem("Start", null, (s, e) => StartFocusCycle());
        trayMenu.Items.Add(startMenuItem);
        trayMenu.Items.Add(new ToolStripSeparator());

        stopMenuItem = new ToolStripMenuItem("Stop", null, (s, e) => StopFocusCycle());
        stopMenuItem.Enabled = false;
        trayMenu.Items.Add(stopMenuItem);
        trayMenu.Items.Add(new ToolStripSeparator());

        trayMenu.Items.Add("Clean", null, (sender, e) => CleanFocusCycle());
        trayMenu.Items.Add(new ToolStripSeparator());

        trayMenu.Items.Add("Exit", null, (sender, e) => {
            trayIcon.Visible = false;
            CleanFocusCycle();
            Application.Exit();
        });

        trayIcon.Icon = new Icon(icon_path);
        trayIcon.Text = "ScreenFocus - waiting";
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        trayIcon.DoubleClick += (sender, e) => {
            this.Show();
            this.Activate();
        };
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if(e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            base.OnFormClosing(e);
        }
    }
}
