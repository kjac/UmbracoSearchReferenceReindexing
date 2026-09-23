using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.Services.ContentIndexing;

namespace Site.ContentChangeStrategies;

public abstract class ReindexRelationsContentChangeStrategyBase : IContentChangeStrategy
{
    private readonly ITrackedReferencesService _trackedReferencesService;
    private readonly IContentChangeStrategy _delegateContentChangeStrategy;
    private readonly IIndexDocumentService _indexDocumentService;
    private readonly ContentState _supportedContentState;

    protected ReindexRelationsContentChangeStrategyBase(
        ITrackedReferencesService trackedReferencesService,
        IContentChangeStrategy delegateContentChangeStrategy,
        IIndexDocumentService indexDocumentService,
        ContentState supportedContentState)
    {
        _trackedReferencesService = trackedReferencesService;
        _delegateContentChangeStrategy = delegateContentChangeStrategy;
        _indexDocumentService = indexDocumentService;
        _supportedContentState = supportedContentState;
    }

    public async Task HandleAsync(IEnumerable<ContentIndexInfo> indexInfos, IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        // Figure out which effective changes should be handled.
        var effectiveChanges = await CalculateEffectiveChanges(changes, cancellationToken);

        // Delegate the actual handling to the change strategy.
        await _delegateContentChangeStrategy.HandleAsync(indexInfos, effectiveChanges, cancellationToken);
    }

    public async Task RebuildAsync(ContentIndexInfo indexInfo, CancellationToken cancellationToken)
        // Just delegate this entirely to the delegate change strategy.
        => await _delegateContentChangeStrategy.RebuildAsync(indexInfo, cancellationToken);

    private async Task<IEnumerable<ContentChange>> CalculateEffectiveChanges(IEnumerable<ContentChange> changes, CancellationToken cancellationToken)
    {
        // Get the document IDs of all relevant changes (that is, changes in the supported content state).
        var changesAsArray = changes as ContentChange[] ?? changes.ToArray();
        var documentIds = changesAsArray
            .Where(change => change.ObjectType is UmbracoObjectTypes.Document
                             && change.ContentState == _supportedContentState)
            .Select(change => change.Id)
            .ToArray();

        if (documentIds.Length is 0)
        {
            // No changes to handle here; short-circuit and let the delegate change strategy handle the changes.
            return changesAsArray;
        }
        
        var relatedDocumentIds = new List<Guid>();
        foreach (var documentId in documentIds)
        {
            // Get all relations for the document
            // NOTE: For simplicity we just fetch the first 1000 relations here. If you expect to have content with
            //       more relations than that, consider iterating through the relations with proper pagination.
            var references = await _trackedReferencesService.GetPagedRelationsForItemAsync(
                documentId,
                UmbracoObjectTypes.Document,
                0,
                1000,
                true);
            if (references.Success)
            {
                relatedDocumentIds.AddRange(references.Result.Items.Select(item => item.NodeKey));
            }
        }

        if (relatedDocumentIds.Count is 0)
        {
            // No relations found; short-circuit and let the delegate change strategy handle the changes.
            return changesAsArray;
        }

        // Play nice; check for cancellation before continuing.
        if (cancellationToken.IsCancellationRequested)
        {
            // Cancellation was requested; short-circuit and let the delegate change strategy deal with it.
            return changesAsArray;
        }
        
        // Clear the cached index values to force a rebuild of the document index values.
        await _indexDocumentService.DeleteAsync(relatedDocumentIds.ToArray(), _supportedContentState is ContentState.Published);
        
        // The effective changes to handle are the original input changes plus the related documents.
        return changesAsArray
            .Union(relatedDocumentIds
                .Except(documentIds)
                .Select(documentId =>
                    ContentChange.Document(documentId, ChangeImpact.Refresh, _supportedContentState)
                )
            );
    }
}