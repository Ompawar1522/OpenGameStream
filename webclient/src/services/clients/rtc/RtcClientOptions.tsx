export default interface RtcClientOptions{
    stunServer?: RTCIceServer;
    turnServer?: RTCIceServer;
    forceTurnServer: boolean;
}