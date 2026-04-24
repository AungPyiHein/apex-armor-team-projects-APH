namespace MiniPos.Frontend.Shared.Services;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

public record ToastMessage(
    Guid Id,
    string Message,
    ToastType Type,
    string? Title = null
);

public class NotificationService
{
    private readonly List<ToastMessage> _toasts = new();
    public IReadOnlyList<ToastMessage> Toasts => _toasts.AsReadOnly();

    public event Action? OnChange;

    public async Task ShowSuccess(string message, string? title = null)
    {
        await Show(message, ToastType.Success, title);
    }

    public async Task ShowError(string message, string? title = null)
    {
        await Show(message, ToastType.Error, title);
    }

    public async Task ShowWarning(string message, string? title = null)
    {
        await Show(message, ToastType.Warning, title);
    }

    public async Task ShowInfo(string message, string? title = null)
    {
        await Show(message, ToastType.Info, title);
    }

    private async Task Show(string message, ToastType type, string? title, int durationMs = 5000)
    {
        var toast = new ToastMessage(Guid.NewGuid(), message, type, title);
        _toasts.Add(toast);
        OnChange?.Invoke();

        await Task.Delay(durationMs);

        Dismiss(toast.Id);
    }

    public void Dismiss(Guid id)
    {
        var toast = _toasts.Find(t => t.Id == id);
        if (toast is not null)
        {
            _toasts.Remove(toast);
            OnChange?.Invoke();
        }
    }
}