"""Build the localized, dependency-free public site on the shared site kit (assets/kit).

Inputs: public-release.json (version, release files, texts, urls) and the text tables below.
Output: index.html (German root), index-en.html, index-ru.html, index-de.html (redirect),
downloader-<lang>.html, Guide-<lang>.html (+ docs/Guide-<lang>.html), peakmp3-<lang>.html (redirects),
website/extension-<lang>.html (the real extension popup for the preview frame), website/site.css.
"""
from pathlib import Path
from html import escape as e
import json
import re

R = Path(__file__).resolve().parents[1]
W = R / 'website'
W.mkdir(exist_ok=True)
D_JSON = json.loads((R / 'public-release.json').read_text(encoding='utf-8'))
LANGS = ('ru', 'de', 'en')
PROFILE = 'https://popovantondev.github.io/popovantondev/'
PAGES = 'https://popovantondev.github.io/'
GITHUB = D_JSON['source_url']
PEAK_PAGES = PAGES + 'PeakMP3/'
PEAK_REPO = 'https://github.com/popovantondev/PeakMP3'
VERSION = D_JSON['version']
PEAK_VERSION = D_JSON['bundled_applications']['PeakMP3']
STACK = 'C# · JavaScript'
KIT = (R / 'assets/kit/KIT_VERSION').read_text(encoding='utf-8').strip()


def asset_url(marker):
    """URL of the release file whose name contains marker (names and data come from public-release.json)."""
    return next(a['url'] for a in D_JSON['assets'] if marker in a['name'])


SETUP_URL = asset_url('Setup.exe')
ZIP_URL = asset_url('FULL.zip')
SUMS_URL = asset_url('SHA256SUMS')
SRC_URL = asset_url('Corresponding-Sources')
SETUP_NAME = SETUP_URL.rsplit('/', 1)[1]

