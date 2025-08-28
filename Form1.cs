using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MultiTabWebView
{
    public partial class Form1 : Form
    {
        private Panel userControlPanel;
        private TabControl tabControl;
        private Panel webViewContainer;
        private GroupBox viewTypeGroupBox;

        private TextBox startIpTextBox;
        private NumericUpDown countNumericUpDown;
        private Button generateButton;
        private Button clearAllButton;

        private Dictionary<TabPage, List<WebView2>> tabWebViewMap;

        private System.Windows.Forms.Timer genTimer;

        private string _startIp = "";
        private int _genCount = 0;
        private int _genIndex = 0;

        public enum ViewType
        {
            Single,
            Double
        }

        public Form1()
        {
            InitializeComponent();
            tabWebViewMap = new Dictionary<TabPage, List<WebView2>>();

            // 5�ʸ��� ����Ǵ� Ÿ�̸� ���� �� ����
            genTimer = new System.Windows.Forms.Timer();
            genTimer.Interval = 500; // 2�� (5000ms)
            genTimer.Tick += OnTimerTick;
            
        }

        // Ÿ�̸� Tick �̺�Ʈ �ڵ鷯 �Լ�
        private async void OnTimerTick(object sender, EventArgs e)
        {
            if (!IsValidIpAddress(_startIp)) return;
            if (_genIndex >= _genCount)
            {
                genTimer.Stop();
                return;
            }

            genTimer.Interval = 1500;

            var baseIp = IPAddress.Parse(_startIp);
            var ipBytes = baseIp.GetAddressBytes();

            // ������ ���� ����
            int newLastOctet = ipBytes[3] + _genIndex;
            if (newLastOctet > 255)
            {
                genTimer.Stop();
                MessageBox.Show("IP �ּ� ������ �ʰ��߽��ϴ�.", "���", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            ipBytes[3] = (byte)newLastOctet;
            var currentIp = new IPAddress(ipBytes).ToString();

            await GenerateTabsAndWebViews(currentIp, 1);

            _genIndex++;
        }

        private async void GenerateButton_Click(object sender, EventArgs e)
        {
            generateButton.Enabled = false;

            try
            {
                _startIp = startIpTextBox.Text.Trim();
                _genCount = (int)countNumericUpDown.Value;

                if (!IsValidIpAddress(_startIp))
                {
                    MessageBox.Show("��ȿ�� IP �ּҸ� �Է����ּ���.", "����",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //await GenerateTabsAndWebViews(startIp, count);

                _genIndex = 0;
                genTimer.Start(); // Ÿ�̸� ����
            }
            catch (Exception ex)
            {
                MessageBox.Show($"�� ���� �� ������ �߻��߽��ϴ�: {ex.Message}", "����",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                generateButton.Enabled = true;
            }
        }

        private async Task GenerateTabsAndWebViews(string startIp, int count)
        {
            var baseIp = IPAddress.Parse(startIp);
            var ipBytes = baseIp.GetAddressBytes();

            for (int i = 0; i < count; i++)
            {
                // IP ���� (������ ���� ����)
                var currentIpBytes = (byte[])ipBytes.Clone();
                currentIpBytes[3] = (byte)(currentIpBytes[3] + i);

                if (currentIpBytes[3] < ipBytes[3]) // �����÷ο� üũ
                {
                    MessageBox.Show($"IP �ּ� ������ �ʰ��߽��ϴ�. ({i}��°���� �ߴ�)", "���",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                }

                var currentIp = new IPAddress(currentIpBytes);
                string tabName = $"{currentIp}";

                // �� ������ ����
                var tabPage = new TabPage(tabName)
                {
                    BackColor = Color.White
                };

                // WebView2 ��Ʈ�ѵ� ����
                var webViews = await CreateWebViewsForTab(tabPage, currentIp.ToString());
                tabWebViewMap[tabPage] = webViews;

                tabControl.TabPages.Add(tabPage);
            }

            // ������ �� ����
            //if (tabControl.TabPages.Count > 0)
            //{
            //    tabControl.SelectedIndex = tabControl.TabPages.Count - 1;
            //}
        }

        private async Task<List<WebView2>> CreateWebViewsForTab(TabPage tabPage, string ip)
        {
            var webViews = new List<WebView2>();

            var webView = new WebView2
            {
                Visible = false // �ʱ⿡�� ����
            };

            // �� Ÿ�Կ� ���� ��ġ�� ũ�� ����
            webView.Dock = DockStyle.Fill;

            tabPage.Controls.Add(webView);
            webViews.Add(webView);

            // WebView2 �ʱ�ȭ �� DOMContentLoaded �ڵ鷯 ����
            await InitializeWebView(webView, ip, 0);
         
            return webViews;
        }

        private async Task InitializeWebView(WebView2 webView, string ip, int index)
        {
            try
            {
                webView.NavigationCompleted += OnNavigationCompleted;
                webView.CoreWebView2InitializationCompleted += OnCoreWebView2InitializationCompleted;
                
                await webView.EnsureCoreWebView2Async();

                webView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;


                // �ʱ� ������ �ε� (IP ��� URL�� �̵�)
                string url = $"http://{ip}:4011";

                webView.Visible = true;
                webView.Source = new Uri(url);
                //webView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                // ���� �� ���� ������ ǥ��
                string errorHtml = $@"
                <!DOCTYPE html>
                <html>
                <head><title>���� ����</title></head>
                <body style='font-family: Arial; padding: 20px; text-align: center;'>
                    <h2 style='color: red;'>���� ���� 22222</h2>
                    <p>IP: {ip} (View {index + 1})</p>
                    <p>����: {ex.Message}</p>
                </body>
                </html>";

                webView.NavigateToString(errorHtml);
                webView.Visible = true;
            }
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (e.IsSuccess)
            {
                // ������ �ε� �Ϸ� �� ũ�� ����

            }
        }

        private void OnCoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            var webView = (Microsoft.Web.WebView2.WinForms.WebView2)sender;

            if (e.IsSuccess)
            {
                Console.WriteLine($"OnCoreWebView2InitializationCompleted => {webView.Source.AbsoluteUri}");

                // JavaScript API �߰�
                //webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                //webView21.CoreWebView2.WebResourceRequested += OnWebResourceRequested;                
            }
        }

        private async void CoreWebView2_DOMContentLoaded(object sender, Microsoft.Web.WebView2.Core.CoreWebView2DOMContentLoadedEventArgs e)
        {
            try
            {
                var webView = (Microsoft.Web.WebView2.Core.CoreWebView2)sender;

                try
                {
                    string tSrc = webView.Source;
                    Uri uri = new Uri(tSrc);
                    string absolutePath = uri.AbsolutePath;

                    Debug.WriteLine($"[CoreWebView2_DOMContentLoaded] ������: {absolutePath}, org: {tSrc}, Doc: {webView.DocumentTitle}");

                    // ����LPR �ʱ�ȭ��
                    if (absolutePath == "/cgi-bin/luci/")
                    {
                        if (webView.DocumentTitle == "camera - Event - LuCI")
                        {
                            string script = @"
                            
                            if (document.body)
                            {
                            const userNameInput2 = document.getElementsByName('luci_username')[0];
                            if (userNameInput2) {
                                userNameInput2.value = 'root';
                            }
                            const passwordInput2 = document.getElementsByName('luci_password')[0];
                            if (passwordInput2) {
                                passwordInput2.value = 'root';

                                passwordInput2.dispatchEvent(new Event('input', {
                                bubbles: true
                                }));
                            }
                            if(userNameInput2 && passwordInput2)
                            {
                                const loginButton = document.querySelector('input[type=""submit""][value=""Login""]');
                                if (loginButton) {
                                    loginButton.click();
                                }
                            }
                            }
                        ";

                            await webView.ExecuteScriptAsync(script);
                        }

                    }
                    // �̱ۺ�LPR �α���
                    else if (absolutePath == "/login")
                    {
                        string script = @"
                            if (document.body)
                            {
                            const userNameInput = document.querySelector('input[formcontrolname=""userName""]');
                            if (userNameInput) {
                                userNameInput.value = 'admin';
                            }
                            const passwordInput = document.querySelector('input[formcontrolname=""password""]');
                            if (passwordInput) {
                                passwordInput.value = 'admin';

                                passwordInput.dispatchEvent(new Event('input', {
                                bubbles: true
                                }));
                            }

                            if(userNameInput && passwordInput)
                            {
                                const signInButton = document.querySelector('.form-actions.right .blue-btn');
                                if (signInButton) {
                                    signInButton.click();
                                    setTimeout(function() {
                                        window.location.href = '/config/Event?menu=LPRInfo';
                                    }, 1000); // 1�� �� �̵�
                                }
                            }
                            }
                        ";

                        await webView.ExecuteScriptAsync(script);

                    }

                }
                catch(UriFormatException ex)
                {
                    Console.WriteLine($"URI ���� ����: {ex.Message}");
                }

                // DOM�� �ε�� �� ������ JavaScript �ڵ�
                /*
                string script = $@"
                            // �������� ���� ǥ��
                            if (document.body) 
                            {{
                                var info = document.createElement('div');
                                info.style.position = 'fixed';
                                info.style.top = '10px';
                                info.style.left = '10px';
                                info.style.background = 'rgba(0,0,0,0.7)';
                                info.style.color = 'white';
                                info.style.padding = '10px';
                                info.style.borderRadius = '5px';
                                info.style.zIndex = '9999';
                                info.innerHTML = 'Time: ' + new Date().toLocaleTimeString();
                                document.body.appendChild(info);
                                
                                // 3�� �� �ڵ� ����
                                setTimeout(function() {{
                                    if (info.parentNode) {{
                                        info.parentNode.removeChild(info);
                                    }}
                                }}, 3000);
                            }}
                        ";
                */
                
            }
            catch (Exception ex)
            {
                // ���� �α� (�ʿ��)
                MessageBox.Show($"DOMContentLoaded �ڵ鷯 ����: {ex.Message}");
            }

        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == null) return;

            // ��� WebView �����
            foreach (var kvp in tabWebViewMap)
            {
                foreach (var webView in kvp.Value)
                {
                    webView.Visible = false;
                }
            }

            // ���õ� ���� WebView�� ���̱�
            if (tabWebViewMap.ContainsKey(tabControl.SelectedTab))
            {
                foreach (var webView in tabWebViewMap[tabControl.SelectedTab])
                {
                    webView.Visible = true;
                }
            }
        }

        private void ClearAllButton_Click(object sender, EventArgs e)
        {
            if (tabControl.TabPages.Count == 0) return;

            var result = MessageBox.Show("��� ���� �����Ͻðڽ��ϱ�?", "Ȯ��", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // ��� WebView ���ҽ� ����
                foreach (var kvp in tabWebViewMap)
                {
                    foreach (var webView in kvp.Value)
                    {
                        webView.Dispose();
                    }
                }

                tabWebViewMap.Clear();
                tabControl.TabPages.Clear();
            }
        }

        private bool IsValidIpAddress(string ip)
        {
            return IPAddress.TryParse(ip, out _);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // ���ҽ� ����
            foreach (var kvp in tabWebViewMap)
            {
                foreach (var webView in kvp.Value)
                {
                    webView.Dispose();
                }
            }

            base.OnFormClosed(e);
        }



    }
}
