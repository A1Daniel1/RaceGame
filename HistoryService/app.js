import 'dotenv/config';
import express from 'express';
import cors from 'cors';
import historyRoutes from './routes/history.js';

const app = express();

app.use(cors());
app.use(express.json());

app.use('/api/history', historyRoutes);

app.get('/health', (_req, res) => {
  res.json({ status: 'ok', service: 'history-service' });
});

app.use((_req, res) => {
  res.status(404).json({ success: false, error: 'Ruta no encontrada' });
});

export default app;