# ---------------------------------------------------------------- chrome and bento text
T = {
    'ru': dict(
        skip='К содержимому', nav='Основная навигация', back='← Все программы', search='Поиск по программам', search_text='Поиск',
        kbd='Ctrl K', theme='Сменить тему', legal='Правовая информация', new='НОВОЕ', pill='Вышла версия {v}', more='Что нового →',
        inst_title='Установка', download='Скачать для Windows', source='Исходный код', guide='Руководство', install='Как установить', requirements='Перед запуском',
        bento='Что внутри', bento_sub='Установка, функции, передача данных и проверка.', feat='Функции', checks='Проверено',
        flow='Как это работает', flow_items=['YouTube', 'Исходный AAC', 'PeakMP3', 'Готовый MP3'],
        status='Предварительная версия', toc='В руководстве', sections='Подробнее',
        pkg_title='Один установщик, две программы',
        pkg_text='В релизе v{v} один общий установщик. Он ставит две программы, у каждой свой репозиторий и свой сайт.',
        pkg_cols=['Программа', 'Версия', 'Что это'], pkg_rows=[
            ['YouTube Downloader', VERSION, 'расширение браузера и локальный компонент (этот сайт)'],
            ['PeakMP3', PEAK_VERSION, 'обработка звука: плотность, сравнение A/B, сохранение MP3']],
        pkg_why='Поэтому кнопка загрузки PeakMP3 ведёт на этот релиз: установщик и файлы лежат здесь.',
        peak_site='Сайт PeakMP3', peak_src='Исходный код PeakMP3',
        lgpl_title='Исходный код FFmpeg и LAME',
        lgpl_text='Медиаинструменты FFmpeg и LAME распространяются по LGPL. Соответствующий исходный код и параметры сборки опубликованы отдельным файлом в том же релизе.',
        lgpl_link='Исходники FFmpeg (ZIP)', lgpl_more='Лицензии и состав компонентов',
        rights_text='Собственный код доступен для ознакомления; неизменённый выпуск — для личного использования. Сторонние лицензии действуют отдельно.',
        inst_text='Шаги из раздела «Установка» руководства.',
        term=[('ps', '1. Скачать установщик или распаковать полный ZIP; закрыть PeakMP3 и загрузки'),
              ('ps', '2. Запустить Setup.exe: установка в профиль пользователя, без прав администратора'),
              ('ps', '3. В мастере открыть страницу расширений: режим разработчика, «Загрузить распакованное»'),
              ('dim', '   %LOCALAPPDATA%\\YTWin7_360_MP3\\extension'),
              ('ok', '4. Выбрать папку extension, закрепить зелёную стрелку, перезагрузить вкладку YouTube')],
        tile_title='Подключение',
        tile_text='Программа и расширение работают локально на вашем ПК. Видео загружаются напрямую с YouTube (googlevideo); собственного облачного сервиса нет.',
        tile_chips=['Напрямую с YouTube', 'Без облачного сервиса'],
        features=['Исходное видео и AAC без перекодирования', 'Загрузка без просмотра', 'Свой набор кнопок', 'MP3 без обработки',
                  'Передача AAC в PeakMP3', 'Windows 7 SP1, 32 бита', 'Обновление с откатом', 'Интерфейс RU · DE · EN'],
        checks_items=['Автоматические проверки в GitHub Actions: документация, данные релиза, JavaScript-тесты расширения',
                      'Проверки в Windows 11 описаны в docs/CHECKS-260.md: сборка, установка, откат, контрольная сумма AAC',
                      'Контрольные суммы SHA-256 для файлов релиза (SHA256SUMS.txt)',
                      'Версия {v} и заметки о выпуске',
                      'Руководство на русском, немецком и английском',
                      'Отдельно не проверялось: Windows 10']),
    'de': dict(
        skip='Zum Inhalt', nav='Hauptnavigation', back='← Alle Programme', search='Programme durchsuchen', search_text='Suchen',
        kbd='Strg K', theme='Farbschema wechseln', legal='Rechtliches', new='NEU', pill='Version {v} ist da', more='Was ist neu →',
        inst_title='Installation', download='Für Windows herunterladen', source='Quellcode', guide='Anleitung', install='Installation', requirements='Vor dem Start',
        bento='Was drin steckt', bento_sub='Installation, Funktionen, Datenverbindung und Prüfung.', feat='Funktionen', checks='Geprüft',
        flow='So funktioniert es', flow_items=['YouTube', 'Original-AAC', 'PeakMP3', 'Fertige MP3'],
        status='Vorabversion', toc='In der Anleitung', sections='Mehr',
        pkg_title='Ein Installationspaket, zwei Programme',
        pkg_text='Das Release v{v} enthält ein gemeinsames Setup. Es installiert zwei Programme, jedes mit eigenem Repository und eigener Website.',
        pkg_cols=['Programm', 'Version', 'Was es ist'], pkg_rows=[
            ['YouTube Downloader', VERSION, 'Browser-Erweiterung und lokaler Host (diese Website)'],
            ['PeakMP3', PEAK_VERSION, 'Audiobearbeitung: Dichte, A/B-Vergleich, Speichern als MP3']],
        pkg_why='Deshalb verweist der Download von PeakMP3 auf dieses Release: Setup und Dateien liegen hier.',
        peak_site='Website von PeakMP3', peak_src='Quellcode von PeakMP3',
        lgpl_title='Quellcode von FFmpeg und LAME',
        lgpl_text='Die Medienwerkzeuge FFmpeg und LAME stehen unter der LGPL. Der passende Quellcode samt Build-Konfiguration liegt als eigene Datei im selben Release.',
        lgpl_link='FFmpeg-Quellcode (ZIP)', lgpl_more='Lizenzen und Komponenten',
        rights_text='Eigener Code dient der Einsicht; unveränderte Programme dürfen privat genutzt werden. Drittanbieterbedingungen gelten separat.',
        inst_text='Die Schritte aus „Installation“ in der Anleitung.',
        term=[('ps', '1. Setup laden oder das vollständige ZIP entpacken; PeakMP3 und aktive Downloads schließen'),
              ('ps', '2. Setup.exe normal starten: Installation im Benutzerprofil, ohne Administratorrechte'),
              ('ps', '3. Im Assistenten die Erweiterungsverwaltung öffnen: Entwicklermodus, „Entpackte Erweiterung laden“'),
              ('dim', '   %LOCALAPPDATA%\\YTWin7_360_MP3\\extension'),
              ('ok', '4. Ordner extension wählen, grünen Pfeil anheften, YouTube-Tab neu laden')],
        tile_title='Verbindung',
        tile_text='Programm und Erweiterung laufen lokal auf Ihrem PC. Die Videos laden sie direkt von YouTube (googlevideo); einen eigenen Cloud-Dienst gibt es nicht.',
        tile_chips=['Direkt von YouTube', 'Kein Cloud-Dienst'],
        features=['Original-Video und AAC ohne Neukodierung', 'Download ohne Wiedergabe', 'Schaltflächen frei wählbar', 'MP3 ohne Bearbeitung',
                  'AAC an PeakMP3 übergeben', 'Windows 7 SP1, 32 Bit', 'Update mit Wiederherstellung', 'Oberfläche RU · DE · EN'],
        checks_items=['Automatische Prüfungen auf GitHub Actions: Dokumentation, Release-Daten, JavaScript-Tests der Erweiterung',
                      'Prüfungen unter Windows 11 sind in docs/CHECKS-260.md dokumentiert: Build, Installation, Rollback, AAC-Prüfsumme',
                      'SHA-256-Prüfsummen für alle Release-Dateien (SHA256SUMS.txt)',
                      'Version {v} mit Release-Notizen',
                      'Anleitung auf Deutsch, Englisch und Russisch',
                      'Nicht separat geprüft: Windows 10']),
    'en': dict(
        skip='Skip to content', nav='Main navigation', back='← All programs', search='Search programs', search_text='Search',
        kbd='Ctrl K', theme='Switch colour scheme', legal='Legal', new='NEW', pill='Version {v} is out', more="What's new →",
        inst_title='Installation', download='Download for Windows', source='Source code', guide='User guide', install='How to install', requirements='Before you start',
        bento="What's inside", bento_sub='Installation, features, data flow and checks.', feat='Features', checks='Checked',
        flow='How it works', flow_items=['YouTube', 'Original AAC', 'PeakMP3', 'Your MP3'],
        status='Preview', toc='In this guide', sections='More',
        pkg_title='One installer, two programs',
        pkg_text='Release v{v} contains one shared installer. It installs two programs, each with its own repository and website.',
        pkg_cols=['Program', 'Version', 'What it is'], pkg_rows=[
            ['YouTube Downloader', VERSION, 'browser extension and native host (this website)'],
            ['PeakMP3', PEAK_VERSION, 'audio processing: density, A/B comparison, saving as MP3']],
        pkg_why='That is why the PeakMP3 download button points to this release: the installer and files are hosted here.',
        peak_site='PeakMP3 website', peak_src='PeakMP3 source code',
        lgpl_title='FFmpeg and LAME source code',
        lgpl_text='The FFmpeg and LAME media tools are LGPL components. Their matching source code and build configuration are published as a separate file in the same release.',
        lgpl_link='FFmpeg corresponding sources (ZIP)', lgpl_more='Licences and components',
        rights_text='Application source is available for inspection, and the unmodified release for personal use. Third-party terms apply separately.',
        inst_text='The steps from “Installation” in the guide.',
        term=[('ps', '1. Download Setup or extract the complete ZIP; close PeakMP3 and active downloads'),
              ('ps', '2. Run Setup.exe: installs into your user profile, no administrator rights'),
              ('ps', '3. In the wizard open the extension manager: Developer mode, “Load unpacked”'),
              ('dim', '   %LOCALAPPDATA%\\YTWin7_360_MP3\\extension'),
              ('ok', '4. Select the extension folder, pin the green arrow, reload the YouTube tab')],
        tile_title='Connection',
        tile_text='The program and the extension run locally on your PC. Videos are downloaded straight from YouTube (googlevideo); there is no cloud service of its own.',
        tile_chips=['Straight from YouTube', 'No cloud service'],
        features=['Original video and AAC without re-encoding', 'Download without playback', 'Choose your own buttons', 'MP3 without processing',
                  'Hand AAC over to PeakMP3', 'Windows 7 SP1, 32 bit', 'Update with restore', 'Interface RU · DE · EN'],
        checks_items=['Automatic checks on GitHub Actions: documentation, release data, extension JavaScript tests',
                      'Checks on Windows 11 are documented in docs/CHECKS-260.md: build, installation, rollback, AAC checksum',
                      'SHA-256 checksums for all release files (SHA256SUMS.txt)',
                      'Version {v} with release notes',
                      'Guide in German, English and Russian',
                      'Not tested separately: Windows 10']),
}

