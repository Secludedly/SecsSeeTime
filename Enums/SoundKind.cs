namespace SecSeeTime.Enums
{
    /// <summary>
    /// Where a library sound's audio comes from.
    /// </summary>
    public enum SoundKind
    {
        /// <summary>Synthesized by Sec's See Time on first use.</summary>
        BuiltIn,

        /// <summary>A file the user imported into the library.</summary>
        UserFile
    }
}
