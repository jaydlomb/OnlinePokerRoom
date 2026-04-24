const jwt = require('jsonwebtoken');
const { joinQueue, leaveQueue, getQueue, tryMatchmaking } = require('./matchmaking');
const { createLobby, playerReady, allReady, getLobby, deleteLobby } = require('./lobby');
const { startGame, handleAction, handlePlayerLeft, registerSocket, getGame } = require('./game/gameManager');
const { v4: uuidv4 } = require('uuid');

let matchmakingTimer = null;
const LOBBY_COUNTDOWN = 30;

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

        //const lobby = tryMatchmaking(3, 6);
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

        broadcastQueueStatus();
    }

    function checkQueue() {
        const queue = getQueue();
        console.log(`Queue length: ${queue.length}`);

        if (queue.length >= 6) {
            launchLobby();
            //} else if (queue.length >= 3 && !matchmakingTimer) {
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

        // Queue with rank range expansion
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

        // Game actions
        socket.on('game:action', (data) => {
            const { lobbyID, userID, action, amount } = data;
            const game = getGame(lobbyID);
            if (!game) return;
            handleAction(io, game, userID, action, amount);
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