# ---------------------------------------------------------------- page texts (German on "Sie")
D = {
    'ru': dict(
        downTitle=['Любимое видео.', 'Твой формат.'], downLead='Сохраняй видео и звук с YouTube. Показывай только те кнопки, которыми пользуешься.',
        downFeatures=[['Исходное качество', 'Видео и AAC сохраняются без перекодирования. Качество MP3 зависит от исходной дорожки.'], ['Без просмотра', 'Загрузка по умолчанию идёт без включения видео. Способ работы меняется в настройках.'], ['Три языка', 'Настройки, сообщения и инструкции доступны на русском, немецком и английском.']],
        formatsTitle='Только нужные кнопки', formatsText='По умолчанию на YouTube видны быстрый 360p и MP3, а также исходные 480p и 720p. Остальные качества и AAC включаются по зелёной стрелке в браузере.',
        audioTitle='Обычный MP3 или звук с характером', audioText='Сохрани MP3 без обработки — или передай исходный AAC в PeakMP3. Программа откроет файл, чтобы ты мог прослушать результат, выбрать плотность и сохранить MP3.', audioBullets=['Исходный AAC без промежуточного MP3', 'Передача в очередь уже открытого PeakMP3', 'Прослушивание и сохранение под твоим контролем'],
        downNote='Интерфейс расширения {v}. Демонстрация настроек, без подключения к браузерному профилю.', limitation='YouTube иногда требует подтверждение сеанса (attestation required). Такой отказ остаётся ограничением Preview; видео автоматически не включается.',
        listenText='Регулятор плотности и переключатель A/B помогают выбрать подходящий вариант.', saveText='Папка по умолчанию — рабочий стол. При сохранении можно выбрать другую.',
        guideTitle='Всё для первого запуска', guideLead='Один комплект. Две программы. Несколько понятных шагов.', fullZip='Полный ZIP со всем комплектом', checksums='Контрольные суммы', guidePreview='YouTube Downloader {v} Preview + PeakMP3 {p}',
        setupTitle='Установка', setupSteps=['Скачай установщик или распакуй полный ZIP. Перед обновлением закрой PeakMP3 и текущие загрузки.', 'Запусти Setup.exe обычным способом. Программы устанавливаются в профиль пользователя без запроса прав администратора.', 'Мастер подключения откроет страницу расширений выбранного браузера. Включи режим разработчика и выбери «Загрузить распакованное».', 'Укажи папку extension, показанную мастером. Закрепи зелёную стрелку. После обновления перезагрузи само расширение, затем вкладку YouTube.'],
        osTitle='Windows 7 и слабые компьютеры', osText='Обязательная цель — Windows 7 SP1, 32 бита и процессор с SSE2. Программы используют .NET Framework 3.5 / CLR2; на новых Windows предусмотрен запуск через CLR4. .NET 4.8 и WebView2 не требуются. Windows, браузер и системная среда .NET устанавливаются отдельно.',
        verified='Пользователь подтвердил работу комплекта на Windows 11 с актуальным Chrome и Windows 7 с Яндексом и Chrome для этой системы. Точные версии браузеров и полный перечень выполненных сценариев пока не зафиксированы. Windows 10 отдельно не проверена.',
        removeTitle='Удаление и возврат версии', removeText='Удаление через приложения Windows убирает программу и интеграцию, сохраняя пользовательские файлы, настройки и аудиокэш. Расширение удаляется отдельно из каждого профиля браузера. Если имеется резервная копия, Restore_previous.cmd возвращает прежнюю установку.',
        helpTitle='Если что-то не получилось', helpText='Проверь, что в браузере загружено расширение {v} из установленной папки. Перезагрузи расширение и вкладку YouTube. Открой зелёную стрелку: там можно проверить связь с PeakMP3. В сообщении об ошибке укажи Windows, браузер, формат и точный текст отказа.', rights='Права и компоненты'),
    'de': dict(
        downTitle=['Ihr Video.', 'Ihr Format.'], downLead='Videos und Audio von YouTube speichern. Nur die Schaltflächen anzeigen, die Sie wirklich brauchen.',
        downFeatures=[['Originalqualität', 'Video und AAC bleiben ohne Neukodierung erhalten. MP3 kann die Qualität der Quelle nicht erhöhen.'], ['Ohne Wiedergabe', 'Standardmäßig lädt das Video, ohne zu spielen. Die Methode lässt sich in den Einstellungen ändern.'], ['Drei Sprachen', 'Einstellungen, Meldungen und Anleitungen gibt es auf Russisch, Deutsch und Englisch.']],
        formatsTitle='Nur Ihre Formate', formatsText='Auf YouTube erscheinen zunächst schnelles 360p und MP3 sowie die Originalstufen 480p und 720p. Weitere Qualitäten und AAC schalten Sie über den grünen Browserpfeil ein.',
        audioTitle='Einfaches MP3 oder mehr Charakter', audioText='MP3 ohne Bearbeitung speichern oder Original-AAC an PeakMP3 übergeben. Das Programm öffnet die Datei zum Anhören, Einstellen der Dichte und Speichern als MP3.', audioBullets=['Original-AAC ohne zwischenzeitliche MP3', 'Übergabe an die Warteschlange des offenen PeakMP3', 'Anhören und Speichern unter Ihrer Kontrolle'],
        downNote='Erweiterungsoberfläche {v}. Einstellungsdemo ohne Verbindung zu einem Browserprofil.', limitation='YouTube kann eine Sitzungsbestätigung verlangen (attestation required). Diese Grenze der Preview bleibt bestehen; das Video startet nicht automatisch.',
        listenText='Dichteregler und A/B-Schalter helfen bei der Wahl der passenden Einstellung.', saveText='Der Speicherdialog beginnt auf dem Desktop. Sie können einen anderen Ordner wählen.',
        guideTitle='Bereit für den ersten Start', guideLead='Ein Paket. Zwei Programme. Ein paar einfache Schritte.', fullZip='Vollständiges ZIP-Paket', checksums='Prüfsummen', guidePreview='YouTube Downloader {v} Preview + PeakMP3 {p}',
        setupTitle='Installation', setupSteps=['Setup laden oder das vollständige ZIP entpacken. PeakMP3 und aktive Downloads vor dem Update schließen.', 'Setup.exe normal starten. Installation im Benutzerprofil ohne Anforderung von Administratorrechten.', 'Der Assistent öffnet die Erweiterungsverwaltung des Browsers. Entwicklermodus einschalten und „Entpackte Erweiterung laden“ wählen.', 'Den angezeigten Ordner extension auswählen und den grünen Pfeil anheften. Nach Updates zuerst die Erweiterung, dann den YouTube-Tab neu laden.'],
        osTitle='Windows 7 und schwache Rechner', osText='Verbindliches Ziel: Windows 7 SP1, 32 Bit und SSE2. Die Programme verwenden .NET Framework 3.5 / CLR2 mit CLR4-Fallback auf neueren Windows-Versionen. .NET 4.8 und WebView2 sind nicht erforderlich. Windows, Browser und System-.NET sind separate Voraussetzungen.',
        verified='Der Nutzer bestätigt Funktion unter Windows 11 mit aktuellem Chrome sowie unter Windows 7 mit passendem Yandex und Chrome. Exakte Browserversionen und vollständige Testszenarien sind noch nicht dokumentiert. Windows 10 wurde nicht separat geprüft.',
        removeTitle='Entfernen und zurückkehren', removeText='Die Windows-Deinstallation entfernt Programm und Integration. Eigene Dateien, Einstellungen und Audiocache bleiben erhalten. Erweiterung separat aus jedem Browserprofil entfernen. Wenn eine Sicherung vorliegt, stellt Restore_previous.cmd die vorherige Installation wieder her.',
        helpTitle='Wenn etwas nicht klappt', helpText='Prüfen, ob Erweiterung {v} aus dem installierten Ordner geladen ist. Erweiterung und YouTube-Tab neu laden. Im grünen Pfeil lässt sich die Verbindung zu PeakMP3 prüfen. Beim Melden Windows, Browser, Format und genauen Fehlertext angeben.', rights='Rechte & Komponenten'),
    'en': dict(
        downTitle=['Your video.', 'Your format.'], downLead='Save videos and audio from YouTube. Show only the download buttons you use.',
        downFeatures=[['Original quality', 'Video and AAC are saved without re-encoding. MP3 quality remains limited by its source.'], ['Without playback', 'Downloads keep the video paused by default. Change the method in extension settings.'], ['Three languages', 'Settings, messages and guides are available in Russian, German and English.']],
        formatsTitle='Just the buttons you need', formatsText='Quick 360p and MP3, plus original 480p and 720p, appear on YouTube by default. Enable the other qualities and AAC using the green browser arrow.',
        audioTitle='Plain MP3 or a little more character', audioText='Save MP3 without processing, or send original AAC to PeakMP3. The application opens the file so you can listen, adjust density and save your MP3.', audioBullets=['Original AAC, without an intermediate MP3', 'Send to the queue in an already open PeakMP3', 'You control listening and saving'],
        downNote='Extension interface {v}. Settings demonstration without a browser-profile connection.', limitation='YouTube may require session verification (attestation required). This remains a Preview limitation; playback will not start automatically.',
        listenText='The density knob and A/B switch help you find the setting you like.', saveText='The save dialog starts on Desktop. You can choose another folder.',
        guideTitle='Ready for your first download', guideLead='One package. Two applications. A few simple steps.', fullZip='Complete ZIP package', checksums='Checksums', guidePreview='YouTube Downloader {v} Preview + PeakMP3 {p}',
        setupTitle='Installation', setupSteps=['Download Setup or extract the complete ZIP. Close PeakMP3 and active downloads before updating.', 'Run Setup.exe normally. It installs into your user profile without requesting administrator privileges.', 'The connection wizard opens your browser’s extension manager. Enable Developer mode and choose Load unpacked.', 'Select the extension folder shown by the wizard and pin the green arrow. After updates, reload the extension itself, then YouTube tabs.'],
        osTitle='Windows 7 and low-powered PCs', osText='Windows 7 SP1, 32 bit and SSE2 remain the required target. Applications use .NET Framework 3.5 / CLR2 with configured CLR4 fallback on newer Windows. .NET 4.8 and WebView2 are not required. Windows, a compatible browser and system .NET remain prerequisites.',
        verified='The user reports success on Windows 11 with current Chrome and on Windows 7 with compatible Yandex and Chrome. Exact browser versions and the full set of tested scenarios are not yet recorded. Windows 10 has not been separately tested.',
        removeTitle='Uninstalling and restoring', removeText='Windows uninstall removes the application and integration while retaining user files, settings and audio cache. Remove the extension separately in each browser profile. If a backup exists, Restore_previous.cmd returns to the previous installation.',
        helpTitle='If something goes wrong', helpText='Check that extension {v} is loaded from the installed folder. Reload the extension and YouTube tab. The green arrow lets you check the connection to PeakMP3. Include Windows, browser, format and the exact error text in a problem report.', rights='Rights & components'),
}


