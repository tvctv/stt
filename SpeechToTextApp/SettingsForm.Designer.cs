namespace SpeechToTextApp
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtHost = new System.Windows.Forms.TextBox();
            this.numPort = new System.Windows.Forms.NumericUpDown();
            this.chkUdp = new System.Windows.Forms.CheckBox();
            this.chkEnableSce = new System.Windows.Forms.CheckBox();
            this.cmbAudio = new System.Windows.Forms.ComboBox();
            this.btnTestAudio = new System.Windows.Forms.Button();
            this.prgLevel = new System.Windows.Forms.ProgressBar();
            this.lblLatency = new System.Windows.Forms.Label();
            this.trkLatency = new System.Windows.Forms.TrackBar();
            this.chkProfanity = new System.Windows.Forms.CheckBox();
            this.chkUseGpu = new System.Windows.Forms.CheckBox();
            this.chkDetailed = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.lblHost = new System.Windows.Forms.Label();
            this.lblPort = new System.Windows.Forms.Label();
            this.lblAudio = new System.Windows.Forms.Label();
            this.lblLevel = new System.Windows.Forms.Label();
            this.lblModel = new System.Windows.Forms.Label();
            this.txtModelPath = new System.Windows.Forms.TextBox();
            this.btnBrowseModel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkLatency)).BeginInit();
            this.SuspendLayout();
            // 
            // txtHost
            // 
            this.txtHost.SetBounds(140, 18, 200, 24);
            this.txtHost.TabIndex = 0;
            // 
            // numPort
            // 
            this.numPort.Maximum = 65535;
            this.numPort.SetBounds(400, 18, 80, 24);
            this.numPort.TabIndex = 2;
            // 
            // chkUdp
            // 
            this.chkUdp.AutoSize = true;
            this.chkUdp.Location = new System.Drawing.Point(500, 20);
            this.chkUdp.TabIndex = 3;
            this.chkUdp.Text = "Use UDP";
            // 
            // chkEnableSce
            // 
            this.chkEnableSce.AutoSize = true;
            this.chkEnableSce.Location = new System.Drawing.Point(500, 50);
            this.chkEnableSce.TabIndex = 4;
            this.chkEnableSce.Text = "Enable SCE Output";
            // 
            // cmbAudio
            // 
            this.cmbAudio.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbAudio.SetBounds(140, 78, 240, 24);
            this.cmbAudio.TabIndex = 5;
            // 
            // btnTestAudio
            // 
            this.btnTestAudio.SetBounds(390, 77, 80, 27);
            this.btnTestAudio.TabIndex = 6;
            this.btnTestAudio.Text = "Test";
            this.btnTestAudio.UseVisualStyleBackColor = true;
            this.btnTestAudio.Click += new System.EventHandler(this.btnTestAudio_Click);
            // 
            // prgLevel
            // 
            this.prgLevel.SetBounds(540, 78, 120, 24);
            this.prgLevel.TabIndex = 8;
            // 
            // lblLatency
            // 
            this.lblLatency.AutoSize = true;
            this.lblLatency.Location = new System.Drawing.Point(20, 160);
            this.lblLatency.TabIndex = 11;
            this.lblLatency.Text = "Latency: 1000 ms";
            // 
            // trkLatency
            // 
            this.trkLatency.LargeChange = 250;
            this.trkLatency.Location = new System.Drawing.Point(140, 152);
            this.trkLatency.Maximum = 3000;
            this.trkLatency.Minimum = 250;
            this.trkLatency.SmallChange = 50;
            this.trkLatency.TabIndex = 12;
            this.trkLatency.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trkLatency.Value = 1000;
            this.trkLatency.ValueChanged += new System.EventHandler(this.trkLatency_ValueChanged);
            // 
            // chkProfanity
            // 
            this.chkProfanity.AutoSize = true;
            this.chkProfanity.Location = new System.Drawing.Point(20, 200);
            this.chkProfanity.TabIndex = 13;
            this.chkProfanity.Text = "Enable Profanity Filter";
            // 
            // chkUseGpu
            // 
            this.chkUseGpu.AutoSize = true;
            this.chkUseGpu.Location = new System.Drawing.Point(220, 200);
            this.chkUseGpu.TabIndex = 14;
            this.chkUseGpu.Text = "Use GPU (if available)";
            // 
            // chkDetailed
            // 
            this.chkDetailed.AutoSize = true;
            this.chkDetailed.Location = new System.Drawing.Point(420, 200);
            this.chkDetailed.TabIndex = 15;
            this.chkDetailed.Text = "Detailed Logging";
            // 
            // btnSave
            // 
            this.btnSave.SetBounds(540, 240, 120, 34);
            this.btnSave.TabIndex = 16;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // lblHost
            // 
            this.lblHost.AutoSize = true;
            this.lblHost.Location = new System.Drawing.Point(20, 22);
            this.lblHost.TabIndex = 17;
            this.lblHost.Text = "SCE Host";
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(360, 22);
            this.lblPort.TabIndex = 18;
            this.lblPort.Text = "Port";
            // 
            // lblAudio
            // 
            this.lblAudio.AutoSize = true;
            this.lblAudio.Location = new System.Drawing.Point(20, 82);
            this.lblAudio.TabIndex = 19;
            this.lblAudio.Text = "Audio Input";
            // 
            // lblLevel
            // 
            this.lblLevel.AutoSize = true;
            this.lblLevel.Location = new System.Drawing.Point(480, 82);
            this.lblLevel.TabIndex = 20;
            this.lblLevel.Text = "Input Level";
            // 
            // lblModel
            // 
            this.lblModel.AutoSize = true;
            this.lblModel.Location = new System.Drawing.Point(20, 122);
            this.lblModel.TabIndex = 21;
            this.lblModel.Text = "Model File";
            // 
            // txtModelPath
            // 
            this.txtModelPath.ReadOnly = true;
            this.txtModelPath.SetBounds(140, 118, 360, 24);
            this.txtModelPath.TabIndex = 9;
            // 
            // btnBrowseModel
            // 
            this.btnBrowseModel.SetBounds(510, 117, 150, 27);
            this.btnBrowseModel.TabIndex = 10;
            this.btnBrowseModel.Text = "Browse...";
            this.btnBrowseModel.UseVisualStyleBackColor = true;
            this.btnBrowseModel.Click += new System.EventHandler(this.btnBrowseModel_Click);
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(700, 300);
            this.Controls.Add(this.btnBrowseModel);
            this.Controls.Add(this.txtModelPath);
            this.Controls.Add(this.lblModel);
            this.Controls.Add(this.lblLevel);
            this.Controls.Add(this.lblAudio);
            this.Controls.Add(this.lblPort);
            this.Controls.Add(this.lblHost);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.chkDetailed);
            this.Controls.Add(this.chkUseGpu);
            this.Controls.Add(this.chkProfanity);
            this.Controls.Add(this.trkLatency);
            this.Controls.Add(this.lblLatency);
            this.Controls.Add(this.prgLevel);
            this.Controls.Add(this.btnTestAudio);
            this.Controls.Add(this.cmbAudio);
            this.Controls.Add(this.chkEnableSce);
            this.Controls.Add(this.chkUdp);
            this.Controls.Add(this.numPort);
            this.Controls.Add(this.txtHost);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Settings";
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trkLatency)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.NumericUpDown numPort;
        private System.Windows.Forms.CheckBox chkUdp;
        private System.Windows.Forms.CheckBox chkEnableSce;
        private System.Windows.Forms.ComboBox cmbAudio;
        private System.Windows.Forms.Button btnTestAudio;
        private System.Windows.Forms.ProgressBar prgLevel;
        private System.Windows.Forms.Label lblLatency;
        private System.Windows.Forms.TrackBar trkLatency;
        private System.Windows.Forms.CheckBox chkProfanity;
        private System.Windows.Forms.CheckBox chkUseGpu;
        private System.Windows.Forms.CheckBox chkDetailed;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lblHost;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.Label lblAudio;
        private System.Windows.Forms.Label lblLevel;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.TextBox txtModelPath;
        private System.Windows.Forms.Button btnBrowseModel;
    }
}

