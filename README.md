# 🏎️ DriftIT — High-Octane 2D Drift & Racing Game

<div align="center">

![Unity](https://img.shields.io/badge/Unity-2022.3%2B-blue?logo=unity&style=for-the-badge)
![Language](https://img.shields.io/badge/C%23-239120?logo=c-sharp&logoColor=white&style=for-the-badge)
![Networking](https://img.shields.io/badge/Netcode%20NGO-Relay-red?style=for-the-badge)
![Backend](https://img.shields.io/badge/Firebase-Realtime%20DB-orange?logo=firebase&style=for-the-badge)
![Deployment](https://img.shields.io/badge/Vercel-Deployed-black?logo=vercel&style=for-the-badge)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

**Master the asphalt, tame the drift, and dominate the world record boards in single-player or 2-player multiplayer showdowns!**

[Live Leaderboard](https://driftit-6dd08-default-rtdb.asia-southeast1.firebasedatabase.app) • [Deploy to Vercel](#-deploying-the-leaderboard-website) • [Controls](#-controls--keybindings) • [Multiplayer Setup](#-multiplayer--relay-configuration)

</div>

---

## 📖 Table of Contents

- [Overview](#-overview)
- [✨ Key Features](#-key-features)
- [🎮 Controls & Keybindings](#-controls--keybindings)
- [🏁 Game Modes](#-game-modes)
- [🗺️ Track & Stage Progression](#️-track--stage-progression)
- [🌐 Global Leaderboard & Web Dashboard](#-global-leaderboard--web-dashboard)
- [🚀 Deploying the Leaderboard Website](#-deploying-the-leaderboard-website)
  - [Deploy to Vercel](#option-1-deploy-to-vercel-recommended)
  - [Deploy to GitHub Pages](#option-2-deploy-to-github-pages-automated)
  - [Deploy to Netlify](#option-3-deploy-to-netlify)
  - [Run Locally](#option-4-run-locally)
- [⚡ Backend & Firebase Setup](#-backend--firebase-setup)
- [🔌 Multiplayer & Relay Configuration](#-multiplayer--relay-configuration)
- [📂 Project Structure](#-project-structure)
- [🛠️ Building & Running the Game](#️-building--running-the-game)
- [📜 License](#-license)

---

## 🌟 Overview

**DriftIT** is a fast-paced, top-down 2D arcade drifting and precision racing game built with Unity and C#. Featuring custom lateral-slip physics, jump ramps, environmental hazards, dynamic particle effects, and cloud-synced leaderboards, DriftIT tests your reflexes and racing lines across single-player time trials and 2-player networked battles.

---

## ✨ Key Features

- **🏎️ Precision Drift Physics**: Lateral traction calculation, inertia preservation, dynamic drift turning multipliers, and realistic tire skid trail generation.
- **👥 2-Player Head-to-Head Multiplayer**: Integrated via **Unity Netcode for GameObjects (NGO)** and **Unity Relay** for seamless, low-latency cross-network room codes without port-forwarding.
- **⏱️ Time-Trial Campaign (6 Levels + Ending)**: Handcrafted levels featuring tight turns, speed ramps, jump gaps, hazards, and secret areas.
- **🐑 Dynamic Hazards & Debuffs**:
  - Interactive entities (e.g., sheep collisions causing temporary traction/speed penalties).
  - Hazard zones, bloodstain physics, death markers, and instant respawn checkpoints.
  - Car health and visual destruction mechanics.
- **📊 Global Top 200 Cloud Leaderboard**:
  - Real-time submission directly from Unity to **Firebase Realtime Database**.
  - Dynamic grading system ($S, A, B, C, D$) based on total clear time and death count.
  - Granular stage-by-stage split times and death counts.
- **🌐 Standalone Web Dashboard (`Website/`)**:
  - Cyberpunk-styled neon arcade interface.
  - 15-second live auto-refresh, instant driver search, and stage breakdown modals.

---

## 🎮 Controls & Keybindings

| Action | Primary Keyboard | Secondary / Alternative | Gamepad (Xbox / DualShock) |
| :--- | :--- | :--- | :--- |
| **Accelerate / Forward** | `W` | `Up Arrow` | `Right Trigger (RT / R2)` or `Left Stick Up` |
| **Brake / Reverse** | `S` | `Down Arrow` | `Left Trigger (LT / L2)` or `Left Stick Down` |
| **Steer Left** | `A` | `Left Arrow` | `Left Stick Left` / `D-Pad Left` |
| **Steer Right** | `D` | `Right Arrow` | `Left Stick Right` / `D-Pad Right` |
| **Handbrake / Drift** | `Space` | `Left Shift` | `Button South (A / Cross)` |
| **Jump Pad / Special** | `Space` *(on ramp)* | `E` | `Button West (X / Square)` |
| **Pause / Menu** | `Escape` | `P` | `Start / Options` |

---

## 🏁 Game Modes

### 1. 🏎️ Single Player Time Attack
- Race through all 6 stages consecutively.
- Every millisecond counts: optimize drift lines, maintain top velocity through corners, avoid track boundaries, and minimize deaths to secure an **S-Rank**.
- Final scores and stage splits are automatically uploaded to the global cloud leaderboard.

### 2. 🌐 2-Player Networked Relay Race
- **Host**: Create a lobby to generate a unique 6-character Join Code.
- **Client**: Input the Join Code to connect instantly over Unity Relay.
- Race wheel-to-wheel in synchronized real-time multiplayer with interpolated vehicle physics.

---

## 🗺️ Track & Stage Progression

| Level | Name / Theme | Key Mechanics & Hazards | Target S-Rank Time |
| :--- | :--- | :--- | :--- |
| **Level 1** | Rookie Circuit | Intro to drift mechanics, sweeping curves | `< 00:25` |
| **Level 2** | S-Bend Slalom | Rapid weight transfer, tight chicanes | `< 00:35` |
| **Level 3** | Leap of Faith | High-speed jumps, gap traversals | `< 00:45` |
| **Level 4** | Hazard Alley | Moving obstacles, sheep collisions, slow debuffs | `< 00:55` |
| **Level 5** | The Gauntlet | Narrow tracks, death zones, multi-lane splits | `< 01:15` |
| **Level 6** | Apex Mastery | Ultimate endurance test combining all hazards | `< 01:30` |
| **Ending** | Victory Podium | Final rankings summary, grade badge & credits | — |

---

## 🌐 Global Leaderboard & Web Dashboard

The repository includes a standalone web app in the [`Website/`](./Website/) folder that connects to Firebase Realtime Database to display live world records.

```
┌────────────────────────────────────────────────────────────────────────┐
│  DriftIT   ● LIVE LEADERBOARD                [ Search Driver... ] (⟳)  │
├───────────────────────────────┬────────────────────────────────────────┤
│  STAGE BREAKDOWN              │  TOP 200 RUNS                          │
│  #1  SpeedDemon  [S]          │  #1  SpeedDemon  01:42.12  0 deaths [S]│
│  Stage 1: 00:14.20 (0 deaths) │  #2  DriftKing   01:45.80  1 deaths [A]│
│  Stage 2: 00:18.05 (0 deaths) │  #3  ApexRacer   01:49.33  0 deaths [A]│
│  Stage 3: 00:22.40 (0 deaths) │  #4  TurboShift  01:54.10  2 deaths [B]│
└───────────────────────────────┴────────────────────────────────────────┘
```

---

## 🚀 Deploying the Leaderboard Website

The [Website](./Website/) directory is fully static (HTML, CSS, Vanilla JS) with zero build steps required.

### Deployed to Vercel on the [Site] (drift-it.vercel.app) .

---

## ⚡ Backend & Firebase Setup

DriftIT uses **Firebase Realtime Database** to persist and sync runs in real-time.

1. Create a project at [Firebase Console](https://console.firebase.google.com).
2. Navigate to **Build > Realtime Database > Create Database**.
3. Set your Database Rules (in test/production mode):
   ```json
   {
     "rules": {
       "leaderboard": {
         ".read": true,
         ".write": true,
         ".indexOn": ["totalTimeSeconds", "dateTime"]
       }
     }
   }
   ```
4. Copy your Database URL:
   `https://<your-project-id>-default-rtdb.firebaseio.com/leaderboard.json`
5. **In Unity**: Update `CloudLeaderboardService.cs` or assign the URL via the Inspector on the `LeaderboardManager` prefab.
6. **On the Website**: Update the `ENDPOINT` constant in [`Website/app.js`](./Website/app.js).

---

## 🔌 Multiplayer & Relay Configuration

DriftIT utilizes **Unity Gaming Services (UGS) Relay** to allow seamless peer-to-peer 2-player networking across any router configuration.

1. Open the project in Unity.
2. Go to **Project Settings > Services** and link your Unity Organization.
3. Enable **Relay** and **Lobby** in your Unity Cloud Dashboard.
4. The game handles authentication anonymously at launch via `RelayManager.cs`.
5. Hosts generate a Join Code; Clients enter the Join Code in the Lobby UI.

---

## 📂 Project Structure

```
DriftIT/
├── .github/
│   └── workflows/
│       └── deploy-pages.yml       # Automated GitHub Pages CI/CD workflow
├── Assets/
│   ├── Animations/                # UI, car effects & transition animations
│   ├── Audio/                     # Engine sounds, drift screeches, SFX, BGM
│   ├── Prefabs/                   # Player cars, network managers, UI prefabs
│   ├── Scenes/                    # MainMenu, Levels 1-6, Ending
│   ├── Script/
│   │   ├── Gameplay/              # LeaderboardManager, LevelTimer, AudioManager
│   │   ├── Networking/            # RelayManager, NetworkBootstrap, NetworkRaceManager
│   │   ├── Player/                # CarControllerSingle, NetworkCarController, CarHealth
│   │   └── UI/                    # LobbyUI, LeaderboardUI, CursorManager, FinishLine
│   ├── Sprites/                   # Vehicles, tire marks, tracks, sheep, obstacles
│   └── TextMesh Pro/              # Arcade font assets and styling
├── Website/                       # 🌐 Standalone Leaderboard Web Dashboard
│   ├── index.html                 # Main dashboard UI
│   ├── style.css                  # Cyberpunk neon responsive styling
│   ├── app.js                     # Realtime Firebase client, filters & sorting
│   ├── vercel.json                # Vercel subfolder configuration
│   └── README.md                  # Web dashboard specific documentation
├── vercel.json                    # Root Vercel deployment configuration
├── .gitignore                     # Unity-optimized gitignore
├── README.md                      # Main project documentation
└── LICENSE                        # MIT License
```

---

## 🛠️ Building & Running the Game

### Prerequisites
- **Unity 2022.3 LTS** (or newer)
- **Universal Render Pipeline (URP)** & **2D Feature Set**
- **Netcode for GameObjects** & **Unity Transport** packages (pre-configured in `Packages/manifest.json`)

### Running in Unity Editor
1. Clone this repository:
   ```bash
   git clone https://github.com/Aadir0/DriftIT.git
   ```
2. Open Unity Hub and click **Add project from disk**.
3. Select the `DriftIT` directory.
4. In the Project window, navigate to `Assets/Scenes/MainMenu.unity` and press **Play**.

### Building Standalone Executable (Windows / Mac / Linux / WebGL)
1. In Unity, go to **File > Build Settings**.
2. Ensure scenes are included in order:
   - `0: Assets/Scenes/MainMenu.unity`
   - `1: Assets/Scenes/Level 1.unity`
   - `2: Assets/Scenes/Level 2.unity`
   - `3: Assets/Scenes/Level 3.unity`
   - `4: Assets/Scenes/Level 4.unity`
   - `5: Assets/Scenes/Level 5.unity`
   - `6: Assets/Scenes/Level 6.unity`
   - `7: Assets/Scenes/Ending.unity`
3. Select your target platform and click **Build and Run**.

---

## 📜 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

<div align="center">
  <sub>Built with ❤️ by Aadir0. Drift hard or go home!</sub>
</div>
