using System.ComponentModel.DataAnnotations;

namespace NphiesBridge.Shared.Configuration
{
    public class AppSettings
    {
        public DatabaseSettings Database { get; set; } = new();
        public JwtSettings Jwt { get; set; } = new();
        public ApiSettings Api { get; set; } = new();
        public LoggingSettings Logging { get; set; } = new();
    }

    public class DatabaseSettings
    {
        [Required]
        public string ConnectionString { get; set; } = string.Empty;

        public bool EnableSensitiveDataLogging { get; set; } = false;
        public int CommandTimeout { get; set; } = 30;
    }

    public class JwtSettings
    {
        [Required]
        public string SecretKey { get; set; } = string.Empty;

        [Required]
        public string Issuer { get; set; } = string.Empty;

        [Required]
        public string Audience { get; set; } = string.Empty;

        public int ExpirationMinutes { get; set; } = 60;
        public int RefreshTokenExpirationDays { get; set; } = 7;
    }

    public class ApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
        public bool EnableSwagger { get; set; } = false;
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    }

    public class LoggingSettings
    {
        public string LogLevel { get; set; } = "Information";
        public string LogPath { get; set; } = "Logs";
        public bool EnableFileLogging { get; set; } = true;
        public bool EnableConsoleLogging { get; set; } = true;
        public int RetentionDays { get; set; } = 30;
    }
}