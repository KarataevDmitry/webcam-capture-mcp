using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using NAudio.Wave;
using OpenCvSharp;
using WebcamMcp.Shared;
using static WebcamMcp.Shared.McpDefaults;
using static WebcamMcp.Shared.ToolArgs;
using Tool = ModelContextProtocol.Protocol.Tool;

static JsonElement Schema(object schema) => JsonSerializer.SerializeToElement(schema);

const int SmXVirtualScreen = 76;
const int SmYVirtualScreen = 77;
const int SmCxVirtualScreen = 78;
const int SmCyVirtualScreen = 79;

[DllImport("user32.dll")]
static extern int GetSystemMetrics(int nIndex);

[DllImport("user32.dll")]
static extern bool EnumDisplayMonitors(
    IntPtr hdc,
    IntPtr lprcClip,
    MonitorEnumProc lpfnEnum,
    IntPtr dwData);

var toolsList = new List<Tool>
{
    new()
    {
        Name = "capture_webcam_frame",
        Description = "Сделать снимок с веб-камеры по явному запросу. Сохраняет изображение в workspace (по умолчанию .cascade-ide/webcam-captures) и возвращает JSON с путём и параметрами кадра.",
        InputSchema = Schema(new
        {
            type = "object",
            properties = new
            {
                workspace_path = new { type = "string", description = "Каталог workspace (корень проекта в Cursor)." },
                camera_index = new { type = "integer", description = "Индекс камеры (по умолчанию 0)." },
                width = new { type = "integer", description = "Желаемая ширина кадра (опционально)." },
                height = new { type = "integer", description = "Желаемая высота кадра (опционально)." },
                warmup_frames = new { type = "integer", description = "Количество прогревочных кадров перед снимком (по умолчанию 5)." },
                image_format = new { type = "string", description = "Формат: jpg или png (по умолчанию jpg)." },
                jpeg_quality = new { type = "integer", description = "Качество JPEG 1..100 (по умолчанию 92)." },
                output_subdir = new { type = "string", description = @"Подкаталог внутри workspace для сохранения кадров (по умолчанию .cascade-ide\webcam-captures)." },
                file_name = new { type = "string", description = "Имя файла без расширения (опционально)." }
            },
            required = new[] { "workspace_path" }
        })
    },
    new()
    {
        Name = "capture_webcam_burst",
        Description = "Сделать быструю серию кадров с веб-камеры. Сохраняет кадры в подпапку внутри workspace и (опционально) собирает короткий видеофайл.",
        InputSchema = Schema(new
        {
            type = "object",
            properties = new
            {
                workspace_path = new { type = "string", description = "Каталог workspace (корень проекта в Cursor)." },
                camera_index = new { type = "integer", description = "Индекс камеры (по умолчанию 0)." },
                width = new { type = "integer", description = "Желаемая ширина кадра (опционально)." },
                height = new { type = "integer", description = "Желаемая высота кадра (опционально)." },
                warmup_frames = new { type = "integer", description = "Количество прогревочных кадров перед серией (по умолчанию 5)." },
                duration_sec = new { type = "integer", description = "Длительность серии в секундах (по умолчанию 2)." },
                target_fps = new { type = "integer", description = "Целевой FPS съёмки (по умолчанию 24)." },
                image_format = new { type = "string", description = "Формат кадров: jpg или png (по умолчанию jpg)." },
                jpeg_quality = new { type = "integer", description = "Качество JPEG 1..100 (по умолчанию 92)." },
                output_subdir = new { type = "string", description = @"Подкаталог внутри workspace для сохранения серии (по умолчанию .cascade-ide\webcam-captures)." },
                burst_name = new { type = "string", description = "Имя серии (опционально)." },
                save_video = new { type = "boolean", description = "Сохранить также видеофайл (по умолчанию false)." },
                video_fps = new { type = "integer", description = "FPS для сохранённого видео (по умолчанию 24)." },
                video_format = new { type = "string", description = "Формат видео: mp4 или avi (по умолчанию mp4)." }
            },
            required = new[] { "workspace_path" }
        })
    },
    new()
    {
        Name = "capture_screen_burst",
        Description = "Сделать быструю серию кадров с экрана. Сохраняет кадры в подпапку внутри workspace и (опционально) собирает короткий видеофайл.",
        InputSchema = Schema(new
        {
            type = "object",
            properties = new
            {
                workspace_path = new { type = "string", description = "Каталог workspace (корень проекта в Cursor)." },
                monitor = new
                {
                    description = "Монитор: номер (1-based слева направо) или 'all'. Если указан и x/y/width/height не заданы, регион берётся автоматически.",
                    anyOf = new object[]
                    {
                        new { type = "integer" },
                        new { type = "string" }
                    }
                },
                x = new { type = "integer", description = "Левая координата области захвата (по умолчанию виртуальный экран)." },
                y = new { type = "integer", description = "Верхняя координата области захвата (по умолчанию виртуальный экран)." },
                width = new { type = "integer", description = "Ширина области захвата (по умолчанию ширина виртуального экрана)." },
                height = new { type = "integer", description = "Высота области захвата (по умолчанию высота виртуального экрана)." },
                duration_sec = new { type = "integer", description = "Длительность серии в секундах (по умолчанию 2)." },
                target_fps = new { type = "integer", description = "Целевой FPS съёмки (по умолчанию 24)." },
                image_format = new { type = "string", description = "Формат кадров: jpg или png (по умолчанию jpg)." },
                jpeg_quality = new { type = "integer", description = "Качество JPEG 1..100 (по умолчанию 92)." },
                output_subdir = new { type = "string", description = @"Подкаталог внутри workspace для сохранения серии (по умолчанию .cascade-ide\screen-captures)." },
                burst_name = new { type = "string", description = "Имя серии (опционально)." },
                save_video = new { type = "boolean", description = "Сохранить также видеофайл (по умолчанию false)." },
                video_fps = new { type = "integer", description = "FPS для сохранённого видео (по умолчанию 24)." },
                video_format = new { type = "string", description = "Формат видео: mp4 или avi (по умолчанию mp4)." }
            },
            required = new[] { "workspace_path" }
        })
    },
    new()
    {
        Name = "capture_audio_burst",
        Description = "Записать короткий аудиофрагмент с микрофона в WAV по явной команде. Сохраняет файл в workspace (по умолчанию .cascade-ide/audio-captures).",
        InputSchema = Schema(new
        {
            type = "object",
            properties = new
            {
                workspace_path = new { type = "string", description = "Каталог workspace (корень проекта в Cursor)." },
                duration_sec = new { type = "integer", description = "Длительность записи в секундах (по умолчанию 10)." },
                sample_rate = new { type = "integer", description = "Частота дискретизации (по умолчанию 16000)." },
                channels = new { type = "integer", description = "Число каналов (по умолчанию 1)." },
                device_number = new { type = "integer", description = "Номер устройства микрофона (по умолчанию 0)." },
                output_subdir = new { type = "string", description = @"Подкаталог внутри workspace (по умолчанию .cascade-ide\audio-captures)." },
                file_name = new { type = "string", description = "Имя wav-файла без расширения (опционально)." }
            },
            required = new[] { "workspace_path" }
        })
    },
    new()
    {
        Name = "capture_av_burst",
        Description = "Одновременная запись короткой A/V-сессии: кадры с веб-камеры + WAV с микрофона + метаданные синхронизации.",
        InputSchema = Schema(new
        {
            type = "object",
            properties = new
            {
                workspace_path = new { type = "string", description = "Каталог workspace (корень проекта в Cursor)." },
                duration_sec = new { type = "integer", description = "Длительность захвата в секундах (по умолчанию 10)." },
                target_fps = new { type = "integer", description = "Целевой FPS для кадров (по умолчанию 24)." },
                camera_index = new { type = "integer", description = "Индекс камеры (по умолчанию 0)." },
                audio_device_number = new { type = "integer", description = "Индекс микрофона (по умолчанию 0)." },
                width = new { type = "integer", description = "Желаемая ширина кадра (опционально)." },
                height = new { type = "integer", description = "Желаемая высота кадра (опционально)." },
                audio_sample_rate = new { type = "integer", description = "Частота аудио (по умолчанию 16000)." },
                audio_channels = new { type = "integer", description = "Каналы аудио (по умолчанию 1)." },
                warmup_frames = new { type = "integer", description = "Прогрев кадров перед записью (по умолчанию 5)." },
                image_format = new { type = "string", description = "Формат кадров: jpg|png (по умолчанию jpg)." },
                jpeg_quality = new { type = "integer", description = "Качество JPEG 1..100 (по умолчанию 92)." },
                output_subdir = new { type = "string", description = @"Подкаталог внутри workspace (по умолчанию .cascade-ide\av-captures)." },
                session_name = new { type = "string", description = "Имя A/V-сессии (опционально)." },
                save_video = new { type = "boolean", description = "Собрать mp4 из кадров (по умолчанию true)." },
                video_fps = new { type = "integer", description = "FPS для mp4 (по умолчанию 24)." }
            },
            required = new[] { "workspace_path" }
        })
    },
    new()
    {
        Name = "capture_screen_av_burst",
        Description = "Одновременная запись короткой A/V-сессии: кадры с экрана + WAV с микрофона + метаданные синхронизации.",
        InputSchema = Schema(new
        {
            type = "object",
            properties = new
            {
                workspace_path = new { type = "string", description = "Каталог workspace (корень проекта в Cursor)." },
                duration_sec = new { type = "integer", description = "Длительность захвата в секундах (по умолчанию 10)." },
                target_fps = new { type = "integer", description = "Целевой FPS для кадров (по умолчанию 24)." },
                audio_device_number = new { type = "integer", description = "Индекс микрофона (по умолчанию 0)." },
                monitor = new
                {
                    description = "Монитор: номер (1-based слева направо) или 'all'. Если указан и x/y/width/height не заданы, регион берётся автоматически.",
                    anyOf = new object[]
                    {
                        new { type = "integer" },
                        new { type = "string" }
                    }
                },
                x = new { type = "integer", description = "Левая координата области экрана (по умолчанию виртуальный экран)." },
                y = new { type = "integer", description = "Верхняя координата области экрана (по умолчанию виртуальный экран)." },
                width = new { type = "integer", description = "Ширина области экрана (по умолчанию виртуальный экран)." },
                height = new { type = "integer", description = "Высота области экрана (по умолчанию виртуальный экран)." },
                audio_sample_rate = new { type = "integer", description = "Частота аудио (по умолчанию 16000)." },
                audio_channels = new { type = "integer", description = "Каналы аудио (по умолчанию 1)." },
                image_format = new { type = "string", description = "Формат кадров: jpg|png (по умолчанию jpg)." },
                jpeg_quality = new { type = "integer", description = "Качество JPEG 1..100 (по умолчанию 92)." },
                output_subdir = new { type = "string", description = @"Подкаталог внутри workspace (по умолчанию .cascade-ide\av-captures)." },
                session_name = new { type = "string", description = "Имя A/V-сессии (опционально)." },
                save_video = new { type = "boolean", description = "Собрать mp4 из кадров (по умолчанию true)." },
                video_fps = new { type = "integer", description = "FPS для mp4 (по умолчанию 24)." }
            },
            required = new[] { "workspace_path" }
        })
    }
};