def fill(value):
    if isinstance(value, str):
        return value.replace('{v}', VERSION).replace('{p}', PEAK_VERSION)
    if isinstance(value, list):
        return [fill(x) for x in value]
    return value


D = {l: {k: fill(v) for k, v in d.items()} for l, d in D.items()}


# ---------------------------------------------------------------- shared pieces
def landing(l):
    return {'de': 'index.html', 'en': 'index-en.html', 'ru': 'index-ru.html'}[l]


def path(page, l):
    if page == 'home':
        return landing(l)
    return ('Guide' if page == 'guide' else page) + '-' + l + '.html'


def catalog(l):
    return PROFILE + landing(l)


def head_assets(rel=''):
    return (f'<script src="{rel}assets/kit/kit-theme.js"></script>'
            f'<link rel="stylesheet" href="{rel}assets/kit/kit.css?v={KIT}">'
            f'<link rel="stylesheet" href="{rel}website/site.css?v={KIT}">'
            f'<script src="{rel}assets/kit/programs.js?v={KIT}" defer></script>'
            f'<script src="{rel}assets/kit/kit.js?v={KIT}" defer></script>'
            f'<script src="{rel}website/site.js?v={KIT}" defer></script>')


def nav(kind, l):
    t = T[l]
    page = 'guide' if kind == 'guide' else 'downloader' if kind == 'downloader' else 'home'
    langs = ''.join(f'<a href="{path(page, x)}" lang="{x}" hreflang="{x}"' + (' aria-current="page"' if x == l else '') + f'>{x.upper()}</a>' for x in LANGS)
    return (
        f'<a class="ppk-skip" href="#content">{e(t["skip"])}</a>\n'
        f'<div class="ppk-nav-wrap" id="ppk-nav-wrap">\n'
        f'  <nav class="ppk-nav" aria-label="{e(t["nav"])}">\n'
        f'    <a class="ppk-brand" href="{landing(l)}"><img src="extension/icons/icon128.png" alt="" width="34" height="34"><span>YouTube Downloader</span></a>\n'
        f'    <span class="ppk-spacer"></span>\n'
        f'    <a class="ppk-back" href="{catalog(l)}">{e(t["back"])}</a>\n'
        f'    <button class="ppk-search-btn" type="button" data-ppk-open-search aria-label="{e(t["search"])}"><span>{e(t["search_text"])}</span><kbd>{e(t["kbd"])}</kbd></button>\n'
        f'    <div class="ppk-langs">{langs}</div>\n'
        f'    <button class="ppk-icon-btn ppk-theme-btn" type="button" data-ppk-theme aria-label="{e(t["theme"])}"></button>\n'
        f'    <a class="ppk-icon-btn ppk-gh-btn" href="{e(GITHUB)}" aria-label="GitHub"></a>\n'
        f'  </nav>\n</div>')


