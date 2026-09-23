using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.ContentChangeStrategies;

public class ReindexRelationsDraftContentChangeStrategy : ReindexRelationsContentChangeStrategyBase
{
    public ReindexRelationsDraftContentChangeStrategy(
        ITrackedReferencesService trackedReferencesService,
        IDraftContentChangeStrategy publishedContentChangeStrategy,
        IIndexDocumentService indexDocumentService)
        : base(trackedReferencesService, publishedContentChangeStrategy, indexDocumentService, ContentState.Draft)
    {
    }
}