import useConnectionStats from "../../../hooks/useConnectionStats";

export default function StatsIndicators() {
    const connectionStats = useConnectionStats();

    return (
        <div style={{"display": "flex", "gap": "10px", "justifyContent": "center", "alignItems": "center"}}>
            <span>{connectionStats.latency.toFixed(0)}ms</span>
            <span>{(connectionStats.bitrate/1000).toFixed(2)}mbps</span>
            <span>{connectionStats.framerate.toFixed(0)}fps</span>
        </div>
    )
}