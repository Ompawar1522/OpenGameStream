import type { MouseButton, ScrollDirection, GamepadAxisEnum, GamepadButtonEnum } from "../../data/InputData";
import ClientBase, { ClientConnectionState } from "../ClientBase";
import type RtcClientOptions from "./RtcClientOptions";
import { ParseRtcMessage } from "./RtcMessageParser";
import RtcMessageWriter from "./RtcMessageWriter";
import { RtcStatsUpdater } from "./RtcStatsUpdater";

export default abstract class RtcClientBase extends ClientBase {
    protected peer?: RTCPeerConnection;
    protected commandChannel?: RTCDataChannel;
    protected messageWriter?: RtcMessageWriter;
    protected statsUpdater?: RtcStatsUpdater;
    protected bufferedCandidates: RTCIceCandidateInit[] = [];


    constructor(private _rtcOptions: RtcClientOptions) {
        super();
    }

    public abstract start(): void;
    public abstract stop(): void;

    protected abstract onLocalCandidate(candidate: RTCPeerConnectionIceEvent): void;
    protected abstract onConnectionState(): void;
    protected abstract onSignalingState(): void;
    protected abstract onIceConnectionState(): void;
    protected abstract onIceGatheringState(): void;

    protected bufferCandidate = (candidate: RTCIceCandidateInit) => {
        this.bufferedCandidates.push(candidate);
    }

    protected pushBufferedCandidates = () => {
        this.bufferedCandidates.forEach(x => {
            try {
                this.peer?.addIceCandidate(x);
            } catch (err) {
                console.error("Failed to add ice candidate: " + err);
            }
        });
    }

    public sendMouseMove(x: number, y: number): void {
        if (this.messageWriter) {
            this.messageWriter.SendMouseMove(x, y);
        }
    }

    public sendMouseButton(button: MouseButton, pressed: boolean): void {
        if (this.messageWriter) {
            this.messageWriter.SendMouseButton(button, pressed);
        }
    }

    public sendMouseScroll(direction: ScrollDirection): void {
        if (this.messageWriter) {
            this.messageWriter.SendMouseScroll(direction);
        }
    }

    public sendKeyboardKey(key: number, pressed: boolean): void {
        if (this.messageWriter) {
            this.messageWriter.SendKeyboardKey(key, pressed);
        }
    }

    public sendGamepadAxis(axis: GamepadAxisEnum, value: number): void {
        if (this.messageWriter) {
            this.messageWriter.SendGamepadAxis(axis, value);
        }
    }

    public sendGamepadButton(button: GamepadButtonEnum, pressed: boolean): void {
        if (this.messageWriter) {
            this.messageWriter.SendGamepadButton(button, pressed);
        }
    }

    protected onCommandMessage = (ev: MessageEvent<any>) => {
        ParseRtcMessage(this, ev);
    }

    protected RecreatePeer = () => {
        this.closePeer();
        this.createPeer();
    }

    protected createPeer = () => {
        this.closePeer();

        this.bufferedCandidates.length = 0;

        const iceServers: RTCIceServer[] = [];

        if (this._rtcOptions.stunServer) {
            iceServers.push(this._rtcOptions.stunServer);
        }
        if (this._rtcOptions.turnServer) {
            iceServers.push(this._rtcOptions.turnServer);
        }

        this.peer = new RTCPeerConnection({
            iceServers: iceServers,
            iceTransportPolicy: this._rtcOptions.forceTurnServer ? "relay" : "all"
        });

        this.peer.onconnectionstatechange = this.onConnectionState.bind(this);
        this.peer.onicecandidate = this.onLocalCandidate.bind(this);
        this.peer.onsignalingstatechange = this.onSignalingState.bind(this);
        this.peer.oniceconnectionstatechange = this.onIceConnectionState.bind(this);
        this.peer.onicegatheringstatechange = this.onIceGatheringState.bind(this);

        this.peer.ontrack = (event) => {
            if (event.track.kind === "video" || event.track.kind === "audio") {
                this.mediaStream.addTrack(event.track);
                console.log("Added remote track: " + event.track.kind);
            }
        };

        this.peer.ondatachannel = (event) => {
            if (event.channel.label === "command") {
                this.commandChannel = event.channel;
                this.commandChannel.onmessage = this.onCommandMessage;

                this.commandChannel.onopen = () => {
                    console.log("Command channel opened");
                    this.messageWriter = new RtcMessageWriter(this.commandChannel!);
                    this.updateConnectedState();
                }

                this.commandChannel.onclose = () => {
                    console.log("Command channel closed");
                    this.updateConnectedState();
                }
            };
        }

        this.statsUpdater = new RtcStatsUpdater(this, this.peer, this.mediaStream);
    }

    protected closePeer = () => {
        this.messageWriter = undefined;
        this.bufferedCandidates.length = 0;

        this.mediaStream.getTracks().forEach(track => {
            track.stop();
            this.mediaStream.removeTrack(track);
        });

        if (this.statsUpdater) {
            this.statsUpdater.stop();
            this.statsUpdater = undefined;
        }

        if (this.commandChannel) {
            this.commandChannel.onopen = null;
            this.commandChannel.onclose = null;
            this.commandChannel.onmessage = null;

            this.commandChannel.close();
            this.commandChannel = undefined;
        }

        if (this.peer) {

            this.peer.ontrack = null;
            this.peer.ondatachannel = null;
            this.peer.onconnectionstatechange = null;
            this.peer.onicecandidate = null;
            this.peer.onsignalingstatechange = null;
            this.peer.oniceconnectionstatechange = null;
            this.peer.onicegatheringstatechange = null;

            this.peer.close();
            this.peer = undefined;

            console.log("Closed peer");
        }
    }

    protected updateConnectedState = () => {
        if (this.state == ClientConnectionState.Closed)
            return;

        if (this.state == ClientConnectionState.Connected) {
            if (!this.isFullyConnected()) {
                this.updateState(ClientConnectionState.Connecting);
                this.onRtcDisconnected();
            }

        } else {
            if (this.isFullyConnected()) {
                this.updateState(ClientConnectionState.Connected);
                this.onRtcConnected();
            }
        }
    };

    protected isFullyConnected = () => {
        return (
            this.peer &&
            this.peer.connectionState == "connected" &&
            this.commandChannel &&
            this.commandChannel.readyState == "open"
        );
    }

    protected onRtcConnected: () => void = () => {}
    protected onRtcDisconnected: () => void = () => {}
}