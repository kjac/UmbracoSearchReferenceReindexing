using Site.ContentChangeStrategies;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Search.BackOffice.DependencyInjection;
using Umbraco.Cms.Search.Core;
using Umbraco.Cms.Search.Core.Configuration;
using Umbraco.Cms.Search.Core.DependencyInjection;
using Umbraco.Cms.Search.Core.Services;
using Umbraco.Cms.Search.Provider.Examine.DependencyInjection;

namespace Site.DependencyInjection;

public sealed class SiteComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder
            // Add core services for search abstractions.
            .AddSearchCore()
            // Add the Examine search provider.
            .AddExamineSearchProvider()
            // Use Umbraco Search for backoffice search.
            .AddBackOfficeSearch();

        // Register the custom change strategies.
        builder.Services.AddSingleton<ReindexRelationsPublishedContentChangeStrategy>();
        builder.Services.AddSingleton<ReindexRelationsDraftContentChangeStrategy>();

        // Re-register the default content indexes to use the custom content change strategies.
        builder.Services.Configure<IndexOptions>(options =>
        {
            options.RegisterContentIndex<IIndexer, ISearcher, ReindexRelationsPublishedContentChangeStrategy>
            (
                Constants.IndexAliases.PublishedContent,
                UmbracoObjectTypes.Document
            );
            options.RegisterContentIndex<IIndexer, ISearcher, ReindexRelationsDraftContentChangeStrategy>
            (
                Constants.IndexAliases.DraftContent,
                UmbracoObjectTypes.Document
            );
        });
    }
}