LEGAL_LINKS = {'ru': ('Сообщить о проблеме', 'Условия использования'),
               'de': ('Problem melden', 'Nutzungsbedingungen'),
               'en': ('Report a problem', 'Terms of use')}


def footer(l):
    t = T[l]
    issue_label, rights_label = LEGAL_LINKS[l]
    return (
        f'<footer class="ppk-footer">\n  <span>© 2026 Anton Popov · YouTube Downloader {e(VERSION)}</span>\n'
        f'  <nav aria-label="{e(t["legal"])}"><a href="{PROFILE}impressum.html">Impressum</a>'
        f'<a href="{PROFILE}datenschutz.html">Datenschutz</a><a href="{e(GITHUB)}">GitHub</a>'
        f'<a href="{e(D_JSON["feedback_url"])}">{issue_label}</a>'
        f'<a href="{e(GITHUB)}/blob/main/RIGHTS.md">{rights_label}</a></nav>\n</footer>')


def download_button(l, classes='ppk-btn ppk-btn-primary'):
    return f'<a class="{classes}" data-download="setup" href="{e(SETUP_URL)}" download>{e(T[l]["download"])}</a>'


def pill(l):
    t = T[l]
    return (f'<a class="ppk-pill" href="{e(D_JSON["release_url"])}"><span class="ppk-new">{e(t["new"])}</span>'
            f'<span>{e(t["pill"].format(v=VERSION))}</span> <span class="ppk-more">{e(t["more"])}</span></a>')


def bento(l):
    t = T[l]
    lines = ''.join(f'<div class="ppk-{k}">{e(x)}</div>' for k, x in t['term'])
    chips = ''.join(f'<span>{e(x)}</span>' for x in t['features'])
    tile = ''.join(f'<span>{e(x)}</span>' for x in t['tile_chips'])
    checks = ''.join(f'<li>{e(x.format(v=VERSION) if "{v}" in x else x)}</li>' for x in t['checks_items'])
    return (
        f'<section class="ppk-bento-section ppk-section ppk-reveal">\n'
        f'<h2>{e(t["bento"])}</h2>\n<p class="ppk-sub">{e(t["bento_sub"])}</p>\n<div class="ppk-bento">\n'
        f'<div class="ppk-tile ppk-wide"><h3>{e(t["inst_title"])}</h3><p>{e(t["inst_text"])}</p>'
        f'<div class="ppk-term" role="group" aria-label="{e(t["inst_title"])}"><div class="ppk-term-bar"><i></i><i></i><i></i></div>'
        f'<div class="ppk-term-out">{lines}</div></div></div>\n'
        f'<div class="ppk-tile"><h3>{e(t["tile_title"])}</h3><p>{e(t["tile_text"])}</p><div class="ppk-chips">{tile}</div></div>\n'
        f'<div class="ppk-tile"><h3>{e(t["feat"])}</h3><div class="ppk-chips">{chips}</div></div>\n'
        f'<div class="ppk-tile ppk-wide"><h3>{e(t["checks"])}</h3><ul class="ppk-checks">{checks}</ul></div>\n'
        f'</div>\n</section>')


