using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

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
        
        public enum ViewType
        {
            Single,
            Double
        }

        public Form1()
        {
            InitializeComponent();
            tabWebViewMap = new Dictionary<TabPage, List<WebView2>>();
        }

        private async void GenerateButton_Click(object sender, EventArgs e)
        {
            generateButton.Enabled = false;

            try
            {
                string startIp = startIpTextBox.Text.Trim();
                int count = (int)countNumericUpDown.Value;

                if (!IsValidIpAddress(startIp))
                {
                    MessageBox.Show("��ȿ�� IP �ּҸ� �Է����ּ���.", "����",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                await GenerateTabsAndWebViews(startIp, count);
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

            // ù ��° �� ����
            if (tabControl.TabPages.Count > 0)
            {
                tabControl.SelectedIndex = 0;
            }
        }

        private async Task<List<WebView2>> CreateWebViewsForTab(TabPage tabPage, string ip)
        {
            var webViews = new List<WebView2>();

            var webView = new WebView2
            {
                Visible = false // �ʱ⿡�� ����
            };

            // �� Ÿ�Կ� ���� ��ġ�� ũ�� ����
            //SetWebViewLayout(webView, i, webViewCount, tabPage);
            webView.Dock = DockStyle.Fill;

            tabPage.Controls.Add(webView);
            webViews.Add(webView);

            // WebView2 �ʱ�ȭ �� DOMContentLoaded �ڵ鷯 ����
            await InitializeWebView(webView, ip, 0);
         
            return webViews;
        }

        private void SetWebViewLayout(WebView2 webView, int index, int totalCount, TabPage tabPage)
        {
            if (totalCount == 1) // �̱ۺ�
            {
                webView.Dock = DockStyle.Fill;
            }
            else if (totalCount == 2) // �����
            {
                webView.Dock = DockStyle.None;
                webView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                if (index == 0) // ����
                {
                    webView.Location = new Point(0, 0);
                    webView.Size = new Size(tabPage.Width / 2 - 2, tabPage.Height);
                }
                else // ������
                {
                    webView.Location = new Point(tabPage.Width / 2 + 2, 0);
                    webView.Size = new Size(tabPage.Width / 2 - 2, tabPage.Height);
                }

                // �� ������ ũ�� ���� �� WebView ũ�⵵ ����
                tabPage.SizeChanged += (s, e) =>
                {
                    if (index == 0)
                    {
                        webView.Size = new Size(tabPage.Width / 2 - 2, tabPage.Height);
                    }
                    else
                    {
                        webView.Location = new Point(tabPage.Width / 2 + 2, 0);
                        webView.Size = new Size(tabPage.Width / 2 - 2, tabPage.Height);
                    }
                };
            }
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
                // DOM �ε� �Ϸ� �̺�Ʈ�� �ڵ鷯 �߰�
                
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

                    // ���� �ʱ�ȭ��
                    if (absolutePath == "/cgi-bin/luci/")
                    {
                        if (webView.DocumentTitle == "camera - Event - LuCI")
                        {
                            // �����
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
                    // �̱ۺ� �α���
                    else if (absolutePath == "/login")
                    {
                        // �̱ۺ� �α���
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
                                }
                            }
                            }
                        ";

                        await webView.ExecuteScriptAsync(script);

                        
                        UriBuilder uriBuilder = new UriBuilder(uri);
                        // Change the Path property.
                        uriBuilder.Path = "/config/Event";
                        uriBuilder.Query = "menu=LPRInfo";
                        // Get the new Uri object.
                        Uri newUri = uriBuilder.Uri;

                        webView.Navigate(newUri.ToString());
                        
                    }
                    else if(absolutePath == "/config/System")
                    {
                        UriBuilder uriBuilder = new UriBuilder(uri);
                        // Change the Path property.
                        uriBuilder.Path = "/config/Event";
                        uriBuilder.Query = "menu=LPRInfo";
                        // Get the new Uri object.
                        Uri newUri = uriBuilder.Uri;

                        webView.Navigate(newUri.ToString());
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
                //System.Diagnostics.Debug.WriteLine($"DOMContentLoaded �ڵ鷯 ����: {ex.Message}");
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

        private void ViewTypeRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            
            // ���� �ǵ��� WebView ���̾ƿ� �籸��
            //await ReconfigureExistingTabs();
        }

        private async Task ReconfigureExistingTabs()
        {
            var tabsToReconfigure = new List<TabPage>(tabWebViewMap.Keys);

            foreach (var tabPage in tabsToReconfigure)
            {
                var existingWebViews = tabWebViewMap[tabPage];

                // ���� WebView ����
                foreach (var webView in existingWebViews)
                {
                    tabPage.Controls.Remove(webView);
                    webView.Dispose();
                }

                // IP ���� (�� �̸�����)
                string ip = tabPage.Text;

                // ���ο� WebView ����
                var newWebViews = await CreateWebViewsForTab(tabPage, ip);
                tabWebViewMap[tabPage] = newWebViews;
            }

            // ���� ���õ� ���� WebView ���̱�
            TabControl_SelectedIndexChanged(null, null);
        }

        private void ClearAllButton_Click(object sender, EventArgs e)
        {
            if (tabControl.TabPages.Count == 0) return;

            var result = MessageBox.Show("��� ���� �����Ͻðڽ��ϱ�?", "Ȯ��",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

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

        private bool IsLocalIpAddress(string ip)
        {
            if (!IPAddress.TryParse(ip, out IPAddress address))
                return false;

            var bytes = address.GetAddressBytes();

            // ���� IP ���� üũ
            return (bytes[0] == 192 && bytes[1] == 168) ||  // 192.168.x.x
                   (bytes[0] == 10) ||                      // 10.x.x.x
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // 172.16.x.x - 172.31.x.x
                   (bytes[0] == 127); // 127.x.x.x (localhost)
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