static string HandleCaptureWebcamFrame(IReadOnlyDictionary<string, JsonElement> args)
{
    var workspacePath = GetRequiredString(args, "workspace_path");
    var cameraIndex = GetOptionalInt(args, "camera_index", 0);
    var warmupFrames = Math.Clamp(GetOptionalInt(args, "warmup_frames", DefaultWarmupFrames), 0, 50);
    var requestedWidth = GetOptionalInt(args, "width", 0);
    var requestedHeight = GetOptionalInt(args, "height", 0);
    var jpegQuality = Math.Clamp(GetOptionalInt(args, "jpeg_quality", DefaultJpegQuality), 1, 100);
    var imageFormat = NormalizeImageFormat(GetOptionalString(args, "image_format") ?? "jpg");
    var outputSubdir = GetOptionalString(args, "output_subdir") ?? DefaultOutputSubdir;
    var fileName = GetOptionalString(args, "file_name");

    var workspaceRoot = Path.GetFullPath(workspacePath.Trim());
    if (File.Exists(workspaceRoot))
    {
        workspaceRoot = Path.GetDirectoryName(workspaceRoot) ?? workspaceRoot;
    }

    if (!Directory.Exists(workspaceRoot))
    {
        throw new ArgumentException($"Workspace directory does not exist: {workspaceRoot}");
    }

    if (Path.IsPathRooted(outputSubdir))
    {
        throw new ArgumentException("output_subdir must be relative to workspace_path.");
    }

    var outputDir = Path.GetFullPath(Path.Combine(workspaceRoot, outputSubdir));
    if (!outputDir.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException("output_subdir points outside of workspace_path.");
    }

    Directory.CreateDirectory(outputDir);

    var safeBaseName = string.IsNullOrWhiteSpace(fileName)
        ? $"webcam-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}"
        : MakeSafeFileName(fileName);
    var outputPath = Path.Combine(outputDir, $"{safeBaseName}.{imageFormat}");

    using var capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.ANY);
    if (!capture.IsOpened())
    {
        throw new ArgumentException($"Camera {cameraIndex} is not available.");
    }

    if (requestedWidth > 0)
    {
        capture.Set(VideoCaptureProperties.FrameWidth, requestedWidth);
    }

    if (requestedHeight > 0)
    {
        capture.Set(VideoCaptureProperties.FrameHeight, requestedHeight);
    }

    using var frame = new Mat();

    for (var i = 0; i < warmupFrames; i++)
    {
        capture.Read(frame);
        Thread.Sleep(40);
    }

    if (!capture.Read(frame) || frame.Empty())
    {
        throw new ArgumentException("Failed to read frame from webcam.");
    }

    var writeOk = imageFormat switch
    {
        "jpg" => Cv2.ImWrite(outputPath, frame, [new ImageEncodingParam(ImwriteFlags.JpegQuality, jpegQuality)]),
        "png" => Cv2.ImWrite(outputPath, frame),
        _ => false
    };

    if (!writeOk)
    {
        throw new ArgumentException("Failed to save captured frame.");
    }

    var result = new
    {
        success = true,
        file_path = outputPath,
        width = frame.Width,
        height = frame.Height,
        camera_index = cameraIndex,
        image_format = imageFormat,
        captured_at_utc = DateTime.UtcNow.ToString("O")
    };

    return JsonSerializer.Serialize(result);
}

