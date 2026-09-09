<p align="center">
  <img src="https://i.imgur.com/t7o8IS7.png" width=500>
</p>

<h3 align="center">A customizable Windows alarm clock built for people who want more than a basic clock app.</h3>

<p align="center">
  <strong>Accurate time • Flexible alarms • Custom audio • YouTube • World clocks • Themes • Audio recording • Windows integration</strong>
</p>

<p align="center">
  <img src="https://i.imgur.com/nKCyvBA.png">
</p>

---

## Overview

**Sec's See Time** is a feature-rich Windows desktop alarm clock built with **C# and WPF on .NET 10**.

The goal is simple: make a desktop clock that feels like an actual application rather than a tiny utility that happens to display the time.

The main clock is designed around a clean, dark, customizable interface with live visual effects, configurable typography, world clocks, an integrated alarm system, custom sound support, and Windows integration.

Whether you want a loud digital alarm, a gentle wake-up sound, a recording of your own voice, a local music/audio file, or a YouTube video, Sec's See Time is designed to give you control over how your alarms behave.

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

<p align="center">
  <img src="https://i.imgur.com/J3jROPR.gif">
</p>

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

---

## YouTube Alarm Support

<p align="center">
  <img src="https://i.imgur.com/hgBCUzY.gif">
</p>

You can use a YouTube video as an alarm source.

Enter a supported YouTube URL when creating an alarm and Sec's See Time will attempt to play it through the application's integrated player.

### Important notes

- A network connection is required when the alarm actually plays.
- YouTube alarm controls are unavailable when the application detects that there is no network connection.
- YouTube playback is handled through the application's embedded WebView2 player.
- YouTube URLs are validated and video IDs are extracted before playback.

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

---

# Appearance Studio

<p align="center">
  <img src="https://i.imgur.com/Go4Veye.gif">
</p>

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

---

## Theme Import / Export

Custom themes can be exported as:

```text
.sstheme.json
```

They can then be imported into another installation of Sec's See Time.

This makes it possible to create a custom appearance once and share it with another user.

---

# Windows Integration

<p align="center">
  <img src="https://i.imgur.com/WhlrO0J.gif">
</p>

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

---

# Alarm Screen

<p align="center">
  <img src="https://i.imgur.com/01trB6g.gif">
</p>

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

---

# Missed Alarms

Sec's See Time includes protection against stale alarms firing after a computer has been asleep or unavailable for an extended period.

The **Missed Alarm Grace Period** determines how late an alarm can be and still trigger.

For example, if an alarm was scheduled for 7:00 AM and the computer was unavailable until noon, the application should not suddenly blast the alarm simply because the machine became available.

The grace period can be configured from Settings.

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

<p align="center">
  <strong>Sec's See Time</strong><br>
  <em>The Windows Master Clock</em>
</p>

<p align="center">
  Built for Windows with C# and WPF.
</p>
