using System.Windows.Forms;

namespace PSIMSLeads
{
    partial class Form1 : System.Windows.Forms.Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.Button btnClearLog;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Method for Designer support – do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.panelHeader = new System.Windows.Forms.Panel();
            this.btnSimulate = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.btnClearLog = new System.Windows.Forms.Button();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.txtConnectLabel = new System.Windows.Forms.Label();
            this.lblSessionCounter = new System.Windows.Forms.Label();
            this.lblIdleTimer = new System.Windows.Forms.Label();
            this.chkLog = new System.Windows.Forms.CheckBox();
            this.panelHeader.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.SlateGray;
            this.panelHeader.Controls.Add(this.btnSimulate);
            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Size = new System.Drawing.Size(686, 52);
            this.panelHeader.TabIndex = 0;
            // 
            // btnSimulate
            // 
            this.btnSimulate.Location = new System.Drawing.Point(599, 12);
            this.btnSimulate.Name = "btnSimulate";
            this.btnSimulate.Size = new System.Drawing.Size(75, 23);
            this.btnSimulate.TabIndex = 1;
            this.btnSimulate.Text = "SimulateQueries";
            this.btnSimulate.UseVisualStyleBackColor = true;
            this.btnSimulate.Click += new System.EventHandler(this.btnSimulate_Click);
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 20F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(17, 9);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(390, 37);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "PSIMSLeads 3.0 – Log Viewer";
            // 
            // txtLog
            // 
            this.txtLog.BackColor = System.Drawing.Color.Black;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 10F);
            this.txtLog.ForeColor = System.Drawing.Color.Lime;
            this.txtLog.Location = new System.Drawing.Point(10, 61);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(666, 391);
            this.txtLog.TabIndex = 1;
            this.txtLog.Text = "";
            this.txtLog.WordWrap = false;
            // 
            // btnClearLog
            // 
            this.btnClearLog.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.btnClearLog.Location = new System.Drawing.Point(10, 459);
            this.btnClearLog.Name = "btnClearLog";
            this.btnClearLog.Size = new System.Drawing.Size(86, 30);
            this.btnClearLog.TabIndex = 2;
            this.btnClearLog.Text = "Clear Log";
            this.btnClearLog.UseVisualStyleBackColor = true;
            this.btnClearLog.Click += new System.EventHandler(this.btnClearLog_Click);
            // 
            // txtConnectLabel
            // 
            this.txtConnectLabel.AutoSize = true;
            this.txtConnectLabel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtConnectLabel.Location = new System.Drawing.Point(116, 466);
            this.txtConnectLabel.Name = "txtConnectLabel";
            this.txtConnectLabel.Size = new System.Drawing.Size(90, 17);
            this.txtConnectLabel.TabIndex = 3;
            this.txtConnectLabel.Text = "Connect Label";
            // 
            // lblSessionCounter
            // 
            this.lblSessionCounter.AutoSize = true;
            this.lblSessionCounter.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSessionCounter.Location = new System.Drawing.Point(344, 466);
            this.lblSessionCounter.Name = "lblSessionCounter";
            this.lblSessionCounter.Size = new System.Drawing.Size(102, 17);
            this.lblSessionCounter.TabIndex = 4;
            this.lblSessionCounter.Text = "Session Counter";
            // 
            // lblIdleTimer
            // 
            this.lblIdleTimer.AutoSize = true;
            this.lblIdleTimer.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblIdleTimer.Location = new System.Drawing.Point(502, 466);
            this.lblIdleTimer.Name = "lblIdleTimer";
            this.lblIdleTimer.Size = new System.Drawing.Size(66, 17);
            this.lblIdleTimer.TabIndex = 5;
            this.lblIdleTimer.Text = "idle Timer";
            // 
            // chkLog
            // 
            this.chkLog.AutoSize = true;
            this.chkLog.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkLog.Location = new System.Drawing.Point(10, 500);
            this.chkLog.Name = "chkLog";
            this.chkLog.Size = new System.Drawing.Size(75, 21);
            this.chkLog.TabIndex = 6;
            this.chkLog.Text = "Logging";
            this.chkLog.UseVisualStyleBackColor = true;
            this.chkLog.CheckedChanged += new System.EventHandler(this.chkLog_CheckedChanged);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.WhiteSmoke;
            this.ClientSize = new System.Drawing.Size(686, 529);
            this.Controls.Add(this.chkLog);
            this.Controls.Add(this.lblIdleTimer);
            this.Controls.Add(this.lblSessionCounter);
            this.Controls.Add(this.txtConnectLabel);
            this.Controls.Add(this.btnClearLog);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.panelHeader);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PSIMSLeads v3.0.0";
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private void btnClearLog_Click(object sender, System.EventArgs e)
        {
            // Clear the log
            this.txtLog.Clear();
        }

        private Timer timer;
        private Label txtConnectLabel;
        private Label lblSessionCounter;
        private Label lblIdleTimer;
        private Button btnSimulate;
        private CheckBox chkLog;
    }
}
