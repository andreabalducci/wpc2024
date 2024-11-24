using System.Text.Json;
using NStore.Core.Persistence;

namespace Sample;

public static class ChunkExtensions
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static string ToJson(this IChunk chunk)
    {
        return JsonSerializer.Serialize(chunk, Options);
    }
    
    public static bool IsFiller(this IChunk chunk)
    {
        return chunk.PartitionId == "::empty";
    }
}