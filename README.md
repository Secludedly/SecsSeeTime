# Sec's See Time (Images and formatting to be added later)

<p align="center">
  <img src="" alt="Sec's See Time logo" width="180">
</p>

<h3 align="center">A customizable Windows alarm clock built for people who want more than a basic clock app.</h3>

<p align="center">
  <strong>Accurate time • Flexible alarms • Custom audio • YouTube • World clocks • Themes • Audio recording • Windows integration</strong>
</p>

<p align="center">
  <img src="" alt="Sec's See Time main clock screenshot">
</p>

---

## Overview

**Sec's See Time** is a feature-rich Windows desktop alarm clock built with **C# and WPF on .NET 10**.

The goal is simple: make a desktop clock that feels like an actual application rather than a tiny utility that happens to display the time.

The main clock is designed around a clean, dark, customizable interface with live visual effects, configurable typography, world clocks, an integrated alarm system, custom sound support, and Windows integration.

Whether you want a loud digital alarm, a gentle wake-up sound, a recording of your own voice, a local music/audio file, or a YouTube video, Sec's See Time is designed to give you control over how your alarms behave.

> **Current application version:** `0.8.0`

---

## Features

### Clock

- Live desktop clock with high-frequency time updates.
- Optional seconds display.
- 12-hour or 24-hour time formatting.
- Current date display.
- Local time-zone identification.
- Consistent time formatting throughout the application.
- Next-alarm information on the main interface.
- Taskbar tooltip/status information.
- Animated visual background effects.
- Optional particles, scanlines, noise, glow, and other visual styling.

### Alarm system

Create alarms with detailed behavior controls:

- Custom alarm names.
- Specific time of day.
- One-time alarms.
- Daily alarms.
- Weekday alarms.
- Weekend alarms.
- Custom day selection.
- Enable/disable alarms without deleting them.
- Individual alarm volume.
- Configurable sound duration.
- Configurable silence duration.
- Repeatable sound/silence cycles.
- Snooze support.
- Configurable snooze duration.
- Missed-alarm grace period.

Alarms continue to be managed by the background alarm engine while the main window is minimized to the tray.

---

## Alarm Sound System

Sec's See Time includes a built-in sound library as well as support for importing your own audio.

### Built-in sounds

Built-in sounds are generated from sound recipes rather than shipped as ordinary audio files. This keeps the application self-contained while allowing synthesized sounds to be cached locally when needed.

Built-in categories include:

- **Alarms**
- **Chimes**
- **Electronic**
- **Ambient**

Examples include:

- Classic Alarm
- Digital Beep
- Electronic Pulse
- Rapid Beep
- Siren
- Klaxon
- Air Horn
- Morning Bells
- Gentle Wake
- Music Box
- And additional synthesized sounds in the built-in catalog.

<p align="center">
  <img src="" alt="Sound Library screenshot">
</p>

### Supported imported audio

The Sound Library supports:

- `.mp3`
- `.wav`
- `.m4a`
- `.aac`
- `.flac`
- `.ogg`
- `.opus`
- `.wma`
- `.aiff`
- `.aif`

Imported sounds can be added to the application's library so they can be reused by multiple alarms.

---

## Record Your Own Alarm

Sec's See Time includes a built-in audio recorder.

You can:

1. Select an available recording device.
2. Monitor the input level.
3. Record a voice memo or other audio.
4. Preview the recording.
5. Name the recording.
6. Add it to your sound library.
7. Use it as an alarm sound.

Recordings are saved as WAV files.

Recording supports a maximum take length of **10 minutes**.

<p align="center">
  <img src="" alt="Audio recording screenshot">
</p>

### Example recording workflow

```text
Select microphone
      ↓
Start recording
      ↓
Monitor input level
      ↓
Stop recording
      ↓
Preview
      ↓
Name recording
      ↓
Add to Sound Library
      ↓
Use with an alarm
```

---

## YouTube Alarm Support

You can use a YouTube video as an alarm source.

Enter a supported YouTube URL when creating an alarm and Sec's See Time will attempt to play it through the application's integrated player.

### Important notes

- A network connection is required when the alarm actually plays.
- YouTube alarm controls are unavailable when the application detects that there is no network connection.
- YouTube playback is handled through the application's embedded WebView2 player.
- YouTube URLs are validated and video IDs are extracted before playback.

