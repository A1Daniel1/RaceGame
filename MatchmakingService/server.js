import 'dotenv/config';
import express from 'express';
import cors from 'cors';
import matchmakingRoutes from './routes/matchmaking.js';

const app = express();
const PORT = process.env.PORT || 3000;

app.use(cors());
app.use(express.json());

app.use('/api/matchmaking', matchmakingRoutes);

app.get('/health', (_req, res) => {
  res.json({ status: 'ok', service: 'matchmaking-service' });
});

app.use((_req, res) => {
  res.status(404).json({ success: false, error: 'Ruta no encontrada' });
});

app.listen(PORT, '0.0.0.0', () => {
  console.log(`matchmaking-service corriendo en puerto ${PORT}`);
});
