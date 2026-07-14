import http from 'http';
import { WebSocketServer } from 'ws';
import dotenv from 'dotenv';
import {
  lobbies,
  addPlayer,
  removePlayer,
  updatePlayerPosition,
  checkFinishLine,
  isLobbyFull,
  createLobbyWithPlayers,
} from './gameManager.js';

dotenv.config();

const PORT = process.env.PORT || 3000;
const TICK_RATE = 33;
const HEARTBEAT_INTERVAL = 15000;
const HEARTBEAT_TIMEOUT = 5000;
const HISTORY_SERVICE_URL = process.env.HISTORY_SERVICE_URL;

const server = http.createServer((req, res) => {
  if (req.method === 'GET' && req.url === '/health') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ status: 'healthy' }));
    return;
  }

  if (req.method === 'POST' && req.url === '/api/lobby/create') {
    let body = '';
    req.on('data', (chunk) => { body += chunk; });
    req.on('end', () => {
      try {
        const { players } = JSON.parse(body);
        if (!players || !Array.isArray(players) || players.length === 0) {
          res.writeHead(400, { 'Content-Type': 'application/json' });
          res.end(JSON.stringify({ success: false, error: 'players array required' }));
          return;
        }

        const result = createLobbyWithPlayers(players);
        const gameServerUrl = process.env.GAME_SERVER_URL || `ws://localhost:${PORT}`;

        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({
          success: true,
          data: {
            lobbyId: result.lobby.id,
            players: result.players,
            gameServerUrl,
          },
        }));
      } catch (err) {
        res.writeHead(400, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ success: false, error: 'Invalid JSON' }));
      }
    });
    return;
  }

  res.writeHead(404);
  res.end();
});

const wss = new WebSocketServer({ server });

const connections = new Map();
const playerLobbies = new Map();
const pendingPongs = new Map();

function broadcastToLobby(lobby, event, data) {
  const message = JSON.stringify({ event, data });
  for (const playerId of Object.keys(lobby.players)) {
    const ws = connections.get(playerId);
    if (ws && ws.readyState === 1) {
      ws.send(message);
    }
  }
}

function sendRaceResults(lobby, winner) {
  if (!HISTORY_SERVICE_URL) return;

  const players = Object.values(lobby.players).map((p, i) => ({
    playerId: p.id,
    username: p.username,
    position: i + 1,
  }));

  const winnerIdx = players.findIndex((p) => p.playerId === winner.id);
  if (winnerIdx > 0) {
    const [w] = players.splice(winnerIdx, 1);
    players.unshift(w);
  }

  const payload = {
    lobbyId: lobby.id,
    players,
    winner: { playerId: winner.id, username: winner.username },
    durationMs: Date.now() - (lobby.createdAt || Date.now()),
    startedAt: new Date(lobby.createdAt || Date.now()).toISOString(),
  };

  fetch(`${HISTORY_SERVICE_URL}/api/history/races`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
  }).catch((err) => console.error('Error enviando resultados a HistoryService:', err.message));
}

function startGameLoop(lobby) {
  if (lobby.gameLoopId) return;

  lobby.raceStarted = true;

  const loopId = setInterval(() => {
    if (lobby.finished) {
      clearInterval(loopId);
      lobby.gameLoopId = null;
      return;
    }

    for (const player of Object.values(lobby.players)) {
      if (checkFinishLine(player)) {
        lobby.finished = true;
        broadcastToLobby(lobby, 'RACE_FINISHED', { winnerId: player.id, winnerUsername: player.username });
        clearInterval(loopId);
        lobby.gameLoopId = null;
        sendRaceResults(lobby, player);
        return;
      }
    }

    const state = {};
    for (const [pid, player] of Object.entries(lobby.players)) {
      state[pid] = {
        posicionX: player.posicionX,
        posicionY: player.posicionY,
        posicionZ: player.posicionZ,
      };
    }
    broadcastToLobby(lobby, 'GAME_STATE', state);
  }, TICK_RATE);

  lobby.gameLoopId = loopId;
}

