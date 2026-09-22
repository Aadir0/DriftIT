// DriftIT — Global Leaderboard Client

const ENDPOINT = 'https://driftit-6dd08-default-rtdb.asia-southeast1.firebasedatabase.app/leaderboard.json';
const AUTO_REFRESH_INTERVAL_MS = 15000;

// State
let allRuns = [];
let filteredRuns = [];
let selectedIndex = 0;
let autoRefreshTimer = null;
let searchQuery = '';

// DOM Elements
const leaderboardRows = document.getElementById('leaderboardRows');
const refreshBtn = document.getElementById('refreshBtn');
const searchInput = document.getElementById('searchInput');
const recordCount = document.getElementById('recordCount');

// Sidebar Elements
const sidebarRank = document.getElementById('sidebarRank');
const sidebarDriverName = document.getElementById('sidebarDriverName');
const sidebarDate = document.getElementById('sidebarDate');
const sidebarGrade = document.getElementById('sidebarGrade');
const stagesList = document.getElementById('stagesList');

// Initialize
document.addEventListener('DOMContentLoaded', () => {
    if (refreshBtn) {
        refreshBtn.addEventListener('click', () => {
            fetchLeaderboardData();
        });
    }

    if (searchInput) {
        searchInput.addEventListener('input', (e) => {
            searchQuery = e.target.value.trim().toLowerCase();
            applyFilter();
        });
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
        leaderboardRows.innerHTML = '<tr><td colspan="6" class="loading-message">Loading leaderboard data...</td></tr>';
    }

    try {
        const response = await fetch(ENDPOINT, {
            headers: { 'Accept': 'application/json' },
            cache: 'no-cache'
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const data = await response.json();
        allRuns = normalizeData(data);
    } catch (err) {
        console.warn('[Leaderboard] Fetch notice:', err.message);
        allRuns = [];
    }

    applyFilter();
}

function formatCleanDate(rawDate) {
    if (!rawDate) return "Recent";
    const str = String(rawDate).trim();
    if (/^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$/.test(str)) {
        return str.substring(0, 16);
    }
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
    }).slice(0, 200);
}

function applyFilter() {
    if (!searchQuery) {
        filteredRuns = allRuns.map((r, i) => ({ ...r, originalRank: i + 1 }));
    } else {
        filteredRuns = allRuns
            .map((r, i) => ({ ...r, originalRank: i + 1 }))
            .filter(r => r.playerName && r.playerName.toLowerCase().includes(searchQuery));
    }

    if (recordCount) {
        recordCount.innerText = `${filteredRuns.length} record${filteredRuns.length === 1 ? '' : 's'}`;
    }

    renderLeaderboard();
}

function renderLeaderboard() {
    if (!leaderboardRows) return;

    if (filteredRuns.length === 0) {
        const msg = searchQuery ? 'No drivers matching search.' : 'No runs submitted yet.';
        leaderboardRows.innerHTML = `<tr><td colspan="6" class="empty-message">${msg}</td></tr>`;
        renderEmptySidebar();
        return;
    }

    let rowsHtml = '';
    filteredRuns.forEach((run, index) => {
        const rank = run.originalRank;
        let rankClass = 'rank-badge';
        if (rank === 1) rankClass += ' rank-1';
        else if (rank === 2) rankClass += ' rank-2';
        else if (rank === 3) rankClass += ' rank-3';

        const gradeClass = `grade-badge grade-${run.grade}`;
        const activeClass = (index === selectedIndex) ? 'active' : '';

        rowsHtml += `
            <tr class="${activeClass}" onclick="selectDriver(${index})" id="row-${index}">
                <td class="col-rank">
                    <span class="${rankClass}">#${rank}</span>
                </td>
                <td class="col-driver">${escapeHtml(run.playerName)}</td>
                <td class="col-total-time text-right">${run.formattedTime}</td>
                <td class="col-total-deaths text-right">${run.totalDeaths}</td>
                <td class="col-grade text-center">
                    <span class="${gradeClass}">${run.grade}</span>
                </td>
                <td class="col-date text-right">${escapeHtml(run.dateTime)}</td>
            </tr>
        `;
    });

    leaderboardRows.innerHTML = rowsHtml;

    if (selectedIndex >= filteredRuns.length) selectedIndex = 0;
    selectDriver(selectedIndex);
}

function selectDriver(index) {
    if (!filteredRuns[index]) {
        renderEmptySidebar();
        return;
    }

    selectedIndex = index;
    const run = filteredRuns[index];
    const rank = run.originalRank;

    // Highlight row
    document.querySelectorAll('#leaderboardRows tr').forEach((el, idx) => {
        if (idx === index) el.classList.add('active');
        else el.classList.remove('active');
    });

    // Update Sidebar Profile
    if (sidebarRank) {
        sidebarRank.innerText = `#${rank}`;
        sidebarRank.className = `rank-badge ${rank === 1 ? 'rank-1' : (rank === 2 ? 'rank-2' : (rank === 3 ? 'rank-3' : ''))}`;
    }
    if (sidebarDriverName) sidebarDriverName.innerText = run.playerName;
    if (sidebarDate) sidebarDate.innerText = run.dateTime;
    if (sidebarGrade) {
        sidebarGrade.innerText = `[${run.grade}]`;
        sidebarGrade.className = `grade-badge grade-${run.grade}`;
    }

    // Render Stage Breakdown
    if (stagesList) {
        if (run.stages && run.stages.length > 0) {
            let stagesHtml = '';
            run.stages.forEach((st, sIdx) => {
                const stageName = st.levelName ? st.levelName.toUpperCase() : `STAGE ${sIdx + 1}`;
                const timeStr = st.formattedTime || "00:00";
                const deaths = st.deaths || 0;

                stagesHtml += `
                    <tr>
                        <td class="col-stage">${escapeHtml(stageName)}</td>
                        <td class="col-time text-right">${timeStr}</td>
                        <td class="col-deaths text-right">${deaths}</td>
                    </tr>
                `;
            });
            stagesList.innerHTML = stagesHtml;
        } else {
            stagesList.innerHTML = '<tr><td colspan="3" class="empty-message">No stage details stored for this run.</td></tr>';
        }
    }
}

function renderEmptySidebar() {
    if (sidebarRank) sidebarRank.innerText = "--";
    if (sidebarDriverName) sidebarDriverName.innerText = "No Driver";
    if (sidebarDate) sidebarDate.innerText = "--";
    if (sidebarGrade) sidebarGrade.innerText = "[--]";
    if (stagesList) stagesList.innerHTML = '<tr><td colspan="3" class="empty-message">No run selected</td></tr>';
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
