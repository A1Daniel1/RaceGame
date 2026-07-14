import jwt from 'jsonwebtoken';
import Player from '../models/Player.js';

const JWT_SECRET = process.env.JWT_SECRET || 'fallback-secret-do-not-use-in-production';

export function generateToken(player) {
  return jwt.sign({ id: player._id, username: player.username }, JWT_SECRET, {
    expiresIn: '7d',
  });
}

export async function authenticate(req, res, next) {
  try {
    const header = req.headers.authorization;
    if (!header || !header.startsWith('Bearer ')) {
      return res.status(401).json({ error: 'Token requerido' });
    }

    const token = header.split(' ')[1];
    const decoded = jwt.verify(token, JWT_SECRET);
    const player = await Player.findById(decoded.id);

    if (!player) {
      return res.status(401).json({ error: 'Token inválido' });
    }

    req.player = player;
    next();
  } catch (error) {
    return res.status(401).json({ error: 'Token inválido o expirado' });
  }
}
