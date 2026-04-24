const lobbies = {};

function createLobby(lobbyID, players) {
    lobbies[lobbyID] = {
        lobbyID,
        players,
        readyPlayers: [],
        gameStarted: false
    };
}

function playerReady(lobbyID, userID) {
    const lobby = lobbies[lobbyID];
    if (!lobby) return null;
    if (!lobby.readyPlayers.includes(userID)) {
        lobby.readyPlayers.push(userID);
    }
    return lobby;
}

function allReady(lobbyID) {
    const lobby = lobbies[lobbyID];
    if (!lobby) return false;
    return lobby.readyPlayers.length === lobby.players.length;
}

function getLobby(lobbyID) {
    return lobbies[lobbyID];
}

function deleteLobby(lobbyID) {
    delete lobbies[lobbyID];
}

module.exports = { createLobby, playerReady, allReady, getLobby, deleteLobby };