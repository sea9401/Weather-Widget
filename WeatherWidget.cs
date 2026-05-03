using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WeatherWidget
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WeatherForm());
        }
    }

    class CityInfo
    {
        public string Name;
        public double Lat;
        public double Lon;
        public CityInfo(string n, double la, double lo) { Name = n; Lat = la; Lon = lo; }
    }

    public class WeatherForm : Form
    {
        string _lat = "37.5665";
        string _lon = "126.9780";
        string _tz  = "Asia/Seoul";

        int  _weatherCode = -1;
        bool _isDay       = true;

        static readonly Tuple<string, CityInfo[]>[] Regions = new[]
        {
            Tuple.Create("서울 / 인천 / 경기", new[] {
                new CityInfo("서울",       37.5665, 126.9780),
                new CityInfo("인천",       37.4563, 126.7052),
                new CityInfo("수원",       37.2636, 127.0286),
                new CityInfo("성남",       37.4200, 127.1267),
                new CityInfo("성남 분당",  37.3826, 127.1190),
                new CityInfo("고양",       37.6584, 126.8320),
                new CityInfo("용인",       37.2411, 127.1776),
                new CityInfo("부천",       37.5034, 126.7660),
                new CityInfo("안산",       37.3219, 126.8309),
                new CityInfo("안양",       37.3943, 126.9568),
                new CityInfo("의정부",     37.7381, 127.0337),
                new CityInfo("화성",       37.1995, 126.8311),
                new CityInfo("평택",       36.9921, 127.1129),
                new CityInfo("시흥",       37.3800, 126.8030),
                new CityInfo("파주",       37.7600, 126.7800),
                new CityInfo("김포",       37.6152, 126.7156),
                new CityInfo("광명",       37.4781, 126.8645),
                new CityInfo("남양주",     37.6360, 127.2160),
                new CityInfo("하남",       37.5392, 127.2148),
                new CityInfo("구리",       37.5944, 127.1296),
                new CityInfo("이천",       37.2722, 127.4350),
                new CityInfo("안성",       37.0080, 127.2797),
                new CityInfo("오산",       37.1499, 127.0772),
            }),
            Tuple.Create("강원", new[] {
                new CityInfo("춘천", 37.8813, 127.7298),
                new CityInfo("원주", 37.3422, 127.9202),
                new CityInfo("강릉", 37.7519, 128.8761),
                new CityInfo("동해", 37.5247, 129.1142),
                new CityInfo("속초", 38.2070, 128.5918),
                new CityInfo("삼척", 37.4499, 129.1655),
                new CityInfo("태백", 37.1641, 128.9856),
            }),
            Tuple.Create("충북", new[] {
                new CityInfo("청주", 36.6424, 127.4890),
                new CityInfo("충주", 36.9910, 127.9259),
                new CityInfo("제천", 37.1326, 128.1909),
            }),
            Tuple.Create("대전 / 세종 / 충남", new[] {
                new CityInfo("대전", 36.3504, 127.3845),
                new CityInfo("세종", 36.4800, 127.2890),
                new CityInfo("천안", 36.8151, 127.1139),
                new CityInfo("아산", 36.7898, 127.0019),
                new CityInfo("서산", 36.7848, 126.4503),
                new CityInfo("공주", 36.4467, 127.1190),
                new CityInfo("보령", 36.3334, 126.6128),
            }),
            Tuple.Create("전북", new[] {
                new CityInfo("전주", 35.8242, 127.1480),
                new CityInfo("군산", 35.9676, 126.7370),
                new CityInfo("익산", 35.9483, 126.9577),
                new CityInfo("정읍", 35.5697, 126.8559),
                new CityInfo("남원", 35.4163, 127.3905),
            }),
            Tuple.Create("광주 / 전남", new[] {
                new CityInfo("광주", 35.1595, 126.8526),
                new CityInfo("목포", 34.8118, 126.3922),
                new CityInfo("여수", 34.7604, 127.6622),
                new CityInfo("순천", 34.9506, 127.4872),
                new CityInfo("나주", 35.0160, 126.7108),
                new CityInfo("광양", 34.9407, 127.6958),
            }),
            Tuple.Create("대구 / 경북", new[] {
                new CityInfo("대구", 35.8714, 128.6014),
                new CityInfo("포항", 36.0190, 129.3435),
                new CityInfo("경주", 35.8562, 129.2247),
                new CityInfo("안동", 36.5684, 128.7294),
                new CityInfo("구미", 36.1196, 128.3445),
                new CityInfo("영주", 36.8056, 128.6240),
                new CityInfo("김천", 36.1397, 128.1136),
                new CityInfo("상주", 36.4108, 128.1591),
            }),
            Tuple.Create("부산 / 울산 / 경남", new[] {
                new CityInfo("부산", 35.1796, 129.0756),
                new CityInfo("울산", 35.5384, 129.3114),
                new CityInfo("창원", 35.2280, 128.6811),
                new CityInfo("진주", 35.1800, 128.1076),
                new CityInfo("김해", 35.2342, 128.8890),
                new CityInfo("양산", 35.3350, 129.0378),
                new CityInfo("거제", 34.8806, 128.6212),
                new CityInfo("통영", 34.8544, 128.4331),
                new CityInfo("사천", 35.0036, 128.0640),
            }),
            Tuple.Create("제주", new[] {
                new CityInfo("제주",   33.4996, 126.5312),
                new CityInfo("서귀포", 33.2541, 126.5601),
            }),
        };

        static string ConfigPath
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WeatherWidget");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "city.txt");
            }
        }

        static bool TryLoadCity(out string name, out string lat, out string lon)
        {
            name = null; lat = null; lon = null;
            try
            {
                if (!File.Exists(ConfigPath)) return false;
                var parts = File.ReadAllText(ConfigPath).Split('|');
                if (parts.Length < 3) return false;
                name = parts[0]; lat = parts[1]; lon = parts[2];
                return !string.IsNullOrEmpty(name);
            }
            catch { return false; }
        }

        static void SaveCity(string name, string lat, string lon)
        {
            try { File.WriteAllText(ConfigPath, name + "|" + lat + "|" + lon); } catch { }
        }

        static void ClearSavedCity()
        {
            try { if (File.Exists(ConfigPath)) File.Delete(ConfigPath); } catch { }
        }

        static string OpacityConfigPath
        {
            get
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "WeatherWidget");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "opacity.txt");
            }
        }

        static double LoadOpacity()
        {
            try
            {
                if (!File.Exists(OpacityConfigPath)) return 0.95;
                double v;
                if (double.TryParse(File.ReadAllText(OpacityConfigPath).Trim(),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out v))
                {
                    if (v < 0.3) v = 0.3;
                    if (v > 1.0) v = 1.0;
                    return v;
                }
            }
            catch { }
            return 0.95;
        }

        static void SaveOpacity(double v)
        {
            try
            {
                File.WriteAllText(OpacityConfigPath,
                    v.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            catch { }
        }

        const int CORNER_RADIUS = 18;

        [DllImport("gdi32.dll")] static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);
        [DllImport("user32.dll")] static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            var rgn = CreateRoundRectRgn(0, 0, Width + 1, Height + 1, CORNER_RADIUS * 2, CORNER_RADIUS * 2);
            SetWindowRgn(Handle, rgn, true);
        }

        // 날씨 테마 색상 묶음
        Color _bgTop, _bgBot, _lineCol, _iconCol, _fg1, _fg2, _fg3, _borderCol;

        void SetTheme(Color top, Color bot, Color line, Color icon,
                      Color t1, Color t2, Color t3, Color border)
        {
            _bgTop = top; _bgBot = bot; _lineCol = line; _iconCol = icon;
            _fg1 = t1; _fg2 = t2; _fg3 = t3; _borderCol = border;
        }

        void LoadTheme()
        {
            int c = _weatherCode;

            if (c < 0) // 초기 기본
            {
                SetTheme(Color.FromArgb(15, 23, 42), Color.FromArgb(15, 23, 42),
                         Color.FromArgb(51, 65, 85), Color.FromArgb(125, 211, 252),
                         Color.FromArgb(241,245,249), Color.FromArgb(148,163,184),
                         Color.FromArgb(71,85,105), Color.FromArgb(70,100,150));
            }
            else if (c == 0) // 맑음
            {
                if (_isDay)
                    SetTheme(Color.FromArgb(14, 90,190), Color.FromArgb(56,170,255),
                             Color.FromArgb(80,255,255,255), Color.FromArgb(255,230,80),
                             Color.White, Color.FromArgb(220,240,255),
                             Color.FromArgb(160,210,255), Color.FromArgb(100,180,255));
                else
                    SetTheme(Color.FromArgb(5,10,40), Color.FromArgb(15,30,80),
                             Color.FromArgb(50,255,255,255), Color.FromArgb(200,210,255),
                             Color.White, Color.FromArgb(180,200,255),
                             Color.FromArgb(100,120,200), Color.FromArgb(50,80,160));
            }
            else if (c == 1 || c == 2) // 구름 조금
            {
                if (_isDay)
                    SetTheme(Color.FromArgb(40,110,200), Color.FromArgb(130,180,240),
                             Color.FromArgb(60,255,255,255), Color.FromArgb(255,200,60),
                             Color.White, Color.FromArgb(220,235,255),
                             Color.FromArgb(170,205,245), Color.FromArgb(80,150,230));
                else
                    SetTheme(Color.FromArgb(10,20,55), Color.FromArgb(40,60,110),
                             Color.FromArgb(40,255,255,255), Color.FromArgb(170,185,255),
                             Color.White, Color.FromArgb(170,190,240),
                             Color.FromArgb(90,115,185), Color.FromArgb(50,80,150));
            }
            else if (c == 3) // 흐림
            {
                SetTheme(Color.FromArgb(70,85,105), Color.FromArgb(110,125,145),
                         Color.FromArgb(40,255,255,255), Color.FromArgb(200,210,220),
                         Color.White, Color.FromArgb(210,218,228),
                         Color.FromArgb(150,165,185), Color.FromArgb(100,115,140));
            }
            else if (c == 45 || c == 48) // 안개
            {
                SetTheme(Color.FromArgb(95,105,115), Color.FromArgb(145,155,165),
                         Color.FromArgb(35,255,255,255), Color.FromArgb(210,215,220),
                         Color.White, Color.FromArgb(215,220,225),
                         Color.FromArgb(160,168,178), Color.FromArgb(110,120,132));
            }
            else if ((c >= 51 && c <= 67) || (c >= 80 && c <= 82)) // 비
            {
                if (_isDay)
                    SetTheme(Color.FromArgb(30,50,90), Color.FromArgb(55,85,130),
                             Color.FromArgb(40,255,255,255), Color.FromArgb(140,180,255),
                             Color.White, Color.FromArgb(180,205,240),
                             Color.FromArgb(120,155,210), Color.FromArgb(60,100,180));
                else
                    SetTheme(Color.FromArgb(10,18,45), Color.FromArgb(25,45,85),
                             Color.FromArgb(30,255,255,255), Color.FromArgb(120,155,230),
                             Color.White, Color.FromArgb(155,185,230),
                             Color.FromArgb(90,125,195), Color.FromArgb(40,75,155));
            }
            else if ((c >= 71 && c <= 77) || c == 85 || c == 86) // 눈
            {
                SetTheme(Color.FromArgb(170,195,225), Color.FromArgb(220,235,250),
                         Color.FromArgb(60,80,100,130), Color.FromArgb(80,130,200),
                         Color.FromArgb(30,50,90), Color.FromArgb(60,80,120),
                         Color.FromArgb(100,125,165), Color.FromArgb(130,165,210));
            }
            else if (c == 95 || c == 96 || c == 99) // 뇌우
            {
                SetTheme(Color.FromArgb(15,12,35), Color.FromArgb(40,30,75),
                         Color.FromArgb(35,255,255,255), Color.FromArgb(255,230,80),
                         Color.White, Color.FromArgb(210,200,240),
                         Color.FromArgb(140,130,180), Color.FromArgb(90,70,150));
            }
            else // 기타 흐림
            {
                SetTheme(Color.FromArgb(70,85,105), Color.FromArgb(110,125,145),
                         Color.FromArgb(40,255,255,255), Color.FromArgb(200,210,220),
                         Color.White, Color.FromArgb(210,218,228),
                         Color.FromArgb(150,165,185), Color.FromArgb(100,115,140));
            }
        }

        Label lblCity, lblTime, lblIcon, lblTemp, lblDesc;
        Label lblFeels, lblHum, lblUpdate;
        Label lblPM10Title, lblPM10Status, lblPM10Num;
        Label lblPM25Title, lblPM25Status, lblPM25Num;

        bool  dragging;
        Point lastCursor;
        Timer clockTimer, dataTimer;

        static readonly double[] OpacityLevels = { 0.5, 0.6, 0.7, 0.8, 0.9, 0.95, 1.0 };
        MenuItem[] _opacityItems;

        public WeatherForm()
        {
            LoadTheme();
            InitForm();
            StartClock();

            string sn, sl, so;
            if (TryLoadCity(out sn, out sl, out so))
            {
                _lat = sl;
                _lon = so;
                lblCity.Text = sn;
                FetchData();
            }
            else
            {
                FetchCurrentLocation();
            }
        }

        void InitForm()
        {
            this.Text             = "날씨 위젯";
            this.FormBorderStyle  = FormBorderStyle.None;
            this.TopMost          = true;
            this.BackColor        = Color.FromArgb(15, 23, 42);
            this.Opacity          = LoadOpacity();
            this.ClientSize       = new Size(310, 355);
            this.StartPosition    = FormStartPosition.Manual;

            var wa = Screen.PrimaryScreen.WorkingArea;
            this.Location = new Point(wa.Width - 332, 80);

            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            this.MouseDown += DragDown;
            this.MouseMove += DragMove;
            this.MouseUp   += DragUp;
            this.Paint     += OnPaint;

            BuildLabels();
            BuildMenu();
        }

        void BuildLabels()
        {
            var fCity = new Font("Malgun Gothic", 11, FontStyle.Bold);
            var fTime = new Font("Malgun Gothic",  8);
            var fIcon = new Font("Segoe UI Symbol", 26);
            var fTemp = new Font("Malgun Gothic",  36, FontStyle.Bold);
            var fDesc = new Font("Malgun Gothic",  11);
            var fSub  = new Font("Malgun Gothic",   9);
            var fAQT  = new Font("Malgun Gothic",   9, FontStyle.Bold);
            var fAQV  = new Font("Malgun Gothic",  14, FontStyle.Bold);
            var fFoot = new Font("Malgun Gothic",   8);

            lblCity = Lbl("위치 감지 중...", 12, 13, 165, 22, fCity, _fg1, ContentAlignment.MiddleLeft);
            lblTime = Lbl("", 165, 15, 84, 17, fTime, _fg3, ContentAlignment.MiddleRight);

            var bX = Lbl("✕", 268, 11, 21, 22,
                new Font("Arial", 11, FontStyle.Bold), _fg3, ContentAlignment.MiddleCenter);
            bX.Cursor = Cursors.Hand;
            bX.MouseEnter += (s, e) => ((Label)s).ForeColor = Color.FromArgb(239, 68, 68);
            bX.MouseLeave += (s, e) => ((Label)s).ForeColor = _fg3;
            bX.Click += (s, e) => Application.Exit();

            var bR = Lbl("↻", 243, 11, 21, 22,
                new Font("Arial", 13), _fg3, ContentAlignment.MiddleCenter);
            bR.Cursor = Cursors.Hand;
            bR.MouseEnter += (s, e) => ((Label)s).ForeColor = Color.FromArgb(96, 165, 250);
            bR.MouseLeave += (s, e) => ((Label)s).ForeColor = _fg3;
            bR.Click += (s, e) => FetchData();

            lblIcon  = Lbl("☁",             0,  51, 310, 42, fIcon, _iconCol, ContentAlignment.MiddleCenter);
            lblTemp  = Lbl("--°C",          0,  93, 310, 62, fTemp, _fg1,     ContentAlignment.MiddleCenter);
            lblDesc  = Lbl("불러오는 중...", 0, 155, 310, 28, fDesc, _fg2,     ContentAlignment.MiddleCenter);
            lblFeels = Lbl("",              5, 187, 150, 20, fSub,  _fg3,     ContentAlignment.MiddleRight);
            lblHum   = Lbl("",           155, 187, 150, 20, fSub,  _fg3,     ContentAlignment.MiddleLeft);

            lblPM10Title = Lbl("미세먼지",   0, 227, 155, 17, fAQT, _fg2, ContentAlignment.MiddleCenter);
            lblPM25Title = Lbl("초미세먼지", 155, 227, 155, 17, fAQT, _fg2, ContentAlignment.MiddleCenter);

            lblPM10Status = Lbl("--",       0, 246, 155, 26, fAQV, _fg2, ContentAlignment.MiddleCenter);
            lblPM10Num    = Lbl("PM10 --",  0, 274, 155, 17, fSub, _fg3, ContentAlignment.MiddleCenter);

            lblPM25Status = Lbl("--",        155, 246, 155, 26, fAQV, _fg2, ContentAlignment.MiddleCenter);
            lblPM25Num    = Lbl("PM2.5 --",  155, 274, 155, 17, fSub, _fg3, ContentAlignment.MiddleCenter);

            lblUpdate = Lbl("우클릭: 메뉴", 0, 328, 310, 16, fFoot, _fg3, ContentAlignment.MiddleCenter);
        }

        void ApplyThemeToLabels()
        {
            LoadTheme();
            lblCity.ForeColor    = _fg1;
            lblTime.ForeColor    = _fg3;
            lblDesc.ForeColor    = _fg2;
            lblFeels.ForeColor   = _fg3;
            lblHum.ForeColor     = _fg3;
            lblIcon.ForeColor      = _iconCol;
            lblPM10Title.ForeColor = _fg2;
            lblPM25Title.ForeColor = _fg2;
            lblPM10Num.ForeColor   = _fg3;
            lblPM25Num.ForeColor   = _fg3;
            lblUpdate.ForeColor    = _fg3;
        }

        Label Lbl(string text, int x, int y, int w, int h,
                  Font font, Color fg, ContentAlignment align)
        {
            var l = new Label {
                Text      = text,
                Bounds    = new Rectangle(x, y, w, h),
                Font      = font,
                ForeColor = fg,
                BackColor = Color.Transparent,
                AutoSize  = false,
                TextAlign = align,
            };
            l.MouseDown += DragDown;
            l.MouseMove += DragMove;
            l.MouseUp   += DragUp;
            this.Controls.Add(l);
            return l;
        }

        void BuildMenu()
        {
            var cm = new ContextMenu();
            cm.MenuItems.Add(new MenuItem("새로고침", (s, e) => FetchData()));
            cm.MenuItems.Add(new MenuItem("-"));

            var locItem = new MenuItem("위치 변경");
            locItem.MenuItems.Add(new MenuItem("자동 감지 (현재 IP 기반)", (s, e) =>
            {
                ClearSavedCity();
                FetchCurrentLocation();
            }));
            locItem.MenuItems.Add(new MenuItem("-"));
            foreach (var region in Regions)
            {
                var rItem = new MenuItem(region.Item1);
                foreach (var city in region.Item2)
                {
                    var c = city;
                    rItem.MenuItems.Add(new MenuItem(c.Name, (s, e) => SetCity(c)));
                }
                locItem.MenuItems.Add(rItem);
            }
            cm.MenuItems.Add(locItem);
            cm.MenuItems.Add(new MenuItem("-"));

            var opItem = new MenuItem("투명도");
            _opacityItems = new MenuItem[OpacityLevels.Length];
            for (int i = 0; i < OpacityLevels.Length; i++)
            {
                var op = OpacityLevels[i];
                var label = ((int)Math.Round(op * 100)) + " %";
                var mi = new MenuItem(label, (s, e) => SetOpacity(op));
                _opacityItems[i] = mi;
                opItem.MenuItems.Add(mi);
            }
            UpdateOpacityChecks();
            cm.MenuItems.Add(opItem);
            cm.MenuItems.Add(new MenuItem("-"));

            var topItem = new MenuItem("항상 위  ✓");
            topItem.Click += (s, e) => {
                this.TopMost = !this.TopMost;
                topItem.Text = this.TopMost ? "항상 위  ✓" : "항상 위";
            };
            cm.MenuItems.Add(topItem);
            cm.MenuItems.Add(new MenuItem("-"));
            cm.MenuItems.Add(new MenuItem("닫기", (s, e) => Application.Exit()));

            this.ContextMenu = cm;
            foreach (Control c in this.Controls)
                c.ContextMenu = cm;
        }

        void SetCity(CityInfo c)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            _lat = c.Lat.ToString(inv);
            _lon = c.Lon.ToString(inv);
            _tz  = "Asia/Seoul";
            lblCity.Text = c.Name;
            SaveCity(c.Name, _lat, _lon);
            FetchData();
        }

        void SetOpacity(double v)
        {
            this.Opacity = v;
            SaveOpacity(v);
            UpdateOpacityChecks();
        }

        void UpdateOpacityChecks()
        {
            if (_opacityItems == null) return;
            for (int i = 0; i < OpacityLevels.Length; i++)
                _opacityItems[i].Checked = Math.Abs(this.Opacity - OpacityLevels[i]) < 0.005;
        }

        void DragDown(object s, MouseEventArgs e)
        {
            var ctl = s as Control;
            if (e.Button == MouseButtons.Left &&
                (ctl == null || ctl.Cursor != Cursors.Hand))
            {
                dragging   = true;
                lastCursor = Cursor.Position;
            }
        }

        void DragMove(object s, MouseEventArgs e)
        {
            if (!dragging) return;
            var pos  = Cursor.Position;
            this.Left += pos.X - lastCursor.X;
            this.Top  += pos.Y - lastCursor.Y;
            lastCursor = pos;
        }

        void DragUp(object s, MouseEventArgs e) { dragging = false; }

        void OnPaint(object s, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width, Height);

            // 그라디언트 배경
            using (var path = RoundedPath(rect, CORNER_RADIUS))
            using (var brush = new LinearGradientBrush(rect, _bgTop, _bgBot, LinearGradientMode.Vertical))
            {
                g.FillPath(brush, path);
            }

            // 날씨 오버레이 장식
            DrawWeatherOverlay(g);

            // 구분선
            using (var pen = new Pen(_lineCol, 1))
            {
                g.DrawLine(pen,  12,  44, 298,  44);
                g.DrawLine(pen,  12, 221, 298, 221);
                g.DrawLine(pen, 155, 225, 155, 321);
                g.DrawLine(pen,  12, 321, 298, 321);
            }

            // 둥근 테두리
            using (var pen = new Pen(_borderCol, 1.5f))
            using (var path = RoundedPath(new Rectangle(1, 1, Width - 2, Height - 2), CORNER_RADIUS))
                g.DrawPath(pen, path);
        }

        void DrawWeatherOverlay(Graphics g)
        {
            int c = _weatherCode;
            if (c < 0) return;

            if (c == 0 && _isDay)
                DrawSunRays(g, 1f);
            else if (c == 0 && !_isDay)
                DrawStars(g);
            else if (c == 1 || c == 2)
            {
                DrawClouds(g, Color.FromArgb(20, 255, 255, 255));
                if (_isDay) DrawSunRays(g, 0.4f);
            }
            else if (c == 3)
                DrawClouds(g, Color.FromArgb(18, 255, 255, 255));
            else if (c == 45 || c == 48)
                DrawFogStripes(g);
            else if ((c >= 51 && c <= 67) || (c >= 80 && c <= 82))
            {
                DrawClouds(g, Color.FromArgb(15, 255, 255, 255));
                DrawRainDrops(g);
            }
            else if ((c >= 71 && c <= 77) || c == 85 || c == 86)
                DrawSnowflakes(g);
            else if (c == 95 || c == 96 || c == 99)
            {
                DrawClouds(g, Color.FromArgb(18, 255, 255, 255));
                DrawLightning(g);
            }
        }

        void DrawSunRays(Graphics g, float alpha)
        {
            int cx = Width - 50, cy = 72;
            using (var pen = new Pen(Color.FromArgb((int)(28 * alpha), 255, 220, 80), 2f))
            {
                for (int i = 0; i < 8; i++)
                {
                    double angle = Math.PI * 2 * i / 8.0;
                    int x1 = cx + (int)(Math.Cos(angle) * 18);
                    int y1 = cy + (int)(Math.Sin(angle) * 18);
                    int x2 = cx + (int)(Math.Cos(angle) * 30);
                    int y2 = cy + (int)(Math.Sin(angle) * 30);
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
            }
            using (var brush = new SolidBrush(Color.FromArgb((int)(22 * alpha), 255, 220, 80)))
                g.FillEllipse(brush, cx - 24, cy - 24, 48, 48);
        }

        void DrawStars(Graphics g)
        {
            var rnd = new Random(42);
            using (var brush = new SolidBrush(Color.FromArgb(80, 220, 230, 255)))
            {
                for (int i = 0; i < 18; i++)
                {
                    int x = rnd.Next(8, Width - 8);
                    int y = rnd.Next(50, 200);
                    int r = rnd.Next(1, 3);
                    g.FillEllipse(brush, x - r, y - r, r * 2, r * 2);
                }
            }
        }

        void DrawClouds(Graphics g, Color col)
        {
            using (var brush = new SolidBrush(col))
            {
                FillCloud(g, brush, 200, 60, 80, 28);
                FillCloud(g, brush,  30, 80, 60, 22);
            }
        }

        void FillCloud(Graphics g, Brush b, int cx, int cy, int w, int h)
        {
            int bx = cx - w / 2, by = cy - h / 2;
            g.FillEllipse(b, bx,         by + h / 4, w / 2,     h * 3 / 4);
            g.FillEllipse(b, bx + w / 3, by,         w * 2 / 3, h);
            g.FillEllipse(b, bx + w / 2, by + h / 4, w / 2,     h * 3 / 4);
        }

        void DrawFogStripes(Graphics g)
        {
            using (var brush = new SolidBrush(Color.FromArgb(18, 255, 255, 255)))
            {
                for (int y = 80; y < 200; y += 22)
                    g.FillRectangle(brush, 0, y, Width, 8);
            }
        }

        void DrawRainDrops(Graphics g)
        {
            var rnd = new Random(7);
            using (var pen = new Pen(Color.FromArgb(45, 160, 200, 255), 1.2f))
            {
                for (int i = 0; i < 22; i++)
                {
                    int x   = rnd.Next(10, Width - 10);
                    int y   = rnd.Next(60, 210);
                    int len = rnd.Next(6, 14);
                    g.DrawLine(pen, x, y, x - 2, y + len);
                }
            }
        }

        void DrawSnowflakes(Graphics g)
        {
            var rnd = new Random(13);
            using (var brush = new SolidBrush(Color.FromArgb(70, 220, 235, 255)))
            using (var pen   = new Pen(Color.FromArgb(55, 200, 220, 255), 1f))
            {
                for (int i = 0; i < 18; i++)
                {
                    int x = rnd.Next(10, Width - 10);
                    int y = rnd.Next(55, 210);
                    int r = rnd.Next(2, 5);
                    g.FillEllipse(brush, x - r, y - r, r * 2, r * 2);
                    g.DrawLine(pen, x - r - 2, y, x + r + 2, y);
                    g.DrawLine(pen, x, y - r - 2, x, y + r + 2);
                }
            }
        }

        void DrawLightning(Graphics g)
        {
            var pts = new Point[] {
                new Point(240, 55), new Point(228, 85),
                new Point(238, 85), new Point(224, 110)
            };
            using (var pen = new Pen(Color.FromArgb(35, 255, 230, 50), 2f))
                g.DrawLines(pen, pts);
        }

        static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X,         r.Y,          d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
            p.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
            p.CloseFigure();
            return p;
        }

        void StartClock()
        {
            clockTimer = new Timer { Interval = 30000 };
            clockTimer.Tick += (s, e) => lblTime.Text = DateTime.Now.ToString("MM/dd HH:mm");
            clockTimer.Start();
            lblTime.Text = DateTime.Now.ToString("MM/dd HH:mm");

            dataTimer = new Timer { Interval = 900000 };
            dataTimer.Tick += (s, e) => FetchData();
            dataTimer.Start();
        }

        void FetchCurrentLocation()
        {
            var bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                using (var wc = new WebClient())
                {
                    wc.Encoding = System.Text.Encoding.UTF8;
                    wc.Headers[HttpRequestHeader.UserAgent] = "WeatherWidget/1.0";
                    e.Result = wc.DownloadString(
                        "http://ip-api.com/json?fields=lat,lon,timezone");
                }
            };
            bw.RunWorkerCompleted += (s, e) =>
            {
                if (e.Error == null)
                {
                    string json = (string)e.Result;
                    double lat = Num(json, "lat");
                    double lon = Num(json, "lon");
                    string tz  = GetJsonStr(json, "timezone");
                    if (lat != 0 || lon != 0)
                    {
                        _lat = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        _lon = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                    if (!string.IsNullOrEmpty(tz)) _tz = tz;
                }
                FetchLocation();
                FetchData();
            };
            bw.RunWorkerAsync();
        }

        void FetchLocation()
        {
            var bw = new BackgroundWorker();
            bw.DoWork += (s, e) =>
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;
                using (var wc = new WebClient())
                {
                    wc.Encoding = System.Text.Encoding.UTF8;
                    wc.Headers[HttpRequestHeader.UserAgent] = "WeatherWidget/1.0";
                    string url = "https://nominatim.openstreetmap.org/reverse?lat=" + _lat
                        + "&lon=" + _lon + "&format=json&accept-language=ko";
                    e.Result = wc.DownloadString(url);
                }
            };
            bw.RunWorkerCompleted += (s, e) =>
            {
                if (e.Error != null) return;
                string json = (string)e.Result;

                string city     = GetJsonStr(json, "city")
                               ?? GetJsonStr(json, "town")
                               ?? GetJsonStr(json, "county")
                               ?? "알 수 없음";
                string district = GetJsonStr(json, "city_district")
                               ?? GetJsonStr(json, "borough")
                               ?? "";

                foreach (var sfx in new[] { "특별자치시", "특별자치도", "특별시", "광역시", "시", "군" })
                    if (city.EndsWith(sfx)) { city = city.Substring(0, city.Length - sfx.Length); break; }

                lblCity.Text = string.IsNullOrEmpty(district) ? city : city + " " + district;
            };
            bw.RunWorkerAsync();
        }

        static string GetJsonStr(string json, string key)
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"([^\"]+)\"");
            if (!m.Success) return null;
            string v = m.Groups[1].Value;
            return (v == "null" || v.Length == 0) ? null : v;
        }

        void FetchData()
        {
            lblDesc.Text = "불러오는 중...";
            var bw = new BackgroundWorker();
            bw.DoWork             += DoFetch;
            bw.RunWorkerCompleted += OnFetched;
            bw.RunWorkerAsync();
        }

        void DoFetch(object sender, DoWorkEventArgs e)
        {
            string tz = Uri.EscapeDataString(_tz);

            string wUrl = "https://api.open-meteo.com/v1/forecast?latitude=" + _lat
                + "&longitude=" + _lon
                + "&current=temperature_2m,apparent_temperature,weather_code,relative_humidity_2m,is_day"
                + "&timezone=" + tz;

            string aUrl = "https://air-quality-api.open-meteo.com/v1/air-quality?latitude=" + _lat
                + "&longitude=" + _lon
                + "&current=pm10,pm2_5"
                + "&timezone=" + tz;

            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

            using (var wc = new WebClient())
            {
                wc.Headers[HttpRequestHeader.UserAgent] = "WeatherWidget/1.0";
                string wj = wc.DownloadString(wUrl);
                string aj = wc.DownloadString(aUrl);
                e.Result = new[] { wj, aj };
            }
        }

        void OnFetched(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                lblDesc.Text   = "연결 오류";
                lblUpdate.Text = "인터넷 연결 확인  ·  우클릭: 새로고침";
                return;
            }
            var r = (string[])e.Result;
            UpdateWeather(r[0]);
            UpdateAir(r[1]);
            lblUpdate.Text = "업데이트 " + DateTime.Now.ToString("HH:mm") + "  ·  우클릭: 메뉴";
        }

        void UpdateWeather(string j)
        {
            double t  = Num(j, "temperature_2m");
            double fe = Num(j, "apparent_temperature");
            double hm = Num(j, "relative_humidity_2m");
            int    cd = (int)Num(j, "weather_code");
            int    id = (int)Num(j, "is_day");

            _weatherCode = cd;
            _isDay       = (id == 1);

            ApplyThemeToLabels();
            this.Invalidate();

            string desc, icon;
            WeatherInfo(cd, out desc, out icon);

            lblTemp.ForeColor = TempColor(t);
            lblTemp.Text  = t.ToString("F1") + "°C";
            lblDesc.Text  = desc;
            lblIcon.Text  = icon;
            lblFeels.Text = "체감 " + fe.ToString("F1") + "°C";
            lblHum.Text   = "습도 " + hm.ToString("F0") + "%";
        }

        void UpdateAir(string j)
        {
            double p25 = Num(j, "pm2_5");
            double p10 = Num(j, "pm10");

            string ps10; Color pc10;
            PMInfo(p10, false, out ps10, out pc10);
            lblPM10Status.ForeColor = pc10;
            lblPM10Status.Text      = ps10;
            lblPM10Num.Text         = "PM10  " + (int)Math.Round(p10);

            string ps25; Color pc25;
            PMInfo(p25, true, out ps25, out pc25);
            lblPM25Status.ForeColor = pc25;
            lblPM25Status.Text      = ps25;
            lblPM25Num.Text         = "PM2.5  " + (int)Math.Round(p25);
        }

        static double Num(string j, string key)
        {
            var m = Regex.Match(j, "\"" + key + "\"\\s*:\\s*(-?\\d+\\.?\\d*)");
            double v = 0;
            if (m.Success)
                double.TryParse(m.Groups[1].Value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out v);
            return v;
        }

        static void WeatherInfo(int c, out string d, out string i)
        {
            switch (c)
            {
                case  0:                   d = "맑음";        i = "☀"; break;
                case  1:                   d = "대체로 맑음";  i = "⛅"; break;
                case  2:                   d = "구름 조금";   i = "⛅"; break;
                case  3:                   d = "흐림";        i = "☁"; break;
                case 45: case 48:          d = "안개";        i = "☁"; break;
                case 51: case 53: case 55: d = "이슬비";      i = "☂"; break;
                case 61: case 63:          d = "비";          i = "☂"; break;
                case 65:                   d = "폭우";        i = "☂"; break;
                case 71: case 73:          d = "눈";          i = "❄"; break;
                case 75:                   d = "폭설";        i = "❄"; break;
                case 77:                   d = "우박";        i = "❄"; break;
                case 80: case 81:          d = "소나기";      i = "☂"; break;
                case 82:                   d = "강한 소나기"; i = "☂"; break;
                case 85: case 86:          d = "눈 소나기";   i = "❄"; break;
                case 95: case 96: case 99: d = "뇌우";        i = "⚡"; break;
                default:                   d = "알 수 없음";  i = "☁"; break;
            }
        }

        static Color TempColor(double t)
        {
            if (t <= 0)  return Color.FromArgb(147, 197, 253);
            if (t <= 10) return Color.FromArgb(191, 219, 254);
            if (t <= 20) return Color.FromArgb(187, 247, 208);
            if (t <= 28) return Color.FromArgb(253, 230, 138);
            if (t <= 35) return Color.FromArgb(253, 186, 116);
            return Color.FromArgb(252, 165, 165);
        }

        static void PMInfo(double val, bool isPM25, out string s, out Color c)
        {
            double t1 = isPM25 ? 15  : 30;
            double t2 = isPM25 ? 35  : 80;
            double t3 = isPM25 ? 75  : 150;

            if      (val <= t1) { s = "좋음";     c = Color.FromArgb( 34, 197,  94); }
            else if (val <= t2) { s = "보통";     c = Color.FromArgb(251, 191,  36); }
            else if (val <= t3) { s = "나쁨";     c = Color.FromArgb(249, 115,  22); }
            else                { s = "매우 나쁨"; c = Color.FromArgb(239,  68,  68); }
        }
    }
}
