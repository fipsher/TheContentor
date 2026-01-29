using System.Text.Json.Serialization;

namespace TheContentor.Infrastructure.Scrappers.Reddit.Models;

public class RedditListItem
{
    [JsonPropertyName("children")]
    public List<RedditNestedJson>? Children { get; set; }

    [JsonPropertyName("after")]
    public string? After { get; set; }

    [JsonPropertyName("dist")]
    public int? Dist { get; set; }

    [JsonPropertyName("modhash")]
    public string? Modhash { get; set; }

    [JsonPropertyName("geo_filter")]
    public string? GeoFilter { get; set; }

    [JsonPropertyName("before")]
    public object? Before { get; set; }

    [JsonPropertyName("approved_at_utc")]
    public object? ApprovedAtUtc { get; set; }

    [JsonPropertyName("subreddit")]
    public string? Subreddit { get; set; }

    [JsonPropertyName("selftext")]
    public string? SelfText { get; set; }

    [JsonPropertyName("author_fullname")]
    public string? AuthorFullname { get; set; }

    [JsonPropertyName("saved")]
    public bool Saved { get; set; }

    [JsonPropertyName("mod_reason_title")]
    public object? ModReasonTitle { get; set; }

    [JsonPropertyName("gilded")]
    public int? Gilded { get; set; }

    [JsonPropertyName("clicked")]
    public bool Clicked { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("link_flair_richtext")]
    public List<object>? LinkFlairRichText { get; set; }

    [JsonPropertyName("subreddit_name_prefixed")]
    public string? SubredditNamePrefixed { get; set; }

    [JsonPropertyName("hidden")]
    public bool Hidden { get; set; }

    [JsonPropertyName("pwls")]
    public int? Pwls { get; set; }

    [JsonPropertyName("link_flair_css_class")]
    public string? LinkFlairCssClass { get; set; }

    [JsonPropertyName("downs")]
    public int? Downs { get; set; }

    [JsonPropertyName("top_awarded_type")]
    public object? TopAwardedType { get; set; }

    [JsonPropertyName("hide_score")]
    public bool HideScore { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("quarantine")]
    public bool Quarantine { get; set; }

    [JsonPropertyName("link_flair_text_color")]
    public string? LinkFlairTextColor { get; set; }

    [JsonPropertyName("upvote_ratio")]
    public double UpvoteRatio { get; set; }

    [JsonPropertyName("author_flair_background_color")]
    public string? AuthorFlairBackgroundColor { get; set; }

    [JsonPropertyName("subreddit_type")]
    public string? SubredditType { get; set; }

    [JsonPropertyName("ups")]
    public int? Ups { get; set; }

    [JsonPropertyName("total_awards_received")]
    public int? TotalAwardsReceived { get; set; }

    [JsonPropertyName("author_flair_template_id")]
    public string? AuthorFlairTemplateId { get; set; }

    [JsonPropertyName("is_original_content")]
    public bool IsOriginalContent { get; set; }

    [JsonPropertyName("user_reports")]
    public List<object>? UserReports { get; set; }

    [JsonPropertyName("secure_media")]
    public object? SecureMedia { get; set; }

    [JsonPropertyName("is_reddit_media_domain")]
    public bool IsRedditMediaDomain { get; set; }

    [JsonPropertyName("is_meta")]
    public bool IsMeta { get; set; }

    [JsonPropertyName("category")]
    public object? Category { get; set; }

    [JsonPropertyName("link_flair_text")]
    public string? LinkFlairText { get; set; }

    [JsonPropertyName("can_mod_post")]
    public bool CanModPost { get; set; }

    [JsonPropertyName("score")]
    public int? Score { get; set; }

    [JsonPropertyName("approved_by")]
    public object? ApprovedBy { get; set; }

    [JsonPropertyName("is_created_from_ads_ui")]
    public bool IsCreatedFromAdsUi { get; set; }

    [JsonPropertyName("author_premium")]
    public bool AuthorPremium { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("edited")]
    public object? Edited { get; set; }

    [JsonPropertyName("author_flair_css_class")]
    public object? AuthorFlairCssClass { get; set; }

    [JsonPropertyName("author_flair_richtext")]
    public List<object>? AuthorFlairRichText { get; set; }

    [JsonPropertyName("content_categories")]
    public object? ContentCategories { get; set; }

    [JsonPropertyName("is_self")]
    public bool IsSelf { get; set; }

    [JsonPropertyName("mod_note")]
    public object? ModNote { get; set; }

