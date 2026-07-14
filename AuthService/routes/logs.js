import { Router } from 'express';
import Log from '../models/Log.js';
import { authenticate } from '../middleware/auth.js';

const router = Router();

router.post('/', authenticate, async (req, res) => {
  try {
    const { timestamp, jugador, evento, rttMs } = req.body;

    if (!timestamp || !jugador || !evento || rttMs === undefined) {
      return res.status(400).json({ error: 'Faltan campos requeridos: timestamp, jugador, evento, rttMs' });
    }

    const log = await Log.create({
      timestamp: new Date(timestamp),
      jugador,
      evento,
      rttMs,
    });

    res.status(201).json({
      id: log._id,
      message: 'Log registrado exitosamente',
    });
  } catch (error) {
    console.error('Error en logs:', error);
    res.status(500).json({ error: 'Error interno del servidor' });
  }
});

export default router;
