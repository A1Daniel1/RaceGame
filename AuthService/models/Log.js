import mongoose from 'mongoose';

const logSchema = new mongoose.Schema({
  timestamp: { type: Date, required: true },
  jugador: { type: String, required: true },
  evento: { type: String, required: true },
  rttMs: { type: Number, required: true },
}, { timestamps: false });

export default mongoose.model('Log', logSchema);
