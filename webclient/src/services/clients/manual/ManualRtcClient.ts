import { ClientConnectionState } from "../ClientBase";
import type { ManualRtcAnswerData } from "../ClientInviteData";
import RtcClientBase from "../rtc/RtcClientBase";
import type ManualRtcClientOptions from "./ManualRtcClientOptions";

export default class ManualRtcClient extends RtcClientBase {
    private _startPromise?: Promise<void>;
    private _close: boolean = false;

    constructor(private _options: ManualRtcClientOptions) {
        super(_options);
    }

    public start(): void {
        if (this._startPromise)
            return;

        this._startPromise = this.internalStart();
    }

    private internalStart = async () => {
        try {
            this.createPeer();
            await this.peer!.setRemoteDescription({
                type: "offer",
                sdp: this._options.offerSdp
            });

            this.throwifClosed();
            const answer = await this.peer!.createAnswer();
            this.throwifClosed();
            await this.peer!.setLocalDescription(answer);
            this.throwifClosed();
        } catch (err) {
            console.error("Failed to start manual Rtc client: " + err);
            this.closePeer();
            this.updateState(ClientConnectionState.Closed);

            alert("Failed to start client: " + err);
        }
    }

    public getAnswer = () => {
        const answerData: ManualRtcAnswerData = {
            Sdp: this.peer!.localDescription!.sdp
        };

        return btoa(JSON.stringify(answerData));
    }

    public stop(): void {
        this._close = true;
        this.closePeer();
    }

    protected onLocalCandidate(candidate: RTCPeerConnectionIceEvent): void {
        if (this._close)
            return;

        //If candidate.candidate is null, then all candidates have been generated and 
        //we can create an answer
        if (!candidate.candidate) {
            console.log("Candidate gathering complete");

            this.updateState(ClientConnectionState.HasAnswer);
        }
    }

    protected onConnectionState(): void {
        this.updateConnectedState();
    }

    protected onSignalingState(): void {

    }

    protected onIceConnectionState(): void {
        this.updateConnectedState();
    }

    protected onIceGatheringState(): void {

    }
    
    protected onRtcConnected = () => {

    };

    protected onRtcDisconnected = () => {
        //Manual clients can't reconnect
        this.updateState(ClientConnectionState.Closed);
        this.stop();
    }

    private throwifClosed() {
        if (this._close)
            throw new Error("Client was closed");
    }
}