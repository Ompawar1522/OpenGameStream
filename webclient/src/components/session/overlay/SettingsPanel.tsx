import usePlayer from "../../../contexts/usePlayer";
import Panel from "../../common/Panel";

export interface SettingsPanelProps{
    close: () => void;
}

export default function SettingsPanel(props: SettingsPanelProps){
    const player = usePlayer();

    return(
        <Panel style={{"backgroundColor": "rgba(0, 0, 0, 1)", "borderColor": "orange", "border": "solid"}} header="Settings">

            <div style={{"display": "flex", "gap": "5px"}}>
                <input type="checkbox" checked={player.stretched} onChange={() => player.setStretched(!player.stretched)}/>
                <span>Stretch to screen</span>
            </div>
            <button className="simple-button" onClick={props.close}>Close</button>
        </Panel>
    )
}