namespace NutriScan.Domain.Enums;

/// <summary>Where a daily nutrition target comes from.</summary>
public enum TargetSource
{
    /// <summary>Fixed fallback used while the profile is incomplete.</summary>
    Default,

    /// <summary>Derived from the profile (Mifflin–St Jeor and macro rules).</summary>
    Calculated,

    /// <summary>Set explicitly by the user.</summary>
    Manual
}
