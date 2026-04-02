using System.Text.Json;
using ModelContextProtocol.Protocol;
using Tool = ModelContextProtocol.Protocol.Tool;

namespace WebcamCaptureMcp;

/// <summary>Каталог MCP-тулов. Согласован с <c>mcp-tools.manifest.json</c> и <c>docs/MCP-TOOLS.md</c> (генерация: <c>tools/ExportMcpManifest</c>).</summary>
internal static class ToolCatalog
{
    private static JsonElement Schema(object schema) => JsonSerializer.SerializeToElement(schema);

    internal static List<Tool> Build() =>
    [
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
    ];
}