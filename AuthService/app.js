import express from 'express';
import cors from 'cors';
import authRoutes from './routes/auth.js';
import logRoutes from './routes/logs.js';

const app = express();

app.use(cors());
app.use(express.json());

app.use('/api/auth', authRoutes);
app.use('/api/logs', logRoutes);

app.get('/health', (_req, res) => {
  res.json({ status: 'ok' });
});

app.get('/', (req, res) => {
  res.status(200).send('Auth Service OK');
});

export default app;
