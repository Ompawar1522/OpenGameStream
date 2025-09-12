import { createContext, useContext, useEffect, useRef, useState } from "react";
import useClient from "./useClient";

export interface PlayerContextType {
    videoRef: React.Ref<HTMLVideoElement | null>,
    volume: number,
    isMuted: boolean,
    updateVolume: (vol: number) => void,
    toggleMuted: () => void,
    stretched: boolean,
    setStretched: (stretch: boolean) => void,
    fullscreen: boolean,
    toggleFullscreen: () => void
}


const PlayerContext = createContext<PlayerContextType | undefined>(undefined);

export const PlayerProvider = ({ children }: { children: React.ReactNode }) => {
    const client = useClient();
    const videoRef = useRef<HTMLVideoElement | null>(null);
    
    const [volume, setVolume] = useState(0);
    const [isMuted, setIsMuted] = useState(false);
    const [stretched, setStretched] = useState(false);
    const [fullscreen, setFullscreen] = useState(false);

    const onVolumeChange = () => {
        setVolume(videoRef.current!.volume);
    };

    const updateVolume = (volume: number) => {
        if (videoRef.current) {
            videoRef.current.volume = volume;
        }
    }

    const toggleMuted = () => {
        if (videoRef.current) {
            videoRef.current.muted = !isMuted;
            setIsMuted(videoRef.current.muted);
        }
    }

    const onFullscreenChanged = () => {
        console.log("Fullscreen changed!");

        setFullscreen(document.fullscreenElement != null);
    }

    const toggleFullscreen = async () => {
        if (document.fullscreenElement) {
            await document.exitFullscreen();
        } else {
            await document.body.requestFullscreen();
        }
    }

    useEffect(() => {
        if (videoRef.current) {
            videoRef.current.addEventListener("volumechange", onVolumeChange);
            document.addEventListener("fullscreenchange", onFullscreenChanged);


            videoRef.current.srcObject = client.mediaStream;
            videoRef.current.play();

            setStretched(videoRef.current.style.objectFit == "fill");
            setFullscreen(document.fullscreenElement != null);
            setIsMuted(videoRef.current.muted);
            setVolume(videoRef.current.volume);
        }

        return (() => {
            if (videoRef.current) {
                videoRef.current.removeEventListener("volumechange", onVolumeChange);
                document.removeEventListener("fullscreenchange", onFullscreenChanged);
            }
        })
    }, []);

    useEffect(() => {
        if (videoRef.current) {
            videoRef.current.style.objectFit = stretched ? "fill" : "contain";
        }
    }, [stretched]);

    return (
        <PlayerContext.Provider value={{
            videoRef,
            volume,
            isMuted,
            updateVolume,
            toggleMuted,
            stretched,
            setStretched,
            fullscreen,
            toggleFullscreen
        }}>
            {children}
        </PlayerContext.Provider>
    )
}

export default function usePlayer(): PlayerContextType {
    const ctx = useContext(PlayerContext);
    if (!ctx) {
        throw new Error("usePlayer must be used within a ClientProvider");
    }
    return ctx;
}