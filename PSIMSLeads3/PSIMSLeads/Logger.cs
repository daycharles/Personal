using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PSIMSLeads;

public class Logger
{
    private static BlockingCollection<string> _logQueue = new();
    private static string _logFilePath;
    private static Task _logWorker;
    private static CancellationTokenSource _cts = new();
    private RichTextBox _textBox;
    private Label _lblIdleTimer;
    private Label _lblSessionCounter;
    private Label _txtConnectLabel;
    public bool IsLoggingEnabled { get; set; } = false;

    public Logger(RichTextBox textBox, Label lblIdleTimer, Label lblSessionCounter, Label txtConnectLabel)
    {
        _textBox = textBox;
        _lblIdleTimer = lblIdleTimer;
        _lblSessionCounter = lblSessionCounter;
        _txtConnectLabel = txtConnectLabel;
        InitLogger();
    }

    private void InitLogger()
    {
        if (!string.IsNullOrEmpty(_logFilePath)) return;

        string[] possiblePaths = ["D:\\PSIMS\\LOG", "E:\\PSIMS\\LOG", "F:\\PSIMS\\LOG", "C:\\PSIMS\\LOG"];
        foreach (var path in possiblePaths)
        {
            if (Directory.Exists(path))
            {
                var dir = Path.Combine(path, "PSIMSLeads3.0");
                Directory.CreateDirectory(dir);
                _logFilePath = Path.Combine(dir, "PSIMSLeads3.txt");
                break;
            }
        }

        _logWorker = Task.Run(async () =>
        {
            using var sw = new StreamWriter(new FileStream(_logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                AutoFlush = true
            };

            foreach (var logEntry in _logQueue.GetConsumingEnumerable(_cts.Token))
            {
                try
                {
                    await sw.WriteLineAsync(logEntry);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Logging error: " + ex.Message);
                }
            }
        });
    }

    public void LogResponse(string text)
    {
        if (!IsLoggingEnabled)
            return;
        var timestamped = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {text}";
        _logQueue.Add(timestamped);

        if (_textBox != null)
        {
            if (_textBox.InvokeRequired)
            {
                _textBox.Invoke((MethodInvoker)delegate
                {
                    _textBox.AppendText(timestamped + Environment.NewLine);
                });
            }
            else
            {
                _textBox.AppendText(timestamped + Environment.NewLine);
            }
        }
    }

    public void UpdateIdleTimeUI(string message)
    {
        if (_lblIdleTimer.InvokeRequired)
            _lblIdleTimer.Invoke(new Action<string>(UpdateIdleTimeUI), message);
        else
            _lblIdleTimer.Text = message;
    }

    public void UpdateSessionTimeUI(string message)
    {
        if (_lblSessionCounter.InvokeRequired)
            _lblSessionCounter.Invoke(new Action<string>(UpdateSessionTimeUI), message);
        else
            _lblSessionCounter.Text = message;
    }

    public void UpdateFooterUI()
    {
        if (_txtConnectLabel.InvokeRequired)
            _txtConnectLabel.Invoke(new Action(UpdateFooterUI));
        else
        {
            LogResponse("Connected to server successfully!");
            _txtConnectLabel.Text = "Connected to server:";
            _txtConnectLabel.Visible = true;
            _lblIdleTimer.Visible = true;
            _lblSessionCounter.Visible = true;
        }
    }

    public void ShutdownLogger()
    {
        _cts.Cancel();
        _logQueue.CompleteAdding();
    }
}
