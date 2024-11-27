using System.ComponentModel.Design;
using System.Text.Json;
using NStore.Core.Persistence;
using NStore.Domain;

namespace Sample;

public static class ChunkExtensions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true, Converters =
        {
            new PolymorphicJsonConverter<object>()
        }
    };

    public static string ToJson(this IChunk chunk)
    {
        return JsonSerializer.Serialize(chunk, Options);
    }

    public static bool IsFiller(this IChunk chunk)
    {
        return chunk.PartitionId == "::empty";
    }
}