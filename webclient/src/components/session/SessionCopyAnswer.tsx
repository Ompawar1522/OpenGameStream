import Panel from "../common/Panel";

export default function SessionCopyAnswer(props: {answer: string}){
    const doCopy = async () => {
        await navigator.clipboard.writeText(props.answer);
    }

    return(
        <Panel header="Answer generated">
            <button className="simple-button" onClick={doCopy}>Copy answer</button>
        </Panel>
    )
}