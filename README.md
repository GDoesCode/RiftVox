# RiftVox 🎙️⚡

RiftVox is a lightweight, decentralized proximity voice chat application designed for League of Legends teammates. By leveraging the official local game APIs and a peer-to-peer audio mesh network, RiftVox introduces spatial audio attenuation to the Summoner's Rift without violating anti-cheat policies.

---

## 🚀 The Core Engineering Challenges

Building a third-party application for a game protected by kernel-level anti-cheat (Riot Vanguard) introduces strict constraints. RiftVox solves these through an intentional, hybrid architecture:

* **Vanguard Compliance:** Rather than scraping system memory or injecting code (which triggers instant bans), RiftVox polls the official **Live Client Data API** via local loopback (`127.0.0.1:2999`).
* **Audio Stability vs. Performance:** To avoid the massive engineering overhead of writing a custom WebRTC jitter buffer and echo cancellation engine in native C#, RiftVox utilizes a **Hybrid Architecture**. A native C# engine handles high-frequency data polling and coordinate math, while a lightweight, embedded **Microsoft WebView2** container hosts Google’s production-grade Chromium WebRTC stack.
* **Cross-Team Syncing (Optional Mode):** To allow proximity chat with friends playing on the opposing team, the application supports an encrypted data-relay mode via the signaling server, passing client coordinates strictly to the audio layer while remaining invisible to the game client.

---

## 🛠️ Tech Stack

* **Backend Engine:** C# (.NET 9)
* **Desktop UI Container:** WPF / WinForms with Microsoft WebView2
* **Audio Layer:** Web Audio API (Logarithmic Gain Nodes)
* **P2P Networking:** WebRTC (DataChannels & MediaStreams)
* **Signaling Infrastructure:** Node.js, Socket.io (Hosted on Linux/Railway)

---

## 📐 Architecture & Data Pipeline

+-----------------------+      Polls X/Y/Z      +-----------------------+
|  League Game Client   | --------------------> |     C# .NET Core      |
|  (Local Port 2999)    |                       | (Distance Calculator) |
+-----------------------+                       +-----------------------+
                                                            |
                                                   Passes Volume Matrix
                                                   via Inter-Process Comm
                                                            v
+-----------------------+   P2P Audio Stream    +-----------------------+
| Remote Teammate (App) | --------------------> |   WebView2 Container  |
|                       |                       |  (Web Audio/WebRTC)   |
+-----------------------+                       +-----------------------+

1. **The Polling Loop:** The C# background service hits the local client API every 100ms to grab player coordinates.
2. **Distance Calculation:** The backend calculates the Euclidean distance between the user and active party members.
3. **IPC Bridge:** Calculated coordinates are passed as JSON strings across the native bridge into the WebView2 instance using high-performance `WebMessages`.
4. **Audio Attenuation:** The JavaScript layer maps the distance to a logarithmic scale and dynamically updates a Web Audio `GainNode` connected to that peer's audio track.

---

## ⚙️ Local Setup & Installation

### Prerequisites
* .NET 9 SDK
* Node.js (v18+ for running the signaling server locally)

### 1. Clone & Install Dependencies
git clone https://github.com/YOUR_USERNAME/RiftVox.git
cd RiftVox/signaling-server
npm install

### 2. Start the Signaling Server
npm start

### 3. Build and Run the Client
Open `src/RiftVox.sln` in Visual Studio 2022 or Rider. Ensure the signaling server URL in your configuration matches your target host, build the solution, and launch the application alongside a live League of Legends match.

---

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## ⚖️ Legal Disclaimer

RiftVox isn't endorsed by Riot Games and doesn't reflect the views or opinions of Riot Games or anyone officially involved in producing or managing Riot Games properties. Riot Games and all associated properties are trademarks or registered trademarks of Riot Games, Inc.
