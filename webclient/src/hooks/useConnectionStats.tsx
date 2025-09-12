import { useEffect, useState } from "react";
import useClient from "../contexts/useClient";
import type { ClientStats } from "../services/clients/ClientStats";

export default function useConnectionStats(){
    const client = useClient();

    const [bitrate, setBitrate] = useState(0);
    const [framerate, setFramerate] = useState(0);
    const [latency, setLatency] = useState(0);
    
    const onStats = (stats: ClientStats) => {
        setFramerate(stats.frameRate);
        setBitrate(stats.downloadRate);
        setLatency(stats.latency);
    }

    useEffect(() => {
        client.on("onStats", onStats);

        return(() => {
            client.off("onStats", onStats);
        })
    }, []);

    return {
        bitrate,
        framerate,
        latency
    }
}