<p align="center">
  <img src="" alt="YouTube alarm screenshot">
</p>

<p align="center">
  <img src="" alt="YouTube alarm GIF demonstration">
</p>

---

## Alarm Sound / Silence Cycle

One of the defining features of Sec's See Time is its configurable alarm cycle.

Instead of simply playing a sound continuously until somebody wakes up and stops it, an alarm can alternate between **sound** and **silence**.

For example:

```text
┌───────────────┐
│ SOUND         │  2 minutes
├───────────────┤
│ SILENCE       │  2 minutes
├───────────────┤
│ SOUND         │  2 minutes
├───────────────┤
│ SILENCE       │  2 minutes
├───────────────┤
│       ...     │
└───────────────┘
```

Both the sound and silence periods can be configured independently.

Available durations include:

- 30 seconds
- 1 minute
- 2 minutes
- 3 minutes
- 5 minutes
- 10 minutes

The cycle can continue until the alarm is dismissed.

---

## Snooze

Alarms can optionally use snooze.

Available preset durations include:

- 1 minute
- 5 minutes
- 10 minutes
- 15 minutes
- 20 minutes
- 30 minutes
- 45 minutes
- 60 minutes

A custom snooze duration can also be configured.

The ringing window provides the controls needed to snooze or stop the current alarm.

---

## World Clock

The World Clock allows multiple cities/time zones to be displayed alongside the main clock.

Features include:

- Searchable time-zone catalog.
- City/time-zone selection.
- Multiple saved clocks.
- Live time updates.
- DST-aware time-zone calculations through Windows' `TimeZoneInfo`.
- Favorite cities.
- Reordering.
- Removing saved cities.
- Optional date display.
- Configurable World Clock typography.
- Configurable World Clock placement.

<p align="center">
  <img src="" alt="World Clock screenshot">
</p>

---

# Appearance Studio

Sec's See Time is designed to be heavily customizable.

The Appearance Studio controls the visual identity of the clock rather than simply changing a single accent color.

## Included themes

The application ships with four preset themes:

### Midnight

The original Sec's See Time appearance.

> Deep violet, soft glow. The original.

### Neon

A high-contrast cyan-on-black appearance.

> Hard cyan on black. Loud and awake.

### Ember

A warm amber theme intended to be easier on the eyes in dark environments.

> Warm amber. Easier on the eyes at 3am.

### Minimal

A stripped-down appearance focused almost entirely on the time.

> No glow, no particles. Just the time.

<p align="center">
  <img src="" alt="Theme selection screenshot">
</p>

---

## Custom Themes

Appearance Studio can create and save custom themes.

Customizable elements include:

### Clock

- Font family.
- Font size.
- Font weight.
- Italic styling.
- Letter spacing.
- Opacity.
- Glow.
- Glow strength.
- Outline.
- Outline thickness.
- Outline color.
- Clock/date visibility.
- Seconds visibility.

### Colors

- Window background.
- Panel background.
- Secondary panel background.
- Panel border.
- Gradient start/middle/end.
- Primary text.
- Secondary text.
- Muted text.
- Accent.
- Accent light.
- Success.
- Danger.

### Background

- Custom background image.
- Image opacity.
- Image stretch mode.
- Background overlay color.
- Background overlay opacity.

### Visual effects

- Scanlines.
- Scanline opacity.
- Noise.
- Particles.
- Particle count.

### World Clock

- Enable/disable.
- Position.
- Font size.
- Date visibility.
- Time-zone visibility.
- Clock font.
- Opacity.

### Alarm screen

Separate alarm-screen glow and playing/silence accent colors can also be customized.

---

## Theme Import / Export

Custom themes can be exported as:

```text
.sstheme.json
```

They can then be imported into another installation of Sec's See Time.

This makes it possible to create a custom appearance once and share it with another user.

<p align="center">
  <img src="" alt="Appearance Studio screenshot">
</p>

<p align="center">
  <img src="" alt="Appearance Studio GIF demonstration">
</p>

---

# Windows Integration

Sec's See Time is designed to behave like a normal Windows desktop application even when the main window isn't visible.

## System Tray

The application can run from the Windows notification area.

The tray menu provides quick access to:

