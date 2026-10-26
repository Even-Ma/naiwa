using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;

namespace NaiwaDesktop
{
    internal sealed class SpriteActionClip
    {
        internal string Id { get; private set; }
        internal string Name { get; private set; }
        internal string Sheet { get; private set; }
        internal int Columns { get; private set; }
        internal int Rows { get; private set; }
        internal int Frames { get; private set; }
        internal double Fps { get; private set; }
        internal int Cycles { get; private set; }
        internal string SourceDescription { get; private set; }
        internal double Duration { get { return Frames / Fps * Cycles; } }
        internal readonly string FullSheetPath;
        internal Bitmap[] DecodedFrames;
        internal bool HasSourceTransparency;

        internal SpriteActionClip(string id, string name, string sheet, string fullSheetPath,
            int columns, int rows, int frames, double fps, int cycles, string sourceDescription)
        {
            Id = id; Name = name; Sheet = sheet; FullSheetPath = fullSheetPath;
            Columns = columns; Rows = rows; Frames = frames; Fps = fps;
            Cycles = cycles; SourceDescription = sourceDescription;
        }
    }

    // Frames belong to this library. Renderers must not dispose returned bitmaps.
    internal sealed class SpriteActionLibrary : IDisposable
    {
        private readonly Dictionary<string, SpriteActionClip> byId = new Dictionary<string, SpriteActionClip>(StringComparer.Ordinal);
        private readonly List<SpriteActionClip> clips = new List<SpriteActionClip>();
        private readonly ReadOnlyCollection<SpriteActionClip> readOnlyClips;
        private readonly object sync = new object();
        private readonly int canvasWidth, canvasHeight;
        private bool disposed;
        internal int Count { get { return clips.Count; } }
        internal ReadOnlyCollection<SpriteActionClip> Clips { get { return readOnlyClips; } }
        internal string UnavailableReason { get; private set; }

        internal SpriteActionLibrary(string actionsFolder) : this(actionsFolder, 288, 368) { }

        internal SpriteActionLibrary(string actionsFolder, int originalWidth, int originalHeight)
        {
            readOnlyClips = clips.AsReadOnly();
            if (originalWidth < 1 || originalHeight < 1 || originalWidth > 2048 || originalHeight > 2048)
                throw new ArgumentOutOfRangeException("originalWidth", "动作画布尺寸无效。");
            canvasWidth = originalWidth; canvasHeight = originalHeight;
            if (String.IsNullOrWhiteSpace(actionsFolder)) throw new ArgumentException("需要提供动作素材文件夹。", "actionsFolder");
            string root = Path.GetFullPath(actionsFolder);
            string manifestPath = Path.Combine(root, "manifest.json");
            if (!File.Exists(manifestPath)) { UnavailableReason = "未安装额外动作素材。"; return; }
            try
            {
                if (new FileInfo(manifestPath).Length > 262144) throw new InvalidDataException("manifest.json 超过大小限制。");
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = 262144;
                Dictionary<string, object> manifest = serializer.DeserializeObject(File.ReadAllText(manifestPath, Encoding.UTF8)) as Dictionary<string, object>;
                if (manifest == null) throw new InvalidDataException("根节点必须是对象。");
                if (Integer(manifest, "version", 1, 1) != 1) throw new InvalidDataException("不支持的动作清单版本。");
                object rawActions;
                if (!manifest.TryGetValue("actions", out rawActions)) throw new InvalidDataException("缺少 actions 数组。");
                IList actions = rawActions as IList;
                if (actions == null || actions.Count > 32) throw new InvalidDataException("actions 必须是最多 32 项的数组。");
                string missing = null;
                foreach (object rawAction in actions)
                {
                    Dictionary<string, object> action = rawAction as Dictionary<string, object>;
                    if (action == null) throw new InvalidDataException("每个动作必须是对象。");
                    string id = Text(action, "id", 64);
                    foreach (char c in id)
                        if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '-' && c != '_')
                            throw new InvalidDataException("动作 id 只能包含小写英文字母、数字、横线和下划线。");
                    if (byId.ContainsKey(id)) throw new InvalidDataException("重复动作 id：" + id);
                    string name = Text(action, "name", 80);
                    string sheet = Text(action, "sheet", 240);
                    string sheetPath = ResolveSheet(root, sheet);
                    int columns = Integer(action, "columns", 1, 32), rows = Integer(action, "rows", 1, 32);
                    int frames = Integer(action, "frames", 2, 256);
                    if (frames > columns * rows) throw new InvalidDataException("动作 " + id + " 的帧数超过网格数量。");
                    double fps = Number(action, "fps", 1, 60);
                    int cycles = Integer(action, "cycles", 1, 20);
                    string source = Text(action, "sourceDescription", 2048);
                    SpriteActionClip clip = new SpriteActionClip(id, name, sheet, sheetPath, columns, rows, frames, fps, cycles, source);
                    if (clip.Duration > 120) throw new InvalidDataException("动作 " + id + " 的播放时长超过 120 秒。");
                    clips.Add(clip); byId.Add(id, clip);
                    if (!File.Exists(sheetPath)) missing = sheet;
                }
                // A source checkout without optional PNGs still starts its original pet.
                if (missing != null)
                {
                    clips.Clear(); byId.Clear(); UnavailableReason = "额外动作素材不完整：" + missing;
                }
            }
            catch (Exception error)
            {
                if (error is OutOfMemoryException) throw;
                throw new InvalidDataException("奶蛙动作清单无效（" + manifestPath + "）：" + error.Message, error);
            }
        }

