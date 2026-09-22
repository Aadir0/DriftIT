// DriftIT — Native Table Wooden Leaderboard (Top 200)

const ENDPOINT = 'https://driftit-6dd08-default-rtdb.asia-southeast1.firebasedatabase.app/leaderboard.json';
const AUTO_REFRESH_INTERVAL_MS = 15000;

// State
let allRuns = [];
let selectedIndex = 0;
let autoRefreshTimer = null;

// DOM Elements
const leaderboardRows = document.getElementById('leaderboardRows');
const refreshBtn = document.getElementById('refreshBtn');

// Sidebar Elements
const sidebarRank = document.getElementById('sidebarRank');
const sidebarDriverName = document.getElementById('sidebarDriverName');
const sidebarDate = document.getElementById('sidebarDate');
const sidebarGrade = document.getElementById('sidebarGrade');
const stagesList = document.getElementById('stagesList');

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    if (refreshBtn) {
        refreshBtn.addEventListener('click', () => fetchLeaderboardData());
    }

    fetchLeaderboardData();
    setupAutoRefresh();
});

function setupAutoRefresh() {
    clearInterval(autoRefreshTimer);
    autoRefreshTimer = setInterval(() => {
        fetchLeaderboardData(true);
    }, AUTO_REFRESH_INTERVAL_MS);
}

async function fetchLeaderboardData(isSilent = false) {
    if (!isSilent && leaderboardRows) {
        leaderboardRows.innerHTML = '<tr><td colspan="6" class="loading-text">Loading Leaderboard...</td></tr>';
    }

    try {
        const response = await fetch(ENDPOINT, {
            headers: { 'Accept': 'application/json' },
            cache: 'no-cache'
        });

        if (!response.ok) {
            throw new Error(`HTTP error ${response.status}`);
        }

        const data = await response.json();
        allRuns = normalizeData(data);
    } catch (err) {
        console.warn('[Leaderboard] Fetch notice:', err.message);
        allRuns = [];
    }

    renderLeaderboard();
}

function formatCleanDate(rawDate) {
    if (!rawDate) return "Recent";
    const str = String(rawDate).trim();
    // If format is YYYY-MM-DD HH:mm:ss, strip off seconds -> YYYY-MM-DD HH:mm
    if (/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$/.test(str)) {
        return str.substring(0, 16);
    }
    // If ISO format like 2026-09-21T03:27:00
    if (str.includes('T')) {
        const parts = str.split('T');
        const datePart = parts[0];
        const timePart = parts[1].substring(0, 5);
        return `${datePart} ${timePart}`;
    }
    return str;
}

