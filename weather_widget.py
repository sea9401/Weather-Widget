import tkinter as tk
import requests
from datetime import datetime
import threading

# ──────────────────────────────────────
#  위치 설정 (원하는 도시로 변경하세요)
# ──────────────────────────────────────
CITY_NAME = "서울"
LATITUDE  = 37.5665
LONGITUDE = 126.9780
TIMEZONE  = "Asia/Seoul"
#
# 주요 도시 좌표 목록:
#   부산  : 35.1796, 129.0756
#   대구  : 35.8714, 128.6014
#   인천  : 37.4563, 126.7052
#   광주  : 35.1595, 126.8526
#   대전  : 36.3504, 127.3845
#   수원  : 37.2636, 127.0286
#   제주  : 33.4996, 126.5312
# ──────────────────────────────────────

# 색상 팔레트 (다크 테마)
BG     = "#0f172a"   # 배경
BG2    = "#1e293b"   # 메뉴 배경
BORDER = "#334155"   # 구분선
FG1    = "#f1f5f9"   # 기본 텍스트
FG2    = "#94a3b8"   # 보조 텍스트
FG3    = "#475569"   # 흐린 텍스트

WEATHER_CODES = {
    0:  ("맑음",        "☼"),
    1:  ("대체로 맑음",  "◕"),
    2:  ("구름 조금",   "◑"),
    3:  ("흐림",        "○"),
    45: ("안개",        "≈"),
    48: ("안개",        "≈"),
    51: ("이슬비",      "·"),
    53: ("이슬비",      "·"),
    55: ("이슬비",      "·"),
    61: ("비",          "☂"),
    63: ("비",          "☂"),
    65: ("폭우",        "☂"),
    71: ("눈",          "✶"),
    73: ("눈",          "✶"),
    75: ("폭설",        "✶"),
    77: ("우박",        "✶"),
    80: ("소나기",      "☂"),
    81: ("소나기",      "☂"),
    82: ("강한 소나기", "☂"),
    85: ("눈 소나기",   "✶"),
    86: ("눈 소나기",   "✶"),
    95: ("뇌우",        "⚡"),
    96: ("뇌우+우박",   "⚡"),
    99: ("강한 뇌우",   "⚡"),
}

AQI_LEVELS = [
    (20,          "매우 좋음", "#22c55e"),
    (40,          "좋음",     "#86efac"),
    (60,          "보통",     "#fbbf24"),
    (80,          "나쁨",     "#f97316"),
    (100,         "매우 나쁨","#ef4444"),
    (float('inf'),"위험",     "#a855f7"),
]


def aqi_status(val):
    for threshold, label, color in AQI_LEVELS:
        if val <= threshold:
            return label, color
    return "위험", "#a855f7"


def temp_color(t):
    if t <= 0:  return "#93c5fd"
    if t <= 10: return "#bfdbfe"
    if t <= 20: return "#bbf7d0"
    if t <= 28: return "#fde68a"
    if t <= 35: return "#fdba74"
    return "#fca5a5"


