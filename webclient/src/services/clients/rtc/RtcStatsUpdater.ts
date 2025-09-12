import type ClientBase from "../ClientBase";

export class RtcStatsUpdater {
    private readonly _timer: NodeJS.Timeout;

    private _lastBytes = { sent: 0, received: 0 };
    private _lastTime = performance.now();
    private _currentRTT = 0;

    constructor(private _client: ClientBase, private _peer: RTCPeerConnection, private _mediaStream: MediaStream) {
        this._timer = setInterval(() => {
            this.onTimerCallback();
        }, 500);
    }

    public stop = () => {
        clearInterval(this._timer);
    }

    private onTimerCallback = async () => {
        if (this._peer) {
            const stats = await this._peer.getStats();

            stats.forEach(report => {
                if (
                    report.type === "candidate-pair" &&
                    report.state === "succeeded" &&
                    report.nominated
                ) {
                    this._currentRTT = report.currentRoundTripTime;
                }
            });

            const { bytesSent, bytesReceived } = await this.getNetworkUsage(this._peer);

            const now = performance.now();
            const deltaT = (now - this._lastTime) / 1000;

            //const sentRate = (bytesSent - this._lastBytes.sent) * 8 / deltaT; // bits/sec
            const recvRate = (bytesReceived - this._lastBytes.received) * 8 / deltaT;

            this._lastBytes = { sent: bytesSent, received: bytesReceived };
            this._lastTime = now;

            let frameRate = 0;

            const videoTracks = this._mediaStream.getVideoTracks();

            if (videoTracks.length == 1) {
                const videoTrack = videoTracks[0];

                const stats = await this._peer!.getStats(videoTrack);

                stats.forEach(report => {
                    if (report.framesPerSecond && report.kind === 'video') {
                        frameRate = report.framesPerSecond ?? 0;
                    }
                });
            }

            this._client.emit("onStats", ({
                latency: this._currentRTT,
                downloadRate: Math.round(recvRate / 1000),
                frameRate: frameRate
            }));
        }
    }

    async getNetworkUsage(pc: RTCPeerConnection) {
        const stats = await pc.getStats();

        let bytesSent = 0;
        let bytesReceived = 0;

        stats.forEach(report => {
            if (report.type === 'outbound-rtp' && !report.isRemote) {
                bytesSent += report.bytesSent ?? 0;
            }

            if (report.type === 'inbound-rtp' && !report.isRemote) {
                bytesReceived += report.bytesReceived ?? 0;
            }
        });

        return { bytesSent, bytesReceived };
    }
}