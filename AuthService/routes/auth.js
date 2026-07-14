import { Router } from 'express';
import Player from '../models/Player.js';
import { generateToken } from '../middleware/auth.js';

const router = Router();

router.post('/register', async (req, res) => {
  try {
    const { username, password } = req.body;

    if (!username || typeof username !== 'string' || username.trim().length === 0) {
      return res.status(400).json({ error: 'El username es obligatorio y no puede estar vacío' });
    }

    if (!password || typeof password !== 'string' || password.length < 6) {
      return res.status(400).json({ error: 'La contraseña es obligatoria y debe tener al menos 6 caracteres' });
    }

    const player = await Player.create({
      username: username.trim(),
      password,
    });

    const token = generateToken(player);

    res.status(201).json({
      id: player._id,
      username: player.username,
      token,
      message: 'Jugador registrado exitosamente',
    });
  } catch (error) {
    if (error.code === 11000) {
      return res.status(409).json({ error: 'El username ya está registrado' });
    }
    console.error('Error en register:', error);
    res.status(500).json({ error: 'Error interno del servidor' });
  }
});

router.post('/login', async (req, res) => {
  try {
    const { username, password } = req.body;

    if (!username || !password) {
      return res.status(400).json({ error: 'Username y contraseña son requeridos' });
    }

    const player = await Player.findOne({ username: username.trim() });
    if (!player) {
      return res.status(401).json({ error: 'Credenciales inválidas' });
    }

    const isMatch = await player.comparePassword(password);
    if (!isMatch) {
      return res.status(401).json({ error: 'Credenciales inválidas' });
    }

    const token = generateToken(player);

    res.json({
      id: player._id,
      username: player.username,
      token,
    });
  } catch (error) {
    console.error('Error en login:', error);
    res.status(500).json({ error: 'Error interno del servidor' });
  }
});

export default router;
