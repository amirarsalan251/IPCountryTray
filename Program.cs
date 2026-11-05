using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("IPCountryTray")]
[assembly: System.Reflection.AssemblyProduct("IPCountryTray")]
[assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.0.0.0")]

namespace IPCountryTray
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayMonitor());
        }
    }

    internal sealed class IpData
    {
        public string Ip = "";
        public string Code = "??";
        public string Country = "";
        public string City = "";
        public string Region = "";
        public string Provider = "";
        public string Error = "";
        public DateTime CheckedAt = DateTime.MinValue;
    }

    internal sealed class TrayMonitor : ApplicationContext
    {
        private readonly Color dark = Color.FromArgb(30, 40, 58);
        private readonly Color muted = Color.FromArgb(160, 180, 206);
        private readonly Color accent = Color.FromArgb(124, 186, 252);
        private readonly NotifyIcon tray = new NotifyIcon();
        private readonly Form panel = new Form();
        private readonly Label directTab = new Label();
        private readonly Label proxyTab = new Label();
        private readonly Label flag = new Label();
        private readonly Label lineIp = new Label();
        private readonly Label lineLocation = new Label();
        private readonly Label lineStatus = new Label();
        private readonly Label settingsButton = new Label();
        private readonly Timer timer = new Timer();
        private readonly ToolTip tips = new ToolTip();
        private readonly JavaScriptSerializer json = new JavaScriptSerializer();
        private readonly string settingsFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IPCountryTray", "proxy.dat");
        private IpData direct = new IpData();
        private IpData proxy = new IpData();
        private bool isProxyTab = false;
        private bool checking = false;
        private bool quitting = false;
        private bool proxyEnabled = false;
        private string proxyHost = "", proxyPort = "", proxyUser = "", proxyPassword = "";
        private Icon generatedTrayIcon;

        public TrayMonitor()
        {
            panel.FormBorderStyle = FormBorderStyle.None;
            panel.ShowInTaskbar = false;
            panel.TopMost = true;
            panel.BackColor = dark;
            panel.ClientSize = new Size(338, 128);
            panel.Font = new Font("Segoe UI", 9);
            panel.StartPosition = FormStartPosition.Manual;
            panel.FormClosing += (s, e) => {
                if (!quitting) { e.Cancel = true; panel.Hide(); }
            };

            SetupTab(directTab, "DIRECT IP", 13, true);
            SetupTab(proxyTab, "PROXY IP", 105, false);
            directTab.Click += (s, e) => SelectTab(false);
            proxyTab.Click += (s, e) => SelectTab(true);

            var refreshButton = HeaderAction("\u21bb", 258, 14);
            tips.SetToolTip(refreshButton, "Refresh both IP addresses now");
            refreshButton.Click += async (s, e) => await UpdateAsync();

            settingsButton.Text = "\u2699";
            settingsButton.Font = new Font("Segoe UI Symbol", 15, FontStyle.Regular);
            settingsButton.ForeColor = Color.FromArgb(208, 220, 234);
            settingsButton.Location = new Point(280, 3);
            settingsButton.Size = new Size(26, 25);
            settingsButton.TextAlign = ContentAlignment.MiddleCenter;
            settingsButton.Cursor = Cursors.Hand;
            settingsButton.Click += async (s, e) => await OpenSettingsAsync();
            tips.SetToolTip(settingsButton, "Configure HTTP/HTTPS proxy");
            panel.Controls.Add(settingsButton);

            var close = HeaderAction("\u00d7", 311, 15);
            close.Click += (s, e) => panel.Hide();
            tips.SetToolTip(close, "Hide monitor (keeps running)");

            flag.Text = "--";
            flag.Location = new Point(14, 35);
            flag.Size = new Size(54, 32);
            flag.BackColor = Color.FromArgb(47, 70, 103);
            flag.ForeColor = Color.White;
            flag.Font = new Font("Segoe UI Semibold", 13);
            flag.TextAlign = ContentAlignment.MiddleCenter;
            panel.Controls.Add(flag);

            lineIp.Text = "Checking IP ...";
            lineIp.Location = new Point(84, 35);
            lineIp.Size = new Size(238, 33);
            lineIp.ForeColor = Color.White;
            lineIp.Font = new Font("Segoe UI Semibold", 13);
            lineIp.AutoEllipsis = true;
            panel.Controls.Add(lineIp);
            lineIp.Click += (s, e) => CopyIp();

            lineLocation.Location = new Point(14, 74);
            lineLocation.Size = new Size(310, 20);
            lineLocation.ForeColor = Color.FromArgb(204, 217, 235);
            lineLocation.AutoEllipsis = true;
            panel.Controls.Add(lineLocation);

            lineStatus.Location = new Point(14, 100);
            lineStatus.Size = new Size(311, 20);
            lineStatus.ForeColor = muted;
            lineStatus.Font = new Font("Segoe UI", 8);
            lineStatus.AutoEllipsis = true;
            panel.Controls.Add(lineStatus);

            panel.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left) {
                    Native.ReleaseCapture();
                    Native.SendMessage(panel.Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
                }
            };

            tray.Icon = SystemIcons.Information;
            tray.Visible = true;
            tray.Text = "IP Country Monitor";
            var menu = new ContextMenuStrip();
            menu.Items.Add("Show monitor", null, (s, e) => ShowPanel());
            menu.Items.Add("Direct IP", null, (s, e) => { SelectTab(false); ShowPanel(); });
            menu.Items.Add("Proxy IP", null, (s, e) => { SelectTab(true); ShowPanel(); });
            menu.Items.Add("Refresh now", null, async (s, e) => await UpdateAsync());
            menu.Items.Add("Copy visible IP", null, (s, e) => CopyIp());
            menu.Items.Add("Proxy settings...", null, async (s, e) => await OpenSettingsAsync());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => Exit());
            tray.ContextMenuStrip = menu;
            tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) ShowPanel(); };
            tray.DoubleClick += (s, e) => ShowPanel();

            LoadSettings();
            timer.Interval = 300000;
            timer.Tick += async (s, e) => await UpdateAsync();
            timer.Start();
            panel.Shown += async (s, e) => await UpdateAsync();
            SelectTab(false);
            ShowPanel();
        }

        private Label HeaderAction(string text, int x, float fontSize)
        {
            var label = new Label {
                Text = text, Font = new Font("Segoe UI", fontSize),
                ForeColor = Color.FromArgb(213, 222, 238),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(x, 3), Size = new Size(25, 25),
                Cursor = Cursors.Hand
            };
            panel.Controls.Add(label);
            return label;
        }

        private void SetupTab(Label label, string title, int x, bool selected)
        {
            label.Text = title;
            label.ForeColor = selected ? Color.White : muted;
            label.BackColor = selected ? Color.FromArgb(45, 74, 109) : Color.Transparent;
            label.Font = new Font("Segoe UI Semibold", 8);
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Location = new Point(x, 4);
            label.Size = new Size(88, 24);
            label.Cursor = Cursors.Hand;
            panel.Controls.Add(label);
        }

        // Switch the visible result without restarting the network checks.
        private void SelectTab(bool viaProxy)
        {
            isProxyTab = viaProxy;
            directTab.BackColor = viaProxy ? dark : Color.FromArgb(45, 74, 109);
            proxyTab.BackColor = viaProxy ? Color.FromArgb(45, 74, 109) : dark;
            directTab.ForeColor = viaProxy ? muted : Color.White;
            proxyTab.ForeColor = viaProxy ? Color.White : muted;
            ShowResult();
        }

        private void ShowResult()
        {
            IpData data = isProxyTab ? proxy : direct;
            if (isProxyTab && !proxyEnabled) {
                flag.Text = "--";
                lineIp.Text = "Proxy disabled";
                lineLocation.Text = "Click the gear icon to set up a proxy";
                lineStatus.Text = "Checks every 5 minutes";
                UpdateTray();
                return;
            }
            flag.Text = !string.IsNullOrWhiteSpace(data.Code) ? data.Code.ToUpperInvariant() : "--";
            flag.BackColor = data.Ip.Length > 0 ? Color.FromArgb(41, 93, 142) : Color.FromArgb(63, 76, 96);
            lineIp.Text = data.Ip.Length > 0 ? data.Ip : "Not available";
            var places = new List<string>();
            if (data.City.Length > 0) places.Add(data.City);
            if (data.Region.Length > 0 && !data.Region.Equals(data.City, StringComparison.OrdinalIgnoreCase)) places.Add(data.Region);
            if (data.Country.Length > 0) places.Add(data.Country);
            lineLocation.Text = places.Count > 0 ? string.Join(", ", places) : "Location unavailable";
            var when = data.CheckedAt == DateTime.MinValue ? "" : "Updated " + data.CheckedAt.ToString("HH:mm:ss");
            lineStatus.Text = data.Error.Length > 0 ? data.Error :
                (when + (data.Provider.Length > 0 ? " | " + data.Provider : ""));
            if (lineStatus.Text.Length == 0) lineStatus.Text = "Checking ...";
            UpdateTray();
        }

        // The tray icon follows the active tab; its tooltip keeps both IPs available.
        private void UpdateTray()
        {
            IpData selected = isProxyTab ? proxy : direct;
            string code = selected.Ip.Length > 0 ? selected.Code : "--";
            if (isProxyTab && !proxyEnabled) code = "PX";
            var created = MakeIcon(code);
            tray.Icon = created;
            if (generatedTrayIcon != null) generatedTrayIcon.Dispose();
            generatedTrayIcon = created;
            string tooltip = "Direct: " + (direct.Ip.Length > 0 ? direct.Ip : "--") +
                " | Proxy: " + (proxy.Ip.Length > 0 ? proxy.Ip : "--");
            tray.Text = tooltip.Length <= 63 ? tooltip : tooltip.Substring(0, 63);
        }

        private static Icon MakeIcon(string code)
        {
            using (var bitmap = new Bitmap(32, 32))
            using (var graphics = Graphics.FromImage(bitmap)) {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.FromArgb(32, 93, 154)))
                    graphics.FillEllipse(brush, 0, 0, 31, 31);
                using (var font = new Font("Segoe UI", 11, FontStyle.Bold, GraphicsUnit.Point))
                using (var brush = new SolidBrush(Color.White)) {
                    string display = string.IsNullOrWhiteSpace(code) ? "--" : code.ToUpperInvariant();
                    var size = graphics.MeasureString(display, font);
                    graphics.DrawString(display, font, brush, (32 - size.Width) / 2, (32 - size.Height) / 2);
                }
                IntPtr handle = bitmap.GetHicon();
                try { return (Icon)Icon.FromHandle(handle).Clone(); }
                finally { Native.DestroyIcon(handle); }
            }
        }

        // Anchor the compact popup to the active screen rather than a fixed monitor.
        private void ShowPanel()
        {
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            panel.Location = new Point(area.Right - panel.Width - 10, area.Bottom - panel.Height - 10);
            panel.Show();
            panel.BringToFront();
        }

        private void CopyIp()
        {
            string ip = isProxyTab ? proxy.Ip : direct.Ip;
            if (!string.IsNullOrWhiteSpace(ip)) Clipboard.SetText(ip);
        }

        // Keep proxy details in a separate dialog so the monitor stays compact.
        private async Task OpenSettingsAsync()
        {
            using (var form = new Form()) {
                form.Text = "Proxy Settings";
                form.Font = new Font("Segoe UI", 9);
                form.ClientSize = new Size(366, 267);
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MinimizeBox = false;
                form.MaximizeBox = false;
                form.ShowInTaskbar = false;
                form.BackColor = Color.FromArgb(244, 247, 251);

                var use = new CheckBox {
                    Text = "Enable HTTP / HTTPS proxy", Checked = proxyEnabled,
                    Location = new Point(17, 15), Size = new Size(330, 25)
                };
                form.Controls.Add(use);
                var server = new TextBox { Text = proxyHost, Location = new Point(119, 55), Size = new Size(225, 24) };
                var port = new TextBox { Text = proxyPort, Location = new Point(119, 88), Size = new Size(225, 24) };
                var username = new TextBox { Text = proxyUser, Location = new Point(119, 121), Size = new Size(225, 24) };
                var password = new TextBox { Text = proxyPassword, Location = new Point(119, 154), Size = new Size(225, 24), UseSystemPasswordChar = true };
                SettingLabel(form, "Server / IP", 17, 57);
                SettingLabel(form, "Port", 17, 90);
                SettingLabel(form, "Username", 17, 123);
                SettingLabel(form, "Password", 17, 156);
                form.Controls.AddRange(new Control[] { server, port, username, password });
                var info = new Label {
                    Text = "Supported: HTTP/HTTPS. Password is encrypted for your Windows account.",
                    ForeColor = Color.DimGray, Location = new Point(17, 187), Size = new Size(331, 33)
                };
                form.Controls.Add(info);
                var cancel = new Button {
                    Text = "Cancel", Location = new Point(250, 224), Size = new Size(94, 29),
                    DialogResult = DialogResult.Cancel
                };
                var save = new Button {
                    Text = "Save & Test", Location = new Point(119, 224), Size = new Size(122, 29),
                    BackColor = Color.FromArgb(40, 106, 169), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                save.Click += (s, e) => {
                    string h = server.Text.Trim(), p = port.Text.Trim();
                    int portNumber;
                    if (use.Checked && (h.Length == 0 || !int.TryParse(p, out portNumber) ||
                        portNumber < 1 || portNumber > 65535)) {
                        MessageBox.Show(form, "Enter a valid proxy server and port (1-65535).",
                            "Invalid proxy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (use.Checked && (h.StartsWith("socks", StringComparison.OrdinalIgnoreCase))) {
                        MessageBox.Show(form, "This build supports HTTP/HTTPS proxies, not SOCKS.",
                            "Unsupported proxy"); return;
                    }
                    if (!WriteSettings(use.Checked, h, p, username.Text, password.Text)) {
                        MessageBox.Show(form, "Unable to save proxy settings.", "Save error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    proxyEnabled = use.Checked; proxyHost = h; proxyPort = p;
                    proxyUser = username.Text; proxyPassword = password.Text;
                    proxy = new IpData();
                    SelectTab(true);
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                };
                form.Controls.Add(save);
                form.Controls.Add(cancel);
                form.AcceptButton = save;
                form.CancelButton = cancel;
                if (form.ShowDialog(panel) == DialogResult.OK) {
                    await UpdateAsync();
                }
            }
        }

        private static void SettingLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), Size = new Size(100, 23) });
        }

        // An invalid or outdated settings file should not prevent startup.
        private void LoadSettings()
        {
            try {
                if (!File.Exists(settingsFile)) return;
                var lines = File.ReadAllLines(settingsFile);
                if (lines.Length != 5) return;
                proxyEnabled = lines[0] == "1";
                proxyHost = Decode(lines[1]);
                proxyPort = Decode(lines[2]);
                proxyUser = Decode(lines[3]);
                proxyPassword = lines[4].Length == 0 ? "" :
                    Encoding.UTF8.GetString(ProtectedData.Unprotect(
                        Convert.FromBase64String(lines[4]), null, DataProtectionScope.CurrentUser));
            } catch {
                proxyEnabled = false;
                proxy = new IpData { Error = "Saved proxy settings cannot be read" };
            }
        }

        // Protect the proxy password with DPAPI under the current Windows account.
        private bool WriteSettings(bool enabled, string host, string port, string user, string pass)
        {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsFile));
                var secret = string.IsNullOrEmpty(pass) ? "" :
                    Convert.ToBase64String(ProtectedData.Protect(
                        Encoding.UTF8.GetBytes(pass), null, DataProtectionScope.CurrentUser));
                File.WriteAllLines(settingsFile, new[] {
                    enabled ? "1" : "0", Encode(host), Encode(port), Encode(user), secret
                });
                return true;
            } catch { return false; }
        }

        private static string Encode(string v) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(v ?? "")); }
        private static string Decode(string v) { return Encoding.UTF8.GetString(Convert.FromBase64String(v)); }

        // Manual refresh and the timer use the same path; skip overlapping requests.
        private async Task UpdateAsync()
        {
            if (checking || quitting) return;
            checking = true;
            lineStatus.Text = "Checking connections ...";
            try {
                Task<IpData> directTask = LookupAsync(false);
                Task<IpData> proxyTask = proxyEnabled ? LookupAsync(true) :
                    Task.FromResult(new IpData { Error = "Proxy disabled" });
                await Task.WhenAll(directTask, proxyTask);
                if (quitting) return;
                direct = directTask.Result;
                proxy = proxyTask.Result;
                ShowResult();
            } catch (Exception ex) {
                if (!quitting) lineStatus.Text = "Update failed: " + ex.Message;
            } finally {
                checking = false;
            }
        }

        // Separate clients ensure direct lookups never inherit the configured proxy.
        private async Task<IpData> LookupAsync(bool useProxy)
        {
            var data = new IpData();
            using (var handler = new HttpClientHandler()) {
                handler.UseProxy = useProxy;
                if (useProxy) {
                    try {
                        string host = proxyHost.Trim();
                        if (!host.Contains("://")) host = "http://" + host;
                        Uri parsed;
                        if (!Uri.TryCreate(host, UriKind.Absolute, out parsed) ||
                            (parsed.Scheme != "http" && parsed.Scheme != "https"))
                            throw new Exception("Use an HTTP or HTTPS proxy server");
                        var uri = new UriBuilder(parsed.Scheme, parsed.Host, int.Parse(proxyPort)).Uri;
                        var selectedProxy = new WebProxy(uri);
                        if (proxyUser.Length > 0)
                            selectedProxy.Credentials = new NetworkCredential(proxyUser, proxyPassword);
                        handler.Proxy = selectedProxy;
                    } catch (Exception e) {
                        data.Error = "Proxy setup: " + e.Message;
                        return data;
                    }
                }

                using (var client = new HttpClient(handler)) {
                    client.Timeout = TimeSpan.FromSeconds(8);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("IPCountryTray/1.0");
                    try {
                        var lookup = Read(await Fetch(client, "https://ipwho.is/"));
                        if (ReadValue(lookup, "success") == "false") throw new Exception("Lookup rejected request");
                        data.Ip = ReadValue(lookup, "ip");
                        if (data.Ip.Length == 0) throw new Exception("Empty IP");
                        data.Code = ReadValue(lookup, "country_code");
                        data.Country = ReadValue(lookup, "country");
                        data.Region = ReadValue(lookup, "region");
                        data.City = ReadValue(lookup, "city");
                        data.Provider = ReadNested(lookup, "connection", "isp");
                        data.CheckedAt = DateTime.Now;
                        return data;
                    } catch (Exception ex) { data.Error = ex.Message; }

                    try {
                        var lookup = Read(await Fetch(client, "https://ipapi.co/json/"));
                        if (ReadValue(lookup, "error").Equals("true", StringComparison.OrdinalIgnoreCase))
                            throw new Exception("Lookup denied");
                        string ip = ReadValue(lookup, "ip");
                        if (ip.Length == 0) throw new Exception("Empty IP");
                        data.Ip = ip;
                        data.Code = ReadValue(lookup, "country_code");
                        data.Country = ReadValue(lookup, "country_name");
                        data.Region = ReadValue(lookup, "region");
                        data.City = ReadValue(lookup, "city");
                        data.Provider = ReadValue(lookup, "org");
                        data.CheckedAt = DateTime.Now;
                        data.Error = "";
                        return data;
                    } catch { }

                    try {
                        var lookup = Read(await Fetch(client, "https://api.ipify.org?format=json"));
                        string ip = ReadValue(lookup, "ip");
                        if (ip.Length == 0) throw new Exception("Empty IP");
                        data.Ip = ip;
                        data.Code = "??";
                        data.Country = "";
                        data.City = "";
                        data.Region = "";
                        data.CheckedAt = DateTime.Now;
                        data.Error = "IP found, location unavailable";
                    } catch {
                        data.Error = useProxy ? "Cannot reach IP lookup through proxy" : "Cannot reach IP lookup directly";
                    }
                }
            }
            return data;
        }

        // Treat non-success responses as lookup failures so fallback providers can run.
        private async Task<string> Fetch(HttpClient client, string url)
        {
            using (var response = await client.GetAsync(url)) {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }

        private Dictionary<string, object> Read(string text)
        {
            return json.Deserialize<Dictionary<string, object>>(text);
        }

        private static string ReadValue(Dictionary<string, object> data, string key)
        {
            object value;
            return data != null && data.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : "";
        }

        private static string ReadNested(Dictionary<string, object> data, string key, string subkey)
        {
            object node;
            return data != null && data.TryGetValue(key, out node)
                ? ReadValue(node as Dictionary<string, object>, subkey) : "";
        }

        // Release the notification icon before closing the application context.
        private void Exit()
        {
            quitting = true;
            timer.Stop();
            timer.Dispose();
            tray.Visible = false;
            tray.Dispose();
            if (generatedTrayIcon != null) generatedTrayIcon.Dispose();
            panel.Close();
            panel.Dispose();
            Application.Exit();
        }
    }

    internal static class Native
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr wp, IntPtr lp);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr h);
    }
}
