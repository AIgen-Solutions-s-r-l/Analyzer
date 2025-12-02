using Serilog.Core;
using Serilog.Events;

namespace AnalyzerCore.Infrastructure.Logging;

/// <summary>
/// Destructuring policy that masks sensitive data in log events.
/// </summary>
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private readonly HashSet<string> _sensitiveProperties;
    private const string MaskedValue = "***MASKED***";

    public SensitiveDataDestructuringPolicy(IEnumerable<string> sensitiveProperties)
    {
        _sensitiveProperties = new HashSet<string>(
            sensitiveProperties,
            StringComparer.OrdinalIgnoreCase);
    }

    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue? result)
    {
        result = null;

        if (value is null)
            return false;

        var valueType = value.GetType();

        // Only process objects with properties, not primitives
        if (valueType.IsPrimitive || valueType == typeof(string) || valueType.IsEnum)
            return false;

        var properties = valueType.GetProperties()
            .Where(p => p.CanRead)
            .ToList();

        if (!properties.Any())
            return false;

        var structureProperties = new List<LogEventProperty>();

        foreach (var property in properties)
        {
            try
            {
                var propValue = property.GetValue(value);

                if (IsSensitiveProperty(property.Name))
                {
                    // Mask sensitive values
                    structureProperties.Add(new LogEventProperty(
                        property.Name,
                        new ScalarValue(MaskedValue)));
                }
                else
                {
                    // Normal destructuring
                    structureProperties.Add(new LogEventProperty(
                        property.Name,
                        propertyValueFactory.CreatePropertyValue(propValue, true)));
                }
            }
            catch
            {
                // If we can't read the property, skip it
            }
        }

        result = new StructureValue(structureProperties, valueType.Name);
        return true;
    }

    private bool IsSensitiveProperty(string propertyName)
    {
        return _sensitiveProperties.Any(s =>
            propertyName.Contains(s, StringComparison.OrdinalIgnoreCase));
    }
}
