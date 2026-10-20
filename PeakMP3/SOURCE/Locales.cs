using System;
using System.Collections.Generic;

namespace PeakMp3Win7
{
    internal static class L10n
    {
        private static readonly Dictionary<string, string[]> Words = new Dictionary<string, string[]>(StringComparer.Ordinal);
        private static void Add(string key, string ru, string de, string en) { Words.Add(key, new string[] { ru, de, en }); }
        static L10n()
        {
            Add("receivedYouTube","Добавлено из YouTube:","Von YouTube hinzugefügt:","Added from YouTube:");
            Add("queuePosition","Очередь {0} из {1}","Titel {0} von {1}","Queue {0} of {1}");
            Add("outputFolderPrompt","Куда сохранить MP3? По умолчанию — рабочий стол. Папка используется для всей очереди.","Wo sollen die MP3-Dateien gespeichert werden? Standard: Desktop. Der Ordner gilt für die gesamte Warteschlange.","Where should the MP3 files be saved? Default: Desktop. The folder applies to the whole queue.");
            Add("densityRangeHint","0–10: смесь · выше 10: лимитер\nОтрицательные: оригинал тише на это число dB","0–10: Mischung · über 10: Limiter\nNegativ: Original um diesen dB-Wert leiser","0–10: blend · above 10: limiter\nNegative: original attenuated by that many dB");
            Add("integerRequired","Введи целое число от −2147483648 до 2147483647","Ganze Zahl von −2147483648 bis 2147483647 eingeben","Enter an integer from −2147483648 to 2147483647");
            Add("autoSelecting","Подбираю плотность…","Dichte wird ermittelt…","Choosing density…");
            Add("autoSelected","Авто:","Auto:","Auto:");
            Add("autoNoZero","регулярный ноль не достигнут","Nullpegel nicht regelmäßig erreicht","regular zero not reached");
            Add("autoFailed","Автоподбор недоступен — выбери плотность вручную","Automatik nicht verfügbar — Dichte manuell wählen","Auto selection unavailable — choose density manually");
            Add("manualDensity","Ручная плотность","Manuelle Dichte","Manual density");
            Add("title", "PeakMP3 — плотное звучание под 0 dB — Windows 7 / 11",
                "PeakMP3 — dichter Klang bis 0 dB — Windows 7 / 11",
                "PeakMP3 — dense sound to 0 dB — Windows 7 / 11");
            Add("add", "Добавить файлы", "Dateien hinzufügen", "Add files");
            Add("remove", "Удалить из списка", "Aus Liste entfernen", "Remove selected");
            Add("clear", "Очистить список", "Liste leeren", "Clear list");
            Add("dense", "Плотное звучание под 0 dB",
                "Dichter Klang bis 0 dB",
                "Dense sound to 0 dB");
            Add("preview", "Прослушать / VU / перемотка", "Vorschau / VU / Suchen", "Preview / VU / Seek");
            Add("previewTitle", "Предпрослушивание — обработанный звук", "Vorschau — bearbeiteter Ton", "Preview — processed sound");
            Add("previewPlay", "▶ Воспроизвести", "▶ Abspielen", "▶ Play");
            Add("previewStop", "■ Стоп", "■ Stopp", "■ Stop");
            Add("pause", "Ⅱ Пауза", "Ⅱ Pause", "Ⅱ Pause");
            Add("resume", "▶ Продолжить", "▶ Fortsetzen", "▶ Resume");
            Add("paused", "(пауза)", "(Pause)", "(paused)");
            Add("previewSync", "Применяется новая плотность с текущего места…",
                "Neue Dichte wird ab der aktuellen Position angewendet…",
                "Reapplying new density at the current position…");
            Add("previewSeek", "Позиция", "Position", "Position");
            Add("previewCal", "Калибровка 0 VU (dBFS):", "0-VU-Bezug (dBFS):", "0 VU reference (dBFS):");
            Add("settings", "Настройки PeakMP3", "PeakMP3 Einstellungen", "PeakMP3 settings");
            Add("meterHint", "−9 dBFS по умолчанию. Быстрая реакция на пики — это не точная копия AIMP.",
                "−9 dBFS ist Standard. Schnelle Spitzenreaktion ist keine exakte AIMP-Kopie.",
                "Default −9 dBFS. Fast peak response is not an exact AIMP clone.");
            Add("previewHint", "Это VU-модель с усреднением ~300 мс, не точная копия алгоритма AIMP. Применяется выбранная обработка.",
                "VU-Modell mit ~300 ms Mittelung, kein exakter AIMP-Nachbau. Ausgewählte Bearbeitung wird wiedergegeben.",
                "VU model with ~300 ms integration, not AIMP's exact algorithm. Plays the selected processing.");
            Add("previewAnalyzing", "Измерение записи для прослушивания…", "Analysiere Titel für die Vorschau…", "Analyzing track for preview…");
            Add("previewError", "Не удалось воспроизвести", "Wiedergabe fehlgeschlagen", "Playback failed");
            Add("previewReady", "Нажми ▶ для прослушивания. Перемотка доступна после определения длительности.",
                "▶ startet die Vorschau. Suchen nach der Dauerermittlung verfügbar.",
                "Press ▶ to preview. Seek works after duration is detected.");
            Add("strength", "Плотность", "Dichte", "Density");
            Add("denseExplain",
                "7 — прежний удачный режим; 9 и 10 — сильнее. Измерения VU и LUFS отличаются. Стерео связано.",
                "7 = bisheriger Favorit; 9/10 = stärker. VU und LUFS unterscheiden sich; Stereo gekoppelt.",
                "7 is the previous favorite, 9/10 are stronger. VU and LUFS differ; stereo linked.");
            Add("denseNote",
                "На 9–10 потолок −0,1 dBFS до MP3; истинные пики готового MP3 могут быть выше 0 dBTP. Слушай на искажения.",
                "VU-Ziel: −2 bis −1 mit Ausschlägen zu 0; AIMP abhängig von Einstellungen. Limiter: −0,1 dBFS auf Stufe 9–10.",
                "VU target: −2 to −1, often touching 0. AIMP settings matter. Pre-MP3 ceiling: −0.1 dBFS at 9–10.");
            Add("offExplain",
                "Обычная конвертация MP3 320 без нормализации, компрессора и лимитера.",
                "Normale MP3-320-Konvertierung ohne Normalisierung, Kompressor oder Limiter.",
                "Standard MP3 320 conversion without normalization, compression or limiting.");
            Add("denseGain", "RMS / усиление / атака A / восстановление R",
                "RMS / Verstärkung / Attack A / Release R",
                "RMS / gain / attack A / release R");
            Add("denseAnalysis", "Анализ уровня и пиков", "Analyse von RMS und Spitzen", "Analyzing RMS and peaks");
            Add("offNote", "Выход MP3: 320 кбит/с, 44,1 кГц. Исходный файл не изменяется.",
                "MP3-Ausgabe: 320 kbit/s, 44,1 kHz. Die Quelldatei bleibt unverändert.",
                "MP3 output: 320 kb/s, 44.1 kHz. The source stays untouched.");
            Add("invalidRms", "Не удалось измерить общий RMS_level; плотная обработка отменена.",
                "RMS_level konnte nicht ermittelt werden. Dichte Bearbeitung abgebrochen.",
                "Overall RMS_level was unavailable; dense processing aborted.");
            Add("conflictingModes", "Выберите только один режим обработки.", "Nur einen Modus wählen.", "Select only one processing mode.");
            Add("normal", "Поднять пики MP3 до 0 dBFS", "MP3-Pegelspitzen auf 0 dBFS anheben", "Raise MP3 peaks to 0 dBFS");
            Add("explain", "Одно усиление для всего трека. Динамика и баланс каналов сохраняются. Без компрессии и лимитера.",
                "Eine Verstärkung für den ganzen Titel. Dynamik und Kanalbalance bleiben erhalten. Ohne Kompressor oder Limiter.",
                "One gain for the entire track. Dynamics and channel balance are preserved. No compressor or limiter.");
            Add("folder", "Папка для готовых MP3:", "Zielordner für fertige MP3:", "Output folder for MP3:");
            Add("browse", "Обзор…", "Durchsuchen…", "Browse…");
            Add("start", "Создать MP3 320", "MP3 320 erstellen", "Create MP3 320");
            Add("cancel", "Отмена", "Abbrechen", "Cancel");
            Add("ready", "Готово к запуску", "Bereit", "Ready");
            Add("working", "Обработка", "Verarbeitung", "Processing");
            Add("complete", "Все файлы обработаны", "Alle Dateien verarbeitet", "All files processed");
            Add("cancelled", "Отменено. Незавершённый файл удалён.", "Abgebrochen. Unvollständige Datei gelöscht.", "Cancelled. Partial output deleted.");
            Add("failed", "Ошибка. Необработанный файл не выдаётся за нормализованный.",
                "Fehler. Eine unverarbeitete Datei wird nicht als normalisiert ausgegeben.",
                "Error. No untreated file will be presented as normalized.");
            Add("preflight", "Проверка фильтров и кодировщика FFmpeg…", "FFmpeg-Filter und Encoder prüfen…", "Checking FFmpeg filters and encoder…");
            Add("item", "Файл", "Datei", "File");
            Add("analysis", "Анализ пиков", "Spitzenanalyse", "Analyzing peaks");
            Add("gain", "Пик и применённое усиление", "Spitzenpegel und angewandte Verstärkung", "Peak and applied gain");
            Add("encode", "Создание MP3", "MP3 erstellen", "Creating MP3");
            Add("verify", "Полное декодирование для проверки", "Vollständiges Dekodieren zur Prüfung", "Fully decoding for verification");
            Add("saved", "Сохранено", "Gespeichert", "Saved");
            Add("select", "Выберите хотя бы один аудиофайл.", "Bitte mindestens eine Audiodatei auswählen.", "Select at least one audio file.");
            Add("nooutput", "Укажите папку результата.", "Bitte den Zielordner angeben.", "Choose an output folder.");
            Add("settingsError", "Не удалось сохранить настройки в профиль.", "Einstellungen konnten nicht im Profil gespeichert werden.", "Could not save settings to the user profile.");
            Add("drop", "Перетащите сюда MP3 / AAC / M4A / MP4 / WAV / FLAC или нажмите «Добавить файлы».",
                "MP3 / AAC / M4A / MP4 / WAV / FLAC hierher ziehen oder „Dateien hinzufügen“ klicken.",
                "Drop MP3 / AAC / M4A / MP4 / WAV / FLAC here or click Add files.");
            Add("note", "−0,1 dBFS — потолок до MP3 на уровнях 9–10; пики после кодирования могут отличаться.",
                "0 dBFS gilt vor der MP3-Kodierung; die Spitzen der MP3-Datei können abweichen.",
                "0 dBFS is the pre-encoding target; the finished MP3 may have different peaks.");
            Add("inputfilter", "Аудио/видео с аудиодорожкой|*.mp3;*.aac;*.m4a;*.mp4;*.wav;*.flac|Все файлы|*.*",
                "Audio/Video mit Tonspur|*.mp3;*.aac;*.m4a;*.mp4;*.wav;*.flac|Alle Dateien|*.*",
                "Audio/video with audio|*.mp3;*.aac;*.m4a;*.mp4;*.wav;*.flac|All files|*.*");
            Add("selectfolder", "Выберите папку MP3", "MP3-Zielordner auswählen", "Choose MP3 destination folder");
            Add("missingFFmpeg", "Не найден ffmpeg.exe рядом с программой. Распакуйте весь комплект.",
                "ffmpeg.exe wurde neben dem Programm nicht gefunden. Bitte das gesamte Paket entpacken.",
                "ffmpeg.exe was not found beside the app. Extract the entire package.");
            Add("missingFilter", "Отсутствует нужный фильтр FFmpeg. Замените FFmpeg на совместимую сборку: ",
                "Benötigter FFmpeg-Filter fehlt. FFmpeg bitte durch eine kompatible Version ersetzen: ",
                "Required FFmpeg filter missing. Replace FFmpeg with a compatible build: ");
            Add("missingEncoder", "Нет кодировщика libmp3lame. Требуется сборка FFmpeg с поддержкой MP3.",
                "libmp3lame fehlt. Eine FFmpeg-Version mit MP3-Encoder ist erforderlich.",
                "libmp3lame is missing. FFmpeg with MP3 encoder support is required.");
            Add("invalidPeak", "Не удалось получить общий Peak_level. Нормализация отменена.",
                "Der gesamte Peak_level konnte nicht ermittelt werden. Normalisierung abgebrochen.",
                "Overall Peak_level is unavailable. Normalization aborted.");
            Add("sourceMissing", "Входной файл не найден: ", "Quelldatei nicht gefunden: ", "Source file not found: ");
            Add("ffmpegFailed", "FFmpeg завершился с ошибкой: ", "FFmpeg wurde mit einem Fehler beendet: ", "FFmpeg failed: ");
            Add("emptyMp3", "FFmpeg создал пустой MP3.", "FFmpeg hat eine leere MP3-Datei erzeugt.", "FFmpeg produced an empty MP3.");
            Add("peakAnalysis", "Выравнивание пиков", "Spitzenabgleich", "Aligning peaks");
            Add("peakGain", "Подъём пиков", "Spitzenanhebung", "Peak makeup");
            Add("calibrationTitle", "Калибровка VU", "VU-Kalibrierung", "VU calibration");
            Add("calibrationHint", "0 VU соответствует выбранному уровню dBFS.\nЗвуковая обработка при этом не меняется.", "0 VU entspricht dem gewählten dBFS-Wert.\nDie Audiobearbeitung bleibt unverändert.", "0 VU corresponds to the selected dBFS level.\nAudio processing remains unchanged.");
            Add("presetSoft", "Мягко", "Sanft", "Soft");
            Add("presetBalance", "Баланс", "Balance", "Balanced");
            Add("presetLoud", "Громко", "Laut", "Loud");
            Add("presetMax", "Максимум", "Maximum", "Maximum");
            Add("noActiveFile", "Добавь аудиофайл или перетащи сюда", "Audiodatei hinzufügen oder ablegen", "Add an audio file or drag one here");
            Add("readingMeta", "Определение формата…", "Format wird erkannt…", "Reading format…");
            Add("compare", "Сравнение:", "Vergleich:", "Compare:");
            Add("compOriginal", "Оригинал", "Original", "Original");
            Add("compProcessed", "После обработки", "Bearbeitet", "Processed");
            Add("comparisonA", "A  Оригинал", "A  Original", "A  Original");
            Add("comparisonLive", "●  Вживую", "●  Live", "●  Live");
            Add("comparisonB", "B  Готовый MP3", "B  Fertiges MP3", "B  Exported MP3");
            Add("queue", "Очередь", "Warteschlange", "Queue");
            Add("dropQueue", "Перетащи сюда файл или несколько файлов", "Datei(en) hierher ziehen", "Drop one or multiple files here");
            Add("waveLoading", "Создаётся звуковая волна…", "Wellenform wird geladen…", "Building waveform…");
            Add("waveEmpty", "Добавь аудиофайл", "Audiodatei hinzufügen", "Add an audio file");
            Add("previewPlaying", "Предпрослушивание · живой звук по выбранному источнику", "Vorschau · gewählte Audioquelle", "Preview · selected audio source");
            Add("meterHintV2", "0 VU = 0 dBFS. Движение — по Analog Meter Classic; отображается более высокий канал.",
                "0 VU = 0 dBFS. Bewegung nach Analog Meter Classic; der höhere Kanal wird angezeigt.",
                "0 VU = 0 dBFS. Analog Meter Classic response; displays the higher channel.");
            Add("listen","Прослушать","Anhören","Listen");
            Add("multipleFiles","Поддерживается несколько файлов","Mehrere Dateien werden unterstützt","Multiple files supported");
            Add("log", "Журнал", "Protokoll", "Log");
        }

        public static string Get(string key, int language)
        {
            string[] variants;
            if (!Words.TryGetValue(key, out variants)) return key;
            if (language < 0 || language > 2) language = 0;
            return variants[language];
        }
    }
}
