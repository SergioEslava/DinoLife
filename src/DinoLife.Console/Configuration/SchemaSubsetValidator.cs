using System.Text.Json;

namespace DinoLife.Cli.Configuration;

internal static class SchemaSubsetValidator
{
    public static bool Validate(JsonElement data, JsonElement schema, out string error)
    {
        List<string> errors = [];
        ValidateInternal(data, schema, "$", errors);
        error = errors.Count == 0 ? string.Empty : string.Join("; ", errors);
        return errors.Count == 0;
    }

    private static void ValidateInternal(JsonElement data, JsonElement schema, string path, List<string> errors)
    {
        string? type = GetString(schema, "type");
        if (!string.IsNullOrWhiteSpace(type) && !MatchesType(data, type))
        {
            errors.Add($"{path}: expected {type}, got {data.ValueKind}");
            return;
        }

        if (schema.TryGetProperty("enum", out JsonElement enumValues) && enumValues.ValueKind == JsonValueKind.Array)
        {
            bool found = false;
            foreach (JsonElement item in enumValues.EnumerateArray())
            {
                if (item.ValueKind == data.ValueKind && item.ToString() == data.ToString())
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                errors.Add($"{path}: value not in enum");
            }
        }

        if (data.ValueKind == JsonValueKind.Object)
        {
            ValidateObject(data, schema, path, errors);
        }
        else if (data.ValueKind == JsonValueKind.Number)
        {
            ValidateNumber(data, schema, path, errors);
        }
        else if (data.ValueKind == JsonValueKind.String)
        {
            ValidateString(data, schema, path, errors);
        }
    }

    private static void ValidateObject(JsonElement data, JsonElement schema, string path, List<string> errors)
    {
        HashSet<string> allowed = [];

        if (schema.TryGetProperty("required", out JsonElement required) && required.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement requiredName in required.EnumerateArray())
            {
                string name = requiredName.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name)) { continue; }
                if (!data.TryGetProperty(name, out _))
                {
                    errors.Add($"{path}: missing required property '{name}'");
                }
            }
        }

        if (schema.TryGetProperty("properties", out JsonElement properties) && properties.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in properties.EnumerateObject())
            {
                allowed.Add(property.Name);
                if (!data.TryGetProperty(property.Name, out JsonElement propertyValue))
                {
                    continue;
                }

                ValidateInternal(propertyValue, property.Value, $"{path}.{property.Name}", errors);
            }
        }

        bool additionalAllowed = true;
        if (schema.TryGetProperty("additionalProperties", out JsonElement additional)
            && additional.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            additionalAllowed = additional.GetBoolean();
        }

        if (!additionalAllowed && allowed.Count > 0)
        {
            foreach (JsonProperty property in data.EnumerateObject())
            {
                if (!allowed.Contains(property.Name))
                {
                    errors.Add($"{path}: additional property '{property.Name}' is not allowed");
                }
            }
        }
    }

    private static void ValidateNumber(JsonElement data, JsonElement schema, string path, List<string> errors)
    {
        if (!data.TryGetDouble(out double value))
        {
            errors.Add($"{path}: invalid number");
            return;
        }

        if (schema.TryGetProperty("minimum", out JsonElement minimum) && minimum.TryGetDouble(out double minVal) && value < minVal)
        {
            errors.Add($"{path}: value {value} is below minimum {minVal}");
        }

        if (schema.TryGetProperty("maximum", out JsonElement maximum) && maximum.TryGetDouble(out double maxVal) && value > maxVal)
        {
            errors.Add($"{path}: value {value} exceeds maximum {maxVal}");
        }
    }

    private static void ValidateString(JsonElement data, JsonElement schema, string path, List<string> errors)
    {
        string value = data.GetString() ?? string.Empty;
        if (schema.TryGetProperty("minLength", out JsonElement minLength)
            && minLength.TryGetInt32(out int minLen)
            && value.Length < minLen)
        {
            errors.Add($"{path}: length {value.Length} is below minimum {minLen}");
        }
    }

    private static bool MatchesType(JsonElement data, string type)
    {
        return type switch
        {
            "object" => data.ValueKind == JsonValueKind.Object,
            "string" => data.ValueKind == JsonValueKind.String,
            "boolean" => data.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "number" => data.ValueKind == JsonValueKind.Number,
            "integer" => data.ValueKind == JsonValueKind.Number && data.TryGetInt32(out _),
            _ => true
        };
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out JsonElement property)) { return null; }
        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }
}
