# webcam-capture-mcp

Лицензия: **MIT** — см. [LICENSE](LICENSE).

MCP-сервер **только захвата**: веб-камера, экран, микрофон, A/V-сессии (камера+звук, экран+звук). Windows, `stdio`, self-contained `win-x64`.

## Зависимости

- Репозиторий **[webcam-mcp-shared](../webcam-mcp-shared)** в соседней папке (см. `WebcamCaptureMcp.csproj`).

## Сборка и публикация

```powershell
cd webcam-capture-mcp
dotnet build WebcamCaptureMcp.sln -c Release
dotnet publish WebcamCaptureMcp.csproj -c Release -o publish
```

Исполняемый файл: `publish\WebcamCaptureMcp.exe`.

## Тулы

| Имя | Назначение |
|-----|------------|
| `capture_webcam_frame` | один кадр с камеры |
| `capture_webcam_burst` | серия кадров (+ опционально mp4/avi) |
| `capture_screen_burst` | серия скриншотов региона |
| `capture_audio_burst` | WAV с микрофона |
| `capture_av_burst` | камера + WAV + `metadata.json` (+ mp4) |
| `capture_screen_av_burst` | экран + WAV + метаданные (+ mp4) |

Анализ, OCR, PDF, Whisper — в **webcam-analysis-mcp**.

## Cursor (`mcp.json`)

```json
"webcam-capture-mcp": {
  "command": "D:\\path\\to\\webcam-capture-mcp\\publish\\WebcamCaptureMcp.exe",
  "args": []
}
```
