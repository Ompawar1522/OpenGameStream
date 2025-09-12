import type RtcClientOptions from "../rtc/RtcClientOptions";

export default interface ManualRtcClientOptions extends RtcClientOptions{
    offerSdp: string;
}