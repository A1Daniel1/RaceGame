import 'dotenv/config';
import mongoose from 'mongoose';
import app from './app.js';

const PORT = process.env.PORT || 3000;
const MONGODB_URI = process.env.MONGODB_URI || 'mongodb://localhost:27017/racegame';

try {
  await mongoose.connect(MONGODB_URI);
  console.log('Conectado a MongoDB');
} catch (error) {
  console.error('Error conectando a MongoDB:', error.message);
  process.exit(1);
}

app.listen(PORT, '0.0.0.0', () => {
  console.log(`Servidor corriendo en puerto ${PORT}`);
});
