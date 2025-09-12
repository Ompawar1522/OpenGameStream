using DataChannelDotnet;
using OGS.Core.Common.Input;

namespace OGS.Core.Clients.Rtc;

public sealed class RtcCommandWriter
{
    private readonly IRtcDataChannel _dataChannel;

    public RtcCommandWriter(IRtcDataChannel dataChannel)
    {
        _dataChannel = dataChannel;
    }

    public void SendInputMethodsUpdate(InputMethods methods)
    {
        Span<byte> buffer = stackalloc byte[4];

        buffer[0] = (byte)HostCommandType.AllowedInputsUpdate;
        buffer[1] = (byte)(methods.HasFlag(InputMethods.Mouse) ? 1 : 0);
        buffer[2] = (byte)(methods.HasFlag(InputMethods.Keyboard) ? 1 : 0);
        buffer[3] = (byte)(methods.HasFlag(InputMethods.Gamepad) ? 1 : 0);

        _dataChannel.Send(buffer);
    }
}
