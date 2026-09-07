using SecSeeTime.Enums;

namespace SecSeeTime.Models
{
    /// <summary>
    /// One entry in the sound library.
    ///
    /// Built-in entries carry a <see cref="SoundRecipe"/> and are
    /// synthesized on demand. Imported entries carry a file path.
    /// Everything else in the app treats the two identically.
    /// </summary>
    public sealed class SoundDefinition
    {
        /// <summary>
        /// Stable key. For built-ins this is the name, which is also
        /// what an alarm stores, so the library can grow without
        /// invalidating anyone's saved alarms.
        /// </summary>
        public string Id { get; init; } = "";

        public string Name { get; init; } = "";

        public SoundCategory Category { get; init; }

        public string Description { get; init; } = "";

        public SoundKind Kind { get; init; }


        /// <summary>Set for <see cref="SoundKind.UserFile"/>.</summary>
        public string? FilePath { get; init; }


        /// <summary>Set for <see cref="SoundKind.BuiltIn"/>.</summary>
        public SoundRecipe? Recipe { get; init; }


        public bool IsUserSound => Kind == SoundKind.UserFile;


        public string CategoryLabel =>
            Category switch
            {
                SoundCategory.Alarms => "ALARMS",
                SoundCategory.Chimes => "CHIMES",
                SoundCategory.Electronic => "ELECTRONIC",
                SoundCategory.Ambient => "AMBIENT",
                SoundCategory.MySounds => "MY SOUNDS",
                _ => "OTHER"
            };


        public override string ToString() => Name;
    }
}
