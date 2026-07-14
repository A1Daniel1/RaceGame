import { v4 as uuidv4 } from 'uuid';

const queue = new Map();
const MAX_PLAYERS = parseInt(process.env.MAX_PLAYERS_PER_LOBBY) || 4;
const GAME_SERVER_URL = process.env.GAME_SERVER_URL;

function join(playerId, username) {
  if (queue.has(playerId)) {
    return { status: 'queued', position: getPosition(playerId) };
  }

  queue.set(playerId, {
    playerId,
    username,
    joinedAt: Date.now(),
  });

  const position = getPosition(playerId);

  if (queue.size >= MAX_PLAYERS) {
    const players = [];
    for (const [id, entry] of queue) {
      if (players.length >= MAX_PLAYERS) break;
      players.push({ playerId: entry.playerId, username: entry.username });
      queue.delete(id);
    }
    return { status: 'matched', position: 0, players };
  }

  return { status: 'queued', position };
}

function leave(playerId) {
  return queue.delete(playerId);
}

function getStatus(playerId) {
  if (!queue.has(playerId)) {
    return { status: 'not_found' };
  }
  return { status: 'waiting', position: getPosition(playerId) };
}

function getPosition(playerId) {
  let pos = 1;
  for (const [id] of queue) {
    if (id === playerId) return pos;
    pos++;
  }
  return -1;
}

function getQueueSize() {
  return queue.size;
}

export { join, leave, getStatus, getQueueSize };
