import "./SessionMain.css"
import InputIndicators from "./overlay/InputIndicators";
import VerticalSeperator from "./overlay/VerticalSeperator";
import StatsIndicators from "./overlay/statsIndicators";
import VolumeSlider from "./overlay/VolumeSlider";
import { BiCog } from "react-icons/bi";
import SettingsPanel from "./overlay/SettingsPanel";
import { useRef, useState } from "react";
import usePlayer from "../../contexts/usePlayer";
import useInput from "../../hooks/useInput";

export default function SessionMain() {
    const player = usePlayer();
    const input = useInput();
    const div = useRef<HTMLDivElement | null>(null);

    const [showSettings, setShowSettings] = useState(false);

    const onClick = (ev: React.MouseEvent<HTMLDivElement, MouseEvent>) => {
        if (ev.target == div.current) {
            input.doGrab();
        }
    }

    return (
        <div style={{ "width": "100%", "height": "100%", "margin": 0, "padding": 0 }}>
            <video ref={player.videoRef} className="sessionVideo" />

            <div style={{ "width": "100%", "height": "100%", "position": "absolute" }}>
                <div ref={div} onClick={onClick} className="overlayContainer">
                    <div className="overlayTopContainer">
                        <BiCog className={showSettings ? "settingsButton settingsVisible" : "settingsButton"} size={20} onClick={() => setShowSettings(!showSettings)} />
                        <VolumeSlider />
                        <VerticalSeperator />
                        <InputIndicators />
                        <VerticalSeperator />
                        <StatsIndicators />
                    </div>

                    <div className="settingsPanelContainer" style={{ "display": showSettings ? "flex" : "none" }}>
                        <SettingsPanel close={() => setShowSettings(false)} />
                    </div>
                </div>
            </div>
        </div>
    )
}