class WeatherWidget:
    def __init__(self):
        self.root = tk.Tk()
        self.root.title("날씨 위젯")
        self.root.overrideredirect(True)        # 프레임 없는 창
        self.root.attributes('-topmost', True)  # 항상 위
        self.root.attributes('-alpha', 0.95)    # 약간 투명
        self.root.configure(bg=BG)
        self.root.resizable(False, False)

        sw = self.root.winfo_screenwidth()
        self.root.geometry(f"270x360+{sw - 290}+80")  # 오른쪽 위 기본 위치

        self._drag_origin = (0, 0)
        self._refresh_id  = None

        self._build_ui()
        self._bind_events()
        self._refresh()
        self._tick_clock()
        self.root.mainloop()

    # ── UI 구성 ─────────────────────────────────────────────────────────────

    def _build_ui(self):
        outer = tk.Frame(self.root, bg=BG, padx=16, pady=12)
        outer.pack(fill='both', expand=True)

        # 헤더
        hdr = tk.Frame(outer, bg=BG)
        hdr.pack(fill='x')

        self.city_lbl = tk.Label(hdr, text=CITY_NAME,
            font=('맑은 고딕', 12, 'bold'), fg=FG1, bg=BG)
        self.city_lbl.pack(side='left')

        close_btn = tk.Label(hdr, text="✕",
            font=('Arial', 12), fg=FG3, bg=BG, cursor='hand2')
        close_btn.pack(side='right')
        close_btn.bind('<Button-1>', lambda _: self.root.destroy())
        close_btn.bind('<Enter>',    lambda _: close_btn.config(fg='#ef4444'))
        close_btn.bind('<Leave>',    lambda _: close_btn.config(fg=FG3))

        refresh_btn = tk.Label(hdr, text="↻",
            font=('Arial', 13), fg=FG3, bg=BG, cursor='hand2')
        refresh_btn.pack(side='right', padx=(0, 6))
        refresh_btn.bind('<Button-1>', lambda _: self._refresh())
        refresh_btn.bind('<Enter>',    lambda _: refresh_btn.config(fg='#60a5fa'))
        refresh_btn.bind('<Leave>',    lambda _: refresh_btn.config(fg=FG3))

        self.time_lbl = tk.Label(hdr, text="",
            font=('맑은 고딕', 8), fg=FG3, bg=BG)
        self.time_lbl.pack(side='right', padx=(0, 8))

        # 구분선
        tk.Frame(outer, height=1, bg=BORDER).pack(fill='x', pady=(8, 0))

        # 날씨 섹션
        w_frame = tk.Frame(outer, bg=BG, pady=14)
        w_frame.pack(fill='x')

        self.icon_lbl = tk.Label(w_frame, text="☁",
            font=('Arial', 26), fg='#7dd3fc', bg=BG)
        self.icon_lbl.pack()

        self.temp_lbl = tk.Label(w_frame, text="--°C",
            font=('맑은 고딕', 40, 'bold'), fg=FG1, bg=BG)
        self.temp_lbl.pack()

        self.desc_lbl = tk.Label(w_frame, text="불러오는 중...",
            font=('맑은 고딕', 11), fg=FG2, bg=BG)
        self.desc_lbl.pack()

        det_row = tk.Frame(w_frame, bg=BG)
        det_row.pack(pady=(5, 0))

        self.feels_lbl = tk.Label(det_row, text="",
            font=('맑은 고딕', 9), fg=FG3, bg=BG)
        self.feels_lbl.pack(side='left', padx=6)

        self.hum_lbl = tk.Label(det_row, text="",
            font=('맑은 고딕', 9), fg=FG3, bg=BG)
        self.hum_lbl.pack(side='left', padx=6)

        # 구분선
        tk.Frame(outer, height=1, bg=BORDER).pack(fill='x', pady=(4, 0))

        # 공기질 섹션
        aq_frame = tk.Frame(outer, bg=BG, pady=12)
        aq_frame.pack(fill='x')

        tk.Label(aq_frame, text="공기질 (Air Quality)",
            font=('맑은 고딕', 9, 'bold'), fg=FG2, bg=BG).pack()

        self.aqi_lbl = tk.Label(aq_frame, text="-- AQI",
            font=('맑은 고딕', 20, 'bold'), fg='#22c55e', bg=BG)
        self.aqi_lbl.pack(pady=(2, 0))

        pm_row = tk.Frame(aq_frame, bg=BG)
        pm_row.pack(pady=(5, 0))

        self.pm25_lbl = tk.Label(pm_row, text="PM2.5  --",
            font=('맑은 고딕', 9), fg=FG3, bg=BG)
        self.pm25_lbl.pack(side='left', padx=10)

        self.pm10_lbl = tk.Label(pm_row, text="PM10  --",
            font=('맑은 고딕', 9), fg=FG3, bg=BG)
        self.pm10_lbl.pack(side='left', padx=10)

        # 구분선 + 푸터
        tk.Frame(outer, height=1, bg=BORDER).pack(fill='x', pady=(10, 0))

        self.update_lbl = tk.Label(outer, text="우클릭: 메뉴",
            font=('맑은 고딕', 8), fg=FG3, bg=BG, pady=4)
        self.update_lbl.pack()

    # ── 이벤트 바인딩 ────────────────────────────────────────────────────────

    def _bind_events(self):
        def bind_rec(w):
            if not isinstance(w, tk.Menu):
                try:
                    # hand2 커서인 버튼은 드래그 제외
                    if w.cget('cursor') != 'hand2':
                        w.bind('<ButtonPress-1>', self._drag_start)
                        w.bind('<B1-Motion>',      self._drag_move)
                except tk.TclError:
                    pass
                w.bind('<ButtonPress-3>', self._show_menu)
            for child in w.winfo_children():
                bind_rec(child)
        bind_rec(self.root)

    def _drag_start(self, e):
        self._drag_origin = (e.x_root - self.root.winfo_x(),
                             e.y_root - self.root.winfo_y())

    def _drag_move(self, e):
        x = e.x_root - self._drag_origin[0]
        y = e.y_root - self._drag_origin[1]
        self.root.geometry(f"+{x}+{y}")

    def _show_menu(self, e):
        m = tk.Menu(self.root, tearoff=0,
                    bg=BG2, fg=FG1,
                    activebackground=BORDER, activeforeground=FG1,
                    font=('맑은 고딕', 9))
        m.add_command(label="  새로고침", command=self._refresh)
        m.add_separator()
        on_top = self.root.attributes('-topmost')
        m.add_command(
            label=f"  항상 위: {'켜짐 ✓' if on_top else '꺼짐'}",
            command=self._toggle_topmost
        )
        m.add_separator()
        m.add_command(label="  닫기", command=self.root.destroy)
        try:
            m.post(e.x_root, e.y_root)
        finally:
            m.grab_release()

    def _toggle_topmost(self):
        self.root.attributes('-topmost', not self.root.attributes('-topmost'))

    # ── 시계 업데이트 ─────────────────────────────────────────────────────────

    def _tick_clock(self):
        self.time_lbl.config(text=datetime.now().strftime("%m/%d %H:%M"))
        self.root.after(30_000, self._tick_clock)

    # ── 데이터 페치 ───────────────────────────────────────────────────────────

    def _refresh(self):
        if self._refresh_id:
            self.root.after_cancel(self._refresh_id)
        self.desc_lbl.config(text="불러오는 중...", fg=FG2)
        threading.Thread(target=self._fetch, daemon=True).start()
        self._refresh_id = self.root.after(1_800_000, self._refresh)  # 30분마다 자동 갱신

    def _fetch(self):
        try:
            weather = requests.get(
                "https://api.open-meteo.com/v1/forecast",
                params={
                    "latitude":  LATITUDE,
                    "longitude": LONGITUDE,
                    "current":   "temperature_2m,apparent_temperature,"
                                 "weather_code,relative_humidity_2m",
                    "timezone":  TIMEZONE,
                },
                timeout=10,
            ).json()

            air = requests.get(
                "https://air-quality-api.open-meteo.com/v1/air-quality",
                params={
                    "latitude":  LATITUDE,
                    "longitude": LONGITUDE,
                    "current":   "pm10,pm2_5,european_aqi",
                    "timezone":  TIMEZONE,
                },
                timeout=10,
            ).json()

            self.root.after(0, lambda: self._apply(weather, air))
        except Exception as ex:
            self.root.after(0, lambda: self._on_error(ex))

    def _apply(self, weather, air):
        cur   = weather.get('current', {})
        temp  = cur.get('temperature_2m')
        feels = cur.get('apparent_temperature')
        hum   = cur.get('relative_humidity_2m')
        code  = cur.get('weather_code', 0)

        desc, icon = WEATHER_CODES.get(code, ("알 수 없음", "?"))

        if temp is not None:
            self.temp_lbl.config(text=f"{temp:.1f}°C", fg=temp_color(float(temp)))
        self.desc_lbl.config(text=desc, fg=FG2)
        self.icon_lbl.config(text=icon)
        if feels is not None:
            self.feels_lbl.config(text=f"체감 {feels:.1f}°C")
        if hum is not None:
            self.hum_lbl.config(text=f"습도 {hum}%")

        ac   = air.get('current', {})
        aqi  = ac.get('european_aqi')
        pm25 = ac.get('pm2_5')
        pm10 = ac.get('pm10')

        if aqi is not None:
            status, color = aqi_status(int(aqi))
            self.aqi_lbl.config(text=f"{status}  AQI {int(aqi)}", fg=color)
        if pm25 is not None:
            self.pm25_lbl.config(text=f"PM2.5  {pm25:.1f} ㎍/㎥")
        if pm10 is not None:
            self.pm10_lbl.config(text=f"PM10  {pm10:.1f} ㎍/㎥")

        self.update_lbl.config(
            text=f"업데이트 {datetime.now().strftime('%H:%M')}  ·  우클릭: 메뉴"
        )

    def _on_error(self, ex):
        self.desc_lbl.config(text="연결 오류", fg='#ef4444')
        self.update_lbl.config(text="인터넷 연결을 확인하세요  ·  우클릭: 새로고침")


if __name__ == '__main__':
    WeatherWidget()
