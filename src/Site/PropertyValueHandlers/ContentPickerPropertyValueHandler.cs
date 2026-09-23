using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Search.Core.Extensions;
using Umbraco.Cms.Search.Core.Models.Indexing;
using Umbraco.Cms.Search.Core.PropertyValueHandlers;

namespace Site.PropertyValueHandlers;

public class ContentPickerPropertyValueHandler : IPropertyValueHandler
{
    private readonly IContentService _contentService;

    public ContentPickerPropertyValueHandler(IContentService contentService)
        => _contentService = contentService;

    public bool CanHandle(string propertyEditorAlias)
        => propertyEditorAlias is Constants.PropertyEditors.Aliases.ContentPicker;

    public IEnumerable<IndexField> GetIndexFields(IProperty property, string? culture, string? segment, bool published, IContentBase contentContext)
    {
        // The document picker stores an UDI, so let's parse that.
        var value = property.GetValue(culture, segment, published) as string;
        if (value.IsNullOrWhiteSpace()
            || UdiParser.TryParse(value, out Udi? udi) is false
            || udi is not GuidUdi guidUdi)
        {
            return [];
        }

        // Get the content name for indexing.
        var contentName = GetContentName(guidUdi.Guid, culture, published);
        
        // Index the content name as text for free text search.
        // Retain the default filtering ability by including the content key as a keyword.
        return contentName.NullOrWhiteSpaceAsNull() is not null
            ? [
                new IndexField(
                    property.Alias,
                    new IndexValue
                    {
                        Keywords = [guidUdi.Guid.AsKeyword()],
                        Texts = [contentName!],
                    },
                    culture,
                    segment)
            ]
            : [];
    }

    private string? GetContentName(Guid key, string? culture, bool published)
    {
        // Fetch the actual content from the content service.
        var content = _contentService.GetById(key);
        if (content is null)
        {
            return null;
        }

        // Return the content name - draft or published, depending on what's being indexed.
        // Use the culture specific names if applicable.
        return culture.NullOrWhiteSpaceAsNull() is not null && content.ContentType.VariesByCulture()
            ? published
                ? content.GetPublishName(culture)
                : content.GetCultureName(culture)
            : published
                ? content.PublishName
                : content.Name;
    }
}