static string HandleCaptureWebcamBurst(IReadOnlyDictionary<string, JsonElement> args)
{
    var workspacePath = GetRequiredString(args, "workspace_path");
    var cameraIndex = GetOptionalInt(args, "camera_index", 0);
    var warmupFrames = Math.Clamp(GetOptionalInt(args, "warmup_frames", DefaultWarmupFrames), 0, 50);
    var requestedWidth = GetOptionalInt(args, "width", 0);
    var requestedHeight = GetOptionalInt(args, "height", 0);
    var durationSec = Math.Clamp(GetOptionalInt(args, "duration_sec", DefaultBurstDurationSec), 1, 60);
    var targetFps = Math.Clamp(GetOptionalInt(args, "target_fps", DefaultBurstTargetFps), 1, 240);
    var jpegQuality = Math.Clamp(GetOptionalInt(args, "jpeg_quality", DefaultJpegQuality), 1, 100);
    var imageFormat = NormalizeImageFormat(GetOptionalString(args, "image_format") ?? "jpg");
    var outputSubdir = GetOptionalString(args, "output_subdir") ?? DefaultOutputSubdir;
    var burstName = GetOptionalString(args, "burst_name");
    var saveVideo = GetOptionalBool(args, "save_video", false);
    var videoFps = Math.Clamp(GetOptionalInt(args, "video_fps", DefaultBurstVideoFps), 1, 240);
    var videoFormat = NormalizeVideoFormat(GetOptionalString(args, "video_format") ?? "mp4");

    var workspaceRoot = Path.GetFullPath(workspacePath.Trim());
    if (File.Exists(workspaceRoot))
    {
        workspaceRoot = Path.GetDirectoryName(workspaceRoot) ?? workspaceRoot;
    }

    if (!Directory.Exists(workspaceRoot))
    {
        throw new ArgumentException($"Workspace directory does not exist: {workspaceRoot}");
    }

    if (Path.IsPathRooted(outputSubdir))
    {
        throw new ArgumentException("output_subdir must be relative to workspace_path.");
    }

    var outputDir = Path.GetFullPath(Path.Combine(workspaceRoot, outputSubdir));
    if (!outputDir.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException("output_subdir points outside of workspace_path.");
    }

    Directory.CreateDirectory(outputDir);

    var safeBurstName = string.IsNullOrWhiteSpace(burstName)
        ? $"burst-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}"
        : MakeSafeFileName(burstName);
    var burstDir = Path.Combine(outputDir, safeBurstName);
    Directory.CreateDirectory(burstDir);

    using var capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.ANY);
    if (!capture.IsOpened())
    {
        throw new ArgumentException($"Camera {cameraIndex} is not available.");
    }

    if (requestedWidth > 0)
    {
        capture.Set(VideoCaptureProperties.FrameWidth, requestedWidth);
    }

    if (requestedHeight > 0)
    {
        capture.Set(VideoCaptureProperties.FrameHeight, requestedHeight);
    }

    using var frame = new Mat();

    for (var i = 0; i < warmupFrames; i++)
    {
        capture.Read(frame);
        Thread.Sleep(20);
    }

    var intervalMs = 1000.0 / targetFps;
    var durationMs = durationSec * 1000.0;
    var stopwatch = Stopwatch.StartNew();
    var nextCaptureAt = 0.0;
    var frameCount = 0;
    var firstFrameAtMs = -1.0;
    var lastFrameAtMs = -1.0;
    string? videoPath = null;
    VideoWriter? writer = null;

    try
    {
        while (stopwatch.Elapsed.TotalMilliseconds <= durationMs)
        {
            var elapsed = stopwatch.Elapsed.TotalMilliseconds;
            if (elapsed < nextCaptureAt)
            {
                var wait = Math.Max(1, (int)(nextCaptureAt - elapsed));
                Thread.Sleep(Math.Min(wait, 5));
                continue;
            }

            if (!capture.Read(frame) || frame.Empty())
            {
                nextCaptureAt += intervalMs;
                continue;
            }

            frameCount++;
            firstFrameAtMs = firstFrameAtMs < 0 ? elapsed : firstFrameAtMs;
            lastFrameAtMs = elapsed;

            var framePath = Path.Combine(burstDir, $"{frameCount:D5}.{imageFormat}");
            var saved = imageFormat switch
            {
                "jpg" => Cv2.ImWrite(framePath, frame, [new ImageEncodingParam(ImwriteFlags.JpegQuality, jpegQuality)]),
                "png" => Cv2.ImWrite(framePath, frame),
                _ => false
            };

            if (!saved)
            {
                throw new ArgumentException($"Failed to save burst frame: {framePath}");
            }

            if (saveVideo)
            {
                if (writer is null)
                {
                    videoPath = Path.Combine(burstDir, $"{safeBurstName}.{videoFormat}");
                    var fourcc = videoFormat == "avi"
                        ? VideoWriter.FourCC('M', 'J', 'P', 'G')
                        : VideoWriter.FourCC('m', 'p', '4', 'v');

                    writer = new VideoWriter(videoPath, fourcc, videoFps, new Size(frame.Width, frame.Height));
                    if (!writer.IsOpened())
                    {
                        writer.Dispose();
                        writer = null;
                        throw new ArgumentException("Failed to initialize video writer. Try video_format='avi' or another resolution.");
                    }
                }

                writer.Write(frame);
            }

            nextCaptureAt += intervalMs;
        }
    }
    finally
    {
        writer?.Release();
        writer?.Dispose();
    }

    if (frameCount == 0)
    {
        throw new ArgumentException("No frames were captured from webcam.");
    }

    var actualDurationMs = Math.Max(1.0, lastFrameAtMs - firstFrameAtMs);
    var actualFps = frameCount == 1 ? 1.0 : ((frameCount - 1) * 1000.0 / actualDurationMs);

    var result = new
    {
        success = true,
        burst_dir = burstDir,
        frames_captured = frameCount,
        target_fps = targetFps,
        actual_fps = Math.Round(actualFps, 2),
        duration_sec = durationSec,
        frame_width = frame.Width,
        frame_height = frame.Height,
        camera_index = cameraIndex,
        image_format = imageFormat,
        video_path = videoPath,
        captured_at_utc = DateTime.UtcNow.ToString("O")
    };

    return JsonSerializer.Serialize(result);
}