    [JsonPropertyName("created")]
    public double Created { get; set; }

    [JsonPropertyName("link_flair_type")]
    public string? LinkFlairType { get; set; }

    [JsonPropertyName("wls")]
    public int? Wls { get; set; }

    [JsonPropertyName("removed_by_category")]
    public object? RemovedByCategory { get; set; }

    [JsonPropertyName("banned_by")]
    public object? BannedBy { get; set; }

    [JsonPropertyName("author_flair_type")]
    public string? AuthorFlairType { get; set; }

    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("allow_live_comments")]
    public bool AllowLiveComments { get; set; }

    [JsonPropertyName("selftext_html")]
    public string? SelfTextHtml { get; set; }

    [JsonPropertyName("likes")]
    public object? Likes { get; set; }

    [JsonPropertyName("suggested_sort")]
    public string? SuggestedSort { get; set; }

    [JsonPropertyName("banned_at_utc")]
    public object? BannedAtUtc { get; set; }

    [JsonPropertyName("view_count")]
    public object? ViewCount { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("no_follow")]
    public bool NoFollow { get; set; }

    [JsonPropertyName("is_crosspostable")]
    public bool IsCrosspostable { get; set; }

    [JsonPropertyName("pinned")]
    public bool Pinned { get; set; }

    [JsonPropertyName("over_18")]
    public bool Over18 { get; set; }

    [JsonPropertyName("all_awardings")]
    public List<object>? AllAwardings { get; set; }

    [JsonPropertyName("awarders")]
    public List<object>? Awarders { get; set; }

    [JsonPropertyName("media_only")]
    public bool MediaOnly { get; set; }

    [JsonPropertyName("can_gild")]
    public bool CanGild { get; set; }

    [JsonPropertyName("spoiler")]
    public bool Spoiler { get; set; }

    [JsonPropertyName("locked")]
    public bool Locked { get; set; }

    [JsonPropertyName("author_flair_text")]
    public string? AuthorFlairText { get; set; }

    [JsonPropertyName("treatment_tags")]
    public List<object>? TreatmentTags { get; set; }

    [JsonPropertyName("visited")]
    public bool Visited { get; set; }

    [JsonPropertyName("removed_by")]
    public object? RemovedBy { get; set; }

    [JsonPropertyName("num_reports")]
    public object? NumReports { get; set; }

    [JsonPropertyName("distinguished")]
    public object? Distinguished { get; set; }

    [JsonPropertyName("subreddit_id")]
    public string? SubredditId { get; set; }

    [JsonPropertyName("author_is_blocked")]
    public bool AuthorIsBlocked { get; set; }

    [JsonPropertyName("mod_reason_by")]
    public object? ModReasonBy { get; set; }

    [JsonPropertyName("removal_reason")]
    public object? RemovalReason { get; set; }

    [JsonPropertyName("link_flair_background_color")]
    public string? LinkFlairBackgroundColor { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("is_robot_indexable")]
    public bool IsRobotIndexable { get; set; }

    [JsonPropertyName("report_reasons")]
    public object? ReportReasons { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("body_html")]
    public string? BodyHtml { get; set; }

    [JsonPropertyName("replies")]
    public object? Replies { get; set; }

    [JsonPropertyName("discussion_type")]
    public object? DiscussionType { get; set; }

    [JsonPropertyName("num_comments")]
    public int NumComments { get; set; }

    [JsonPropertyName("send_replies")]
    public bool SendReplies { get; set; }

    [JsonPropertyName("contest_mode")]
    public bool ContestMode { get; set; }

    [JsonPropertyName("mod_reports")]
    public List<object>? ModReports { get; set; }

    [JsonPropertyName("author_patreon_flair")]
    public bool AuthorPatreonFlair { get; set; }

    [JsonPropertyName("author_flair_text_color")]
    public string? AuthorFlairTextColor { get; set; }

    [JsonPropertyName("permalink")]
    public string? Permalink { get; set; }

    [JsonPropertyName("stickied")]
    public bool Stickied { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("subreddit_subscribers")]
    public int? SubredditSubscribers { get; set; }

    [JsonPropertyName("created_utc")]
    public double CreatedUtc { get; set; }

    [JsonPropertyName("num_crossposts")]
    public int? NumCrossposts { get; set; }

    [JsonPropertyName("media")]
    public object? Media { get; set; }

    [JsonPropertyName("is_video")]
    public bool IsVideo { get; set; }

    [JsonPropertyName("link_flair_template_id")]
    public string? LinkFlairTemplateId { get; set; }
}
