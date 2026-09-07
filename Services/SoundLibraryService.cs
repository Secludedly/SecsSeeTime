using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SecSeeTime.Enums;
using SecSeeTime.Models;

namespace SecSeeTime.Services
{
    /// <summary>
    /// The sound library.
    ///
    /// Holds the free built-in catalog plus anything the user has
    /// imported, and resolves either one to a playable file on disk.
    /// Built-ins are synthesized on first use and cached per volume,
    /// because the WAV player they run through has no volume control.
    /// </summary>
    public sealed class SoundLibraryService
    {
        private static readonly JsonSerializerOptions JsonOptions =
            new() { WriteIndented = true };

        private static readonly string[] SupportedExtensions =
        {
            ".mp3", ".wav", ".m4a", ".aac", ".flac",
            ".ogg", ".opus", ".wma", ".aiff", ".aif"
        };

        private readonly string _root;
        private readonly string _cacheFolder;
        private readonly string _importFolder;
        private readonly string _indexPath;

        private readonly List<SoundDefinition> _userSounds = new();

        private readonly object _sync = new();


        public SoundLibraryService(string? root = null)
        {
            _root = root ?? Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "SecsSeeTime");

            _cacheFolder = Path.Combine(_root, "Sounds");
            _importFolder = Path.Combine(_root, "Library");
            _indexPath = Path.Combine(_importFolder, "library.json");

            LoadUserSounds();

            PruneStaleCache();
        }


        // =============================================================
        // EVENTS
        // =============================================================

        public event EventHandler? LibraryChanged;


        // =============================================================
        // CATALOG
        // =============================================================

        public IReadOnlyList<SoundDefinition> BuiltIn =>
            BuiltInSounds.All;


        public IReadOnlyList<SoundDefinition> UserSounds
        {
            get
            {
                lock (_sync)
                {
                    return _userSounds.ToList();
                }
            }
        }


        public IReadOnlyList<SoundDefinition> All =>
            BuiltInSounds.All.Concat(UserSounds).ToList();


        /// <summary>Categories that currently contain something.</summary>
        public IReadOnlyList<SoundCategory> Categories
        {
            get
            {
                List<SoundCategory> categories = new()
                {
                    SoundCategory.Alarms,
                    SoundCategory.Chimes,
                    SoundCategory.Electronic,
                    SoundCategory.Ambient
                };

                if (UserSounds.Count > 0)
                    categories.Add(SoundCategory.MySounds);

                return categories;
            }
        }


        public IReadOnlyList<SoundDefinition> InCategory(SoundCategory category)
        {
            return All
                .Where(sound => sound.Category == category)
                .ToList();
        }


        public SoundDefinition Default =>
            FindByName(BuiltInSounds.DefaultSoundName)
            ?? BuiltInSounds.All[0];