static string HandleCaptureScreenBurst(IReadOnlyDictionary<string, JsonElement> args)
{
    var workspacePath = GetRequiredString(args, "workspace_path");
    var durationSec = Math.Clamp(GetOptionalInt(args, "duration_sec", DefaultBurstDurationSec), 1, 60);
    var targetFps = Math.Clamp(GetOptionalInt(args, "target_fps", DefaultBurstTargetFps), 1, 240);
    var jpegQuality = Math.Clamp(GetOptionalInt(args, "jpeg_quality", DefaultJpegQuality), 1, 100);
    var imageFormat = NormalizeImageFormat(GetOptionalString(args, "image_format") ?? "jpg");
    var outputSubdir = GetOptionalString(args, "output_subdir") ?? DefaultScreenOutputSubdir;
    var burstName = GetOptionalString(args, "burst_name");
    var saveVideo = GetOptionalBool(args, "save_video", false);
    var videoFps = Math.Clamp(GetOptionalInt(args, "video_fps", DefaultBurstVideoFps), 1, 240);
    var videoFormat = NormalizeVideoFormat(GetOptionalString(args, "video_format") ?? "mp4");

    var workspaceRoot = Path.GetFullPath(workspacePath.Trim());
    if (File.Exists(workspaceRoot))
    {
        workspaceRoot = Path.GetDirectoryName(workspaceRoot) ?? workspaceRoot;
    }

    if (!Directory.Exists(workspaceRoot))
    {
        throw new ArgumentException($"Workspace directory does not exist: {workspaceRoot}");
    }

    if (Path.IsPathRooted(outputSubdir))
    {
        throw new ArgumentException("output_subdir must be relative to workspace_path.");
    }

    var outputDir = Path.GetFullPath(Path.Combine(workspaceRoot, outputSubdir));
    if (!outputDir.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException("output_subdir points outside of workspace_path.");
    }

    Directory.CreateDirectory(outputDir);

    var safeBurstName = string.IsNullOrWhiteSpace(burstName)
        ? $"screen-burst-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}"
        : MakeSafeFileName(burstName);
    var burstDir = Path.Combine(outputDir, safeBurstName);
    Directory.CreateDirectory(burstDir);

    var monitorNumber = GetOptionalMonitorNumber(args, "monitor");
    var hasExplicitRegion = args.ContainsKey("x") || args.ContainsKey("y") || args.ContainsKey("width") || args.ContainsKey("height");
    var region = ResolveCaptureRegion(monitorNumber, hasExplicitRegion);

    var captureX = GetOptionalInt(args, "x", region.X);
    var captureY = GetOptionalInt(args, "y", region.Y);
    var captureWidth = Math.Max(1, GetOptionalInt(args, "width", region.Width));
    var captureHeight = Math.Max(1, GetOptionalInt(args, "height", region.Height));

    var intervalMs = 1000.0 / targetFps;
    var durationMs = durationSec * 1000.0;
    var stopwatch = Stopwatch.StartNew();
    var nextCaptureAt = 0.0;
    var frameCount = 0;
    var firstFrameAtMs = -1.0;
    var lastFrameAtMs = -1.0;
    string? videoPath = null;
    VideoWriter? writer = null;

    try
    {
        while (stopwatch.Elapsed.TotalMilliseconds <= durationMs)
        {
            var elapsed = stopwatch.Elapsed.TotalMilliseconds;
            if (elapsed < nextCaptureAt)
            {
                var wait = Math.Max(1, (int)(nextCaptureAt - elapsed));
                Thread.Sleep(Math.Min(wait, 5));
                continue;
            }

            using var bitmap = new System.Drawing.Bitmap(
                captureWidth,
                captureHeight,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    captureX,
                    captureY,
                    0,
                    0,
                    new System.Drawing.Size(captureWidth, captureHeight),
                    System.Drawing.CopyPixelOperation.SourceCopy);
            }

            frameCount++;
            firstFrameAtMs = firstFrameAtMs < 0 ? elapsed : firstFrameAtMs;
            lastFrameAtMs = elapsed;

            var framePath = Path.Combine(burstDir, $"{frameCount:D5}.{imageFormat}");
            SaveBitmapToPath(bitmap, framePath, imageFormat, jpegQuality);

            if (saveVideo)
            {
                using var frameMat = Cv2.ImRead(framePath, ImreadModes.Color);
                if (frameMat.Empty())
                {
                    throw new ArgumentException($"Failed to read saved frame for video: {framePath}");
                }
                if (writer is null)
                {
                    videoPath = Path.Combine(burstDir, $"{safeBurstName}.{videoFormat}");
                    var fourcc = videoFormat == "avi"
                        ? VideoWriter.FourCC('M', 'J', 'P', 'G')
                        : VideoWriter.FourCC('m', 'p', '4', 'v');

                    writer = new VideoWriter(videoPath, fourcc, videoFps, new OpenCvSharp.Size(frameMat.Width, frameMat.Height));
                    if (!writer.IsOpened())
                    {
                        writer.Dispose();
                        writer = null;
                        throw new ArgumentException("Failed to initialize video writer. Try video_format='avi' or another screen size.");
                    }
                }

                writer.Write(frameMat);
            }

            nextCaptureAt += intervalMs;
        }
    }
    finally
    {
        writer?.Release();
        writer?.Dispose();
    }

    if (frameCount == 0)
    {
        throw new ArgumentException("No frames were captured from screen.");
    }

    var actualDurationMs = Math.Max(1.0, lastFrameAtMs - firstFrameAtMs);
    var actualFps = frameCount == 1 ? 1.0 : ((frameCount - 1) * 1000.0 / actualDurationMs);

    var result = new
    {
        success = true,
        burst_dir = burstDir,
        frames_captured = frameCount,
        target_fps = targetFps,
        actual_fps = Math.Round(actualFps, 2),
        duration_sec = durationSec,
        capture_region = new { x = captureX, y = captureY, width = captureWidth, height = captureHeight },
        image_format = imageFormat,
        video_path = videoPath,
        captured_at_utc = DateTime.UtcNow.ToString("O")
    };

    return JsonSerializer.Serialize(result);
}
static string HandleCaptureAudioBurst(IReadOnlyDictionary<string, JsonElement> args)
{
    var workspacePath = GetRequiredString(args, "workspace_path");
    var durationSec = Math.Clamp(GetOptionalInt(args, "duration_sec", DefaultAudioDurationSec), 1, 300);
    var sampleRate = Math.Clamp(GetOptionalInt(args, "sample_rate", DefaultAudioSampleRate), 8000, 96000);
    var channels = Math.Clamp(GetOptionalInt(args, "channels", DefaultAudioChannels), 1, 2);
    var deviceNumber = Math.Clamp(GetOptionalInt(args, "device_number", 0), 0, 32);
    var outputSubdir = GetOptionalString(args, "output_subdir") ?? DefaultAudioOutputSubdir;
    var fileName = GetOptionalString(args, "file_name");

    var workspaceRoot = Path.GetFullPath(workspacePath.Trim());
    if (File.Exists(workspaceRoot))
    {
        workspaceRoot = Path.GetDirectoryName(workspaceRoot) ?? workspaceRoot;
    }

    if (!Directory.Exists(workspaceRoot))
    {
        throw new ArgumentException($"Workspace directory does not exist: {workspaceRoot}");
    }

    if (Path.IsPathRooted(outputSubdir))
    {
        throw new ArgumentException("output_subdir must be relative to workspace_path.");
    }

    var outputDir = Path.GetFullPath(Path.Combine(workspaceRoot, outputSubdir));
    if (!outputDir.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException("output_subdir points outside of workspace_path.");
    }

    Directory.CreateDirectory(outputDir);

    var safeBaseName = string.IsNullOrWhiteSpace(fileName)
        ? $"audio-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}"
        : MakeSafeFileName(fileName);
    var outputPath = Path.Combine(outputDir, $"{safeBaseName}.wav");

    if (WaveInEvent.DeviceCount == 0)
    {
        throw new ArgumentException("No recording devices were found.");
    }

    if (deviceNumber >= WaveInEvent.DeviceCount)
    {
        throw new ArgumentException($"device_number {deviceNumber} is out of range. Available devices: {WaveInEvent.DeviceCount}.");
    }

    using var waveIn = new WaveInEvent
    {
        DeviceNumber = deviceNumber,
        WaveFormat = new WaveFormat(sampleRate, 16, channels),
        BufferMilliseconds = 50
    };

    using var writer = new WaveFileWriter(outputPath, waveIn.WaveFormat);
    using var completed = new ManualResetEventSlim(false);

    Exception? recordingError = null;

    waveIn.DataAvailable += (_, eventArgs) =>
    {
        writer.Write(eventArgs.Buffer, 0, eventArgs.BytesRecorded);
        writer.Flush();
    };

    waveIn.RecordingStopped += (_, eventArgs) =>
    {
        recordingError = eventArgs.Exception;
        completed.Set();
    };

    waveIn.StartRecording();
    Thread.Sleep(durationSec * 1000);
    waveIn.StopRecording();

    if (!completed.Wait(TimeSpan.FromSeconds(5)))
    {
        throw new ArgumentException("Timeout while finalizing audio recording.");
    }

    if (recordingError is not null)
    {
        throw new ArgumentException("Audio capture failed: " + recordingError.Message);
    }

    var fileInfo = new FileInfo(outputPath);
    if (!fileInfo.Exists || fileInfo.Length <= 44)
    {
        throw new ArgumentException("Recorded file is empty.");
    }

    var result = new
    {
        success = true,
        file_path = outputPath,
        duration_sec = durationSec,
        sample_rate = sampleRate,
        channels,
        device_number = deviceNumber,
        bytes = fileInfo.Length,
        captured_at_utc = DateTime.UtcNow.ToString("O")
    };

    return JsonSerializer.Serialize(result);
}