- Show Clock
- Manage Alarms
- World Clock
- Appearance Studio
- Start with Windows
- Minimize to Tray
- Exit

Left-clicking the tray icon returns the clock window.

<p align="center">
  <img src="" alt="System tray screenshot">
</p>

---

## Start with Windows

The application can register itself to launch when the current Windows user signs in.

When launched through the startup entry, Sec's See Time can automatically minimize to the system tray.

This allows alarms to be armed without requiring the main clock window to remain visible.

---

## Minimize to Tray

When enabled, minimizing the main window sends Sec's See Time to the notification area instead of leaving an ordinary taskbar window open.

The alarm engine continues running in the background.

The setting can be disabled if you prefer the normal Windows minimize/close behavior.

---

## Windows Notifications

When enabled, Sec's See Time can display a Windows notification through its notification-area integration when an alarm begins ringing.

Notifications are informational only; the alarm sound and alarm window operate independently.

---

# Alarm Screen

When an alarm triggers, Sec's See Time presents a dedicated ringing interface.

The alarm screen can show:

- Alarm name.
- Current alarm state.
- Sound/silence phase.
- Cycle information.
- Remaining time.
- Progress.
- Snooze controls.
- Stop/dismiss controls.
- Theme-specific visual effects.
- YouTube playback when the alarm source is YouTube.

<p align="center">
  <img src="" alt="Alarm ringing screen screenshot">
</p>

<p align="center">
  <img src="" alt="Alarm ringing screen GIF demonstration">
</p>

---

# Missed Alarms

Sec's See Time includes protection against stale alarms firing after a computer has been asleep or unavailable for an extended period.

The **Missed Alarm Grace Period** determines how late an alarm can be and still trigger.

For example, if an alarm was scheduled for 7:00 AM and the computer was unavailable until noon, the application should not suddenly blast the alarm simply because the machine became available.

The grace period can be configured from Settings.

---

# Settings

The Settings window contains several application-wide controls.

## Windows Integration

- Start with Windows.
- Minimize to tray.
- Show Windows notifications.
- Test notification.

## Clock

- Global 12/24-hour format.

The selected format is used throughout the application, including:

- Main clock.
- World Clock.
- Alarm times.
- Alarm screen.
- Next-alarm display.
- Taskbar tooltip.

## Alarm Behavior

- Missed alarm grace period.

## Data

- Open the application's data folder.
- Reset preferences.

Resetting preferences does **not** remove alarms, saved themes, or World Clock cities.

---

# Data Storage

Sec's See Time stores user data under the Windows Local Application Data directory:

```text
%LOCALAPPDATA%\SecsSeeTime
```

The application uses this location for persistent application data such as:

```text
%LOCALAPPDATA%\SecsSeeTime
│
├── alarms.json
├── settings.json
├── Sounds\
├── Library\
└── custom theme data
```

The exact contents can change as the application evolves.

### Imported sounds

Imported library audio is copied into the application's user data area when the user chooses to add a file to the library.

This prevents an alarm from breaking simply because the original source file was moved.

### Built-in sound cache

Built-in synthesized sounds are rendered to WAV and cached locally as needed.

---

# Technology

Sec's See Time is built using the following technologies:

| Technology | Purpose |
|---|---|
| **C#** | Application language |
| **.NET 10** | Application runtime/framework |
| **WPF** | Windows desktop UI |
| **XAML** | UI layout and styling |
| **NAudio** | Audio recording/playback support |
| **LibVLCSharp.WPF** | Media playback integration |
| **LibVLC** | Media playback backend |
| **Microsoft WebView2** | Embedded YouTube/browser playback |
| **Windows Forms NotifyIcon** | System tray integration |
| **Windows Registry** | Windows startup registration |
| **System.TimeZoneInfo** | Time-zone/DST calculations |
| **System.Text.Json** | Persistent JSON data |

---

# Project Structure

The project is organized into several areas:

