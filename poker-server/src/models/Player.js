const mongoose = require('mongoose');

const PlayerSchema = new mongoose.Schema({
    username: { type: String, required: true, unique: true },
    password: { type: String, required: true },
    chips: { type: Number, default: 1000 },
    rank: { type: Number, default: 1000 }
});

module.exports = mongoose.model('Player', PlayerSchema);