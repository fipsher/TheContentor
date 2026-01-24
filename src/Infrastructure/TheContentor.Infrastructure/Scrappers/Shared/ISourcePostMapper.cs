using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;

namespace TheContentor.Infrastructure.Scrappers.Shared;

public interface ISourcePostMapper<TPlatformPost>
{
    SourcePlatform Platform { get; }
    SourcePost Map(TPlatformPost post);
}