        private static string Text(Dictionary<string, object> data, string key, int maxLength)
        {
            object value;
            if (!data.TryGetValue(key, out value) || !(value is string)) throw new InvalidDataException("缺少或无效的文本字段：" + key);
            string text = ((string)value).Trim();
            if (text.Length == 0 || text.Length > maxLength) throw new InvalidDataException("文本字段长度无效：" + key);
            return text;
        }
        private static double Number(Dictionary<string, object> data, string key, double minimum, double maximum)
        {
            object value;
            if (!data.TryGetValue(key, out value) || value == null || value is string || value is bool)
                throw new InvalidDataException("缺少或无效的数值字段：" + key);
            double result;
            try { result = Convert.ToDouble(value, CultureInfo.InvariantCulture); }
            catch (Exception error) { throw new InvalidDataException("数值字段无效：" + key, error); }
            if (Double.IsNaN(result) || Double.IsInfinity(result) || result < minimum || result > maximum)
                throw new InvalidDataException("数值字段超出范围：" + key);
            return result;
        }
        private static int Integer(Dictionary<string, object> data, string key, int minimum, int maximum)
        {
            double value = Number(data, key, minimum, maximum);
            if (Math.Truncate(value) != value) throw new InvalidDataException("字段必须为整数：" + key);
            return (int)value;
        }
        private static string ResolveSheet(string root, string relativePath)
        {
            if (Path.IsPathRooted(relativePath)) throw new InvalidDataException("sheet 必须是素材文件夹内的相对路径。");
            string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) || !String.Equals(Path.GetExtension(fullPath), ".png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("sheet 必须指向素材文件夹内的 PNG 文件。");
            return fullPath;
        }

        internal SpriteActionClip Find(string id)
        {
            if (id == null) return null;
            SpriteActionClip clip;
            return byId.TryGetValue(id, out clip) ? clip : null;
        }

        internal Bitmap GetFrame(string id, int index)
        {
            lock (sync)
            {
                if (disposed) throw new ObjectDisposedException("SpriteActionLibrary");
                SpriteActionClip clip = Find(id);
                if (clip == null) return null;
                if (clip.DecodedFrames == null) LoadFrames(clip);
                return clip.DecodedFrames[Math.Max(0, Math.Min(clip.Frames - 1, index))];
            }
        }

