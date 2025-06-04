// File: Form1.cs

#define TESTQUERY

using System;
using System.Net;
using System.Windows.Forms;
using Castle.Core.Logging;
using PSIMSLeads.PSIMSLeads;

namespace PSIMSLeads;

public partial class Form1 : Form
{
    private ConfigurationReader _configReader;
    private FoxTalkClient _foxTalkClient;
    private Logger _logger;

    public Form1()
    {
        InitializeComponent();
        timer.Interval = 5000; // Set timer interval to 5 seconds
        timer.Tick += Timer_Tick;

        _configReader = new ConfigurationReader();
        _configReader.LoadSettings();
        _logger = new Logger(txtLog, lblIdleTimer, lblSessionCounter, txtConnectLabel);
        _foxTalkClient = new FoxTalkClient(_logger, txtLog, lblIdleTimer, lblSessionCounter, txtConnectLabel);

        ConnectToFTServerAsync();
    }

    private void ShowMessage(string message)
    {
        timer.Start();
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        timer.Stop();
    }

    private async void ConnectToFTServerAsync()
    {
        var settings = _configReader.LoadSettings();
        var ipAddress = IPAddress.Parse(settings.TargetIP);

        try
        {
            await _foxTalkClient.ConnectAsync(settings.SendIPAddress, settings.SendPort);

            txtConnectLabel.Text = $"Connected to server {settings.SendIPAddress}:{settings.SendPort}";
            txtConnectLabel.Visible = true;

            lblSessionCounter.Visible = true;

            await _foxTalkClient.StartCoreListeningThread().ConfigureAwait(true);
        }
        catch (Exception ex)
        {
            _logger.LogResponse($"Error connecting: {ex.Message}");
        }
    }

    public void UpdateErrorLabel(string message)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(UpdateErrorLabel), message);
        }
        else
        {
            txtLog.AppendText($"{DateTime.Now:HH:mm:ss} - {message}{Environment.NewLine}");
            ShowMessage(message);
        }

        _logger.LogResponse(message);
    }

    private void btnSimulate_Click(object sender, EventArgs e)
    {
        _ = _foxTalkClient.SimulateConcurrentQueries(5, 10);
    }

    private void chkLog_CheckedChanged(object sender, EventArgs e)
    {
        _logger.IsLoggingEnabled = chkLog.Checked;
    }
}