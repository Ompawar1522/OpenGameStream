using OGS.Core.Config;
using OGS.Core.Host;
using OGS.Core.Ui;
using OGS.Services;
using OGS.ViewModels;
using StrongInject;

namespace OGS.Di;


[Register<HostContext, IHostContext>(Scope.SingleInstance)]
[Register<HostMediaHandler>(Scope.SingleInstance)]
[Register<HostInputHandler>(Scope.SingleInstance)]
[Register<ConfigService, IConfigService>(Scope.SingleInstance)]

[Register<App>(Scope.SingleInstance)]
[Register<MainWindowViewModel>(Scope.SingleInstance)]
[Register<DialogManager, IDialogManager>(Scope.SingleInstance)]
public class BaseDiModule
{
}