function normalizeData(rawData) {
    if (!rawData) return [];

    let runs = [];
    if (Array.isArray(rawData)) {
        runs = rawData.filter(Boolean);
    } else if (typeof rawData === 'object') {
        if (Array.isArray(rawData.leaderboard)) {
            runs = rawData.leaderboard;
        } else {
            runs = Object.values(rawData).filter(item => item && item.playerName);
        }
    }

    // Sort: Fastest time ascending, then lowest deaths ascending
    runs.sort((a, b) => {
        const timeA = parseFloat(a.totalTimeSeconds) || 0;
        const timeB = parseFloat(b.totalTimeSeconds) || 0;
        if (timeA !== timeB) return timeA - timeB;
        return (parseInt(a.totalDeaths) || 0) - (parseInt(b.totalDeaths) || 0);
    });

    // Clean & format
    return runs.map((item) => {
        const timeSec = parseFloat(item.totalTimeSeconds) || 0;
        const mins = Math.floor(timeSec / 60);
        const secs = Math.floor(timeSec % 60);
        const formatted = item.formattedTime || `${String(mins).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
        const deaths = parseInt(item.totalDeaths) || 0;
        const grade = item.grade || "A";
        const date = formatCleanDate(item.dateTime || item.dateString);

        return {
            ...item,
            totalTimeSeconds: timeSec,
            formattedTime: formatted,
            totalDeaths: deaths,
            grade: grade,
            dateTime: date,
            stages: Array.isArray(item.stages) ? item.stages : []
        };
    }).slice(0, 200); // Cap at Top 200
}

function renderLeaderboard() {
    if (!leaderboardRows) return;

    if (allRuns.length === 0) {
        leaderboardRows.innerHTML = '<tr><td colspan="6" class="loading-text" style="color:#8e9bae;">No runs registered yet.</td></tr>';
        renderEmptySidebar();
        return;
    }

    let rowsHtml = '';
    allRuns.forEach((run, index) => {
        const rank = index + 1;
        let rankClass = 'rank-norm';
        if (rank === 1) rankClass = 'rank-1';
        else if (rank === 2) rankClass = 'rank-2';
        else if (rank === 3) rankClass = 'rank-3';

        const gradeClass = `grade-${run.grade}`;
        const activeClass = (index === selectedIndex) ? 'active' : '';

        rowsHtml += `
            <tr class="${activeClass}" onclick="selectDriver(${index})" id="row-${index}">
                <td class="col-rank ${rankClass}">#${rank}</td>
                <td class="col-driver">${escapeHtml(run.playerName)}</td>
                <td class="col-time">${run.formattedTime}</td>
                <td class="col-deaths">${run.totalDeaths}</td>
                <td class="col-grade ${gradeClass}">[${run.grade}]</td>
                <td class="col-date">${escapeHtml(run.dateTime)}</td>
            </tr>
        `;
    });

    leaderboardRows.innerHTML = rowsHtml;

    if (selectedIndex >= allRuns.length) selectedIndex = 0;
    selectDriver(selectedIndex);
}

function selectDriver(index) {
    if (!allRuns[index]) return;

    selectedIndex = index;
    const run = allRuns[index];
    const rank = index + 1;

    // Highlight row
    document.querySelectorAll('#leaderboardRows tr').forEach((el, idx) => {
        if (idx === index) el.classList.add('active');
        else el.classList.remove('active');
    });

    // Update Sidebar
    if (sidebarRank) {
        sidebarRank.innerText = `#${rank}`;
        sidebarRank.className = `driver-rank ${rank === 1 ? 'rank-1' : (rank === 2 ? 'rank-2' : (rank === 3 ? 'rank-3' : 'rank-norm'))}`;
    }
    if (sidebarDriverName) sidebarDriverName.innerText = run.playerName;
    if (sidebarDate) sidebarDate.innerText = run.dateTime;
    if (sidebarGrade) {
        sidebarGrade.innerText = `[${run.grade}]`;
        sidebarGrade.className = `driver-grade grade-${run.grade}`;
    }

    // Render Stage Breakdown
    if (stagesList) {
        if (run.stages && run.stages.length > 0) {
            let stagesHtml = '';
            run.stages.forEach((st, sIdx) => {
                const stageName = st.levelName ? st.levelName.toUpperCase() : `LEVEL ${sIdx + 1}`;
                const timeStr = st.formattedTime || "00:00";
                const deaths = st.deaths || 0;

                stagesHtml += `
                    <tr>
                        <td class="st-col-name">${stageName}</td>
                        <td class="st-col-time">${timeStr}</td>
                        <td class="st-col-deaths">${deaths}</td>
                    </tr>
                `;
            });
            stagesList.innerHTML = stagesHtml;
        } else {
            stagesList.innerHTML = '<tr><td colspan="3" class="stages-empty">No stage breakdown stored for this run.</td></tr>';
        }
    }
}

function renderEmptySidebar() {
    if (sidebarRank) sidebarRank.innerText = "--";
    if (sidebarDriverName) sidebarDriverName.innerText = "No Driver";
    if (sidebarDate) sidebarDate.innerText = "--";
    if (sidebarGrade) sidebarGrade.innerText = "[--]";
    if (stagesList) stagesList.innerHTML = '<tr><td colspan="3" class="stages-empty">No runs available</td></tr>';
}

function escapeHtml(text) {
    if (!text) return '';
    return String(text)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}
