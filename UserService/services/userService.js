import Player from "../models/Player.js";
import Log from "../models/Log.js";

class UserService {
  async registerPlayer(username) {
    if (!username || !username.trim()) {
      const error = new Error("El username es obligatorio");
      error.statusCode = 400;
      throw error;
    }

    const existing = await Player.findOne({ username: username.trim() });
    if (existing) {
      await Log.create({
        action: "REGISTER",
        username: username.trim(),
        status: "FAILURE",
        message: "El jugador ya está registrado",
      });
      const error = new Error("El jugador ya está registrado");
      error.statusCode = 400;
      throw error;
    }

    const player = await Player.create({ username: username.trim() });

    await Log.create({
      action: "REGISTER",
      username: username.trim(),
      status: "SUCCESS",
      message: "Jugador registrado exitosamente",
    });

    return player;
  }

  async getPlayerById(id) {
    const player = await Player.findById(id);
    if (!player) {
      const error = new Error("Jugador no encontrado");
      error.statusCode = 404;
      throw error;
    }
    return player;
  }

  async getPlayerByUsername(username) {
    const player = await Player.findOne({ username });
    if (!player) {
      const error = new Error("Jugador no encontrado");
      error.statusCode = 404;
      throw error;
    }
    return player;
  }

  async getAllPlayers() {
    return Player.find().sort({ createdAt: -1 });
  }

  async deletePlayer(id) {
    const player = await Player.findByIdAndDelete(id);
    if (!player) {
      const error = new Error("Jugador no encontrado");
      error.statusCode = 404;
      throw error;
    }
    return player;
  }
}

export default new UserService();
