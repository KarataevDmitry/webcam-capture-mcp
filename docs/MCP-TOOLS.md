# Webcam Capture MCP — каталог тулов

<!-- GENERATED:ToolCatalog START -->

> Автогенерация из `ToolCatalog.Build()`. Не править этот блок вручную.
>
> Обновление: из каталога проекта выполнить `dotnet run --project tools/ExportMcpManifest -- --write`.
>
> Тексты совпадают с полем `description` у инструментов MCP; полная схема — в `inputSchema`.

### `capture_webcam_frame`

Сделать снимок с веб-камеры по явному запросу. Сохраняет изображение в workspace (по умолчанию .cascade-ide/webcam-captures) и возвращает JSON с путём и параметрами кадра.

### `capture_webcam_burst`

Сделать быструю серию кадров с веб-камеры. Сохраняет кадры в подпапку внутри workspace и (опционально) собирает короткий видеофайл.

### `capture_screen_burst`

Сделать быструю серию кадров с экрана. Сохраняет кадры в подпапку внутри workspace и (опционально) собирает короткий видеофайл.

### `capture_audio_burst`

Записать короткий аудиофрагмент с микрофона в WAV по явной команде. Сохраняет файл в workspace (по умолчанию .cascade-ide/audio-captures).

### `capture_av_burst`

Одновременная запись короткой A/V-сессии: кадры с веб-камеры + WAV с микрофона + метаданные синхронизации.

### `capture_screen_av_burst`

Одновременная запись короткой A/V-сессии: кадры с экрана + WAV с микрофона + метаданные синхронизации.

<!-- GENERATED:ToolCatalog END -->

