using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace NaiwaDesktop
{
    // A deterministic clock. The controller supplies elapsed monotonic time, so
    // pausing and completion can be checked without waiting for a real timer.
    internal sealed class FocusClock
    {
        internal const double SessionSeconds = 25 * 60;
        private double remaining = SessionSeconds;
        internal bool FocusMode { get; private set; }
        internal bool Running { get; private set; }
        internal bool Completed { get; private set; }
        internal int RemainingSeconds { get { return (int)Math.Ceiling(Math.Max(0, remaining)); } }
        internal double Progress { get { return Math.Max(0, Math.Min(1, 1 - remaining / SessionSeconds)); } }
        internal bool HasProgress { get { return remaining < SessionSeconds; } }

        internal void SetMode(bool focus)
        {
            FocusMode = focus;
            if (!focus) Running = false;
        }
        internal void Start()
        {
            FocusMode = true;
            if (Completed) Reset();
            Running = true;
        }
        internal void Pause() { Running = false; }
        internal void Reset()
        {
            Running = false;
            Completed = false;
            remaining = SessionSeconds;
        }
        internal bool Advance(double elapsedSeconds)
        {
            if (!Running || elapsedSeconds <= 0 || Double.IsNaN(elapsedSeconds) || Double.IsInfinity(elapsedSeconds)) return false;
            remaining = Math.Max(0, remaining - elapsedSeconds);
            if (remaining > 0) return false;
            Running = false;
            Completed = true;
            return true;
        }
    }

    // The hidden window retains this controller. Closing the panel never stops
    // the focus session; disposing it when the pet exits releases both timers.
    internal sealed class CompanionController : IDisposable
    {
        private readonly PetWindow pet;
        private readonly Timer timer = new Timer();
        private readonly Stopwatch watch = Stopwatch.StartNew();
        private double last;
        internal readonly FocusClock Clock = new FocusClock();
        internal event EventHandler Changed;
        internal bool TimerEnabled { get { return timer.Enabled; } }

        internal CompanionController(PetWindow petWindow)
        {
            pet = petWindow;
            timer.Interval = 250;
            timer.Tick += Tick;
            timer.Start();
        }
        private void Accumulate()
        {
            double now = watch.Elapsed.TotalSeconds;
            bool complete = Clock.Advance(now - last);
            last = now;
            if (complete) pet.NotifyFocusComplete();
        }
        private void Tick(object sender, EventArgs e) { Accumulate(); Publish(); }
        internal void SetMode(bool focus) { Accumulate(); Clock.SetMode(focus); Publish(); }
        internal void ToggleRunning()
        {
            Accumulate();
            if (Clock.Running) Clock.Pause(); else Clock.Start();
            Publish();
        }
        internal void Reset() { Accumulate(); Clock.Reset(); Publish(); }
        private void Publish() { if (Changed != null) Changed(this, EventArgs.Empty); }
        public void Dispose() { timer.Stop(); timer.Dispose(); watch.Stop(); }
    }

    internal static class CompanionStyle
    {
        internal static readonly Color Cream = Color.FromArgb(247, 246, 234);
        internal static readonly Color Card = Color.FromArgb(255, 254, 247);
        internal static readonly Color Green = Color.FromArgb(42, 69, 45);
        internal static readonly Color Muted = Color.FromArgb(119, 131, 111);
        internal static readonly Color Border = Color.FromArgb(224, 229, 209);
        internal static readonly Color Mint = Color.FromArgb(233, 239, 220);
        internal static readonly Color Yellow = Color.FromArgb(244, 211, 112);
        internal static Font Font(float size, FontStyle style) { return new Font("Microsoft YaHei UI", size, style, GraphicsUnit.Point); }
        internal static GraphicsPath Rounded(RectangleF rect, float radius)
        {
            float d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
        internal static void Fill(Graphics g, Color color, RectangleF rect, float radius)
        {
            if (rect.Width <= 0 || rect.Height <= 0) return;
            using (GraphicsPath path = Rounded(rect, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2)))
            using (SolidBrush brush = new SolidBrush(color)) g.FillPath(brush, path);
        }
        internal static Label Label(string text, int x, int y, int width, int height, float size, Color color, FontStyle style)
        {
            return new Label { Text = text, Location = new Point(x, y), Size = new Size(width, height), Font = Font(size, style), ForeColor = color, BackColor = Color.Transparent, AutoSize = false, TextAlign = ContentAlignment.MiddleLeft, UseMnemonic = false };
        }
    }

    internal sealed class CompanionCard : Panel
    {
        internal CompanionCard()
        {
            BackColor = CompanionStyle.Cream;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath path = CompanionStyle.Rounded(new RectangleF(.5f, .5f, Width - 1, Height - 1), 19 * e.Graphics.DpiX / 96f))
            {
                using (SolidBrush brush = new SolidBrush(CompanionStyle.Card)) e.Graphics.FillPath(brush, path);
                using (Pen pen = new Pen(CompanionStyle.Border)) e.Graphics.DrawPath(pen, path);
            }
        }
    }

    internal sealed class CompanionButton : Button
    {
        internal bool Selected;
        internal bool Accent;
        private bool over, pressed;
        internal CompanionButton(string text)
        {
            Text = text; Cursor = Cursors.Hand; FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0; TabStop = true;
            Font = CompanionStyle.Font(9f, FontStyle.Regular);
            ForeColor = CompanionStyle.Green; BackColor = CompanionStyle.Card;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnMouseEnter(EventArgs e) { over = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { over = pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnGotFocus(EventArgs e) { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor); g.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = Selected ? CompanionStyle.Green : Accent ? CompanionStyle.Yellow : CompanionStyle.Mint;
            if (over && Enabled) fill = ControlPaint.Light(fill, .1f);
            if (pressed && Enabled) fill = ControlPaint.Dark(fill, .04f);
            if (!Enabled) fill = Color.FromArgb(239, 240, 229);
            CompanionStyle.Fill(g, fill, new RectangleF(0, 0, Width - 1, Height - 1), 10 * g.DpiX / 96f);
            TextRenderer.DrawText(g, Text, Font, new Rectangle(2, 1, Width - 4, Height - 2), !Enabled ? CompanionStyle.Muted : Selected ? CompanionStyle.Card : CompanionStyle.Green, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);
            if (Focused && ShowFocusCues)
                using (Pen pen = new Pen(CompanionStyle.Muted)) { pen.DashStyle = DashStyle.Dot; using (GraphicsPath p = CompanionStyle.Rounded(new RectangleF(3, 3, Width - 7, Height - 7), 7)) g.DrawPath(pen, p); }
        }
    }

    internal sealed class CompanionSwitch : CheckBox
    {
        internal CompanionSwitch(string label)
        {
            Text = label; AccessibleName = label; Cursor = Cursors.Hand; AutoSize = false;
            Font = CompanionStyle.Font(9.5f, FontStyle.Regular); ForeColor = CompanionStyle.Green;
            BackColor = CompanionStyle.Card;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnCheckedChanged(EventArgs e) { Invalidate(); base.OnCheckedChanged(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.Clear(BackColor); g.SmoothingMode = SmoothingMode.AntiAlias;
            float unit = Height / 29f;
            TextRenderer.DrawText(g, Text, Font, new Rectangle(0, 0, Width - (int)(65 * unit), Height), ForeColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            RectangleF track = new RectangleF(Width - 45 * unit, (Height - 21 * unit) / 2f, 43 * unit, 21 * unit);
            CompanionStyle.Fill(g, Checked ? CompanionStyle.Green : Color.FromArgb(215, 221, 203), track, 11 * unit);
            using (SolidBrush brush = new SolidBrush(CompanionStyle.Card)) g.FillEllipse(brush, track.X + (Checked ? 24 : 3) * unit, track.Y + 3 * unit, 15 * unit, 15 * unit);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(0, 0, Width - 1, Height - 1));
        }
    }

    internal sealed class CompanionScale : Control
    {
        private int value = 85;
        private bool dragging;
        internal event EventHandler ValueChanged;
        internal int Value
        {
            get { return value; }
            set { int next = Math.Max(50, Math.Min(150, value)); if (next == this.value) return; this.value = next; Invalidate(); }
        }
        internal CompanionScale()
        {
            Cursor = Cursors.Hand; BackColor = CompanionStyle.Card; TabStop = true;
            AccessibleName = "奶蛙大小，50% 至 150%，方向键调整";
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        }
        private void SetPointer(int x)
        {
            double inset = 8 * Height / 30.0;
            int next = 50 + (int)Math.Round(Math.Max(0, Math.Min(1, (x - inset) / (Width - 2 * inset))) * 100);
            SetValue(next);
        }
        private void SetValue(int next)
        {
            int before = value; Value = next;
            if (before != value && ValueChanged != null) ValueChanged(this, EventArgs.Empty);
        }
        protected override void OnMouseDown(MouseEventArgs e)
        { base.OnMouseDown(e); if (e.Button != MouseButtons.Left) return; Focus(); dragging = true; Capture = true; SetPointer(e.X); }
        protected override void OnMouseMove(MouseEventArgs e) { base.OnMouseMove(e); if (dragging) SetPointer(e.X); }
        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); dragging = false; Capture = false; }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) dragging = false; }
        protected override bool IsInputKey(Keys keyData)
        { Keys key = keyData & Keys.KeyCode; return key == Keys.Left || key == Keys.Right || key == Keys.Home || key == Keys.End || base.IsInputKey(keyData); }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Left) SetValue(value - 5);
            else if (e.KeyCode == Keys.Right) SetValue(value + 5);
            else if (e.KeyCode == Keys.Home) SetValue(50);
            else if (e.KeyCode == Keys.End) SetValue(150);
            else { base.OnKeyDown(e); return; }
            e.Handled = true;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.Clear(BackColor); g.SmoothingMode = SmoothingMode.AntiAlias;
            float unit = Height / 30f, inset = 8 * unit;
            float y = Height / 2f, x = inset + (Width - 2 * inset) * (value - 50) / 100f;
            CompanionStyle.Fill(g, CompanionStyle.Mint, new RectangleF(inset, y - 3 * unit, Width - 2 * inset, 6 * unit), 3 * unit);
            CompanionStyle.Fill(g, CompanionStyle.Green, new RectangleF(inset, y - 3 * unit, x - inset, 6 * unit), 3 * unit);
            using (SolidBrush b = new SolidBrush(CompanionStyle.Yellow)) g.FillEllipse(b, x - 7 * unit, y - 7 * unit, 14 * unit, 14 * unit);
            using (Pen p = new Pen(CompanionStyle.Green, 1.5f * unit)) g.DrawEllipse(p, x - 7 * unit, y - 7 * unit, 14 * unit, 14 * unit);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(g, new Rectangle(0, 0, Width - 1, Height - 1));
        }
    }

    internal sealed class CompanionProgress : Control
    {
        internal double Progress;
        internal CompanionProgress()
        { BackColor = CompanionStyle.Card; SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); }
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            CompanionStyle.Fill(e.Graphics, CompanionStyle.Mint, new RectangleF(0, 0, Width, Height), Height / 2f);
            CompanionStyle.Fill(e.Graphics, CompanionStyle.Green, new RectangleF(0, 0, (float)(Width * Progress), Height), Height / 2f);
        }
    }

    internal sealed class CompanionPreview : Control
    {
        private readonly PetWindow pet;
        private readonly Assets assets;
        internal CompanionPreview(PetWindow p, Assets a)
        {
            pet = p; assets = a; BackColor = CompanionStyle.Card;
            AccessibleName = "奶蛙原版动作预览";
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.Clear(BackColor); g.SmoothingMode = SmoothingMode.AntiAlias;
            GraphicsState state = g.Save();
            g.ScaleTransform(Width / 246f, Height / 242f);
            using (SolidBrush brush = new SolidBrush(CompanionStyle.Mint)) g.FillEllipse(brush, 23, 23, 199, 199);
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(222, 230, 201)))
            {
                g.FillEllipse(brush, 18, 65, 6, 6); g.FillEllipse(brush, 215, 174, 7, 7);
                g.FillEllipse(brush, 203, 30, 4, 4);
            }
            using (Pen pen = new Pen(Color.FromArgb(189, 202, 165), 1.5f))
            { g.DrawLine(pen, 26, 188, 26, 202); g.DrawLine(pen, 19, 195, 33, 195); }
            g.Restore(state);
            int scale = Math.Max(1, (int)Math.Min(21200.0 / assets.Width * Width / 246, 23600.0 / assets.Height * Height / 242));
            using (Bitmap frame = Renderer.Render(assets, pet.Motion, scale))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImageUnscaled(frame, (Width - frame.Width) / 2, (Height - frame.Height) / 2);
            }
        }
    }

    internal sealed class CompanionWindow : Form
    {
        private readonly PetWindow pet;
        private readonly CompanionController controller;
        private readonly Timer previewTimer = new Timer();
        private readonly CompanionPreview preview;
        private readonly CompanionSwitch onTop, sound, autoHide;
        private readonly CompanionScale scale;
        private readonly Label scaleValue, status, timerLabel, timerHint;
        private readonly CompanionButton restMode, focusMode, start, reset;
        private readonly CompanionProgress progress;
        private bool syncing;
        private string lastStatus;
        internal FocusClock Clock { get { return controller.Clock; } }
        internal bool TimersStopped { get { return !previewTimer.Enabled && !controller.TimerEnabled; } }

        internal CompanionWindow(PetWindow petWindow, Assets assets)
        {
            SuspendLayout();
            pet = petWindow;
            controller = new CompanionController(pet);
            Text = "奶蛙桌宠"; Icon = pet.Icon;
            BackColor = CompanionStyle.Cream; ForeColor = CompanionStyle.Green;
            Font = CompanionStyle.Font(9f, FontStyle.Regular);
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(736, 646);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false; MinimizeBox = true;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            AddLabel(this, "N A I W A  /  C O M P A N I O N", 29, 20, 385, 20, 8f, CompanionStyle.Muted, FontStyle.Regular);
            AddLabel(this, "奶蛙桌宠", 26, 46, 390, 43, 23f, CompanionStyle.Green, FontStyle.Bold);
            Label motto = AddLabel(this, "大笑也是一种充电", 548, 43, 160, 31, 9f, CompanionStyle.Green, FontStyle.Regular);
            motto.TextAlign = ContentAlignment.MiddleCenter; motto.BackColor = CompanionStyle.Yellow;

            CompanionCard hero = Card(28, 108, 680, 252);
            preview = new CompanionPreview(pet, assets) { Location = new Point(8, 5), Size = new Size(246, 242) };
            hero.Controls.Add(preview);
            AddLabel(hero, "今天也请多关照", 280, 22, 355, 23, 9f, CompanionStyle.Muted, FontStyle.Regular);
            AddLabel(hero, "你忙你的，我陪着。", 276, 52, 375, 40, 19.5f, CompanionStyle.Green, FontStyle.Bold);
            status = AddLabel(hero, "● 正在发呆，偶尔眨眨眼", 280, 102, 365, 24, 9.5f, CompanionStyle.Green, FontStyle.Regular);
            AddLabel(hero, "戳戳软肚皮，收获一点好心情。\n双击奶蛙，让快乐晃起来。", 280, 134, 365, 43, 9f, CompanionStyle.Muted, FontStyle.Regular);
            CompanionButton laugh = Button(hero, "让它大笑", 280, 192, 108, 36, true);
            CompanionButton stand = Button(hero, "站起来", 398, 192, 102, 36, false);
            CompanionButton home = Button(hero, "回到右下角", 510, 192, 146, 36, false);
            laugh.Click += delegate { pet.Laugh(); RefreshPet(); };
            stand.Click += delegate { pet.StopLaugh(); RefreshPet(); };
            home.Click += delegate { pet.ReturnHome(); RefreshPet(); };

            CompanionCard preferences = Card(28, 378, 326, 222);
            AddLabel(preferences, "陪伴偏好", 20, 16, 150, 25, 12f, CompanionStyle.Green, FontStyle.Bold);
            AddLabel(preferences, "SETTINGS", 218, 19, 88, 22, 7.5f, CompanionStyle.Muted, FontStyle.Regular);
            onTop = Switch(preferences, "置顶显示", 51);
            sound = Switch(preferences, "原版笑声", 86);
            autoHide = Switch(preferences, "全屏时自动藏好", 121);
            onTop.CheckedChanged += delegate { if (!syncing) pet.SetOnTop(onTop.Checked); };
            sound.CheckedChanged += delegate { if (!syncing) pet.SetSound(sound.Checked); };
            autoHide.CheckedChanged += delegate { if (!syncing) pet.SetAutoHide(autoHide.Checked); };
            Panel divider = new Panel { BackColor = CompanionStyle.Border, Location = new Point(20, 164), Size = new Size(286, 1) };
            preferences.Controls.Add(divider);
            AddLabel(preferences, "奶蛙大小", 20, 178, 79, 26, 9f, CompanionStyle.Green, FontStyle.Regular);
            scale = new CompanionScale { Location = new Point(105, 177), Size = new Size(141, 30) };
            preferences.Controls.Add(scale);
            scaleValue = AddLabel(preferences, "85%", 258, 177, 51, 30, 9f, CompanionStyle.Muted, FontStyle.Regular);
            scale.ValueChanged += delegate { if (!syncing) { pet.SetSize(scale.Value); scaleValue.Text = scale.Value + "%"; } };

            CompanionCard focus = Card(372, 378, 336, 222);
            AddLabel(focus, "一起专注", 20, 16, 215, 25, 12f, CompanionStyle.Green, FontStyle.Bold);
            AddLabel(focus, "25 MIN", 256, 19, 61, 22, 7.5f, CompanionStyle.Muted, FontStyle.Regular);
            restMode = Button(focus, "摸鱼陪伴", 20, 51, 143, 31, false);
            focusMode = Button(focus, "专注 25 分钟", 173, 51, 143, 31, false);
            restMode.Click += delegate { controller.SetMode(false); };
            focusMode.Click += delegate { controller.SetMode(true); };
            timerLabel = AddLabel(focus, "歇会儿", 18, 88, 165, 54, 21f, CompanionStyle.Green, FontStyle.Bold);
            timerHint = AddLabel(focus, "慢慢来，也很好。", 187, 106, 132, 27, 8f, CompanionStyle.Muted, FontStyle.Regular);
            progress = new CompanionProgress { Location = new Point(20, 151), Size = new Size(296, 5) };
            focus.Controls.Add(progress);
            start = Button(focus, "开始专注", 20, 175, 201, 32, true);
            reset = Button(focus, "重置", 231, 175, 85, 32, false);
            start.Click += delegate { controller.ToggleRunning(); };
            reset.Click += delegate { controller.Reset(); };

            AddLabel(this, "戳戳肚子  ·  双击大笑  ·  按住拖动", 29, 614, 370, 20, 8f, CompanionStyle.Muted, FontStyle.Regular);
            Label footer = AddLabel(this, "关闭面板后，奶蛙依然在。", 429, 614, 278, 20, 8f, CompanionStyle.Muted, FontStyle.Regular);
            footer.TextAlign = ContentAlignment.MiddleRight;

            controller.Changed += delegate { if (Visible) RefreshClock(); };
            previewTimer.Interval = 80;
            previewTimer.Tick += delegate { SyncSettings(); RefreshPet(); };
            VisibleChanged += delegate { if (Visible) { SyncSettings(); RefreshClock(); RefreshPet(); previewTimer.Start(); } else previewTimer.Stop(); };
            SyncSettings(); RefreshClock();
            ResumeLayout(false);
            PerformLayout();
        }

        private CompanionCard Card(int x, int y, int width, int height)
        { CompanionCard card = new CompanionCard { Location = new Point(x, y), Size = new Size(width, height) }; Controls.Add(card); return card; }
        private static Label AddLabel(Control parent, string text, int x, int y, int width, int height, float size, Color color, FontStyle fontStyle)
        { Label label = CompanionStyle.Label(text, x, y, width, height, size, color, fontStyle); parent.Controls.Add(label); return label; }
        private static CompanionButton Button(Control parent, string text, int x, int y, int width, int height, bool accent)
        { CompanionButton button = new CompanionButton(text) { Location = new Point(x, y), Size = new Size(width, height), Accent = accent }; parent.Controls.Add(button); return button; }
        private static CompanionSwitch Switch(Control parent, string text, int y)
        { CompanionSwitch toggle = new CompanionSwitch(text) { Location = new Point(20, y), Size = new Size(286, 29) }; parent.Controls.Add(toggle); return toggle; }
        private void SyncSettings()
        {
            syncing = true;
            try { onTop.Checked = pet.TopMost; sound.Checked = pet.SoundEnabled; autoHide.Checked = pet.AutoHideEnabled; scale.Value = pet.ScalePercent; scaleValue.Text = pet.ScalePercent + "%"; }
            finally { syncing = false; }
        }
        private void RefreshPet()
        {
            string text = pet.HiddenForFullscreen ? "● 全屏中，奶蛙先安静躲一会儿" : pet.Motion.Playing ? "● 哈哈哈，快乐正在发生" : pet.Motion.Resting ? "● 笑累了，躺平充个电" : pet.Motion.Holding ? "● 被你捧起来啦" : pet.Motion.Poking ? "● 肚皮软软，烦恼散散" : "● 正在发呆，偶尔眨眨眼";
            if (lastStatus != text) { status.Text = text; lastStatus = text; }
            preview.Invalidate();
        }
        private void RefreshClock()
        {
            FocusClock clock = controller.Clock;
            restMode.Selected = !clock.FocusMode; focusMode.Selected = clock.FocusMode;
            restMode.Invalidate(); focusMode.Invalidate();
            if (!clock.FocusMode)
            { timerLabel.Text = "歇会儿"; timerHint.Text = "慢慢来，也很好。"; }
            else
            {
                int seconds = clock.RemainingSeconds;
                timerLabel.Text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
                timerHint.Text = clock.Completed ? "辛苦啦！歇一会儿。" : clock.Running ? "我在这里，陪你。" : clock.HasProgress ? "暂停中，随时继续。" : "从一件小事开始。";
            }
            start.Text = clock.Running ? "暂停一下" : clock.Completed ? "再来一轮" : clock.HasProgress ? "继续专注" : "开始专注";
            reset.Enabled = clock.HasProgress || clock.Running || clock.Completed;
            start.Invalidate(); progress.Progress = clock.FocusMode ? clock.Progress : 0; progress.Invalidate();
        }
        protected override void OnActivated(EventArgs e) { base.OnActivated(e); SyncSettings(); }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); }
            base.OnFormClosing(e);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { previewTimer.Stop(); previewTimer.Dispose(); controller.Dispose(); }
            base.Dispose(disposing);
        }
    }

    internal static class CompanionTests
    {
        private static void Check(bool condition, string name, List<string> log)
        {
            if (!condition) throw new Exception("FAIL: " + name);
            log.Add("PASS: " + name);
        }
        private static T Find<T>(Control parent, string text) where T : Control
        {
            foreach (Control child in parent.Controls)
            {
                T match = child as T;
                if (match != null && child.Text == text) return match;
                T nested = Find<T>(child, text);
                if (nested != null) return nested;
            }
            return null;
        }
        internal static void Run(Assets assets, string folder, List<string> log)
        {
            FocusClock clock = new FocusClock();
            Check(!clock.FocusMode && !clock.Running && clock.RemainingSeconds == 1500, "focus clock starts in relaxed companion mode", log);
            clock.Start();
            Check(clock.FocusMode && clock.Running && !clock.Advance(300) && clock.RemainingSeconds == 1200, "focus clock starts a 25 minute session", log);
            clock.Pause();
            Check(!clock.Advance(50) && clock.RemainingSeconds == 1200, "paused focus clock retains remaining time", log);
            clock.Start(); clock.Advance(100);
            Check(clock.Running && clock.RemainingSeconds == 1100, "focus clock resumes without resetting progress", log);
            clock.SetMode(false);
            Check(!clock.FocusMode && !clock.Running && !clock.Advance(200) && clock.RemainingSeconds == 1100, "relaxed mode pauses a focus session", log);
            clock.SetMode(true); clock.Start();
            Check(!clock.Advance(Double.NaN) && !clock.Advance(Double.PositiveInfinity) && !clock.Advance(Double.NegativeInfinity) && !clock.Advance(-20) && clock.RemainingSeconds == 1100, "focus clock rejects invalid elapsed time", log);
            Check(clock.Advance(1100.1) && clock.Completed && !clock.Running && clock.RemainingSeconds == 0 && !clock.Advance(1), "focus clock completes once and clamps to zero", log);
            clock.Start();
            Check(clock.Running && !clock.Completed && clock.RemainingSeconds == 1500, "completed focus session can start again", log);
            clock.Advance(4.2); clock.Pause(); clock.Reset();
            Check(!clock.Running && !clock.Completed && !clock.HasProgress && clock.RemainingSeconds == 1500 && clock.Progress == 0, "focus reset clears elapsed time and completion", log);

            using (PetWindow pet = new PetWindow(assets, true))
            using (CompanionWindow panel = new CompanionWindow(pet, assets))
            {
                pet.Show(); panel.Show(); Application.DoEvents();
                Check(panel.Visible && panel.IsHandleCreated, "companion panel opens alongside the pet", log);
                using (Bitmap bitmap = new Bitmap(panel.Width, panel.Height))
                { panel.DrawToBitmap(bitmap, new Rectangle(Point.Empty, panel.Size)); bitmap.Save(Path.Combine(folder, "companion-panel.png"), ImageFormat.Png); }

                CompanionButton laugh = Find<CompanionButton>(panel, "让它大笑");
                CompanionButton stand = Find<CompanionButton>(panel, "站起来");
                Check(laugh != null && stand != null, "companion action buttons are accessible controls", log);
                laugh.PerformClick();
                Check(pet.Motion.Playing, "companion laugh button starts the original action", log);
                stand.PerformClick();
                Check(!pet.Motion.Playing && !pet.Motion.Resting, "companion stand button restores idle", log);
                CompanionSwitch top = Find<CompanionSwitch>(panel, "置顶显示");
                bool oldTop = pet.TopMost; top.Checked = !oldTop;
                Check(pet.TopMost != oldTop, "companion preference toggle updates the pet", log);

                Find<CompanionButton>(panel, "专注 25 分钟").PerformClick();
                Find<CompanionButton>(panel, "开始专注").PerformClick();
                Check(panel.Clock.FocusMode && panel.Clock.Running, "companion focus controls start a session", log);
                panel.Clock.Advance(125);
                Find<CompanionButton>(panel, "暂停一下").PerformClick();
                Check(!panel.Clock.Running && panel.Clock.RemainingSeconds <= 1375 && panel.Clock.RemainingSeconds >= 1374, "companion pause control preserves progress", log);
                Find<CompanionButton>(panel, "继续专注").PerformClick();
                panel.Close(); Application.DoEvents();
                Check(!panel.Visible && !panel.IsDisposed && panel.Clock.Running && !panel.TimersStopped, "closing companion panel hides it and retains the running session", log);
                panel.Show(); Application.DoEvents();
                Check(panel.Visible && panel.Clock.Running, "companion panel can reopen with the same focus session", log);
                Find<CompanionButton>(panel, "重置").PerformClick();
                Check(!panel.Clock.Running && panel.Clock.RemainingSeconds == 1500, "companion reset control starts from 25 minutes", log);
                panel.Dispose();
                Check(panel.IsDisposed && panel.TimersStopped, "disposing companion panel releases both timers", log);
                pet.Close();
            }
        }
    }
}
