import 'dotenv/config';
import mongoose from 'mongoose';
import app from './app.js';

const PORT = process.env.PORT || 3000;
const MONGODB_URI = process.env.MONGODB_URI;

if (!MONGODB_URI) {
  console.error('MONGODB_URI no esta configurado');
  process.exit(1);
}

try {
  await mongoose.connect(MONGODB_URI);
  console.log('Conectado a MongoDB (history)');
} catch (error) {
  console.error('Error conectando a MongoDB:', error.message);
  process.exit(1);
}

app.listen(PORT, '0.0.0.0', () => {
  console.log(`history-service corriendo en puerto ${PORT}`);
});
