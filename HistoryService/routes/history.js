import { Router } from 'express';
import Race from '../models/Race.js';

const router = Router();

router.post('/races', async (req, res) => {
  try {
    const { lobbyId, players, winner, durationMs, startedAt } = req.body;

    if (!lobbyId || !players || !winner || durationMs === undefined) {
      return res.status(400).json({
        success: false,
        error: 'Faltan campos requeridos: lobbyId, players, winner, durationMs',
      });
    }

    const race = await Race.create({
      lobbyId,
      players,
      winner,
      durationMs,
      startedAt: startedAt ? new Date(startedAt) : new Date(Date.now() - durationMs),
      finishedAt: new Date(),
    });

    res.status(201).json({ success: true, data: race });
  } catch (error) {
    console.error('Error al guardar carrera:', error);
    res.status(500).json({ success: false, error: 'Error interno del servidor' });
  }
});

router.get('/races', async (req, res) => {
  try {
    const page = Math.max(1, parseInt(req.query.page) || 1);
    const limit = Math.min(50, Math.max(1, parseInt(req.query.limit) || 10));
    const skip = (page - 1) * limit;

    const [races, total] = await Promise.all([
      Race.find().sort({ finishedAt: -1 }).skip(skip).limit(limit),
      Race.countDocuments(),
    ]);

    res.json({
      success: true,
      data: races,
      pagination: { page, limit, total, pages: Math.ceil(total / limit) },
    });
  } catch (error) {
    console.error('Error al listar carreras:', error);
    res.status(500).json({ success: false, error: 'Error interno del servidor' });
  }
});

router.get('/races/:id', async (req, res) => {
  try {
    const race = await Race.findById(req.params.id);
    if (!race) {
      return res.status(404).json({ success: false, error: 'Carrera no encontrada' });
    }
    res.json({ success: true, data: race });
  } catch (error) {
    console.error('Error al obtener carrera:', error);
    res.status(500).json({ success: false, error: 'Error interno del servidor' });
  }
});

router.get('/players/:id/stats', async (req, res) => {
  try {
    const playerId = req.params.id;

    const races = await Race.find({ 'players.playerId': playerId });

    const totalRaces = races.length;
    const wins = races.filter((r) => r.winner.playerId === playerId).length;
    const totalPositions = races.reduce((sum, race) => {
      const player = race.players.find((p) => p.playerId === playerId);
      return sum + (player ? player.position : 0);
    }, 0);

    const stats = {
      playerId,
      totalRaces,
      wins,
      winRate: totalRaces > 0 ? Math.round((wins / totalRaces) * 100) : 0,
      averagePosition: totalRaces > 0 ? Math.round((totalPositions / totalRaces) * 100) / 100 : 0,
    };

    res.json({ success: true, data: stats });
  } catch (error) {
    console.error('Error al obtener stats:', error);
    res.status(500).json({ success: false, error: 'Error interno del servidor' });
  }
});

export default router;
