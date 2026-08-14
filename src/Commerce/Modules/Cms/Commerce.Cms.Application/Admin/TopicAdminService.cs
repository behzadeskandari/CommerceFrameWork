using Commerce.Cms.Application.Abstractions;
using Commerce.Cms.Application.Security;
using Commerce.Cms.Contracts.Admin;
using Commerce.Cms.Domain.Entities;
using Commerce.Framework.Core.Errors;
using Commerce.Framework.Core.Results;

namespace Commerce.Cms.Application.Admin;

public sealed class TopicAdminService(ICmsRepository repository, IContentHtmlSanitizer sanitizer) : ITopicAdminService
{
    public async Task<Result<IReadOnlyList<TopicSummaryDto>>> ListAsync(int? storeId, CancellationToken cancellationToken = default)
    {
        var topics = await repository.ListTopicsAsync(storeId, cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<TopicSummaryDto>>(topics.Select(MapSummary).ToList());
    }

    public async Task<Result<TopicDetailDto>> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var topic = await repository.GetTopicByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return topic is null
            ? Result.Failure<TopicDetailDto>(Error.NotFound($"Topic '{id}' was not found."))
            : Result.Success(MapDetail(topic));
    }

    public async Task<Result<TopicDetailDto>> CreateAsync(CreateTopicRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (await repository.TopicSystemNameExistsAsync(request.StoreId, request.SystemName, null, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<TopicDetailDto>(Error.Validation($"Topic system name '{request.SystemName}' already exists."));
        }

        var topic = Topic.Create(request.StoreId, request.SystemName, request.IsPublished, request.PublishedFromUtc, request.PublishedToUtc);
        foreach (var loc in request.Localizations)
        {
            topic.AddLocalization(loc.LanguageId, loc.Title, sanitizer.Sanitize(loc.Body), loc.MetaTitle, loc.MetaDescription);
        }

        await repository.AddTopicAsync(topic, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(topic));
    }

    public async Task<Result<TopicDetailDto>> UpdateAsync(int id, UpdateTopicRequest request, CancellationToken cancellationToken = default)
    {
        var topic = await repository.GetTopicByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (topic is null)
        {
            return Result.Failure<TopicDetailDto>(Error.NotFound($"Topic '{id}' was not found."));
        }

        if (await repository.TopicSystemNameExistsAsync(topic.StoreId, request.SystemName, topic.Id, cancellationToken).ConfigureAwait(false))
        {
            return Result.Failure<TopicDetailDto>(Error.Validation($"Topic system name '{request.SystemName}' already exists."));
        }

        topic.Update(request.SystemName, request.IsPublished, request.PublishedFromUtc, request.PublishedToUtc);
        var localizations = request.Localizations.Select(loc =>
            TopicLocalization.Create(topic.Id, loc.LanguageId, loc.Title, sanitizer.Sanitize(loc.Body), loc.MetaTitle, loc.MetaDescription)).ToList();
        topic.ReplaceLocalizations(localizations);
        await repository.SaveTopicAsync(topic, cancellationToken).ConfigureAwait(false);
        return Result.Success(MapDetail(topic));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var topic = await repository.GetTopicByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (topic is null)
        {
            return Result.Failure(Error.NotFound($"Topic '{id}' was not found."));
        }

        await repository.DeleteTopicAsync(topic, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private static TopicSummaryDto MapSummary(Topic topic)
    {
        var first = topic.Localizations.FirstOrDefault();
        return new TopicSummaryDto(topic.Id, topic.StoreId, topic.SystemName, topic.IsPublished, first?.Title, topic.UpdatedAtUtc);
    }

    private static TopicDetailDto MapDetail(Topic topic) =>
        new(
            topic.Id,
            topic.StoreId,
            topic.SystemName,
            topic.IsPublished,
            topic.PublishedFromUtc,
            topic.PublishedToUtc,
            topic.Localizations.Select(x => new TopicLocalizationDto(x.Id, x.LanguageId, x.Title, x.Body, x.MetaTitle, x.MetaDescription)).ToList(),
            topic.CreatedAtUtc,
            topic.UpdatedAtUtc);
}