```text
SecSeeTime/
│
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── SecSeeTime.csproj
│
├── Assets/
│   ├── Fonts/
│   ├── Images/
│   ├── Sounds/
│   └── icon.ico
│
├── Controls/
│   ├── LetterSpacedTextBlock.cs
│   ├── YouTubePlayerControl.xaml
│   └── YouTubePlayerControl.xaml.cs
│
├── Enums/
│   ├── AlarmPhase.cs
│   ├── AlarmRepeatMode.cs
│   ├── AlarmSoundType.cs
│   ├── SoundCategory.cs
│   ├── SoundKind.cs
│   ├── TimeFormatMode.cs
│   └── WaveShape.cs
│
├── Helpers/
│   ├── TimeHelper.cs
│   └── WindowHelper.cs
│
├── Models/
│   ├── Alarm.cs
│   ├── AlarmSessionState.cs
│   ├── AlarmSettings.cs
│   ├── AlarmTheme.cs
│   ├── AlarmTriggeredEventArgs.cs
│   ├── SoundDefinition.cs
│   ├── SoundRecipe.cs
│   └── WorldClockEntry.cs
│
├── Services/
│   ├── AlarmPlaybackService.cs
│   ├── AlarmService.cs
│   ├── AlarmSources.cs
│   ├── AudioPreviewService.cs
│   ├── AudioRecordingService.cs
│   ├── AudioService.cs
│   ├── BuiltInSounds.cs
│   ├── NotificationService.cs
│   ├── SoundLibraryService.cs
│   ├── StartupService.cs
│   ├── StorageService.cs
│   ├── ThemePersistenceService.cs
│   ├── ThemeService.cs
│   ├── TimeFormatService.cs
│   ├── TimeService.cs
│   ├── ToneSynthesizer.cs
│   ├── TrayService.cs
│   └── YouTubeService.cs
│
├── Themes/
│   ├── Palette.xaml
│   └── ThemePresets.cs
│
├── ViewModels/
│   └── WorldClockTile.cs
│
└── Views/
    ├── AlarmManagerWindow.*
    ├── AlarmWindow.*
    ├── AppearanceStudioWindow.*
    ├── RecordAudioWindow.*
    ├── SettingsWindow.*
    ├── SoundLibraryWindow.*
    ├── ThemePickerWindow.*
    └── WorldClockWindow.*
```

---

# Building From Source

## Requirements

For development, you will need:

- Windows 10 or Windows 11.
- Visual Studio 2026.
- .NET 10 SDK.
- A Windows desktop development environment with WPF support.
- Internet access for restoring NuGet packages and using YouTube alarms.

The project targets:

```text
net10.0-windows
```

The primary release target is:

```text
win-x64
```

---

## Clone the Repository

```bash
git clone https://github.com/Secludedly/SecsSeeTime.git
cd SecsSeeTime
```

Then open:

```text
SecSeeTime.slnx
```

in Visual Studio.

---

## Restore Dependencies

Visual Studio should restore the project's NuGet dependencies automatically.

The project currently uses packages including:

```xml
LibVLCSharp.WPF
NAudio
VideoLAN.LibVLC.Windows
Microsoft.Web.WebView2
```

---

## Build

For a normal development build:

```bash
dotnet build
```

For a Release build:

```bash
dotnet build --configuration Release
```

---

# Publishing

The application is designed to be published for 64-bit Windows.

The included publish profile targets:

```text
Release
win-x64
```

The publish output is configured for a single-file deployment.

For a self-contained release intended for users who may not already have the required .NET runtime installed, use a self-contained publish configuration.

Example:

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

For production distribution, the resulting published application can be wrapped in a Windows installer such as an MSI or EXE installer.

> **Note:** WebView2 availability on the target machine can still matter for embedded web content. Windows 10/11 systems commonly have the WebView2 Runtime installed, but a production installer should account for machines where it is missing.

---

# Distribution

The GitHub repository contains the source code for Sec's See Time.

A production Windows release can be distributed as an installer such as:

```text
SecSeeTime-Setup-1.0.0.exe
```

A typical installer should install the application under:

```text
C:\Program Files\Sec's See Time\
```

and create Start Menu/Desktop shortcuts as desired.

---

# Design Philosophy

Sec's See Time was built around a few simple ideas:

### The clock should feel like an application.

A clock does not have to look like a calculator display floating in the corner of a desktop.

### Alarms should be flexible.

Different people wake up differently. Some want a gentle chime. Some need an air horn from the depths of hell.

### Customization should go deeper than a color picker.

Themes can affect typography, gradients, particles, glow, backgrounds, world clocks, and the alarm screen.

### The application should stay useful in the background.

The tray, startup integration, background alarm engine, and notifications allow the main window to disappear without turning the application off.

