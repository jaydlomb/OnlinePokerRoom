const queue = [];

function joinQueue(player) {
    if (queue.find(p => p.userID === player.userID)) return;
    queue.push(player);
    queue.sort((a, b) => a.rank - b.rank);
}

function leaveQueue(userID) {
    const index = queue.findIndex(p => p.userID === userID);
    if (index !== -1) queue.splice(index, 1);
}

function getPosition(userID) {
    return queue.findIndex(p => p.userID === userID) + 1;
}

function getQueue() {
    return queue;
}

// min = 2
function tryMatchmaking(min = 2, max = 6, baseRankDiff = 200) {
    if (queue.length < min) return null;

    for (let i = 0; i <= queue.length - min; i++) {
        const anchor = queue[i];
        const waitSeconds = (Date.now() - anchor.joinedAt) / 1000;
        const maxRankDiff = baseRankDiff + Math.floor(waitSeconds / 10) * 50; // +50 every 10 seconds

        const group = queue.filter(p => Math.abs(p.rank - anchor.rank) <= maxRankDiff);

        if (group.length >= min) {
            const lobby = group.slice(0, max);
            lobby.forEach(p => {
                const idx = queue.indexOf(p);
                queue.splice(idx, 1);
            });
            return lobby;
        }
    }

    return null;
}

module.exports = { joinQueue, leaveQueue, getPosition, getQueue, tryMatchmaking };