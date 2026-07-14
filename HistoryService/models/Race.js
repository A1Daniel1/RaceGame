import mongoose from 'mongoose';

const raceSchema = new mongoose.Schema({
  lobbyId: {
    type: String,
    required: true,
  },
  players: [{
    playerId: { type: String, required: true },
    username: { type: String, required: true },
    position: { type: Number, required: true },
  }],
  winner: {
    playerId: { type: String, required: true },
    username: { type: String, required: true },
  },
  durationMs: {
    type: Number,
    required: true,
  },
  startedAt: {
    type: Date,
    required: true,
  },
  finishedAt: {
    type: Date,
    required: true,
  },
}, { timestamps: true });

raceSchema.index({ 'players.playerId': 1 });
raceSchema.index({ finishedAt: -1 });

export default mongoose.model('Race', raceSchema);