static string HandleCaptureAvBurst(IReadOnlyDictionary<string, JsonElement> args)
{
    var workspacePath = GetRequiredString(args, "workspace_path");
    var durationSec = Math.Clamp(GetOptionalInt(args, "duration_sec", DefaultAudioDurationSec), 1, 300);
    var targetFps = Math.Clamp(GetOptionalInt(args, "target_fps", DefaultBurstTargetFps), 1, 120);
    var cameraIndex = GetOptionalInt(args, "camera_index", 0);
    var audioDeviceNumber = GetOptionalInt(args, "audio_device_number", 0);
    var requestedWidth = GetOptionalInt(args, "width", 0);
    var requestedHeight = GetOptionalInt(args, "height", 0);
    var audioSampleRate = Math.Clamp(GetOptionalInt(args, "audio_sample_rate", DefaultAudioSampleRate), 8000, 96000);
    var audioChannels = Math.Clamp(GetOptionalInt(args, "audio_channels", DefaultAudioChannels), 1, 2);
    var warmupFrames = Math.Clamp(GetOptionalInt(args, "warmup_frames", DefaultWarmupFrames), 0, 50);
    var imageFormat = NormalizeImageFormat(GetOptionalString(args, "image_format") ?? "jpg");
    var jpegQuality = Math.Clamp(GetOptionalInt(args, "jpeg_quality", DefaultJpegQuality), 1, 100);
    var outputSubdir = GetOptionalString(args, "output_subdir") ?? DefaultAvOutputSubdir;
    var sessionName = GetOptionalString(args, "session_name");
    var saveVideo = GetOptionalBool(args, "save_video", true);
    var videoFps = Math.Clamp(GetOptionalInt(args, "video_fps", DefaultBurstVideoFps), 1, 120);

    var workspaceRoot = Path.GetFullPath(workspacePath.Trim());
    if (File.Exists(workspaceRoot))
    {
        workspaceRoot = Path.GetDirectoryName(workspaceRoot) ?? workspaceRoot;
    }

    if (!Directory.Exists(workspaceRoot))
    {
        throw new ArgumentException($"Workspace directory does not exist: {workspaceRoot}");
    }

    if (Path.IsPathRooted(outputSubdir))
    {
        throw new ArgumentException("output_subdir must be relative to workspace_path.");
    }

    var outputDir = Path.GetFullPath(Path.Combine(workspaceRoot, outputSubdir));
    if (!outputDir.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException("output_subdir points outside of workspace_path.");
    }

    Directory.CreateDirectory(outputDir);

    var safeSessionName = string.IsNullOrWhiteSpace(sessionName)
        ? $"av-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}"
        : MakeSafeFileName(sessionName);
    var sessionDir = Path.Combine(outputDir, safeSessionName);
    var framesDir = Path.Combine(sessionDir, "frames");
    Directory.CreateDirectory(framesDir);

    var audioPath = Path.Combine(sessionDir, "audio.wav");
    var metadataPath = Path.Combine(sessionDir, "metadata.json");
    var videoPath = saveVideo ? Path.Combine(sessionDir, "video.mp4") : null;

    if (WaveInEvent.DeviceCount == 0)
    {
        throw new ArgumentException("No recording devices were found.");
    }

    if (audioDeviceNumber < 0 || audioDeviceNumber >= WaveInEvent.DeviceCount)
    {
        throw new ArgumentException($"audio_device_number {audioDeviceNumber} is out of range. Available devices: {WaveInEvent.DeviceCount}.");
    }

    using var capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.ANY);
    if (!capture.IsOpened())
    {
        throw new ArgumentException($"Camera {cameraIndex} is not available.");
    }

    if (requestedWidth > 0)
    {
        capture.Set(VideoCaptureProperties.FrameWidth, requestedWidth);
    }

    if (requestedHeight > 0)
    {
        capture.Set(VideoCaptureProperties.FrameHeight, requestedHeight);
    }

    using var waveIn = new WaveInEvent
    {
        DeviceNumber = audioDeviceNumber,
        WaveFormat = new WaveFormat(audioSampleRate, 16, audioChannels),
        BufferMilliseconds = 50
    };
    using var audioWriter = new WaveFileWriter(audioPath, waveIn.WaveFormat);
    using var audioCompleted = new ManualResetEventSlim(false);

    Exception? audioError = null;
    var audioLock = new object();

    waveIn.DataAvailable += (_, eventArgs) =>
    {
        lock (audioLock)
        {
            audioWriter.Write(eventArgs.Buffer, 0, eventArgs.BytesRecorded);
            audioWriter.Flush();
        }
    };

    waveIn.RecordingStopped += (_, eventArgs) =>
    {
        audioError = eventArgs.Exception;
        audioCompleted.Set();
    };

    using var frame = new Mat();
    VideoWriter? videoWriter = null;
    var frameTimestampsMs = new List<int>();
    var frameCount = 0;

    var startUtc = DateTime.UtcNow;
    var durationMs = durationSec * 1000.0;
    var intervalMs = 1000.0 / targetFps;
    var stopwatch = Stopwatch.StartNew();
    var nextCaptureAt = 0.0;

    try
    {
        for (var i = 0; i < warmupFrames; i++)
        {
            capture.Read(frame);
            Thread.Sleep(15);
        }

        waveIn.StartRecording();

        while (stopwatch.Elapsed.TotalMilliseconds <= durationMs)
        {
            var elapsed = stopwatch.Elapsed.TotalMilliseconds;
            if (elapsed < nextCaptureAt)
            {
                var waitMs = Math.Max(1, (int)(nextCaptureAt - elapsed));
                Thread.Sleep(Math.Min(waitMs, 5));
                continue;
            }

            if (!capture.Read(frame) || frame.Empty())
            {
                nextCaptureAt += intervalMs;
                continue;
            }

            frameCount++;
            var frameFileName = $"{frameCount:D5}.{imageFormat}";
            var framePath = Path.Combine(framesDir, frameFileName);
            var saved = imageFormat switch
            {
                "jpg" => Cv2.ImWrite(framePath, frame, [new ImageEncodingParam(ImwriteFlags.JpegQuality, jpegQuality)]),
                "png" => Cv2.ImWrite(framePath, frame),
                _ => false
            };

            if (!saved)
            {
                throw new ArgumentException($"Failed to save video frame: {framePath}");
            }

            if (saveVideo)
            {
                if (videoWriter is null)
                {
                    videoWriter = new VideoWriter(
                        videoPath!,
                        VideoWriter.FourCC('m', 'p', '4', 'v'),
                        videoFps,
                        new Size(frame.Width, frame.Height));
                    if (!videoWriter.IsOpened())
                    {
                        videoWriter.Dispose();
                        videoWriter = null;
                        throw new ArgumentException("Failed to initialize MP4 writer for A/V capture.");
                    }
                }

                videoWriter.Write(frame);
            }

            frameTimestampsMs.Add((int)Math.Round(elapsed));
            nextCaptureAt += intervalMs;
        }
    }
    finally
    {
        waveIn.StopRecording();
        videoWriter?.Release();
        videoWriter?.Dispose();
    }

    if (!audioCompleted.Wait(TimeSpan.FromSeconds(8)))
    {
        throw new ArgumentException("Timeout while finalizing audio recording.");
    }

    if (audioError is not null)
    {
        throw new ArgumentException("Audio capture failed: " + audioError.Message);
    }

    var audioInfo = new FileInfo(audioPath);
    if (!audioInfo.Exists || audioInfo.Length <= 44)
    {
        throw new ArgumentException("A/V capture produced empty audio track.");
    }

    if (frameCount == 0)
    {
        throw new ArgumentException("A/V capture produced no video frames.");
    }

    var actualDurationMs = stopwatch.Elapsed.TotalMilliseconds;
    var actualFps = actualDurationMs > 0 ? frameCount * 1000.0 / actualDurationMs : 0;
    var metadata = new
    {
        session_dir = sessionDir,
        start_utc = startUtc.ToString("O"),
        requested_duration_sec = durationSec,
        actual_duration_ms = (int)Math.Round(actualDurationMs),
        camera_index = cameraIndex,
        audio_device_number = audioDeviceNumber,
        frame_width = frame.Width,
        frame_height = frame.Height,
        frame_format = imageFormat,
        frame_count = frameCount,
        frame_timestamps_ms = frameTimestampsMs,
        target_fps = targetFps,
        actual_fps = Math.Round(actualFps, 2),
        audio_path = audioPath,
        video_path = videoPath
    };
    File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

    var result = new
    {
        success = true,
        session_dir = sessionDir,
        frames_dir = framesDir,
        audio_path = audioPath,
        video_path = videoPath,
        metadata_path = metadataPath,
        frame_count = frameCount,
        actual_fps = Math.Round(actualFps, 2),
        duration_sec = durationSec,
        captured_at_utc = DateTime.UtcNow.ToString("O")
    };

    return JsonSerializer.Serialize(result);
}

