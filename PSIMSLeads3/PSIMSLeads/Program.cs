// File: Program.cs
using System;
using System.Threading;
using System.Windows.Forms;

namespace PSIMSLeads
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            // Ensure single instance of the application
            using (var mutex = new Mutex(true, "PSIMSLeadsSingletonMutex", out var createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "Another instance of PSIMSLeads is already running.",
                        "Instance Already Running",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.ApplicationExit += (s, e) =>
                {
                    FoxTalkClient.GetFoxTalkClient()?.Close();
                };

                Application.Run(new Form1());
            }
        }
    }
}