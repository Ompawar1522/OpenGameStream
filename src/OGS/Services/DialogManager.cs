using Avalonia.Controls;
using MsBox.Avalonia.Base;
using OGS.Core.Ui;
using System.Threading.Tasks;

namespace OGS.Services;

public sealed class DialogManager : IDialogManager
{
    internal Window MainWindowInstance { get; set; } = null!;

    public async Task<T> ShowMessageBoxAsync<T>(IMsBox<T> box)
    {
        return await box.ShowWindowDialogAsync(MainWindowInstance);
    }
}