### User audio should be first-class.

If the built-in sounds aren't enough, bring your own audio or record something directly inside the application.

---

# Screenshots & GIFs

The following placeholders are intentionally left with empty image URLs so repository maintainers can add their own screenshots/GIFs later.

## Main Clock

![Main Clock]( )

## Alarm Manager

![Alarm Manager]( )

## Alarm Ringing Screen

![Alarm Screen]( )

## Sound Library

![Sound Library]( )

## Audio Recorder

![Audio Recorder]( )

## World Clock

![World Clock]( )

## Theme Picker

![Theme Picker]( )

## Appearance Studio

![Appearance Studio]( )

## System Tray

![System Tray]( )

## Alarm Creation Demo

![Alarm Creation Demo GIF]( )

## Theme Customization Demo

![Theme Customization GIF]( )

## Alarm Playback Demo

![Alarm Playback GIF]( )

---

# Known Considerations

### YouTube requires an internet connection

YouTube alarm sources cannot function normally without network access.

### Local audio depends on the source file

If an alarm points directly at a local file rather than copying it into the Sec's See Time library, moving or deleting that file can make the alarm unavailable.

Using **Add to Library** is recommended for audio you intend to keep.

### WebView2

The embedded YouTube player uses Microsoft Edge WebView2.

### Windows startup

The Start with Windows feature uses the current user's Windows Registry `Run` entry. It does not require administrator privileges.

### System tray behavior

Minimize-to-tray keeps the application process alive. Use **Exit** from the tray menu when you actually want to terminate Sec's See Time.

---

# Troubleshooting

## The application does not play an imported sound

Check that:

1. The file still exists.
2. The format is supported.
3. The file can be opened by another media player.
4. The audio device is functioning.
5. The alarm volume is above zero.

For long-term reliability, add the audio file to the Sec's See Time Sound Library.

---

## YouTube alarms are unavailable

Check:

1. Internet connectivity.
2. The YouTube URL.
3. Whether WebView2 is installed and functioning.
4. Whether the video is available for playback.

---

## The application does not start with Windows

Open:

**Settings → Windows Integration**

and verify:

```text
Start with Windows
```

is enabled.

If necessary, disable it and enable it again to recreate the startup registration.

---

## An alarm fires after the computer was asleep

Check:

**Settings → Alarm Behavior → Missed alarm grace period**

Increase or decrease the grace period depending on how you want missed alarms handled.

---

# Contributing

Contributions, bug reports, feature ideas, and improvements are welcome.

Before submitting a pull request:

1. Keep changes focused.
2. Preserve existing application behavior unless the change intentionally modifies it.
3. Test alarm scheduling and playback.
4. Test both normal and minimized-to-tray operation when modifying Windows integration.
5. Test imported audio when modifying sound-library functionality.
6. Test theme persistence when modifying Appearance Studio.
7. Test Release builds before submitting changes intended for distribution.

---

# Bug Reports

When reporting a bug, please include:

- Windows version.
- Sec's See Time version.
- Whether the app was built from source or installed from a release.
- Steps to reproduce the issue.
- Expected behavior.
- Actual behavior.
- Relevant error messages.
- Whether the problem occurs after restarting the application.
- Any relevant files or logs that can be safely shared.

---

# Roadmap

Potential future improvements may include:

- Additional built-in sound recipes.
- Additional theme presets.
- More advanced alarm scheduling.
- Additional media sources.
- More Windows integration.
- Further customization of the alarm screen.
- Additional visualization effects.
- Improved distribution and installer tooling.

---

# License

No open-source license is currently specified in the project repository.

If you intend to permit others to modify, redistribute, or reuse the project, add an appropriate `LICENSE` file to the repository and replace this section with the applicable license information.

---

# Credits & Technologies

Sec's See Time is built with open-source and Microsoft technologies including:

- C#
- .NET
- WPF
- NAudio
- LibVLCSharp
- LibVLC
- WebView2

Please see the project's NuGet package references for the exact versions used by the current source release.

---

# Repository

**GitHub:**

https://github.com/Secludedly/SecsSeeTime

---

<p align="center">
  <strong>Sec's See Time</strong><br>
  <em>The Windows Master Clock</em>
</p>

<p align="center">
  Built for Windows with C# and WPF.
</p>
