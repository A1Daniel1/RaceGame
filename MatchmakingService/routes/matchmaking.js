import { Router } from 'express';
import * as matchmaking from '../services/matchmaking.js';

const router = Router();

router.post('/join', async (req, res) => {
  try {
    const { playerId, username } = req.body;

    if (!playerId || !username) {
      return res.status(400).json({
        success: false,
        error: 'Faltan campos requeridos: playerId, username',
      });
    }

    const result = matchmaking.join(playerId, username);

    if (result.status === 'matched') {
      let lobbyCreated = false;
      try {
        const gameServerUrl = process.env.GAME_SERVER_URL;
        const response = await fetch(`${gameServerUrl}/api/lobby/create`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ players: result.players }),
        });

        if (response.ok) {
          const lobbyData = await response.json();
          lobbyCreated = true;
          return res.json({
            success: true,
            data: {
              status: 'ready',
              lobbyId: lobbyData.data.lobbyId,
              gameServerUrl: `ws://${new URL(gameServerUrl).host}`,
              players: lobbyData.data.players,
            },
          });
        }
      } catch (err) {
        console.error('Error al crear lobby en GameServer:', err.message);
      }

      if (!lobbyCreated) {
        for (const p of result.players) {
          matchmaking.leave(p.playerId);
        }
        return res.status(503).json({
          success: false,
          error: 'No se pudo crear el lobby. Intenta de nuevo.',
        });
      }
    }

    res.json({
      success: true,
      data: {
        status: result.status,
        position: result.position,
      },
    });
  } catch (error) {
    console.error('Error en matchmaking join:', error);
    res.status(500).json({ success: false, error: 'Error interno del servidor' });
  }
});

router.delete('/leave', (req, res) => {
  try {
    const { playerId } = req.body;

    if (!playerId) {
      return res.status(400).json({
        success: false,
        error: 'Falta campo requerido: playerId',
      });
    }

    const removed = matchmaking.leave(playerId);
    res.json({ success: true, data: { removed } });
  } catch (error) {
    console.error('Error en matchmaking leave:', error);
    res.status(500).json({ success: false, error: 'Error interno del servidor' });
  }
});

router.get('/status/:playerId', (req, res) => {
  try {
    const status = matchmaking.getStatus(req.params.playerId);
    res.json({ success: true, data: status });
  } catch (error) {
    console.error('Error en matchmaking status:', error);
    res.status(500).json({ success: false, error: 'Error interno del servidor' });
  }
});

export default router;