def package_section(l, heading_level=2):
    """Shared installer with PeakMP3 and the LGPL source offer (landing and guide)."""
    t = T[l]
    rows = ''.join('<tr>' + ''.join(f'<td>{e(c)}</td>' if i else f'<th scope="row">{e(c)}</th>' for i, c in enumerate(r)) + '</tr>' for r in t['pkg_rows'])
    head = ''.join(f'<th scope="col">{e(c)}</th>' for c in t['pkg_cols'])
    return (
        f'<section class="ppk-section ppk-reveal yt-package" id="package">\n<h2>{e(t["pkg_title"])}</h2>\n'
        f'<p class="ppk-sub">{e(t["pkg_text"].format(v=VERSION))}</p>\n'
        f'<div class="yt-table"><table><thead><tr>{head}</tr></thead><tbody>{rows}</tbody></table></div>\n'
        f'<p>{e(t["pkg_why"])} <code>{e(SETUP_NAME)}</code></p>\n'
        f'<div class="ppk-actions"><a class="ppk-btn" href="{PEAK_PAGES}index{"" if l == "de" else "-" + l}.html">{e(t["peak_site"])} ↗</a>'
        f'<a class="ppk-btn" href="{PEAK_REPO}">{e(t["peak_src"])} ↗</a></div>\n'
        f'<h3>{e(t["lgpl_title"])}</h3>\n<p>{e(t["lgpl_text"])}</p>\n'
        f'<div class="ppk-actions"><a class="ppk-btn" data-download="sources" href="{e(SRC_URL)}">{e(t["lgpl_link"])}</a>'
        f'<a class="ppk-btn" href="{e(GITHUB)}/blob/main/THIRD_PARTY_NOTICES.md">{e(t["lgpl_more"])}</a></div>\n</section>')


def extension_frame(l):
    return (f'<figure class="ppk-preview"><div class="ppk-stage extension-frame">'
            f'<iframe src="website/extension-{l}.html" title="YouTube Downloader · {l.upper()}" loading="eager" scrolling="no" height="560"></iframe></div>'
            f'<figcaption>{e(D[l]["downNote"])}</figcaption></figure>')


def downloader(l):
    t, d = T[l], D[l]
    hero = (
        f'<div class="ppk-hero"><div>\n{pill(l)}\n<h1>YouTube Downloader</h1>\n'
        f'<p class="ppk-lead"><strong>{e(" ".join(d["downTitle"]))}</strong> {e(d["downLead"])}</p>\n'
        f'<span class="ppk-status">{e(t["status"])} · {e(VERSION)}</span> <span class="ppk-chip yt-meta">{e(D_JSON["platform"])}</span>\n'
        f'<div class="ppk-actions">\n{download_button(l)}\n'
        f'<a class="ppk-btn ppk-btn-gh" href="{e(GITHUB)}">{e(t["source"])} <span class="ppk-chip"><i class="ppk-dot"></i>{e(STACK)}</span></a>\n'
        f'<a class="ppk-btn" href="{path("guide", l)}">{e(t["guide"])} ↗</a>\n</div>\n'
        f'<p class="ppk-requirements"><strong>{e(t["requirements"])}</strong>{e(D_JSON["requirements"][l])}</p>\n</div>\n{extension_frame(l)}</div>')
    features = '<div class="yt-features">' + ''.join(f'<article><span class="yt-num">0{i + 1}</span><h3>{e(a)}</h3><p>{e(b)}</p></article>' for i, (a, b) in enumerate(d['downFeatures'])) + '</div>'
    formats = (
        f'<section class="ppk-section ppk-reveal yt-split" id="formats"><div><h2>{e(d["formatsTitle"])}</h2><p>{e(d["formatsText"])}</p></div>'
        f'<div class="ppk-chips yt-formats"><span>360p · H.264</span><span>MP3 320</span><span>480p</span><span>720p</span></div></section>')
    audio = (
        f'<section class="ppk-section ppk-reveal yt-split" id="audio"><div><h2>{e(d["audioTitle"])}</h2><p>{e(d["audioText"])}</p>'
        f'<a class="ppk-btn" href="{PEAK_PAGES}index{"" if l == "de" else "-" + l}.html">PeakMP3 ↗</a></div>'
        f'<ul class="ppk-checks">' + ''.join(f'<li>{e(x)}</li>' for x in d['audioBullets']) + '</ul></section>')
    flow = (f'<section class="ppk-section ppk-process ppk-reveal" aria-labelledby="flow-title"><h2 id="flow-title">{e(t["flow"])}</h2>'
            f'<ol class="ppk-flow">' + ''.join(f'<li>{e(x)}</li>' for x in t['flow_items']) + '</ol></section>')
    return (f'<main class="ppk-shell" id="content">\n{hero}\n{features}\n{bento(l)}\n{formats}\n{audio}\n{package_section(l)}\n{flow}\n'
            f'<p class="ppk-notice">{e(d["limitation"])}</p>\n</main>')


