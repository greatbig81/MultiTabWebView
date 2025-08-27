namespace MultiTabWebView
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1044, 757);
            Name = "Form1";
            Text = "멀티웹뷰";

            // 상단 유저 UI 패널 (100px 높이)
            userControlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.LightGray,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            // WebView 컨테이너 (나머지 공간)
            webViewContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            this.Controls.Add(webViewContainer);
            this.Controls.Add(userControlPanel);

            InitializeUserControls();
            InitializeTabControl();

            ResumeLayout(false);
        }

        private void InitializeUserControls()
        {
            // IP 시작 주소 입력
            var startIpLabel = new Label
            {
                Text = "시작 IP:",
                Location = new Point(10, 15),
                Size = new Size(60, 20)
            };
            userControlPanel.Controls.Add(startIpLabel);

            startIpTextBox = new TextBox
            {
                Location = new Point(75, 12),
                Size = new Size(120, 20),
                Text = "192.168.1.1"
            };
            userControlPanel.Controls.Add(startIpTextBox);

            // 갯수 입력
            var countLabel = new Label
            {
                Text = "갯수:",
                Location = new Point(210, 15),
                Size = new Size(40, 20)
            };
            userControlPanel.Controls.Add(countLabel);

            countNumericUpDown = new NumericUpDown
            {
                Location = new Point(255, 12),
                Size = new Size(60, 20),
                Minimum = 1,
                Maximum = 20,
                Value = 3
            };
            userControlPanel.Controls.Add(countNumericUpDown);

            // 생성 버튼
            generateButton = new Button
            {
                Text = "탭 생성",
                Location = new Point(330, 10),
                Size = new Size(80, 25),
                BackColor = Color.LightBlue
            };
            generateButton.Click += GenerateButton_Click;
            userControlPanel.Controls.Add(generateButton);

            // 모두 지우기 버튼
            clearAllButton = new Button
            {
                Text = "모두 지우기",
                Location = new Point(420, 10),
                Size = new Size(80, 25),
                BackColor = Color.LightCoral
            };
            clearAllButton.Click += ClearAllButton_Click;
            userControlPanel.Controls.Add(clearAllButton);

            
        }

        private void InitializeTabControl()
        {
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Multiline = true
            };
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
            //userTabPanel.Controls.Add(tabControl);
            webViewContainer.Controls.Add(tabControl);
        }

        #endregion
    }
}
