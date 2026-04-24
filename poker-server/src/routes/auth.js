const express = require('express');
const router = express.Router();
const bcrypt = require('bcrypt');
const jwt = require('jsonwebtoken');
const Player = require('../models/Player');

// Register
router.post('/register', async (req, res) => {
    try {
        const { username, password } = req.body;
        const hashed = await bcrypt.hash(password, 10);
        const player = new Player({ username, password: hashed });
        await player.save();
        res.json({ message: 'Player created' });
    } catch (err) {
        res.status(400).json({ error: err.message });
    }
});

// Login
router.post('/login', async (req, res) => {
    try {
        const { username, password } = req.body;
        const player = await Player.findOne({ username });
        if (!player) return res.status(404).json({ error: 'Player not found' });

        const match = await bcrypt.compare(password, player.password);
        if (!match) return res.status(401).json({ error: 'Invalid password' });

        const token = jwt.sign({ id: player._id }, process.env.JWT_SECRET, { expiresIn: '7d' });
        res.json({ token, userID: player._id, username: player.username, chips: player.chips, rank: player.rank });
    } catch (err) {
        res.status(500).json({ error: err.message });
    }
});

module.exports = router;