namespace TheContentor.Infrastructure.Options;

/// <summary>Options for local file system storage.</summary>
public class LocalStorageOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "LocalStorage";

    /// <summary>Base directory for all blob containers.</summary>
    public string BasePath { get; set; } = string.Empty;
}
