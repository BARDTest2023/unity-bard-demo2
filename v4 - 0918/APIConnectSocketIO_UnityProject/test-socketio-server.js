#!/usr/bin/env node

/**
 * Simple Socket.IO Test Server for Unity Testing
 * Compatible with AdonisJS v5 Socket.IO implementation
 * 
 * Usage:
 * 1. Install dependencies: npm install express socket.io cors
 * 2. Run server: node test-socketio-server.js
 * 3. Server will run on http://localhost:3333
 * 4. Update Unity SocketIOManager to use http://localhost:3333
 */

const express = require('express');
const { createServer } = require('http');
const { Server } = require('socket.io');
const cors = require('cors');

const app = express();
const server = createServer(app);

// Configure Socket.IO with CORS for Unity compatibility
const io = new Server(server, {
  cors: {
    origin: "*", // Allow Unity connections from any origin
    methods: ["GET", "POST"],
    credentials: false
  },
  transports: ['websocket', 'polling'], // Support both transports
  allowEIO3: true // Support older Engine.IO versions
});

// Enable CORS for Express routes
app.use(cors());
app.use(express.json());

// Basic HTTP route for testing server accessibility
app.get('/', (req, res) => {
  res.json({
    status: 'Socket.IO Test Server Running',
    timestamp: new Date().toISOString(),
    connectedClients: io.engine.clientsCount,
    version: '1.0.0'
  });
});

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({
    status: 'healthy',
    uptime: process.uptime(),
    memory: process.memoryUsage()
  });
});

// Socket.IO connection handling
io.on('connection', (socket) => {
  console.log(`🎮 Unity client connected: ${socket.id}`);
  console.log(`📊 Total connected clients: ${io.engine.clientsCount}`);
  
  // Log connection details
  const clientInfo = {
    id: socket.id,
    address: socket.handshake.address,
    userAgent: socket.handshake.headers['user-agent'],
    query: socket.handshake.query,
    transport: socket.conn.transport.name
  };
  console.log('Client info:', JSON.stringify(clientInfo, null, 2));

  // Send welcome message
  socket.emit('message', {
    type: 'welcome',
    message: 'Connected to Socket.IO test server',
    serverId: 'test-server-001',
    timestamp: new Date().toISOString()
  });

  // Handle game data from Unity
  socket.on('gameData', (data) => {
    console.log('📨 Received game data:', JSON.stringify(data, null, 2));
    
    try {
      // Simulate processing time
      setTimeout(() => {
        // Calculate a mock score based on the received data
        let calculatedScore = 0;
        
        if (data.data && Array.isArray(data.data)) {
          // Sum up scores from all metrics
          calculatedScore = data.data.reduce((sum, metric) => {
            return sum + (metric.score || 0) + (metric.precision || 0) * 100;
          }, 0);
          
          // Add some randomness to make it realistic
          calculatedScore += Math.random() * 20 - 10; // ±10 random points
          calculatedScore = Math.max(0, Math.round(calculatedScore * 100) / 100); // Round to 2 decimals
        } else {
          // Default score if no data
          calculatedScore = Math.random() * 100;
        }

        // Send score response back to Unity
        const scoreResponse = {
          messageId: data.messageId || `response_${Date.now()}`,
          value: calculatedScore,
          timestamp: new Date().toISOString(),
          processed: true
        };

        console.log('📤 Sending score response:', JSON.stringify(scoreResponse, null, 2));
        socket.emit('score', scoreResponse);

        // Send additional message for testing
        socket.emit('message', {
          type: 'processing_complete',
          message: `Processed ${data.game || 'unknown'} game data successfully`,
          score: calculatedScore
        });

      }, 100 + Math.random() * 200); // Simulate 100-300ms processing time

    } catch (error) {
      console.error('❌ Error processing game data:', error);
      
      socket.emit('error', {
        type: 'processing_error',
        message: 'Failed to process game data',
        error: error.message
      });
    }
  });

  // Handle test messages
  socket.on('test', (data) => {
    console.log('🧪 Test message received:', data);
    socket.emit('test_response', {
      received: data,
      timestamp: new Date().toISOString(),
      echo: true
    });
  });

  // Handle ping/pong for connection testing
  socket.on('ping', (data) => {
    console.log('🏓 Ping received:', data);
    socket.emit('pong', {
      ...data,
      serverTime: new Date().toISOString()
    });
  });

  // Handle disconnection
  socket.on('disconnect', (reason) => {
    console.log(`👋 Unity client disconnected: ${socket.id}, reason: ${reason}`);
    console.log(`📊 Remaining connected clients: ${io.engine.clientsCount}`);
  });

  // Handle connection errors
  socket.on('error', (error) => {
    console.error(`❌ Socket error for ${socket.id}:`, error);
  });
});

// Handle server errors
io.engine.on('connection_error', (err) => {
  console.error('❌ Connection error:', err);
});

// Start the server
const PORT = process.env.PORT || 3333;
server.listen(PORT, () => {
  console.log('🚀 Socket.IO Test Server started successfully!');
  console.log(`📡 Server running on: http://localhost:${PORT}`);
  console.log(`🔗 Socket.IO endpoint: http://localhost:${PORT}/socket.io/`);
  console.log('');
  console.log('🎮 Unity Configuration:');
  console.log(`   Server URL: http://localhost`);
  console.log(`   Server Port: ${PORT}`);
  console.log('');
  console.log('📋 Available endpoints:');
  console.log(`   GET  /        - Server status`);
  console.log(`   GET  /health  - Health check`);
  console.log('');
  console.log('🔧 To test with Unity:');
  console.log('   1. Set SocketIOManager serverUrl to "http://localhost"');
  console.log(`   2. Set SocketIOManager serverPort to ${PORT}`);
  console.log('   3. Enable "Bypass API Validation" in Unity');
  console.log('   4. Press Play and click Connect');
  console.log('');
  console.log('Press Ctrl+C to stop the server');
});

// Graceful shutdown
process.on('SIGINT', () => {
  console.log('\n🛑 Shutting down server gracefully...');
  server.close(() => {
    console.log('✅ Server closed successfully');
    process.exit(0);
  });
});

process.on('SIGTERM', () => {
  console.log('\n🛑 Received SIGTERM, shutting down gracefully...');
  server.close(() => {
    console.log('✅ Server closed successfully');
    process.exit(0);
  });
});
