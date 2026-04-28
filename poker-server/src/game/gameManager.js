const { createGame, dealHands, nextCommunityCards, getActivePlayers, determineWinner } = require('./pokerGame');

const activeGames = {};

function startGame(io, lobbyID, players) {
    console.log(`startGame called for lobby ${lobbyID} with ${players.length} players`);
    const game = createGame(lobbyID, players);
    activeGames[lobbyID] = game;
    startHand(io, game);
}

function startHand(io, game) {
    console.log(`startHand called, hand #${game.handCount}`);
    if (game.handCount >= game.maxHands || game.players.filter(p => p.chips > 0).length < 2) {
        endGame(io, game);
        return;
    }

    dealHands(game);

    game.players.forEach(p => {
        const socket = getSocket(p.userID);
        console.log(`Socket for ${p.userID}: ${socket ? 'found' : 'NOT FOUND'}`);
        if (socket) {
            socket.emit('game:hand', { cards: p.holeCards });
            console.log(`Emitted game:hand to ${p.userID}`);
        }
    });

    broadcastGameState(io, game);
    requestAction(io, game);
}

function broadcastGameState(io, game) {
    console.log(`Broadcasting game state - player chips:`);
    game.players.forEach(p => console.log(`  ${p.username}: ${p.chips} chips`));

    io.to(game.lobbyID).emit('game:state', {
        phase: game.phase,
        pot: game.pot,
        communityCards: game.communityCards,
        activePlayer: game.players[game.activePlayerIndex].userID,
        currentBet: game.currentBet,
        players: game.players.map(p => ({
            userID: p.userID,
            username: p.username,
            chips: p.chips,
            folded: p.folded
        }))
    });
}

function requestAction(io, game) {
    const player = game.players[game.activePlayerIndex];
    const socket = getSocket(player.userID);
    if (socket) {
        socket.emit('game:action_request', {
            userID: player.userID,
            actions: getAvailableActions(game, player)
        });
    }
}

function getAvailableActions(game, player) {
    const actions = ['fold'];
    if (player.currentBet === game.currentBet) actions.push('check');
    if (player.currentBet < game.currentBet) actions.push('call');
    if (player.chips > 0) actions.push('raise');
    return actions;
}

function handleAction(io, game, userID, action, amount) {
    const player = game.players.find(p => p.userID === userID);
    if (!player) return;

    switch (action) {
        case 'fold':
            player.folded = true;
            break;
        case 'check':
            break;
        case 'call':
            const callAmount = game.currentBet - player.currentBet;
            player.chips -= callAmount;
            player.currentBet += callAmount;
            game.pot += callAmount;
            break;
        case 'raise':
            const raiseAmount = amount - player.currentBet;
            player.chips -= raiseAmount;
            player.currentBet = amount;
            game.currentBet = amount;
            game.pot += raiseAmount;
            break;
    }

    // Mark this player as having acted this round
    game.actedThisRound.add(userID);

    io.to(game.lobbyID).emit('game:action_broadcast', {
        userID,
        action,
        amount: amount || 0,
        pot: game.pot
    });

    const notFolded = game.players.filter(p => !p.folded);

    // Only one player left — they win without showdown, no need to reveal hands
    if (notFolded.length === 1) {
        const winner = determineWinner(game);
        io.to(game.lobbyID).emit('game:showdown', {
            winners: [{ userID: winner.userID, hand: winner.hand, handName: winner.hand?.name, pot: game.pot }],
            hands: []
        });
        setTimeout(() => startHand(io, game), 3000);
        return;
    }

    advanceTurn(io, game);
}

