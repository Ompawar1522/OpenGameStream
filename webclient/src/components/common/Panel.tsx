import "./Panel.css"

export interface PanelProps {
    header?: string
}

export default function Panel({ header, children, style }: { header: string, children: React.ReactNode, style?: React.CSSProperties }) {
    return (
        <div className="panelContainer">
            <div className="panelSubContainer" style={style}>
                <h2 className="panelHeader">{header}</h2>
                <div className="horizontalSeperator" />
                <div className="panelBody">
                    {children}
                </div>
            </div>
        </div>
    )
}