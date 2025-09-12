import { LuVolume, LuVolume1, LuVolume2, LuVolumeOff } from "react-icons/lu";
import "./VolumeSlider.css"
import usePlayer from "../../../contexts/usePlayer";

export default function VolumeSlider() {
    const player = usePlayer();

    const getIcon = () => {
        if(player.isMuted || player.volume == 0)
            return <LuVolumeOff className="muteButton" onClick={player.toggleMuted} size={20}/>
        if(player.volume < 0.2)
            return <LuVolume className="muteButton" onClick={player.toggleMuted} size={20}/>
        else if(player.volume < 0.7)
            return <LuVolume1 className="muteButton" onClick={player.toggleMuted} size={20}/>
        else
            return <LuVolume2 className="muteButton" onClick={player.toggleMuted} size={20}/>
    }

    return (
        <>
            {getIcon()}
            <input style={{"width": "70px"}} type="range" min={0} max={1} step={0.01} value={player.volume} onChange={(ev) => player.updateVolume(ev.target.valueAsNumber)}/>
        </>
    )
}