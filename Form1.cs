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
                    MessageBox.Show("유효한 IP 주소를 입력해주세요.", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                await GenerateTabsAndWebViews(startIp, count);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"탭 생성 중 오류가 발생했습니다: {ex.Message}", "오류",
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
                // IP 생성 (마지막 옥텟 증가)
                var currentIpBytes = (byte[])ipBytes.Clone();
                currentIpBytes[3] = (byte)(currentIpBytes[3] + i);

                if (currentIpBytes[3] < ipBytes[3]) // 오버플로우 체크
                {
                    MessageBox.Show($"IP 주소 범위를 초과했습니다. ({i}번째에서 중단)", "경고",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                }

                var currentIp = new IPAddress(currentIpBytes);
                string tabName = $"{currentIp}";

                // 탭 페이지 생성
                var tabPage = new TabPage(tabName)
                {
                    BackColor = Color.White
                };

                // WebView2 컨트롤들 생성
                var webViews = await CreateWebViewsForTab(tabPage, currentIp.ToString());
                tabWebViewMap[tabPage] = webViews;

                tabControl.TabPages.Add(tabPage);
            }

            // 첫 번째 탭 선택
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
                Visible = false // 초기에는 숨김
            };

            // 뷰 타입에 따라 위치와 크기 설정
            //SetWebViewLayout(webView, i, webViewCount, tabPage);
            webView.Dock = DockStyle.Fill;

            tabPage.Controls.Add(webView);
            webViews.Add(webView);

            // WebView2 초기화 및 DOMContentLoaded 핸들러 설정
            await InitializeWebView(webView, ip, 0);
         
            return webViews;
        }

        private void SetWebViewLayout(WebView2 webView, int index, int totalCount, TabPage tabPage)
        {
            if (totalCount == 1) // 싱글뷰
            {
                webView.Dock = DockStyle.Fill;
            }
            else if (totalCount == 2) // 더블뷰
            {
                webView.Dock = DockStyle.None;
                webView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

                if (index == 0) // 왼쪽
                {
                    webView.Location = new Point(0, 0);
                    webView.Size = new Size(tabPage.Width / 2 - 2, tabPage.Height);
                }
                else // 오른쪽
                {
                    webView.Location = new Point(tabPage.Width / 2 + 2, 0);
                    webView.Size = new Size(tabPage.Width / 2 - 2, tabPage.Height);
                }

                // 탭 페이지 크기 변경 시 WebView 크기도 조정
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


                // 초기 페이지 로드 (IP 기반 URL로 이동)
                string url = $"http://{ip}:4011";

                webView.Visible = true;
                webView.Source = new Uri(url);
                //webView.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                // 에러 시 에러 페이지 표시
                string errorHtml = $@"
                <!DOCTYPE html>
                <html>
                <head><title>연결 오류</title></head>
                <body style='font-family: Arial; padding: 20px; text-align: center;'>
                    <h2 style='color: red;'>연결 오류 22222</h2>
                    <p>IP: {ip} (View {index + 1})</p>
                    <p>오류: {ex.Message}</p>
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
                // 페이지 로드 완료 후 크기 조정

            }
        }

        private void OnCoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            var webView = (Microsoft.Web.WebView2.WinForms.WebView2)sender;

            if (e.IsSuccess)
            {
                Console.WriteLine($"OnCoreWebView2InitializationCompleted => {webView.Source.AbsoluteUri}");

                // JavaScript API 추가
                //webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                //webView21.CoreWebView2.WebResourceRequested += OnWebResourceRequested;
                // DOM 로딩 완료 이벤트에 핸들러 추가
                
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

                    Debug.WriteLine($"[CoreWebView2_DOMContentLoaded] 서브경로: {absolutePath}, org: {tSrc}, Doc: {webView.DocumentTitle}");

                    // 듀얼뷰 초기화면
                    if (absolutePath == "/cgi-bin/luci/")
                    {
                        if (webView.DocumentTitle == "camera - Event - LuCI")
                        {
                            // 더블뷰
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
                    // 싱글뷰 로그인
                    else if (absolutePath == "/login")
                    {
                        // 싱글뷰 로그인
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
                    Console.WriteLine($"URI 형식 오류: {ex.Message}");
                }

                // DOM이 로드된 후 실행할 JavaScript 코드
                /*
                string script = $@"
                            // 페이지에 정보 표시
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
                                
                                // 3초 후 자동 제거
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
                // 에러 로깅 (필요시)
                //System.Diagnostics.Debug.WriteLine($"DOMContentLoaded 핸들러 오류: {ex.Message}");
                MessageBox.Show($"DOMContentLoaded 핸들러 오류: {ex.Message}");
            }

        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab == null) return;

            // 모든 WebView 숨기기
            foreach (var kvp in tabWebViewMap)
            {
                foreach (var webView in kvp.Value)
                {
                    webView.Visible = false;
                }
            }

            // 선택된 탭의 WebView만 보이기
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
            
            // 기존 탭들의 WebView 레이아웃 재구성
            //await ReconfigureExistingTabs();
        }

        private async Task ReconfigureExistingTabs()
        {
            var tabsToReconfigure = new List<TabPage>(tabWebViewMap.Keys);

            foreach (var tabPage in tabsToReconfigure)
            {
                var existingWebViews = tabWebViewMap[tabPage];

                // 기존 WebView 제거
                foreach (var webView in existingWebViews)
                {
                    tabPage.Controls.Remove(webView);
                    webView.Dispose();
                }

                // IP 추출 (탭 이름에서)
                string ip = tabPage.Text;

                // 새로운 WebView 생성
                var newWebViews = await CreateWebViewsForTab(tabPage, ip);
                tabWebViewMap[tabPage] = newWebViews;
            }

            // 현재 선택된 탭의 WebView 보이기
            TabControl_SelectedIndexChanged(null, null);
        }

        private void ClearAllButton_Click(object sender, EventArgs e)
        {
            if (tabControl.TabPages.Count == 0) return;

            var result = MessageBox.Show("모든 탭을 삭제하시겠습니까?", "확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // 모든 WebView 리소스 해제
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

            // 로컬 IP 범위 체크
            return (bytes[0] == 192 && bytes[1] == 168) ||  // 192.168.x.x
                   (bytes[0] == 10) ||                      // 10.x.x.x
                   (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) || // 172.16.x.x - 172.31.x.x
                   (bytes[0] == 127); // 127.x.x.x (localhost)
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 리소스 정리
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
