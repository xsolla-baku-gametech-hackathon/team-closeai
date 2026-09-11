<div align="center">

# 🌌 NetGHOST
**Jitter-Free, Mathematically Driven Network Simulation (Unity URP)**

[![Unity Version](https://img.shields.io/badge/Unity-2022.3%2B-black?logo=unity)](#)
[![Render Pipeline](https://img.shields.io/badge/Pipeline-URP-blue)](#)
[![Architecture](https://img.shields.io/badge/Complexity-O(1)-success)](#)
[![License](https://img.shields.io/badge/License-MIT-orange)](#)

*A frame-rate independent algorithm that recovers motion with $C^2$ continuity during high ping and packet loss without freezing frames.*

---

</div>

## 📖 About the Project

**Inertial Kinematic Netcode** is an innovative architecture designed to prevent the jarring teleportation (*rubberbanding*) of objects during network interruptions (packet loss) and sudden ping spikes in multiplayer or real-time simulation environments.

Unlike traditional, heavy *Rollback* and *Resimulation* systems, this project decouples the server and visual layers. Instead of recalculating past frames, it uses kinematic formulas and polynomial curves to deliver a **perfectly smooth, zero-CPU-strain ($< 0.05\text{ ms}$)** visual experience.

## ⚔️ Problem vs. Solution (Classic vs. Inertial)

| Feature | 🔴 Classic Netcode | 🟢 Inertial Netcode (Our Solution) |
| :--- | :--- | :--- |
| **Network Spike (Packet Loss)** | Object instantly freezes (*Freeze*) | Motion continues seamlessly via kinematic inertia |
| **Recovery Moment** | Harsh Teleportation (*Hard Snap*) | Velocity and acceleration are smoothed out |
| **CPU Load** | Heavy ($O(N \cdot K)$ Resimulation) | Near Zero ($O(1)$ Analytical Formula) |
| **Error Handling** | Unbounded divergence (*Breakout*) | Exponential damping and Risk Envelope |

## 🧬 3-Phase Technical Architecture

During network disruptions and recoveries, the system processes motion through the following 3 mathematical phases:

### 1. Damped Prediction Phase (Kinematic Extrapolation)
The moment packet loss begins, derivative vectors ($v, a, \omega$) are extracted from the last 3 received *snapshots*. To prevent the object from endlessly flying off the map, the motion is subjected to an exponential damping algorithm:
$$v(t) = v_0 e^{-\lambda t} + a_0 t e^{-\lambda t}$$

### 2. Bounded Uncertainty Phase (Risk Envelope)
As the packet loss prolongs, the system's margin of error increases. This error matrix $R(t)$ is visually represented to the user in real-time as an expanding holographic ring shifting from neon green to warning red. Simultaneously, the prediction duration is clamped within a safe limit (e.g., $2.0\text{ s}$).

### 3. Reconciliation Phase ($C^2$ Continuity)
When a new authoritative network packet finally arrives, the object is not visually teleported instantly. Instead, a 5th-degree Hermite polynomial curve ($S(t) = 6t^5 - 15t^4 + 10t^3$) is applied, zeroing out velocity and acceleration spikes during the transition. This results in flawless visual continuity that is completely imperceptible to the human eye.

---

## 🚀 Installation & 1-Click Startup

This project is built with the highest standards of editor automation. There is **no need** to manually build the scene or link objects in the hierarchy.

### Requirements:
* Unity 2022.3 or newer (URP must be active).
* `sun.jpg` and `earth.jpg` files placed in the `Assets/Textures/` folder (used for procedural space and planetary materials).

### Setup Steps:
1. Open the project in Unity.
2. Run the custom Editor script from the top menu:
   > **`Tools` ➔ `Auto Build Complete Scene`**
3. You're all set! The script will instantly and automatically generate:
   * Split-Screen cameras and independent telemetry UI panels.
   * Fully illuminated, realistic Sun and self-rotating Earth spheres.
   * A procedurally generated infinite space background (*Starfield*) with 3000 dynamic stars.
   * All network simulators and script bindings.
4. Hit `Play` and press **[Space]** to toggle Packet Loss, allowing you to see the visual difference side-by-side.

---

## 🛠 Technical Highlights (Pitch Points)

* 🎯 **Time-Domain Precision (FPS-Agnostic):** Whether the application runs at 30, 60, or 144 FPS makes no difference—the algorithm is bound to real-time math, not frame rates.
* 🧩 **Decoupled Engine Architecture:** The server's logic and the client's visualization are entirely separate. This allows easy integration into any physics engine or custom protocol.
* ⚡ **Ultra-Low CPU Execution Complexity:** Completely prevents frame-time spikes with a constant $O(1)$ execution complexity per component.

<br>

<div align="center">
  <sub>Author: Ümid Əsədov</sub>
</div>
