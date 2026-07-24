using Microsoft.JSInterop;

namespace AggilleDFe.Web.Services;

public class ThemeService(IJSRuntime jsRuntime)
{
    private const string StorageKey = "aggilledfe-theme";

    public bool IsDarkMode { get; private set; }

    public event Action? OnChange;

    public async Task InitializeAsync()
    {
        var tema = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        IsDarkMode = tema == "dark";
    }

    public async Task ToggleAsync()
    {
        IsDarkMode = !IsDarkMode;
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, IsDarkMode ? "dark" : "light");
        OnChange?.Invoke();
    }
}