function handleJoinLobby(ws, data) {
  const { username } = data;
  if (!username) {
    ws.send(JSON.stringify({ event: 'ERROR', data: { message: 'username required' } }));
    return;
  }

  const { player, lobby } = addPlayer(username);
  connections.set(player.id, ws);
  playerLobbies.set(player.id, lobby.id);

  ws.send(JSON.stringify({
    event: 'JOIN_LOBBY',
    data: { playerId: player.id, lobbyId: lobby.id, username: player.username },
  }));

  if (isLobbyFull(lobby)) {
    broadcastToLobby(lobby, 'START_RACE', { lobbyId: lobby.id });
    startGameLoop(lobby);
  }
}

function handleUpdatePosition(ws, data) {
  const { playerId, ...positionData } = data;
  if (!playerId) return;

  const result = updatePlayerPosition(playerId, positionData);
  if (!result) {
    ws.send(JSON.stringify({ event: 'ERROR', data: { message: 'player not found' } }));
  }
}

function handleUpdateInput(ws, data) {
  handleUpdatePosition(ws, data);
}

function handlePong(playerId) {
  pendingPongs.set(playerId, Date.now());
}

function setupHeartbeat() {
  const interval = setInterval(() => {
    const now = Date.now();

    for (const [playerId, ws] of connections.entries()) {
      if (ws.readyState !== 1) {
        cleanupPlayer(playerId);
        continue;
      }

      const lastPong = pendingPongs.get(playerId);
      if (lastPong && now - lastPong > HEARTBEAT_TIMEOUT) {
        cleanupPlayer(playerId);
        continue;
      }

      ws.ping();
    }
  }, HEARTBEAT_INTERVAL);

  server.on('close', () => clearInterval(interval));
}

function cleanupPlayer(playerId) {
  const ws = connections.get(playerId);
  if (ws) {
    try { ws.terminate(); } catch {}
  }
  connections.delete(playerId);
  playerLobbies.delete(playerId);
  pendingPongs.delete(playerId);

  const lobby = removePlayer(playerId);
  if (lobby && Object.keys(lobby.players).length === 0) {
    if (lobby.gameLoopId) {
      clearInterval(lobby.gameLoopId);
    }
    delete lobbies[lobby.id];
  }
}

wss.on('connection', (ws) => {
  ws.on('message', (raw) => {
    let parsed;
    try {
      parsed = JSON.parse(raw.toString());
    } catch {
      ws.send(JSON.stringify({ event: 'ERROR', data: { message: 'invalid JSON' } }));
      return;
    }

    const { event, data } = parsed;

    switch (event) {
      case 'JOIN_LOBBY':
        handleJoinLobby(ws, data);
        break;
      case 'UPDATE_POSITION':
        handleUpdatePosition(ws, data);
        break;
      case 'UPDATE_INPUT':
        handleUpdateInput(ws, data);
        break;
      case 'pong':
        handlePong(data?.playerId);
        break;
      default:
        ws.send(JSON.stringify({ event: 'ERROR', data: { message: `unknown event: ${event}` } }));
    }
  });

  ws.on('pong', () => {
    for (const [playerId, conn] of connections.entries()) {
      if (conn === ws) {
        handlePong(playerId);
        break;
      }
    }
  });

  ws.on('close', () => {
    for (const [playerId, conn] of connections.entries()) {
      if (conn === ws) {
        cleanupPlayer(playerId);
        break;
      }
    }
  });

  ws.on('error', () => {
    for (const [playerId, conn] of connections.entries()) {
      if (conn === ws) {
        cleanupPlayer(playerId);
        break;
      }
    }
  });
});

setupHeartbeat();

server.listen(PORT, '0.0.0.0', () => {
  console.log(`gp-game-server running on port ${PORT}`);
});