static string HandleCaptureScreenAvBurst(IReadOnlyDictionary<string, JsonElement> args)
{
    var workspacePath = GetRequiredString(args, "workspace_path");
    var durationSec = Math.Clamp(GetOptionalInt(args, "duration_sec", DefaultAudioDurationSec), 1, 300);
    var targetFps = Math.Clamp(GetOptionalInt(args, "target_fps", DefaultBurstTargetFps), 1, 120);
    var audioDeviceNumber = GetOptionalInt(args, "audio_device_number", 0);
    var monitorNumber = GetOptionalMonitorNumber(args, "monitor");
    var hasExplicitRegion = args.ContainsKey("x") || args.ContainsKey("y") || args.ContainsKey("width") || args.ContainsKey("height");
    var region = ResolveCaptureRegion(monitorNumber, hasExplicitRegion);
    var captureX = GetOptionalInt(args, "x", region.X);
    var captureY = GetOptionalInt(args, "y", region.Y);
    var captureWidth = Math.Max(1, GetOptionalInt(args, "width", region.Width));
    var captureHeight = Math.Max(1, GetOptionalInt(args, "height", region.Height));
    var audioSampleRate = Math.Clamp(GetOptionalInt(args, "audio_sample_rate", DefaultAudioSampleRate), 8000, 96000);
    var audioChannels = Math.Clamp(GetOptionalInt(args, "audio_channels", DefaultAudioChannels), 1, 2);
    var imageFormat = NormalizeImageFormat(GetOptionalString(args, "image_format") ?? "jpg");
    var jpegQuality = Math.Clamp(GetOptionalInt(args, "jpeg_quality", DefaultJpegQuality), 1, 100);
    var outputSubdir = GetOptionalString(args, "output_subdir") ?? DefaultAvOutputSubdir;
    var sessionName = GetOptionalString(args, "session_name");
    var saveVideo = GetOptionalBool(args, "save_video", true);
    var videoFps = Math.Clamp(GetOptionalInt(args, "video_fps", DefaultBurstVideoFps), 1, 120);

    var workspaceRoot = Path.GetFullPath(workspacePath.Trim());
    if (File.Exists(workspaceRoot))
    {
        workspaceRoot = Path.GetDirectoryName(workspaceRoot) ?? workspaceRoot;
    }

    if (!Directory.Exists(workspaceRoot))
    {
        throw new ArgumentException($"Workspace directory does not exist: {workspaceRoot}");
    }

    if (Path.IsPathRooted(outputSubdir))
    {
        throw new ArgumentException("output_subdir must be relative to workspace_path.");
    }

    var outputDir = Path.GetFullPath(Path.Combine(workspaceRoot, outputSubdir));
    if (!outputDir.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
    {
        throw new ArgumentException("output_subdir points outside of workspace_path.");
    }

    Directory.CreateDirectory(outputDir);

    var safeSessionName = string.IsNullOrWhiteSpace(sessionName)
        ? $"screen-av-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}"
        : MakeSafeFileName(sessionName);
    var sessionDir = Path.Combine(outputDir, safeSessionName);
    var framesDir = Path.Combine(sessionDir, "frames");
    Directory.CreateDirectory(framesDir);

    var audioPath = Path.Combine(sessionDir, "audio.wav");
    var metadataPath = Path.Combine(sessionDir, "metadata.json");
    var videoPath = saveVideo ? Path.Combine(sessionDir, "video.mp4") : null;

    if (WaveInEvent.DeviceCount == 0)
    {
        throw new ArgumentException("No recording devices were found.");
    }

    if (audioDeviceNumber < 0 || audioDeviceNumber >= WaveInEvent.DeviceCount)
    {
        throw new ArgumentException($"audio_device_number {audioDeviceNumber} is out of range. Available devices: {WaveInEvent.DeviceCount}.");
    }

    using var waveIn = new WaveInEvent
    {
        DeviceNumber = audioDeviceNumber,
        WaveFormat = new WaveFormat(audioSampleRate, 16, audioChannels),
        BufferMilliseconds = 50
    };
    using var audioWriter = new WaveFileWriter(audioPath, waveIn.WaveFormat);
    using var audioCompleted = new ManualResetEventSlim(false);

    Exception? audioError = null;
    var audioLock = new object();
    waveIn.DataAvailable += (_, eventArgs) =>
    {
        lock (audioLock)
        {
            audioWriter.Write(eventArgs.Buffer, 0, eventArgs.BytesRecorded);
            audioWriter.Flush();
        }
    };
    waveIn.RecordingStopped += (_, eventArgs) =>
    {
        audioError = eventArgs.Exception;
        audioCompleted.Set();
    };

    VideoWriter? videoWriter = null;
    var frameTimestampsMs = new List<int>();
    var frameCount = 0;
    var frameWidth = captureWidth;
    var frameHeight = captureHeight;

    var startUtc = DateTime.UtcNow;
    var durationMs = durationSec * 1000.0;
    var intervalMs = 1000.0 / targetFps;
    var stopwatch = Stopwatch.StartNew();
    var nextCaptureAt = 0.0;

    try
    {
        waveIn.StartRecording();
        while (stopwatch.Elapsed.TotalMilliseconds <= durationMs)
        {
            var elapsed = stopwatch.Elapsed.TotalMilliseconds;
            if (elapsed < nextCaptureAt)
            {
                var waitMs = Math.Max(1, (int)(nextCaptureAt - elapsed));
                Thread.Sleep(Math.Min(waitMs, 5));
                continue;
            }

            using var bitmap = new System.Drawing.Bitmap(
                captureWidth,
                captureHeight,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(
                    captureX,
                    captureY,
                    0,
                    0,
                    new System.Drawing.Size(captureWidth, captureHeight),
                    System.Drawing.CopyPixelOperation.SourceCopy);
            }

            frameCount++;
            var frameFileName = $"{frameCount:D5}.{imageFormat}";
            var framePath = Path.Combine(framesDir, frameFileName);
            SaveBitmapToPath(bitmap, framePath, imageFormat, jpegQuality);

            if (saveVideo)
            {
                using var frameMat = Cv2.ImRead(framePath, ImreadModes.Color);
                if (frameMat.Empty())
                {
                    throw new ArgumentException($"Failed to read saved frame for video: {framePath}");
                }

                frameWidth = frameMat.Width;
                frameHeight = frameMat.Height;

                if (videoWriter is null)
                {
                    videoWriter = new VideoWriter(
                        videoPath!,
                        VideoWriter.FourCC('m', 'p', '4', 'v'),
                        videoFps,
                        new Size(frameMat.Width, frameMat.Height));
                    if (!videoWriter.IsOpened())
                    {
                        videoWriter.Dispose();
                        videoWriter = null;
                        throw new ArgumentException("Failed to initialize MP4 writer for screen A/V capture.");
                    }
                }

                videoWriter.Write(frameMat);
            }

            frameTimestampsMs.Add((int)Math.Round(elapsed));
            nextCaptureAt += intervalMs;
        }
    }
    finally
    {
        waveIn.StopRecording();
        videoWriter?.Release();
        videoWriter?.Dispose();
    }

    if (!audioCompleted.Wait(TimeSpan.FromSeconds(8)))
    {
        throw new ArgumentException("Timeout while finalizing audio recording.");
    }

    if (audioError is not null)
    {
        throw new ArgumentException("Audio capture failed: " + audioError.Message);
    }

    var audioInfo = new FileInfo(audioPath);
    if (!audioInfo.Exists || audioInfo.Length <= 44)
    {
        throw new ArgumentException("Screen A/V capture produced empty audio track.");
    }

    if (frameCount == 0)
    {
        throw new ArgumentException("Screen A/V capture produced no video frames.");
    }

    var actualDurationMs = stopwatch.Elapsed.TotalMilliseconds;
    var actualFps = actualDurationMs > 0 ? frameCount * 1000.0 / actualDurationMs : 0;
    var metadata = new
    {
        session_dir = sessionDir,
        source = "screen",
        start_utc = startUtc.ToString("O"),
        requested_duration_sec = durationSec,
        actual_duration_ms = (int)Math.Round(actualDurationMs),
        capture_region = new { x = captureX, y = captureY, width = captureWidth, height = captureHeight },
        audio_device_number = audioDeviceNumber,
        frame_width = frameWidth,
        frame_height = frameHeight,
        frame_format = imageFormat,
        frame_count = frameCount,
        frame_timestamps_ms = frameTimestampsMs,
        target_fps = targetFps,
        actual_fps = Math.Round(actualFps, 2),
        audio_path = audioPath,
        video_path = videoPath
    };
    File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));

    var result = new
    {
        success = true,
        session_dir = sessionDir,
        frames_dir = framesDir,
        audio_path = audioPath,
        video_path = videoPath,
        metadata_path = metadataPath,
        frame_count = frameCount,
        actual_fps = Math.Round(actualFps, 2),
        duration_sec = durationSec,
        capture_region = new { x = captureX, y = captureY, width = captureWidth, height = captureHeight },
        captured_at_utc = DateTime.UtcNow.ToString("O")
    };

    return JsonSerializer.Serialize(result);
}

