using Serilog.Core;
using Serilog.Events;

namespace Blog.Api.Core;

public class LogSanitizer : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "PasswordHash", "NewPassword",
        "Token", "AccessToken", "RefreshToken", "Authorization",
        "Secret", "ApiKey", "ConnectionString",
        "Cookie",
        "CreditCard", "SSN", "Email", "PhoneNumber"
    };

    private const string Redacted = "[REDACTED]";

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue? result)
    {
        result = null;
        return false;
    }

    public static LogEventPropertyValue Sanitize(LogEventPropertyValue value, string propertyName)
    {
        if (SensitiveProperties.Contains(propertyName))
            return new ScalarValue(Redacted);

        if (value is StructureValue sv)
        {
            var sanitized = sv.Properties
                .Select(p => new LogEventProperty(p.Name, Sanitize(p.Value, p.Name)))
                .ToList();
            return new StructureValue(sanitized, sv.TypeTag);
        }

        return value;
    }
}

public class LogSanitizingEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var propertiesToUpdate = new List<LogEventProperty>();

        foreach (var property in logEvent.Properties)
        {
            var sanitized = LogSanitizer.Sanitize(property.Value, property.Key);
            if (!ReferenceEquals(sanitized, property.Value))
                propertiesToUpdate.Add(new LogEventProperty(property.Key, sanitized));
        }

        foreach (var property in propertiesToUpdate)
            logEvent.AddOrUpdateProperty(property);
    }
}
