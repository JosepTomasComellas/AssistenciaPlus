using MudBlazor;

namespace AssistenciaPlus.Web.Services;

public class BreadcrumbService
{
    private List<BreadcrumbItem> _items = [];

    public IReadOnlyList<BreadcrumbItem> Items => _items;
    public event Action? OnChange;

    public void Set(params BreadcrumbItem[] items)
    {
        _items = [..items];
        OnChange?.Invoke();
    }

    public void Clear()
    {
        _items = [];
        OnChange?.Invoke();
    }
}