        private void LoadFrames(SpriteActionClip clip)
        {
            Bitmap[] decoded = new Bitmap[clip.Frames];
            Bitmap[] cells = new Bitmap[clip.Frames];
            try
            {
                using (FileStream stream = File.OpenRead(clip.FullSheetPath))
                using (Image sheet = Image.FromStream(stream, true, true))
                {
                    if (sheet.RawFormat.Guid != ImageFormat.Png.Guid) throw new InvalidDataException("精灵图必须是 PNG。");
                    if (sheet.Width < clip.Columns || sheet.Height < clip.Rows || sheet.Width > 8192 || sheet.Height > 8192 || (long)sheet.Width * sheet.Height > 33554432)
                        throw new InvalidDataException("精灵图尺寸无效。");
                    if (sheet.Width % clip.Columns != 0 || sheet.Height % clip.Rows != 0)
                        throw new InvalidDataException("精灵图宽高必须能被 columns 和 rows 整除。");
                    Rectangle commonBounds = Rectangle.Empty;
                    bool sourceTransparency = true;
                    int width = sheet.Width / clip.Columns, height = sheet.Height / clip.Rows;
                    for (int i = 0; i < clip.Frames; i++)
                    {
                        int column = i % clip.Columns, row = i / clip.Columns;
                        int left = width * column, top = height * row;
                        // Copy the whole grid cell first to avoid sampling a neighboring pose.
                        Bitmap cell = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
                        cells[i] = cell;
                        using (Graphics cellGraphics = Graphics.FromImage(cell))
                        {
                            cellGraphics.CompositingMode = CompositingMode.SourceCopy;
                            cellGraphics.DrawImage(sheet, new Rectangle(0, 0, width, height), left, top, width, height, GraphicsUnit.Pixel);
                        }
                        int transparentPixels;
                        Rectangle visibleBounds = AlphaBounds(cell, out transparentPixels);
                        if (transparentPixels <= width * height / 20) sourceTransparency = false;
                        if (visibleBounds.IsEmpty) throw new InvalidDataException("第 " + (i + 1) + " 帧没有可见姿态。");
                        commonBounds = commonBounds.IsEmpty ? visibleBounds : Rectangle.Union(commonBounds, visibleBounds);
                    }
                    // One common source rectangle and transform preserves relative motion.
                    // No frame is individually recentered, cropped, warped or rotated.
                    commonBounds.Inflate(1, 1);
                    commonBounds.Intersect(new Rectangle(0, 0, width, height));
                    double scale = Math.Min(Math.Max(1, canvasWidth - 20) / (double)commonBounds.Width,
                        Math.Max(1, canvasHeight - 44) / (double)commonBounds.Height);
                    int fittedWidth = Math.Max(1, (int)Math.Round(commonBounds.Width * scale));
                    int fittedHeight = Math.Max(1, (int)Math.Round(commonBounds.Height * scale));
                    Rectangle destination = new Rectangle((canvasWidth - fittedWidth) / 2,
                        Math.Max(0, canvasHeight - 14 - fittedHeight), fittedWidth, fittedHeight);
                    for (int i = 0; i < clip.Frames; i++)
                    {
                        Bitmap canvas = new Bitmap(canvasWidth, canvasHeight, PixelFormat.Format32bppPArgb);
                        decoded[i] = canvas;
                        using (Graphics graphics = Graphics.FromImage(canvas))
                        using (ImageAttributes attributes = new ImageAttributes())
                        {
                            graphics.Clear(Color.Transparent);
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            attributes.SetWrapMode(WrapMode.TileFlipXY);
                            graphics.DrawImage(cells[i], destination, commonBounds.X, commonBounds.Y,
                                commonBounds.Width, commonBounds.Height, GraphicsUnit.Pixel, attributes);
                        }
                    }
                    clip.HasSourceTransparency = sourceTransparency;
                }
                clip.DecodedFrames = decoded;
            }
            catch (Exception error)
            {
                foreach (Bitmap frame in decoded) if (frame != null) frame.Dispose();
                if (error is OutOfMemoryException) throw;
                throw new InvalidDataException("无法读取奶蛙动作「" + clip.Name + "」（" + clip.Sheet + "）：" + error.Message, error);
            }
            finally { foreach (Bitmap cell in cells) if (cell != null) cell.Dispose(); }
        }

        private static Rectangle AlphaBounds(Bitmap image, out int transparentPixels)
        {
            transparentPixels = 0;
            int left = image.Width, right = -1, top = image.Height, bottom = -1;
            BitmapData data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            try
            {
                byte[] row = new byte[image.Width * 4];
                for (int y = 0; y < image.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                    for (int x = 0; x < image.Width; x++)
                    {
                        if (row[x * 4 + 3] < 16) transparentPixels++;
                        if (row[x * 4 + 3] == 0) continue;
                        if (x < left) left = x; if (x > right) right = x;
                        if (y < top) top = y; if (y > bottom) bottom = y;
                    }
                }
            }
            finally { image.UnlockBits(data); }
            return right < left ? Rectangle.Empty : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
        }

