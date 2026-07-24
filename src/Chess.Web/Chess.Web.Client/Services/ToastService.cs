namespace Chess.Web.Client.Services;

public class ToastService
{
    public event Action<string, Models.ToastType>? OnShowToast;

    public void Show(string message, Models.ToastType type = Models.ToastType.Success)
    {
        OnShowToast?.Invoke(message, type);
    }
}
