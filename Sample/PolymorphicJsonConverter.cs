using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sample;

public class PolymorphicJsonConverter<T> : JsonConverter<T>
{
    private const string TypeDiscriminatorField = "Type";

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;

        if (!root.TryGetProperty(TypeDiscriminatorField, out JsonElement typeProperty))
        {
            throw new JsonException($"Missing type discriminator field '{TypeDiscriminatorField}'");
        }

        string typeName = typeProperty.GetString();
        Type actualType = Type.GetType(typeName);

        if (actualType == null)
        {
            throw new JsonException($"Unknown type: {typeName}");
        }

        return (T) JsonSerializer.Deserialize(root.GetRawText(), actualType, options)!;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // Start the JSON object
        writer.WriteStartObject();

        // Write the type discriminator
        writer.WriteString(TypeDiscriminatorField, value.GetType().Name);

        // Serialize the rest of the object using the specific type
        string json = JsonSerializer.Serialize(value, value.GetType(), options);

        // Write the remaining properties
        using (JsonDocument document = JsonDocument.Parse(json))
        {
            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                if (property.Name != TypeDiscriminatorField) // Avoid duplicating the type field
                {
                    property.WriteTo(writer);
                }
            }
        }

        writer.WriteEndObject();
    }
}