        public SoundDefinition? FindByName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            return All.FirstOrDefault(
                sound => string.Equals(
                    sound.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase));
        }


        public SoundDefinition? FindById(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return All.FirstOrDefault(
                sound => string.Equals(
                    sound.Id,
                    id,
                    StringComparison.Ordinal));
        }


        /// <summary>
        /// Resolves the entry an alarm refers to, falling back to the
        /// default if the alarm names a sound that no longer exists.
        /// </summary>
        public SoundDefinition Resolve(Alarm alarm)
        {
            if (alarm.SoundType == AlarmSoundType.LocalFile &&
                !string.IsNullOrWhiteSpace(alarm.LocalAudioPath))
            {
                SoundDefinition? imported =
                    UserSounds.FirstOrDefault(
                        sound => string.Equals(
                            sound.FilePath,
                            alarm.LocalAudioPath,
                            StringComparison.OrdinalIgnoreCase));

                if (imported != null)
                    return imported;

                return new SoundDefinition
                {
                    Id = alarm.LocalAudioPath!,
                    Name = Path.GetFileNameWithoutExtension(alarm.LocalAudioPath)!,
                    Category = SoundCategory.MySounds,
                    Description = alarm.LocalAudioPath!,
                    Kind = SoundKind.UserFile,
                    FilePath = alarm.LocalAudioPath
                };
            }

            return FindByName(alarm.BuiltInSoundName) ?? Default;
        }


        // =============================================================
        // PLAYABLE FILES
        // =============================================================

        /// <summary>
        /// Returns a file the audio engine can play, rendering and
        /// caching a built-in sound if this is the first time it has
        /// been asked for at this volume.
        /// </summary>
        public string? GetPlayableFile(
            SoundDefinition sound,
            double volume)
        {
            if (sound.Kind == SoundKind.UserFile)
            {
                return File.Exists(sound.FilePath)
                    ? sound.FilePath
                    : null;
            }

            if (sound.Recipe == null)
                return null;

            // Ten percent steps: the volume slider snaps to those, and
            // it keeps the cache to eleven files per sound at worst.
            int level = Math.Clamp((int)Math.Round(volume * 10), 0, 10);

            string path = Path.Combine(
                _cacheFolder,
                $"{Sanitize(sound.Name)}_r{BuiltInSounds.RecipeVersion}_v{level:00}.wav");

            lock (_sync)
            {
                if (File.Exists(path))
                    return path;

                try
                {
                    Directory.CreateDirectory(_cacheFolder);

                    ToneSynthesizer.RenderToWav(
                        path,
                        sound.Recipe,
                        level / 10.0);

                    return path;
                }
                catch
                {
                    // A sound that will not render must not stop the
                    // alarm engine from trying the next thing.
                    return null;
                }
            }
        }


        // =============================================================
        // IMPORT
        // =============================================================

        public static bool IsSupportedFile(string path)
        {
            string extension =
                Path.GetExtension(path).ToLowerInvariant();

            return Array.IndexOf(SupportedExtensions, extension) >= 0;
        }


        public static string SupportedExtensionsLabel =>
            "MP3 • WAV • M4A • AAC • FLAC • OGG • OPUS • WMA • AIFF";


        /// <summary>
        /// Copies an audio file into the library so the alarm keeps
        /// working after the original is moved, renamed or deleted.
        /// </summary>
        public SoundDefinition? Import(
            string sourcePath,
            out string error)
        {
            error = "";

            try
            {
                if (!File.Exists(sourcePath))
                {
                    error = "That file could not be found.";
                    return null;
                }

                if (!IsSupportedFile(sourcePath))
                {
                    error =
                        "That file type is not supported.\n\n" +
                        "Supported: " + SupportedExtensionsLabel;

                    return null;
                }

                Directory.CreateDirectory(_importFolder);

                string extension = Path.GetExtension(sourcePath);

                string id = Guid.NewGuid().ToString("N");

                string destination =
                    Path.Combine(_importFolder, id + extension);

                File.Copy(sourcePath, destination, overwrite: false);

                SoundDefinition definition =
                    new SoundDefinition
                    {
                        Id = id,
                        Name = UniqueName(
                            Path.GetFileNameWithoutExtension(sourcePath)),
                        Category = SoundCategory.MySounds,
                        Description =
                            $"Imported {DateTime.Now:d} • " +
                            extension.TrimStart('.').ToUpperInvariant(),
                        Kind = SoundKind.UserFile,
                        FilePath = destination
                    };

                lock (_sync)
                {
                    _userSounds.Add(definition);
                }

                SaveUserSounds();

                LibraryChanged?.Invoke(this, EventArgs.Empty);

                return definition;
            }
            catch (Exception ex)
            {
                error = "The sound could not be imported.\n\n" + ex.Message;

                return null;
            }
        }


        public bool Remove(SoundDefinition sound)
        {
            if (sound.Kind != SoundKind.UserFile)
                return false;

            lock (_sync)
            {
                SoundDefinition? existing =
                    _userSounds.FirstOrDefault(
                        item => item.Id == sound.Id);

                if (existing == null)
                    return false;

                _userSounds.Remove(existing);
            }

            try
            {
                if (File.Exists(sound.FilePath))
                    File.Delete(sound.FilePath!);
            }
            catch
            {
                // The index entry is gone either way; a stray file is
                // not worth failing the operation over.
            }

            SaveUserSounds();

            LibraryChanged?.Invoke(this, EventArgs.Empty);

            return true;
        }


        public bool Rename(SoundDefinition sound, string newName)
        {
            if (sound.Kind != SoundKind.UserFile ||
                string.IsNullOrWhiteSpace(newName))
            {
                return false;
            }

            lock (_sync)
            {
                int index = _userSounds.FindIndex(
                    item => item.Id == sound.Id);

                if (index < 0)
                    return false;

                _userSounds[index] = new SoundDefinition
                {
                    Id = sound.Id,
                    Name = newName.Trim(),
                    Category = SoundCategory.MySounds,
                    Description = sound.Description,
                    Kind = SoundKind.UserFile,
                    FilePath = sound.FilePath
                };
            }

            SaveUserSounds();

            LibraryChanged?.Invoke(this, EventArgs.Empty);

            return true;
        }


        private string UniqueName(string? candidate)
        {
            string name =
                string.IsNullOrWhiteSpace(candidate)
                    ? "Imported Sound"
                    : candidate.Trim();

            if (FindByName(name) == null)
                return name;

            for (int suffix = 2; suffix < 500; suffix++)
            {
                string attempt = $"{name} ({suffix})";

                if (FindByName(attempt) == null)
                    return attempt;
            }

            return $"{name} ({Guid.NewGuid():N})";
        }


        // =============================================================
        // PERSISTENCE
        // =============================================================

        private void LoadUserSounds()
        {
            try
            {
                if (!File.Exists(_indexPath))
                    return;

                List<ImportRecord>? records =
                    JsonSerializer.Deserialize<List<ImportRecord>>(
                        File.ReadAllText(_indexPath),
                        JsonOptions);

                if (records == null)
                    return;

                foreach (ImportRecord record in records)
                {
                    string path =
                        Path.Combine(_importFolder, record.FileName);

                    // Skip entries whose audio has gone missing rather
                    // than offering the user a sound that cannot play.
                    if (!File.Exists(path))
                        continue;

                    _userSounds.Add(
                        new SoundDefinition
                        {
                            Id = record.Id,
                            Name = record.Name,
                            Category = SoundCategory.MySounds,
                            Description = record.Description,
                            Kind = SoundKind.UserFile,
                            FilePath = path
                        });
                }
            }
            catch
            {
                // A damaged index costs the user their import list, not
                // their ability to start the app.
            }
        }


        private void SaveUserSounds()
        {
            try
            {
                Directory.CreateDirectory(_importFolder);

                List<ImportRecord> records =
                    UserSounds
                        .Select(sound => new ImportRecord
                        {
                            Id = sound.Id,
                            Name = sound.Name,
                            Description = sound.Description,
                            FileName = Path.GetFileName(sound.FilePath) ?? ""
                        })
                        .ToList();

                string temporary = _indexPath + ".tmp";

                File.WriteAllText(
                    temporary,
                    JsonSerializer.Serialize(records, JsonOptions));

                File.Move(temporary, _indexPath, overwrite: true);
            }
            catch
            {
                // Best-effort.
            }
        }


        /// <summary>
        /// Deletes cache files rendered by an older recipe version, so
        /// an improved sound is not shadowed by its previous take.
        /// </summary>
        private void PruneStaleCache()
        {
            try
            {
                if (!Directory.Exists(_cacheFolder))
                    return;

                string marker = $"_r{BuiltInSounds.RecipeVersion}_v";

                foreach (string file in
                         Directory.GetFiles(_cacheFolder, "*.wav"))
                {
                    if (!Path.GetFileName(file).Contains(
                            marker,
                            StringComparison.Ordinal))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch
            {
                // The cache regenerates itself; pruning is optional.
            }
        }


        private static string Sanitize(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');

            return value;
        }


        // =============================================================
        // ON-DISK SHAPE
        // =============================================================

        private sealed class ImportRecord
        {
            public string Id { get; set; } = "";

            public string Name { get; set; } = "";

            public string Description { get; set; } = "";

            public string FileName { get; set; } = "";
        }
    }
}
