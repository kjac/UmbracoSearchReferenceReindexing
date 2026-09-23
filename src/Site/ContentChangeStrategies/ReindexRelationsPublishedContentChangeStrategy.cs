using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.ContentChangeStrategies;

public class ReindexRelationsPublishedContentChangeStrategy : ReindexRelationsContentChangeStrategyBase
{
    public ReindexRelationsPublishedContentChangeStrategy(
        ITrackedReferencesService trackedReferencesService,
        IPublishedContentChangeStrategy publishedContentChangeStrategy,
        IIndexDocumentService indexDocumentService)
        : base(trackedReferencesService, publishedContentChangeStrategy, indexDocumentService, ContentState.Published)
    {
    }
}