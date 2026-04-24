const { getBestHand } = require('./handEvaluator');
const { v4: uuidv4 } = require('uuid');

const SUITS = ['Clubs', 'Diamonds', 'Hearts', 'Spades'];
const RANKS = [2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14];

function createDeck() {
    const deck = [];
    for (const suit of SUITS)
        for (const rank of RANKS)
            deck.push({ suit, rank });
    return deck;
}

function shuffle(deck) {
    for (let i = deck.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [deck[i], deck[j]] = [deck[j], deck[i]];
    }
    return deck;
}

function createGame(lobbyID, players) {
    return {
        lobbyID,
        players: players.map(p => ({
            userID: p.userID,
            username: p.username,
            chips: p.chips,
            rank: p.rank,
            holeCards: [],
            folded: false,
            allIn: false,
            currentBet: 0
        })),
        deck: [],
        communityCards: [],
        pot: 0,
        phase: 'preflop',
        activePlayerIndex: 0,
        currentBet: 0,
        dealerIndex: 0,
        handCount: 0,
        maxHands: 50,
        smallBlind: 5,
        bigBlind: 10
    };
}

function dealHands(game) {
    console.log(`Dealing hands to ${game.players.length} players`);
    game.deck = shuffle(createDeck());
    game.communityCards = [];
    game.pot = 0;
    game.currentBet = 0;
    game.phase = 'preflop';

    game.players.forEach(p => {
        console.log(`Sending hole cards to ${p.userID}`);
        p.holeCards = [game.deck.pop(), game.deck.pop()];
        p.folded = false;
        p.allIn = false;
        p.currentBet = 0;
    });

    // Post blinds
    const sbIndex = (game.dealerIndex + 1) % game.players.length;
    const bbIndex = (game.dealerIndex + 2) % game.players.length;

    game.players[sbIndex].chips -= game.smallBlind;
    game.players[sbIndex].currentBet = game.smallBlind;
    game.players[bbIndex].chips -= game.bigBlind;
    game.players[bbIndex].currentBet = game.bigBlind;
    game.pot += game.smallBlind + game.bigBlind;
    game.currentBet = game.bigBlind;

    game.activePlayerIndex = (bbIndex + 1) % game.players.length;
    game.handCount++;
}

function nextCommunityCards(game) {
    if (game.phase === 'preflop') {
        game.communityCards.push(game.deck.pop(), game.deck.pop(), game.deck.pop());
        game.phase = 'flop';
    } else if (game.phase === 'flop') {
        game.communityCards.push(game.deck.pop());
        game.phase = 'turn';
    } else if (game.phase === 'turn') {
        game.communityCards.push(game.deck.pop());
        game.phase = 'river';
    }

    game.currentBet = 0;
    game.players.forEach(p => p.currentBet = 0);
    game.activePlayerIndex = (game.dealerIndex + 1) % game.players.length;
}

function getActivePlayers(game) {
    return game.players.filter(p => !p.folded && !p.allIn);
}

function determineWinner(game) {
    const eligible = game.players.filter(p => !p.folded);
    let winner = null;
    let bestRank = -1;

    eligible.forEach(p => {
        const result = getBestHand(p.holeCards, game.communityCards);
        if (result.rank > bestRank) {
            bestRank = result.rank;
            winner = { ...p, hand: result };
        }
    });

    if (winner) winner.chips += game.pot;
    game.pot = 0;
    game.dealerIndex = (game.dealerIndex + 1) % game.players.length;

    return winner;
}

module.exports = { createGame, dealHands, nextCommunityCards, getActivePlayers, determineWinner };