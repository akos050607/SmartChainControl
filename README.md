# 🏭 SmartChainControl – Next-Gen 3D Warehouse Simulation

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet)
![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?style=for-the-badge&logo=blazor)
![Babylon.js](https://img.shields.io/badge/Babylon.js-3D_Engine-BB464B?style=for-the-badge&logo=webgl)
![SignalR](https://img.shields.io/badge/SignalR-Realtime-blue?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Active_Development-success?style=for-the-badge)

**SmartChainControl** is a high-fidelity, real-time 3D warehouse simulation system. It demonstrates autonomous drone (AGV) pathfinding, task scheduling, and battery lifecycle management in a futuristic environment.

---

## 📸 Visuals

<p align="center">
  <img src="docs/image_2541ad.png" alt="3D Overview" width="45%">
  <img src="docs/image_bd4eee.png" alt="Drone Fleet" width="45%">
</p>
<p align="center">
  <img src="docs/image_6a5940.png" alt="Charging Station" width="80%">
</p>

> *Real-time 3D visualization featuring procedural drones, dynamic lighting, and a tactical HUD.*

---

## 🚀 Key Features

### 🧠 Intelligent Backend (Server)
* **Centralized Simulation:** Tick-based architecture running at 60Hz for smooth synchronization.
* **Pathfinding:** A* Dynamic route calculation avoiding static obstacles (shelves) and optimizing travel time.
* **Battery Lifecycle:** Every drone simulates battery drain based on usage.
  * **Auto-Return:** Drones automatically abort tasks and return to charging pads when battery is low (<30%).
  * **Charging Cycle:** Simulated charging time with visual feedback.
* **Real-time Comms:** **SignalR** with **MessagePack** protocol ensures low-latency, binary data transmission to the frontend.

### 🎨 Immersive Frontend (Client)
* **High-End 3D Engine:** Built with **Babylon.js**. Features PBR materials, real-time floor reflections, and dynamic lighting.
* **Procedural Drones:** No external 3D models used! Drones are generated via code with spinning propellers and floating animations.
* **Dynamic Light Signals:**
  * 🔴🔵 **Identity:** Robots emit their unique team color.
  * 🚨 **Hauling:** Intense strobe effect activates when carrying cargo.
  * ⚪ **Charging:** Soft, pulsing white light when docked and charging.
* **Sci-Fi HUD:** "Glassmorphism" UI panels displaying real-time telemetry (Speed, Battery %, Task Status).
* **Tactical Radar:** Live 2D minimap for total warehouse situational awareness.

---

## 🛠️ Tech Stack

| Component | Technology | Description |
| :--- | :--- | :--- |
| **Backend** | .NET 9 / ASP.NET Core | Simulation logic, SignalR Hub, Background Services. |
| **Frontend** | Blazor WebAssembly | C# in the browser, UI logic, JS Interop. |
| **3D Engine** | Babylon.js | WebGL rendering, shadows, animations, picking. |
| **Protocol** | SignalR + MessagePack | High-performance binary real-time communication. |
| **Styling** | CSS3 (Glassmorphism) | Modern, translucent UI design. |

---

## 🕹️ Controls

* **Camera Rotation:** Left Mouse Button + Drag
* **Camera Pan:** Right Mouse Button + Drag
* **Zoom:** Mouse Wheel
* **Select Unit:** Click on any flying drone to open the **Detail Panel** (Battery, ID, Cargo status).

---

*2026 - SmartChainControl Project*