        public void Dispose()
        {
            lock (sync)
            {
                if (disposed) return;
                disposed = true;
                foreach (SpriteActionClip clip in clips)
                {
                    if (clip.DecodedFrames == null) continue;
                    foreach (Bitmap frame in clip.DecodedFrames) frame.Dispose();
                    clip.DecodedFrames = null;
                }
            }
        }
    }

    internal sealed class MemePlayback
    {
        private SpriteActionLibrary library;
        private SpriteActionClip current;
        private double started = -1, elapsed;
        internal SpriteActionClip Current { get { return current; } }
        internal double Started { get { return started; } }
        internal double Elapsed { get { return elapsed; } }
        internal bool IsPlaying { get { return current != null; } }
        internal int FrameIndex { get { return current == null ? 0 : (int)Math.Floor(elapsed * current.Fps) % current.Frames; } }
        internal MemePlayback() { }
        internal MemePlayback(SpriteActionLibrary actionLibrary) { Bind(actionLibrary); }
        internal void Bind(SpriteActionLibrary actionLibrary)
        {
            if (Object.ReferenceEquals(library, actionLibrary)) return;
            Stop(); library = actionLibrary;
        }
        internal bool Start(string id, double time)
        {
            if (library == null || Double.IsNaN(time) || Double.IsInfinity(time)) return false;
            SpriteActionClip clip = library.Find(id);
            if (clip == null) return false;
            current = clip; started = time; elapsed = 0;
            return true;
        }
        internal bool Update(double now)
        {
            if (current == null) return false;
            if (Double.IsNaN(now) || Double.IsInfinity(now)) return true;
            elapsed = Math.Max(elapsed, Math.Max(0, now - started));
            if (elapsed >= current.Duration) { Stop(); return false; }
            return true;
        }
        internal void Stop() { current = null; started = -1; elapsed = 0; }
    }

    internal static class MemeActionTests
    {
        private static void Check(bool condition, string name, List<string> log)
        { if (!condition) throw new Exception("FAIL: " + name); log.Add("PASS: " + name); }

        internal static void Run(Assets assets, string folder, List<string> log)
        {
            SpriteActionLibrary library = assets.Memes;
            Check(library != null && library.Count > 0, "meme action manifest and bundled sprites are present", log);
            foreach (string id in new string[] { "shufflestep", "headbob", "holdlaugh" })
                Check(library.Find(id) != null, "meme action is available: " + id, log);
            foreach (SpriteActionClip clip in library.Clips)
            {
                HashSet<ulong> poses = new HashSet<ulong>();
                for (int i = 0; i < clip.Frames; i++)
                {
                    Bitmap frame = library.GetFrame(clip.Id, i);
                    Check(frame.Width == assets.Width && frame.Height == assets.Height, clip.Id + " frame " + i + " fits the original transparent canvas", log);
                    int transparent, visible;
                    poses.Add(Fingerprint(frame, out transparent, out visible));
                    Check(transparent > frame.Width * frame.Height / 20 && visible > 100, clip.Id + " frame " + i + " contains a visible pose and transparent background", log);
                }
                Check(clip.HasSourceTransparency, clip.Id + " source PNG cells have a transparent background", log);
                Check(poses.Count >= Math.Min(3, clip.Frames), clip.Id + " has distinct sprite poses", log);
                Check(Math.Abs(clip.Duration - clip.Frames / clip.Fps * clip.Cycles) < .000001, clip.Id + " duration includes every playback cycle", log);
                SaveContactSheet(library, clip, folder);
                TestClock(library, clip, log);
            }

            string absent = Path.Combine(folder, "meme-optional-absent");
            using (SpriteActionLibrary optional = new SpriteActionLibrary(absent))
                Check(optional.Count == 0 && optional.Find("shufflestep") == null, "missing optional meme resources leave an empty library", log);
            string fixtures = Path.Combine(folder, "meme-loader-checks");
            Directory.CreateDirectory(fixtures);
            string fixtureManifest = Path.Combine(fixtures, "manifest.json");
            File.WriteAllText(fixtureManifest, "{\"version\":1,\"actions\":[{\"id\":\"missing\",\"name\":\"missing\",\"sheet\":\"sheets/absent.png\",\"columns\":4,\"rows\":2,\"frames\":8,\"fps\":8,\"cycles\":2,\"sourceDescription\":\"test missing optional file\"}]}", Encoding.UTF8);
            using (SpriteActionLibrary optional = new SpriteActionLibrary(fixtures))
                Check(optional.Count == 0, "missing optional PNG leaves an empty library without blocking startup", log);
            File.WriteAllText(fixtureManifest, "{\"version\":1,\"actions\":[{\"id\":\"invalid\"}]}", Encoding.UTF8);
            bool invalidReported = false;
            try { using (SpriteActionLibrary invalid = new SpriteActionLibrary(fixtures)) { } }
            catch (InvalidDataException error) { invalidReported = error.Message.IndexOf("动作清单", StringComparison.Ordinal) >= 0; }
            Check(invalidReported, "malformed meme manifest reports a clear validation error", log);
        }

        private static void TestClock(SpriteActionLibrary library, SpriteActionClip clip, List<string> log)
        {
            MemePlayback playback = new MemePlayback(library);
            Check(!playback.IsPlaying && playback.Current == null && playback.FrameIndex == 0, clip.Id + " playback starts idle", log);
            Check(playback.Start(clip.Id, 100) && playback.Current == clip && playback.FrameIndex == 0, clip.Id + " starts at the first frame", log);
            playback.Update(100 + 1.25 / clip.Fps);
            Check(playback.FrameIndex == 1, clip.Id + " advances using the monotonic playback clock", log);
            double beforeInvalid = playback.Elapsed;
            Check(!playback.Start("unknown-action", 200) && playback.Current == clip && playback.Started == 100 && playback.Elapsed == beforeInvalid, clip.Id + " invalid id preserves the current action", log);
            if (clip.Cycles > 1)
            {
                playback.Update(100 + (clip.Frames + .25) / clip.Fps);
                Check(playback.IsPlaying && playback.FrameIndex == 0, clip.Id + " wraps to frame zero on the next cycle", log);
            }
            playback.Update(100 + clip.Duration - .25 / clip.Fps);
            Check(playback.IsPlaying && playback.FrameIndex == clip.Frames - 1, clip.Id + " reaches the final frame of the final cycle", log);
            Check(!playback.Update(100 + clip.Duration + .001) && !playback.IsPlaying && playback.Current == null, clip.Id + " finishes and returns to idle", log);
            Check(!playback.Update(1000), clip.Id + " completed action does not restart itself", log);
            playback.Start(clip.Id, 200);
            playback.Stop();
            Check(!playback.IsPlaying && playback.Current == null && playback.Elapsed == 0 && playback.Started == -1, clip.Id + " explicit stop cancels the action", log);
        }

        private static ulong Fingerprint(Bitmap frame, out int transparent, out int visible)
        {
            transparent = 0; visible = 0;
            Rectangle bounds = new Rectangle(0, 0, frame.Width, frame.Height);
            BitmapData data = frame.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);
            try
            {
                byte[] row = new byte[frame.Width * 4];
                ulong hash = 14695981039346656037UL;
                for (int y = 0; y < frame.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, row.Length);
                    for (int x = 0; x < row.Length; x += 4)
                    {
                        if (row[x + 3] < 16) transparent++;
                        if (row[x + 3] > 128) visible++;
                        for (int c = 0; c < 4; c++) hash = unchecked((hash ^ row[x + c]) * 1099511628211UL);
                    }
                }
                return hash;
            }
            finally { frame.UnlockBits(data); }
        }

        private static void SaveContactSheet(SpriteActionLibrary library, SpriteActionClip clip, string folder)
        {
            const int columns = 4, width = 180, height = 254, header = 44;
            int rows = (clip.Frames + columns - 1) / columns;
            using (Bitmap output = new Bitmap(columns * width, header + rows * height, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(output))
            using (Font title = new Font("Microsoft YaHei UI", 12f))
            using (Font label = new Font("Microsoft YaHei UI", 9f))
            using (Brush ink = new SolidBrush(Color.FromArgb(52, 75, 57)))
            using (Brush tile = new SolidBrush(Color.FromArgb(226, 236, 223)))
            {
                graphics.Clear(Color.FromArgb(247, 249, 241));
                graphics.DrawString(clip.Name + " / " + clip.Id + " / " + clip.Fps.ToString("0.##", CultureInfo.InvariantCulture) + " fps", title, ink, 12, 10);
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                for (int i = 0; i < clip.Frames; i++)
                {
                    int left = (i % columns) * width, top = header + (i / columns) * height;
                    for (int y = 0; y < height - 24; y += 12)
                        for (int x = 0; x < width; x += 12)
                            if ((x / 12 + y / 12) % 2 == 0) graphics.FillRectangle(tile, left + x, top + y, 12, Math.Min(12, height - 24 - y));
                    Bitmap frame = library.GetFrame(clip.Id, i);
                    float scale = Math.Min((width - 12f) / frame.Width, (height - 30f) / frame.Height);
                    float drawWidth = frame.Width * scale, drawHeight = frame.Height * scale;
                    graphics.DrawImage(frame, left + (width - drawWidth) / 2, top + (height - 24 - drawHeight) / 2, drawWidth, drawHeight);
                    graphics.DrawString((i + 1).ToString(CultureInfo.InvariantCulture), label, ink, left + 8, top + height - 21);
                }
                output.Save(Path.Combine(folder, "meme-" + clip.Id + "-contact.png"), ImageFormat.Png);
            }
        }
    }
}
