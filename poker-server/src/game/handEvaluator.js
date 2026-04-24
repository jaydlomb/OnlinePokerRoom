function getHandRank(cards) {
    const ranks = cards.map(c => c.rank).sort((a, b) => b - a);
    const suits = cards.map(c => c.suit);

    const rankCounts = {};
    ranks.forEach(r => rankCounts[r] = (rankCounts[r] || 0) + 1);
    const counts = Object.values(rankCounts).sort((a, b) => b - a);

    const isFlush = suits.every(s => s === suits[0]);
    const isStr8 = ranks[0] - ranks[4] === 4 && new Set(ranks).size === 5;
    const isRoyalStr8 = isStr8 && ranks[0] === 14;

    if (isFlush && isRoyalStr8) return { rank: 9, name: 'Royal Flush' };
    if (isFlush && isStr8) return { rank: 8, name: 'Straight Flush' };
    if (counts[0] === 4) return { rank: 7, name: 'Four of a Kind' };
    if (counts[0] === 3 && counts[1] === 2) return { rank: 6, name: 'Full House' };
    if (isFlush) return { rank: 5, name: 'Flush' };
    if (isStr8) return { rank: 4, name: 'Straight' };
    if (counts[0] === 3) return { rank: 3, name: 'Three of a Kind' };
    if (counts[0] === 2 && counts[1] === 2) return { rank: 2, name: 'Two Pair' };
    if (counts[0] === 2) return { rank: 1, name: 'One Pair' };
    return { rank: 0, name: 'High Card' };
}

function getBestHand(holeCards, communityCards) {
    const all = [...holeCards, ...communityCards];
    let best = null;

    // Try all combinations of 5 from 7 cards
    for (let i = 0; i < all.length; i++) {
        for (let j = i + 1; j < all.length; j++) {
            const five = all.filter((_, idx) => idx !== i && idx !== j);
            const result = getHandRank(five);
            if (!best || result.rank > best.rank) {
                best = { ...result, cards: five };
            }
        }
    }
    return best;
}

module.exports = { getBestHand };