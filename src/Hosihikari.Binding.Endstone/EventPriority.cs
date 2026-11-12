namespace Hosihikari.Binding.Endstone;

/// <summary>Dispatch priority, mirroring the carrier's six levels.</summary>
public enum EventPriority
{
    /// <summary>Runs first.</summary>
    Lowest = 0,

    /// <summary>Runs after <see cref="Lowest"/>.</summary>
    Low = 1,

    /// <summary>The default.</summary>
    Normal = 2,

    /// <summary>Runs after <see cref="Normal"/>.</summary>
    High = 3,

    /// <summary>Runs after <see cref="High"/>.</summary>
    Highest = 4,

    /// <summary>Runs last; observe only, do not modify or cancel.</summary>
    Monitor = 5,
}