def guide(l):
    t, d = T[l], D[l]
    names = [d['setupTitle'], d['audioTitle'], d['osTitle'], d['removeTitle'], d['helpTitle'], d['rights']]
    ids = ['install', 'audio', 'windows', 'uninstall', 'help', 'rights']
    toc = ''.join(f'<a href="#{a}">{e(b)}</a>' for a, b in zip(ids, names))
    install = ('<ol>' + ''.join(f'<li>{e(s)}</li>' for s in d['setupSteps']) + '</ol><p><code>%LOCALAPPDATA%\\YTWin7_360_MP3\\extension</code></p>'
               + download_button(l) + '<div class="download-links"><a data-download="zip" href="' + e(ZIP_URL) + '">' + e(d['fullZip']) + '</a><a data-download="sha" href="' + e(SUMS_URL) + '">' + e(d['checksums']) + '</a></div>')
    rights = (f'<p>{e(t["rights_text"])}</p><p><a href="{e(GITHUB)}/blob/main/RIGHTS.md">RIGHTS.md</a> · '
              f'<a href="{e(GITHUB)}/blob/main/THIRD_PARTY_NOTICES.md">FFmpeg / LAME / components</a></p>'
              f'<h3>{e(t["pkg_title"])}</h3><p>{e(t["pkg_text"].format(v=VERSION))} {e(t["pkg_why"])}</p>'
              f'<ul>' + ''.join(f'<li><strong>{e(r[0])} {e(r[1])}</strong> — {e(r[2])}</li>' for r in t['pkg_rows']) + '</ul>'
              f'<h3>{e(t["lgpl_title"])}</h3><p>{e(t["lgpl_text"])}</p>'
              f'<p><a data-download="sources" href="{e(SRC_URL)}">{e(t["lgpl_link"])}</a></p>')
    sections = [install,
                f'<p>{e(d["audioText"])}</p><p>{e(d["listenText"])} {e(d["saveText"])}</p>',
                f'<p>{e(d["osText"])}</p><p>{e(d["verified"])}</p>',
                f'<p>{e(d["removeText"])}</p>',
                f'<p>{e(d["helpText"])}</p><p class="ppk-notice">{e(d["limitation"])}</p>',
                rights]
    content = ''.join(f'<section id="{a}"><h2>{e(b)}</h2>{c}</section>' for a, b, c in zip(ids, names, sections))
    return (f'<main class="ppk-shell" id="content"><header class="yt-guide-head"><p class="yt-kicker">{e(d["guidePreview"])}</p>'
            f'<h1>{e(d["guideTitle"])}</h1><p class="ppk-lead">{e(d["guideLead"])}</p></header>'
            f'<div class="yt-guide-layout"><nav class="yt-toc" aria-label="{e(t["toc"])}">{toc}</nav>'
            f'<div class="yt-guide-content ppk-prose">{content}</div></div></main>')


def page(kind, l):
    title = 'YouTube Downloader' if kind in ('home', 'downloader') else T[l]['guide'] + ' — YouTube Downloader + PeakMP3'
    body = {'home': downloader, 'downloader': downloader, 'guide': guide}[kind](l)
    alt = ''.join(f'<link rel="alternate" hreflang="{x}" href="{path("guide" if kind == "guide" else "downloader" if kind == "downloader" else "home", x)}">' for x in LANGS)
    desc = D_JSON['description'][l]
    return (f'<!doctype html>\n<html lang="{l}">\n<head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">'
            f'<meta name="color-scheme" content="light dark"><meta name="description" content="{e(desc)}">'
            f'<meta name="application-version" content="{e(VERSION)}"><title>{e(title)}</title>'
            f'<link rel="icon" href="extension/icons/icon128.png">{head_assets()}{alt}</head>\n'
            f'<body class="ppk-page yt-site {kind}" data-lang="{l}" data-page="{kind}">\n{nav(kind, l)}\n{body}\n{footer(l)}\n</body>\n</html>\n')


def redirect_peak(l):
    dest = f'{PEAK_PAGES}index.html' if l == 'de' else f'{PEAK_PAGES}index-{l}.html'
    return (f'<!doctype html><html lang="{l}"><head><meta charset="utf-8"><script>location.replace("{dest}"+location.hash);</script>'
            f'<meta http-equiv="refresh" content="0;url={dest}">'
            f'<title>PeakMP3</title><link rel="canonical" href="{dest}"></head>'
            f'<body><a href="{dest}">PeakMP3</a></body></html>')


def redirect_de():
    return f'''<!doctype html>
<html lang="de">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<script>location.replace('index.html'+location.hash)</script>
<meta http-equiv="refresh" content="0; url=index.html">
<link rel="canonical" href="{PAGES}YouTubeDownloader/index.html">
<meta name="robots" content="noindex">
<title>YouTube Downloader — Weiterleitung</title>
</head>
<body>
<p>Diese Seite ist umgezogen: <a href="index.html">YouTube Downloader — Startseite</a></p>
<p><a href="index-en.html" lang="en" hreflang="en">English</a> · <a href="index-ru.html" lang="ru" hreflang="ru">Русский</a> · <a href="downloader-de.html" lang="de" hreflang="de">Downloader</a></p>
</body>
</html>
'''


def extension_demo(l):
    s = (R / 'extension/popup.html').read_text(encoding='utf-8-sig')
    stub = '''<script>window.chrome={i18n:{getUILanguage:()=>'%s'},runtime:{lastError:null,getURL:p=>new URL('../extension/'+p,location.href).href,sendMessage:(m,cb)=>setTimeout(()=>cb({ok:false}),50)},storage:{onChanged:{addListener:()=>{}},local:{get:(k,cb)=>queueMicrotask(()=>cb({[k]:{language:'%s',theme:'light'}})),set:(x,cb)=>queueMicrotask(cb)}},tabs:{query:(q,cb)=>cb([]),sendMessage:(i,m,cb)=>cb()}};</script>''' % (l, l)
    return s.replace('<head>', '<head><base href="../extension/">' + stub).replace('<body>', '<body style="width:auto;min-width:0">').replace('</head>', '<style>html{overflow:hidden}body{overflow:visible}</style></head>')


