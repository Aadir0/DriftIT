# 🏎️ DriftIT — Global Leaderboard Web Dashboard

[![Vercel](https://img.shields.io/badge/Vercel-Deployment%20Ready-black?logo=vercel&style=flat-square)](https://vercel.com)
[![Firebase](https://img.shields.io/badge/Firebase-Realtime%20Database-orange?logo=firebase&style=flat-square)](https://firebase.google.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](../LICENSE)

This is the standalone web client and world-record leaderboard dashboard for **[DriftIT](../README.md)**. It displays the **Top 200** world record runs submitted by players across all platforms in real time.

---

## ⚡ Live Features

- 🏎️ **Top 200 Global Clears**: Displays ranking (#1 to #200), driver handle, total completion time, death count, run rank grade ($S, A, B, C, D$), and run timestamp.
- 📊 **Stage Breakdown Inspector**: Click on any driver to inspect their stage-by-stage split times and death counts for Levels 1 through 6.
- 🔄 **Live Auto-Sync**: Auto-refreshes every 15 seconds against Firebase Realtime Database.
- 🔍 **Real-time Filter & Search**: Search by driver name on the fly with instant DOM re-indexing.
- 📱 **Cyberpunk Arcade UI**: Fully responsive neon dark mode with glassmorphic cards, custom arcade typography, and smooth micro-animations.

---

## 🚀 Instant Deployment

This folder contains a pure static web application (HTML5, CSS3, Vanilla JavaScript) and requires **no build step**.

### Option 1: Deploy to Vercel (Recommended)
1. Go to [vercel.com/new](https://vercel.com/new).
2. Import the repository.
3. The root [`../vercel.json`](../vercel.json) will automatically serve this folder, or you can set **Root Directory** to `Website`.
4. Click **Deploy**.

### Option 2: Deploy to GitHub Pages (Automated)
A GitHub Actions workflow is included at [`.github/workflows/deploy-pages.yml`](../.github/workflows/deploy-pages.yml). Pushing to `main` will automatically build and publish to GitHub Pages.

### Option 3: Deploy to Netlify / Cloudflare Pages
- **Netlify**: Drag & drop this `Website` folder directly into [app.netlify.com/drop](https://app.netlify.com/drop).
- **Cloudflare Pages**: Connect the Git repository and set the output directory to `Website`.

---

## 💻 Running Locally

### Using Python HTTP Server:
```bash
# From within the Website directory
python -m http.server 8000
```
Open `http://localhost:8000` in your web browser.

### Using VS Code:
Right-click `index.html` and choose **Open with Live Server**.

---

## 🔌 Firebase Realtime Database Configuration

The dashboard connects to Firebase via REST endpoint:
- **Default Endpoint**: Configured in `app.js`:
  ```javascript
  const ENDPOINT = 'https://driftit-6dd08-default-rtdb.asia-southeast1.firebasedatabase.app/leaderboard.json';
  ```
- **Custom Backend**: To point to your own Firebase instance, update the `ENDPOINT` variable in `app.js` with your Firebase database URL (`https://<project-id>-default-rtdb.firebaseio.com/leaderboard.json`).

---

## 📁 File Structure

```
Website/
├── index.html        # Semantic HTML5 layout with sidebar & leaderboard table
├── style.css         # Cyberpunk arcade theme, responsive flex/grid layouts
├── app.js            # Realtime data fetching, normalization, search & stage inspector
├── vercel.json       # Vercel deployment caching & clean URLs config
├── fonts/            # Local arcade font assets
└── sprites/          # UI badges and graphical assets
```
