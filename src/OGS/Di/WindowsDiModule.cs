using OGS.Core.Platform;
using OGS.Windows;
using OGS.Windows.Audio;
using OGS.Windows.Video;
using StrongInject;

namespace OGS.Di;

[RegisterModule(typeof(BaseDiModule))]
[Register<WindowsPlatform, IPlatform>(Scope.SingleInstance)]
[Register<WinAudioCaptureFactory>(Scope.SingleInstance)]
[Register<D3DCaptureFactory>(Scope.SingleInstance)]
[Register<D3DProcessorFactory>(Scope.SingleInstance)]

public sealed partial class WindowsDiContainer : IContainer<App>
{
}