SITE_CSS = '''/* Page rules of the YouTube Downloader site. Tokens, header, footer and components come from assets/kit/kit.css. */
:root{--ppk-accent:#087f49;--ppk-accent-ink:#ffffff;--ppk-tint:#e9f4ed;--ppk-hero:#0c3b27;--ppk-hero-end:#1d7a52}
:root[data-theme="dark"]{--ppk-accent:#69df9e;--ppk-accent-ink:#04281a;--ppk-tint:#17301f;--ppk-hero:#0c3b27;--ppk-hero-end:#1d7a52}
@media (prefers-color-scheme:dark){:root:not([data-theme]){--ppk-accent:#69df9e;--ppk-accent-ink:#04281a;--ppk-tint:#17301f;--ppk-hero:#0c3b27;--ppk-hero-end:#1d7a52}}
.yt-meta{margin-left:6px}
.extension-frame{display:grid;place-items:center;padding:18px}
.extension-frame iframe{display:block;width:min(100%,380px);border:1px solid var(--ppk-line);border-radius:var(--ppk-radius-m);background:#fff}
.yt-features{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:16px;margin:28px 0}
.yt-features article{padding:22px;border:1px solid var(--ppk-line);border-radius:var(--ppk-radius-l);background:var(--ppk-panel)}
.yt-features h3{margin:8px 0}.yt-features p{margin:0;color:var(--ppk-muted)}
.yt-num{display:inline-block;color:var(--ppk-accent);font:700 14px var(--ppk-mono)}
.yt-split{display:grid;grid-template-columns:minmax(0,1fr) minmax(0,1fr);gap:28px;align-items:center;padding:28px;border:1px solid var(--ppk-line);border-radius:var(--ppk-radius-l);background:var(--ppk-panel);margin-top:20px}
.yt-split h2{margin-top:0}.yt-split p{color:var(--ppk-muted)}
.yt-formats{align-content:center}
.yt-package{margin-top:20px;padding:28px;border:1px solid var(--ppk-line);border-radius:var(--ppk-radius-l);background:var(--ppk-panel)}
.yt-package h2{margin-top:0}.yt-package h3{margin:24px 0 8px}
.yt-table{max-width:100%;overflow-x:auto;margin:12px 0}
.yt-table table{border-collapse:collapse;width:100%}
.yt-table th,.yt-table td{padding:10px 12px;border:1px solid var(--ppk-line);text-align:left;vertical-align:top}
.yt-table thead th{background:var(--ppk-tint)}
.ppk-notice{margin:20px 0 0;padding:12px 14px;border-radius:var(--ppk-radius-m);background:var(--ppk-tint);color:var(--ppk-ink);font-size:15px}
.yt-guide-head{padding:36px 0 12px}.yt-guide-head h1{margin:6px 0 12px}
.yt-kicker{margin:0;color:var(--ppk-accent);font-weight:700;font-size:14px;letter-spacing:.04em}
.yt-guide-layout{display:grid;grid-template-columns:230px minmax(0,1fr);gap:32px;align-items:start;margin:12px 0 40px}
.yt-toc{position:sticky;top:96px;display:grid;gap:4px;padding:12px;border:1px solid var(--ppk-line);border-radius:var(--ppk-radius-m);background:var(--ppk-panel)}
.yt-toc a{padding:8px 10px;border-radius:var(--ppk-radius-s);color:var(--ppk-ink);text-decoration:none;font-size:15px}
.yt-toc a:hover{background:var(--ppk-tint)}
.yt-guide-content section{margin:0 0 20px;padding:24px 28px;border:1px solid var(--ppk-line);border-radius:var(--ppk-radius-l);background:var(--ppk-panel)}
.yt-guide-content h2{margin-top:0}
.yt-guide-content code{overflow-wrap:anywhere}
.download-links{display:flex;flex-wrap:wrap;gap:8px 18px;margin-top:12px}
.yt-site img{max-width:100%;height:auto}
.yt-site .ppk-flow{grid-template-columns:repeat(4,minmax(0,1fr))}
@media (max-width:900px){.yt-site .ppk-flow{grid-template-columns:repeat(2,minmax(0,1fr))}}
@media (max-width:520px){.yt-site .ppk-flow{grid-template-columns:1fr}}
@media (max-width:900px){.yt-guide-layout{grid-template-columns:1fr}.yt-toc{position:static;grid-template-columns:repeat(2,minmax(0,1fr))}}
@media (max-width:760px){.yt-features{grid-template-columns:1fr}.yt-split{grid-template-columns:1fr;padding:22px}.yt-package{padding:22px}.yt-guide-content section{padding:20px}}
@media (max-width:420px){.yt-toc{grid-template-columns:1fr}}
'''

SITE_JS = '''(() => {
  // Local preview: download buttons point at files in dist/ (names are taken from the release links).
  if (['localhost', '127.0.0.1'].includes(location.hostname)) {
    document.querySelectorAll('[data-download]').forEach(a => {
      a.href = 'dist/' + decodeURIComponent(a.getAttribute('href').split('/').pop());
    });
  }
  // Fit the real popup preview to its contents, including expanded settings.
  document.querySelectorAll('.extension-frame iframe').forEach(frame => {
    frame.addEventListener('load', () => {
      const body = frame.contentDocument && frame.contentDocument.body;
      if (!body) return;
      const resize = () => { frame.style.height = Math.ceil(body.getBoundingClientRect().height + 2) + 'px'; };
      new ResizeObserver(resize).observe(body);
      resize();
    });
  });
})();
'''


def main():
    for l in LANGS:
        for kind in ('home', 'downloader', 'guide'):
            (R / path(kind, l)).write_text(page(kind, l), encoding='utf-8', newline='\n')
        (R / path('peakmp3', l)).write_text(redirect_peak(l), encoding='utf-8', newline='\n')
        guide_copy = re.sub(r'(href|src)="((?:assets/|website/|extension/|index|Guide-|downloader-|peakmp3-)[^"]*)"', r'\1="../\2"',
                            (R / path('guide', l)).read_text(encoding='utf-8'))
        (R / 'docs' / path('guide', l)).write_text(guide_copy, encoding='utf-8', newline='\n')
        (W / ('extension-' + l + '.html')).write_text(extension_demo(l), encoding='utf-8', newline='\n')
    (R / 'index-de.html').write_text(redirect_de(), encoding='utf-8', newline='\n')
    (W / 'site.css').write_text(SITE_CSS, encoding='utf-8', newline='\n')
    (W / 'site.js').write_text(SITE_JS, encoding='utf-8', newline='\n')
    print('Built localized pages on the shared kit and 3 real extension UI previews')


if __name__ == '__main__':
    main()