static (int X, int Y, int Width, int Height) ResolveCaptureRegion(int? monitorNumber, bool hasExplicitRegion)
{
    if (!hasExplicitRegion && monitorNumber.HasValue)
    {
        var monitor = GetMonitorRegion(monitorNumber.Value);
        return (monitor.Left, monitor.Top, monitor.Width, monitor.Height);
    }

    return (
        GetSystemMetrics(SmXVirtualScreen),
        GetSystemMetrics(SmYVirtualScreen),
        GetSystemMetrics(SmCxVirtualScreen),
        GetSystemMetrics(SmCyVirtualScreen)
    );
}

static WinRect GetMonitorRegion(int monitorNumber)
{
    var monitors = EnumerateMonitors();
    if (monitors.Count == 0)
    {
        throw new ArgumentException("No monitors were detected.");
    }

    if (monitorNumber < 1 || monitorNumber > monitors.Count)
    {
        throw new ArgumentException($"monitor {monitorNumber} is out of range. Available monitors: 1..{monitors.Count}.");
    }

    return monitors[monitorNumber - 1];
}

static List<WinRect> EnumerateMonitors()
{
    var monitors = new List<WinRect>();
    MonitorEnumProc callback = (IntPtr _, IntPtr _, ref WinRect rect, IntPtr _) =>
    {
        monitors.Add(rect);
        return true;
    };

    if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero))
    {
        throw new ArgumentException("Failed to enumerate display monitors.");
    }

    return monitors
        .OrderBy(m => m.Left)
        .ThenBy(m => m.Top)
        .ToList();
}

