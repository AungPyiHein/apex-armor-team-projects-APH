using Microsoft.AspNetCore.Components;

namespace MiniPos.Frontend.Shared.Components;

public abstract class FilterablePageBase : ComponentBase
{
    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    [Parameter]
    [SupplyParameterFromQuery(Name = "q")]
    public string SearchTerm { get; set; } = string.Empty;

    [Parameter]
    [SupplyParameterFromQuery(Name = "page")]
    public int CurrentPage { get; set; } = 1;

    [Parameter]
    [SupplyParameterFromQuery(Name = "size")]
    public int PageSize { get; set; } = 10;

    private Dictionary<string, object?> DefaultQuery(string term = "", int page = 1, int size = 10)
    {
        return new Dictionary<string, object?>
        {
            { "q", term },
            { "page", page },
            { "size", size }
        };
    }

    protected void AppendQuery(string key, object? value)
    {
        var queries = DefaultQuery();
        queries.Add(key, value);
        var newUrl = Navigation.GetUriWithQueryParameters(queries);
        Navigation.NavigateTo(newUrl, replace: false);
    }

    protected void UpdateUrlState(string term, int page, int size)
    {
        var queries = DefaultQuery();
        var newUrl = Navigation.GetUriWithQueryParameters(queries);
        Navigation.NavigateTo(newUrl, replace: false);
    }

    protected void HandleSearchTermChanged(string? term)
    {
        UpdateUrlState(term, CurrentPage, PageSize);
    }

    protected void HandlePageChanged(int page)
    {
        UpdateUrlState(SearchTerm, page, PageSize);
    }

    protected void HandlePageSizeChanged(int size)
    {
        UpdateUrlState(SearchTerm, CurrentPage, size);
    }

    protected void HandleNextPage()
    {
        UpdateUrlState(SearchTerm, CurrentPage + 1, PageSize);
    }

    protected void HandlePreviousPage()
    {
        UpdateUrlState(SearchTerm, CurrentPage - 1, PageSize);
    }

    protected void HandleFirstPage()
    {
        UpdateUrlState(SearchTerm, 1, PageSize);
    }
}