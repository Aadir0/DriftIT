# DriftIT — Global Leaderboard Website (Top 200)

This is the standalone web dashboard for **DriftIT**, displaying the **Top 200** world record runs submitted by players across all devices.

---

## Features
- 🏎️ **Top 200 Global Clears**: Displays ranking (#1 to #200), driver name, total time, death count, grade ($S, A, B, C, \dots$), and run timestamp.
- 📊 **Stage Breakdown Modal**: Click "STAGES" on any record to view stage-by-stage times and deaths for Level 1 through Level 6.
- ⚡ **Live Auto-Refresh**: Auto-syncs every 15 seconds (can be toggled or manually refreshed).
- 🔍 **Real-time Filter & Search**: Search by driver name, filter by rank grade, or sort by fastest time, lowest deaths, or recent runs.
- 📱 **Mobile & Desktop Responsive**: Cyberpunk neon arcade styling optimized for all screen sizes.

---

## How to Run Locally

### Option 1: Direct in Browser
Simply double-click `index.html` or right-click and choose **Open with Browser** (Chrome / Edge / Firefox).

### Option 2: Using VS Code Live Server or Python
In terminal:
```bash
cd Website
python -m http.server 8000
```
Then visit `http://localhost:8000`.

---

## Connecting to Your Live Firebase Backend

1. Go to [Firebase Console](https://console.firebase.google.com) and create a free project.
2. In the left menu, select **Build > Realtime Database > Create Database**.
3. In **Rules**, set read/write rules to public (or secured via secret):
   ```json
   {
     "rules": {
       ".read": true,
       ".write": true
     }
   }
   ```
4. Copy your Database URL (e.g. `https://your-project-id-default-rtdb.firebaseio.com/leaderboard`).
5. In Unity:
   - Enter this URL into `CloudLeaderboardService` (or in Inspector).
6. On the Website:
   - Click the **API Badge** at the bottom-right of the web page to paste your Firebase database URL (`https://your-project-id-default-rtdb.firebaseio.com/leaderboard.json`).

---

## Free Hosting
You can host this `Website/` folder completely for free with:
- **GitHub Pages**: Push this repo and enable GitHub Pages on the `/Website` directory or branch.
- **Vercel / Netlify**: Drag & drop the `Website` folder directly into Netlify / Vercel dashboard.