function advanceTurn(io, game) {
    const active = getActivePlayers(game);

    const bettingDone = active.every(p => p.currentBet === game.currentBet) &&
        active.every(p => game.actedThisRound.has(p.userID));

    if (bettingDone) {
        if (game.phase === 'river') {
            const winner = determineWinner(game);
            io.to(game.lobbyID).emit('game:showdown', {
                winners: [{ userID: winner.userID, hand: winner.hand, handName: winner.hand?.name, pot: game.pot }],
                hands: game.players
                    .filter(p => !p.folded)
                    .map(p => ({ userID: p.userID, cards: p.holeCards }))
            });
            setTimeout(() => startHand(io, game), 3000);
        } else {
            nextCommunityCards(game);
            broadcastGameState(io, game);
            requestAction(io, game);
        }
        return;
    }

    const startIndex = game.activePlayerIndex;
    let next = (startIndex + 1) % game.players.length;

    while (game.players[next].folded || game.players[next].allIn) {
        next = (next + 1) % game.players.length;
        if (next === startIndex) break;
    }

    game.activePlayerIndex = next;

    broadcastGameState(io, game);
    requestAction(io, game);
}

function handlePlayerLeft(io, game, userID) {
    const player = game.players.find(p => p.userID === userID);
    if (!player) return;

    player.disconnected = true;
    player.disconnectTimer = setTimeout(() => {
        player.permanentlyRemoved = true;
        io.to(game.lobbyID).emit('game:player_left', { userID, action: 'permanently_removed' });

        const activePlayers = game.players.filter(p => !p.folded && !p.permanentlyRemoved);
        if (activePlayers.length === 1) {
            const winner = determineWinner(game);
            io.to(game.lobbyID).emit('game:showdown', {
                winners: [{ userID: winner.userID, hand: winner.hand, handName: winner.hand?.name, pot: game.pot }],
                hands: []
            });
            setTimeout(() => startHand(io, game), 3000);
        }
    }, 2 * 60 * 1000);

    io.to(game.lobbyID).emit('game:player_left', { userID, action: 'auto_fold' });

    if (game.players[game.activePlayerIndex].userID === userID) {
        advanceTurn(io, game);
    }
}

function handlePlayerRejoined(io, game, userID, socket) {
    const player = game.players.find(p => p.userID === userID);
    if (!player || player.permanentlyRemoved) return false;

    clearTimeout(player.disconnectTimer);
    player.disconnected = false;
    player.disconnectTimer = null;

    registerSocket(userID, socket);
    socket.join(game.lobbyID);

    socket.emit('game:state', {
        phase: game.phase,
        pot: game.pot,
        communityCards: game.communityCards,
        activePlayer: game.players[game.activePlayerIndex].userID,
        currentBet: game.currentBet
    });

    socket.emit('game:hand', { cards: player.holeCards });

    io.to(game.lobbyID).emit('game:player_rejoined', { userID });

    return true;
}

async function endGame(io, game) {
    const Player = require('../models/Player');

    const sorted = [...game.players]
        .sort((a, b) => b.chips - a.chips);

    const rankChanges = sorted.map((p, index) => {
        let change = 0;
        if (index === 0) change = 30;
        else if (index === 1) change = 10;
        else if (index === sorted.length - 1) change = -20;
        else change = -10;
        return { ...p, rankChange: change };
    });

    for (const p of rankChanges) {
        const newRank = Math.max(0, p.rank + p.rankChange);
        await Player.findByIdAndUpdate(p.userID, { chips: p.chips, rank: newRank });
    }

    io.to(game.lobbyID).emit('game:over', {
        players: rankChanges.map(p => ({
            userID: p.userID,
            username: p.username,
            chips: p.chips,
            rank: p.rank + p.rankChange,
            rankChange: p.rankChange
        }))
    });

    delete activeGames[game.lobbyID];
}

const socketRegistry = {};

function registerSocket(userID, socket) {
    socketRegistry[userID] = socket;
}

function getSocket(userID) {
    return socketRegistry[userID];
}

function getGame(lobbyID) {
    return activeGames[lobbyID];
}

module.exports = { startGame, handleAction, handlePlayerLeft, handlePlayerRejoined, registerSocket, getGame, broadcastGameState, requestAction };