using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ScreenFocus;

public partial class Form1
{
    // UI 控件定义移动到这里
    private DataGridView recordsGrid;
    private Label statsLabel;
    private Panel topPanel;

    private void InitializeGrid()
    {
        this.Text = "ScreenFocus Records";
        this.Size = new Size(1200, 800);
        this.Icon = new Icon(icon_path); 

        // top pannel
        topPanel = new Panel();
        topPanel.Dock = DockStyle.Top;
        topPanel.Height = 100;
        topPanel.Padding = new Padding(5);
        topPanel.BackColor = Color.FromArgb(240, 240, 240);

        // stats label
        statsLabel = new Label();
        statsLabel.Dock = DockStyle.Fill;
        statsLabel.TextAlign = ContentAlignment.MiddleLeft;
        statsLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        statsLabel.Text = "Calculating...";

        topPanel.Controls.Add(statsLabel);
        this.Controls.Add(topPanel);

        // grid
        recordsGrid = new DataGridView();
        recordsGrid.Dock = DockStyle.Fill;
        recordsGrid.AllowUserToAddRows = false;
        recordsGrid.AllowUserToDeleteRows = false;
        recordsGrid.ReadOnly = true;
        recordsGrid.RowHeadersVisible = true;
        recordsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        recordsGrid.BackgroundColor = Color.White;
        recordsGrid.BorderStyle = BorderStyle.None;
        
        recordsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.WhiteSmoke;
        recordsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        recordsGrid.EnableHeadersVisualStyles = false;

        recordsGrid.Columns.Add("Time", "Start Time (UTC+8)");
        recordsGrid.Columns.Add("Duration", "Duration");

        this.Controls.Add(recordsGrid);
        recordsGrid.BringToFront();
    }

    // reload and refresh grid
    private void ReloadRecords()
    {
        string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "records.json");

        recordsGrid.Rows.Clear();
        statsLabel.Text = "No records yet.";

        if (!File.Exists(filePath)) return;

        try
        {
            string jsonContent = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(jsonContent)) return;

            var records = JsonSerializer.Deserialize<List<FocusRecord>>(jsonContent);

            if (records != null && records.Count > 0)
            {
                var sortedRecords = records.OrderByDescending(r => r.timestamp);
                foreach (var r in sortedRecords)
                {
                    DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(r.timestamp);
                    DateTimeOffset utc8Time = dateTimeOffset.ToOffset(TimeSpan.FromHours(8));
                    string timeStr = utc8Time.ToString("yyyy-MM-dd HH:mm:ss");
                    string durationStr = $"{r.elapsedMinutes} min";
                    recordsGrid.Rows.Add(timeStr, durationStr);
                }
                
                CalculateAndShowStats(records);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Load data error: " + ex.Message);
        }
    }

    // stats
    private void CalculateAndShowStats(List<FocusRecord> records)
    {
        int uniqueDays = records
            .Select(r => DateTimeOffset.FromUnixTimeSeconds(r.timestamp)
                                       .ToOffset(TimeSpan.FromHours(8))
                                       .ToString("yyyy-MM-dd"))
            .Distinct()
            .Count();

        int totalMinutes = records.Sum(r => r.elapsedMinutes);
        double averageMinutes = uniqueDays > 0 ? (double)totalMinutes / uniqueDays : 0;

        statsLabel.Text = $"📅 Total Days: {uniqueDays} days\n" +
                          $"⏳ Avg Focus: {averageMinutes:F1} min / day";
    }
}
