const lobbies = {};

let nextLobbyId = 1;
let nextPlayerId = 1;

const MAX_PLAYERS_PER_LOBBY = 4;
const FINISH_LINE_X = 500;

function getOrCreateAvailableLobby() {
  for (const lobbyId of Object.keys(lobbies)) {
    const lobby = lobbies[lobbyId];
    if (Object.keys(lobby.players).length < MAX_PLAYERS_PER_LOBBY) {
      return lobby;
    }
  }
  const lobbyId = String(nextLobbyId++);
  const lobby = {
    id: lobbyId,
    players: {},
    raceStarted: false,
    finished: false,
    gameLoopId: null,
  };
  lobbies[lobbyId] = lobby;
  return lobby;
}

function addPlayer(username) {
  const lobby = getOrCreateAvailableLobby();
  const playerId = String(nextPlayerId++);
  const player = {
    id: playerId,
    username,
    posicionX: 0,
    posicionY: 0,
    posicionZ: 0,
  };
  lobby.players[playerId] = player;
  return { player, lobby };
}

function removePlayer(playerId) {
  for (const lobby of Object.values(lobbies)) {
    if (lobby.players[playerId]) {
      delete lobby.players[playerId];
      if (lobby.gameLoopId && Object.keys(lobby.players).length === 0) {
        clearInterval(lobby.gameLoopId);
        delete lobbies[lobby.id];
      }
      return lobby;
    }
  }
  return null;
}

function updatePlayerPosition(playerId, data) {
  for (const lobby of Object.values(lobbies)) {
    if (lobby.players[playerId]) {
      const player = lobby.players[playerId];
      if (data.posicionX !== undefined) player.posicionX = data.posicionX;
      if (data.posicionY !== undefined) player.posicionY = data.posicionY;
      if (data.posicionZ !== undefined) player.posicionZ = data.posicionZ;
      return { player, lobby };
    }
  }
  return null;
}

function checkFinishLine(player) {
  return player.posicionX >= FINISH_LINE_X;
}

function isLobbyFull(lobby) {
  return Object.keys(lobby.players).length >= MAX_PLAYERS_PER_LOBBY;
}

function createLobbyWithPlayers(players) {
  const lobbyId = String(nextLobbyId++);
  const lobby = {
    id: lobbyId,
    players: {},
    raceStarted: false,
    finished: false,
    gameLoopId: null,
    createdAt: Date.now(),
  };

  const assignedPlayers = [];
  for (const p of players) {
    lobby.players[p.playerId] = {
      id: p.playerId,
      username: p.username,
      posicionX: 0,
      posicionY: 0,
      posicionZ: 0,
    };
    assignedPlayers.push({ playerId: p.playerId, username: p.username });
  }

  lobbies[lobbyId] = lobby;
  return { lobby, players: assignedPlayers };
}

export {
  lobbies,
  addPlayer,
  removePlayer,
  updatePlayerPosition,
  checkFinishLine,
  isLobbyFull,
  createLobbyWithPlayers,
  MAX_PLAYERS_PER_LOBBY,
  FINISH_LINE_X,
};
