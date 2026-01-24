using AutoMapper;
using TheContentor.Domain.Entities;
using TheContentor.Domain.Enums;
using TheContentor.Infrastructure.Scrappers.Reddit.Models;

namespace TheContentor.Infrastructure.Mappings;

/// <summary>
/// AutoMapper profile for Reddit-related mappings.
/// </summary>
public class RedditMappingProfile : Profile
{
    public RedditMappingProfile()
    {
        CreateMap<RedditPost, SourcePost>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Platform, opt => opt.MapFrom(_ => SourcePlatform.Reddit))
            .ForMember(dest => dest.ExternalId, opt => opt.MapFrom(src => src.ExternalId))
            .ForMember(dest => dest.ExternalUrl, opt => opt.MapFrom(src => src.ExternalUrl.ToString()))
            .ForMember(dest => dest.Community, opt => opt.MapFrom(src => src.Community))
            .ForMember(dest => dest.CommunityExternalId, opt => opt.MapFrom(src => src.CommunityExternalId))
            .ForMember(dest => dest.Flairs, opt => opt.MapFrom(src => src.Flairs))
            .ForMember(dest => dest.AuthorExternalId, opt => opt.MapFrom(src => src.AuthorExternalId))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.AuthorName))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.RawText, opt => opt.MapFrom(src => src.RawText))
            .ForMember(dest => dest.RawHtml, opt => opt.MapFrom(src => src.RawHtml))
            .ForMember(dest => dest.WordCount, opt => opt.MapFrom(src => src.WordCount))
            .ForMember(dest => dest.Language, opt => opt.MapFrom(src => src.Language ?? "en"))
            .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.Score ?? 0))
            .ForMember(dest => dest.CommentCount, opt => opt.MapFrom(src => src.CommentCount))
            .ForMember(dest => dest.UpvoteRatio, opt => opt.MapFrom(src => src.UpvoteRatio))
            .ForMember(dest => dest.IsNsfw, opt => opt.MapFrom(src => src.IsNsfw))
            .ForMember(dest => dest.IsSpoiler, opt => opt.MapFrom(src => src.IsSpoiler))
            .ForMember(dest => dest.CreatedUtc, opt => opt.MapFrom(src => src.CreatedUtc))
            .ForMember(dest => dest.IngestedUtc, opt => opt.MapFrom(_ => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.LastRefreshedUtc, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(_ => SourcePostStatus.Raw))
            .ForMember(dest => dest.StatusReason, opt => opt.Ignore())
            .ForMember(dest => dest.ContentHash, opt => opt.Ignore())
            .ForMember(dest => dest.MetadataJson, opt => opt.MapFrom(src => src.MetadataJson))
            .ForMember(dest => dest.Comments, opt => opt.MapFrom(src => src.Comments))
            .ForMember(dest => dest.MetricSnapshots, opt => opt.Ignore());

        CreateMap<RedditPost, RedditPostData>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SourcePost, opt => opt.Ignore())
            .ForMember(dest => dest.Subreddit, opt => opt.MapFrom(src => src.Subreddit))
            .ForMember(dest => dest.Permalink, opt => opt.MapFrom(src => src.Permalink))
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName))
            .ForMember(dest => dest.IsSelfPost, opt => opt.MapFrom(src => src.IsSelfPost))
            .ForMember(dest => dest.LinkUrl, opt => opt.MapFrom(src => src.LinkUrl))
            .ForMember(dest => dest.Domain, opt => opt.MapFrom(src => src.Domain))
            .ForMember(dest => dest.FlairText, opt => opt.MapFrom(src => src.FlairText))
            .ForMember(dest => dest.IsAuthorDeleted, opt => opt.MapFrom(src => src.IsAuthorDeleted))
            .ForMember(dest => dest.AuthorCreatedUtc, opt => opt.MapFrom(src => src.AuthorCreatedUtc))
            .ForMember(dest => dest.AuthorLinkKarma, opt => opt.MapFrom(src => src.AuthorLinkKarma))
            .ForMember(dest => dest.AuthorCommentKarma, opt => opt.MapFrom(src => src.AuthorCommentKarma))
            .ForMember(dest => dest.IsLocked, opt => opt.MapFrom(src => src.IsLocked))
            .ForMember(dest => dest.IsRemoved, opt => opt.MapFrom(src => src.IsRemoved))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted))
            .ForMember(dest => dest.IsStickied, opt => opt.MapFrom(src => src.IsStickied))
            .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(src => src.IsArchived))
            .ForMember(dest => dest.TotalAwardsReceived, opt => opt.MapFrom(src => src.TotalAwardsReceived));

        CreateMap<RedditComment, SourceComment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.SourcePostId, opt => opt.Ignore())
            .ForMember(dest => dest.SourcePost, opt => opt.Ignore())
            .ForMember(dest => dest.ExternalId, opt => opt.MapFrom(src => src.ExternalId))
            .ForMember(dest => dest.ParentExternalId, opt => opt.Ignore())
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.AuthorName))
            .ForMember(dest => dest.RawText, opt => opt.MapFrom(src => src.Body))
            .ForMember(dest => dest.Score, opt => opt.MapFrom(src => src.Score ?? 0))
            .ForMember(dest => dest.CreatedUtc, opt => opt.MapFrom(src => src.CreatedUtc))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.AuthorName == "[deleted]" || src.Body == "[deleted]"))
            .ForMember(dest => dest.MetadataJson, opt => opt.MapFrom(src => src.MetadataJson));
    }
}