static void SaveBitmapToPath(System.Drawing.Bitmap bitmap, string outputPath, string imageFormat, int jpegQuality)
{
    if (imageFormat == "png")
    {
        bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
        return;
    }

    var codec = GetImageCodec("image/jpeg");
    if (codec is null)
    {
        bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Jpeg);
        return;
    }

    using var parameters = new System.Drawing.Imaging.EncoderParameters(1);
    parameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(
        System.Drawing.Imaging.Encoder.Quality,
        (long)Math.Clamp(jpegQuality, 1, 100));
    bitmap.Save(outputPath, codec, parameters);
}

static System.Drawing.Imaging.ImageCodecInfo? GetImageCodec(string mimeType) =>
    System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders()
        .FirstOrDefault(codec => string.Equals(codec.MimeType, mimeType, StringComparison.OrdinalIgnoreCase));

var options = new McpServerOptions
{
    ServerInfo = new Implementation { Name = "WebcamCaptureMcp", Version = "0.1.0" },
    ProtocolVersion = "2024-11-05",
    Capabilities = new ServerCapabilities { Tools = new ToolsCapability { ListChanged = false } },
    Handlers = new McpServerHandlers
    {
        ListToolsHandler = (_, _) => ValueTask.FromResult(new ListToolsResult { Tools = toolsList }),

        CallToolHandler = (request, _) =>
        {
            var name = request.Params?.Name ?? "";
            var args = request.Params?.Arguments is IReadOnlyDictionary<string, JsonElement> a
                ? a
                : FrozenDictionary<string, JsonElement>.Empty;

            try
            {
                var text = name switch
                {
                    "capture_webcam_frame" => HandleCaptureWebcamFrame(args),
                    "capture_webcam_burst" => HandleCaptureWebcamBurst(args),
                    "capture_screen_burst" => HandleCaptureScreenBurst(args),
                    "capture_audio_burst" => HandleCaptureAudioBurst(args),
                    "capture_av_burst" => HandleCaptureAvBurst(args),
                    "capture_screen_av_burst" => HandleCaptureScreenAvBurst(args),
                    _ => throw new ArgumentException($"Unknown tool: {name}.")
                };

                return ValueTask.FromResult(new CallToolResult
                {
                    Content = [new TextContentBlock { Text = text }],
                    IsError = false
                });
            }
            catch (ArgumentException ex)
            {
                return ValueTask.FromResult(new CallToolResult
                {
                    Content = [new TextContentBlock { Text = $"Error: {ex.Message}" }],
                    IsError = true
                });
            }
            catch (Exception ex)
            {
                return ValueTask.FromResult(new CallToolResult
                {
                    Content = [new TextContentBlock { Text = "Error: " + ex.Message }],
                    IsError = true
                });
            }
        }
    }
};

var transport = new StdioServerTransport("WebcamCaptureMcp");
await using var server = McpServer.Create(transport, options);
await server.RunAsync();
return 0;

delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref WinRect lprcMonitor, IntPtr dwData);

[StructLayout(LayoutKind.Sequential)]
struct WinRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}
