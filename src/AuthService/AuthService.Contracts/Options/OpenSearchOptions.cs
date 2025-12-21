namespace AuthService.Contracts.Options;

/// <summary>
/// Настройки интеграции с OpenSearch, мапятся на секцию конфигурации "OpenSearch".
/// </summary>
public sealed class OpenSearchOptions
{
    public const string SECTION_NAME = "OpenSearch";

    public bool Enabled { get; set; } = false;

    public string? Url { get; set; }

    public string IndexPrefix { get; set; } = "auth-service";

    public string? Username { get; set; }

    public string? Password { get; set; }
}