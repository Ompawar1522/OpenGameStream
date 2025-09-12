using MsBox.Avalonia.Base;

namespace OGS.Core.Ui;

public interface IDialogManager
{
    Task<T> ShowMessageBoxAsync<T>(IMsBox<T> box);
}
