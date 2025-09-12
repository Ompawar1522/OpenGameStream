# 🎮 OpenGameStream
### Stream & play games and emulators with friends

> ⚠️ **This is a proof of concept. Intended for testing**

**TLDR: The host generates an invite code. The code is pasted into the web app and a P2P connection is established. Host sends video, client sends mouse/keyboard/gamepad inputs.**

## 🎥 Demo

[![Demo](https://github.com/user-attachments/assets/78172c81-b3b4-49bb-983d-3c195067c6bd)](https://github.com/user-attachments/assets/78172c81-b3b4-49bb-983d-3c195067c6bd)

## ✨ Why not just use Parsec/Sunshine?
Parsec & Sunsine are focused on enabling unattended remote access. This project only focuses on streaming your games while maintaining privacy. 

Key differences: 
  - Supports **display capture, window capture or game capture**
  - **Fully portable host application** - no install, and runs without elevated privileges
  - **Client is just a web app** - send an invite code to your friend and they can play instantly.
  - Control individual client keyboard/mouse/gamepad access
  - **Fully decentralized** - there are no dedicated backend servers, meaning there is also no user accounts or user data.

## 🚀 Getting Started

### Prerequisite

- 🧩 [ViGEmBus Driver](https://github.com/nefarius/ViGEmBus/releases) *(required for creating virtual gamepads)*

### Steps

1.  [Download the latest build](https://github.com/ZetrocDev/OpenGameStream/releases)
2.  Extract and run `OGS.exe`
3.  Click **"Create Client"**
4.  Copy the **Client invite code**
5.  Paste the **client invite code** into the client app (https://opengamestream.pages.dev/)

You’re connected! 🎉



## ⚙️ How It Works

OpenGameStream uses MQTT brokers to negotiate a peer-to-peer connection via WebRtc. Any MQTT server can be used. The server will always be treated as untrusted and messages are encrypted end-to-end with an encryption key contained in the invite code.

### Connection Flow

1. The host generates a **base64-encoded invite code** with:
   - MQTT broker URL (and optional credentials)
   - AES-256 encryption key
   - Topics for signaling (publish/subscribe)
2. The client pastes this code into the browser app
3. A **WebRTC connection** is established using the MQTT server for signalling

> Fully offline connections (no MQTT) are possible with manual signaling, however this requires the client to copy an answer code back to the host.

---

## 🖥️ Platform Support

<table>
  <tr>
    <th>Windows</th>
    <th></th>
    <th>Linux </th>
    <th></th>
    <th>MacOs</th>
    <th></th>
  </tr>
  <tr>
    <td>Nvidia GPU</td>
    <td>✅</td>
    <td>Nvidia GPU</td>
    <td>❌</td>
    <td>Nvidia GPU</td>
    <td>❌</td>
  </tr>
  <tr>
    <td>Intel GPU</td>
    <td>✅</td>
    <td>Intel GPU</td>
    <td>❌</td>
    <td>Intel GPU</td>
    <td>❌</td>
  </tr>
  <tr>
    <td>AMD GPU</td>
    <td>❌</td>
    <td>AMD GPU</td>
    <td>❌</td>
    <td>AMD GPU</td>
    <td>❌</td>
  </tr>
  <tr>
    <td>Display Capture</td>
    <td>✅</td>
    <td>Portal Capture</td>
    <td>❌</td>
    <td/>
    <td/>
  </tr>
  <tr>
    <td>Window Capture</td>
    <td>✅</td>
    <td>KMS capture</td>
    <td>❌</td>
       <td/>
    <td/>
  </tr>
  <tr>
    <td>Game (D3D11) Capture</td>
    <td>✅</td>
    <td>NvFBC capture</td>
    <td>❌</td>
    <td/>
    <td/>
  </tr>
  <tr>
    <td/>
    <td/>
    <td>Pipewire capture</td>
    <td>❌</td>
    <td/>
    <td/>
  </tr>
</table>

> 🔧 Linux support is planned — stay tuned.

---

## 🧰 Tech Stack

- **C# / .NET 9** (compiled with Native AOT)
- [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen) ([Custom build](https://github.com/ZetrocDev/FFmpeg-min-gpu-build))
- [DataChannelDotnet](https://github.com/ZetrocDev/DataChannelDotnet) – WebRTC wrapper
- [AvaloniaUI](https://github.com/AvaloniaUI/Avalonia) – Cross-platform UI
- [ViGEmBus](https://github.com/nefarius/ViGEmBus) – Gamepad emulation *(may be replaced)*
- **Client**: React + static HTML
