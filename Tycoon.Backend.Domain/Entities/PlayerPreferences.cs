namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Stores per-player Synaptix presentation and accessibility preferences.
    /// One row per player; created on first PUT, never deleted.
    /// </summary>
    public sealed class PlayerPreferences
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PlayerId { get; set; }

        /// <summary>Synaptix presentation mode: kids, teen, or adult.</summary>
        public string SynaptixMode { get; set; } = "adult";

        /// <summary>Preferred home surface: hub, arena, labs, pathways, journey, circles, command.</summary>
        public string PreferredSurface { get; set; } = "hub";

        /// <summary>Whether reduced-motion mode is enabled.</summary>
        public bool ReducedMotion { get; set; }

        /// <summary>Tone preference: playful, balanced, or competitive.</summary>
        public string TonePreference { get; set; } = "balanced";

        /// <summary>Equipped avatar item type (e.g., "avatar:default", "cosmetic:avatar-neon").</summary>
        public string? AvatarItemType { get; set; }

        /// <summary>Comma-separated equipped cosmetic item types.</summary>
        public string EquippedCosmeticItemTypesCsv { get; set; } = string.Empty;

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
