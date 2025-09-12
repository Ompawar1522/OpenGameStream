import type ClientBase from "../ClientBase";

enum HostCommandType {
    Unknown = 0,
    AllowedInputsUpdate
}

export function ParseRtcMessage(client: ClientBase, message: MessageEvent<any>) {
    const buffer = message.data as ArrayBuffer;
    const view = new DataView(buffer);

    const type: HostCommandType = view.getInt8(0);
    console.log("PARSE " + type);

    switch (type) {
        case HostCommandType.AllowedInputsUpdate:
            ParseInputMethodsUpdate(client, view);
            return;
    }
}

function ParseInputMethodsUpdate(client: ClientBase, data: DataView) {
    if (data.byteLength != 4)
        return;

    console.log("Updating methods!");

    const methods = {
        mouse: data.getInt8(1) > 0,
        keyboard: data.getInt8(2) > 0,
        gamepad: data.getInt8(3) > 0
    }

    client.inputMethods = methods;
    client.emit("onInputMethods", methods);
}