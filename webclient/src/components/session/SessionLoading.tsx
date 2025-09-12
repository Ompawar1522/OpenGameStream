import { ClipLoader } from "react-spinners";
import Panel from "../common/Panel";

export default function SessionLoading(){
    return(
        <Panel header="Connecting...">
            <ClipLoader size={30} color="white"/>
        </Panel>
    )
}