require('dotenv').config();
const express = require('express');
const mongoose = require('mongoose');
const http = require('http');
const { Server } = require('socket.io');

const app = express();
app.use(express.json());

const server = http.createServer(app);
const io = new Server(server);

mongoose.connect(process.env.MONGO_URI)
    .then(() => console.log('MongoDB connected'))
    .catch(err => console.error(err));

// Routes
app.use('/auth', require('./routes/auth'));

// Socket
require('./socket')(io);

server.listen(process.env.PORT, () => {
    console.log(`Server running on port ${process.env.PORT}`);
});