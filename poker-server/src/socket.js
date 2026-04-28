const jwt = require('jsonwebtoken');
const { joinQueue, leaveQueue, getQueue, tryMatchmaking } = require('./matchmaking');
const { createLobby, playerReady, allReady, getLobby, deleteLobby } = require('./lobby');
const { startGame, handleAction, handlePlayerLeft, handlePlayerRejoined, registerSocket, getGame, broadcastGameState, requestAction } = require('./game/gameManager');
const { v4: uuidv4 } = require('uuid');

let matchmakingTimer = null;
const LOBBY_COUNTDOWN = 30;
const readyPlayers = {};

module.exports = (io) => {
    io.use((socket, next) => {
        const token = socket.handshake.auth.token;
        if (!token) return next(new Error('No token'));
        try {
            const decoded = jwt.verify(token, process.env.JWT_SECRET);
            socket.userID = decoded.id;
            next();
        } catch {
            next(new Error('Invalid token'));
        }
    });

    function broadcastQueueStatus() {
        const queue = getQueue();
        queue.forEach((p, index) => {
            p.socket.emit('queue:status', { position: index + 1, playersInQueue: queue.length });
        });
    }

    function launchLobby() {
        console.log('launchLobby called');
        clearTimeout(matchmakingTimer);
        matchmakingTimer = null;

        const lobby = tryMatchmaking(2, 6);
        if (!lobby) return;

        const lobbyID = uuidv4();
        createLobby(lobbyID, lobby);

        lobby.forEach(p => {
            console.log(`Sending lobby:created to ${p.userID}`);
            p.socket.join(lobbyID);
            p.socket.emit('lobby:created', {
                lobbyID,
                players: lobby.map(l => ({ userID: l.userID, username: l.username, chips: l.chips, rank: l.rank }))
            });
        });

        startGame(io, lobbyID, lobby);

        broadcastQueueStatus();
    }

    function checkQueue() {
        const queue = getQueue();
        console.log(`Queue length: ${queue.length}`);

        if (queue.length >= 6) {
            launchLobby();
        } else if (queue.length >= 2 && !matchmakingTimer) {
            matchmakingTimer = setTimeout(() => {
                launchLobby();
            }, LOBBY_COUNTDOWN * 1000);

            queue.forEach(p => p.socket.emit('queue:countdown', { seconds: LOBBY_COUNTDOWN }));
        } else if (queue.length < 3 && matchmakingTimer) {
            clearTimeout(matchmakingTimer);
            matchmakingTimer = null;
            queue.forEach(p => p.socket.emit('queue:countdown_cancelled'));
        }
    }

    io.on('connection', (socket) => {
        console.log(`Player connected: ${socket.userID}`);
        registerSocket(socket.userID, socket);

        // Queue
        socket.on('queue:join', (data) => {
            joinQueue({ userID: socket.userID, username: data.username, chips: data.chips, rank: data.rank, socket, joinedAt: Date.now() });
            broadcastQueueStatus();
            checkQueue();
        });

        socket.on('queue:leave', () => {
            leaveQueue(socket.userID);
            broadcastQueueStatus();
            checkQueue();
        });

        // Lobby
        socket.on('Lobby:ready', (data) => {
            const { lobbyID } = data;
            const lobby = playerReady(lobbyID, socket.userID);
            if (!lobby) return;

            io.to(lobbyID).emit('Lobby:player_ready', { userID: socket.userID });

            if (allReady(lobbyID)) {
                io.to(lobbyID).emit('Lobby:game_start', { lobbyID });
                const lobbyData = getLobby(lobbyID);
                startGame(io, lobbyID, lobbyData.players);
            }
        });

        // Game ready � resend state after Unity scene loads
        socket.on('game:ready', (data) => {
            console.log(`game:ready received from ${data.userID} for lobby ${data.lobbyID}`);
            const game = getGame(data.lobbyID);
            if (!game) {
                console.log(`No game found for lobby ${data.lobbyID}`);
                return;
            }

            // Resend hole cards to this player
            const player = game.players.find(p => p.userID === data.userID);
            if (player) socket.emit('game:hand', { cards: player.holeCards });

            // Always broadcast state so all listeners catch it
            broadcastGameState(io, game);

            // Track ready signals per lobby (2 per player � GameUI + PlayerSlotsManager)
            if (!readyPlayers[data.lobbyID]) readyPlayers[data.lobbyID] = new Set();
            readyPlayers[data.lobbyID].add(`${data.userID}_${readyPlayers[data.lobbyID].size}`);

            console.log(`Ready count for lobby ${data.lobbyID}: ${readyPlayers[data.lobbyID].size} / ${game.players.length * 2}`);

            // Only fire requestAction once all players have both scripts ready
            if (readyPlayers[data.lobbyID].size >= game.players.length * 2) {
                console.log(`All players ready for lobby ${data.lobbyID}, requesting action`);
                requestAction(io, game);
                delete readyPlayers[data.lobbyID];
            }
        });

        // Game actions
        socket.on('game:action', (data) => {
            const { lobbyID, userID, action, amount } = data;
            const game = getGame(lobbyID);
            if (!game) return;
            handleAction(io, game, userID, action, amount);
        });

        // Rejoin
        socket.on('game:rejoin', (data) => {
            const { lobbyID } = data;
            const game = getGame(lobbyID);
            if (!game) {
                socket.emit('game:rejoin_failed', { reason: 'Game not found' });
                return;
            }
            const success = handlePlayerRejoined(io, game, socket.userID, socket);
            if (!success) {
                socket.emit('game:rejoin_failed', { reason: 'Player permanently removed' });
            }
        });

        // Disconnect
        socket.on('disconnect', () => {
            leaveQueue(socket.userID);
            broadcastQueueStatus();
            checkQueue();

            Object.keys(socket.rooms).forEach(room => {
                const lobby = getLobby(room);
                if (lobby) {
                    io.to(room).emit('game:player_left', { userID: socket.userID, action: 'auto_fold' });
                    const game = getGame(room);
                    if (game) handlePlayerLeft(io, game, socket.userID);
                }
            });

            console.log(`Player disconnected: ${socket.userID}`);
        });
    });
};