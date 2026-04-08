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
    public int CurrentPage { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "size")]
    public int PageSize { get; set; }

    protected void AppendUrl(string key, object? value)
    {
        var queryParameters = new Dictionary<string, object?>
        {
            { key, value }
        };
        var newUrl = Navigation.GetUriWithQueryParameters(queryParameters);
        Navigation.NavigateTo(newUrl, replace: false);
    }
    
    protected void UpdateUrlState(string? term, int page, int size)
    {
        var queryParameters = new Dictionary<string, object?>
        {
            { "q", string.IsNullOrWhiteSpace(term) ? null : term },
            { "page", page < 1 ? 1 : page },
            { "size", size < 10 ? 10 : size }
        };

        var newUrl = Navigation.GetUriWithQueryParameters(queryParameters);
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