import userService from "../services/userService.js";

export async function register(req, res) {
  try {
    const { username } = req.body;
    const player = await userService.registerPlayer(username);
    res.status(201).json({ success: true, data: player });
  } catch (error) {
    const statusCode = error.statusCode || 500;
    res.status(statusCode).json({
      success: false,
      error: error.message || "Error interno del servidor",
    });
  }
}

export async function getPlayer(req, res) {
  try {
    const { id } = req.params;
    const player = await userService.getPlayerById(id);
    res.json({ success: true, data: player });
  } catch (error) {
    const statusCode = error.statusCode || 500;
    res.status(statusCode).json({
      success: false,
      error: error.message || "Error interno del servidor",
    });
  }
}

export async function listPlayers(req, res) {
  try {
    const players = await userService.getAllPlayers();
    res.json({ success: true, data: players });
  } catch (error) {
    res.status(500).json({
      success: false,
      error: error.message || "Error interno del servidor",
    });
  }
}

export async function removePlayer(req, res) {
  try {
    const { id } = req.params;
    const player = await userService.deletePlayer(id);
    res.json({ success: true, data: player });
  } catch (error) {
    const statusCode = error.statusCode || 500;
    res.status(statusCode).json({
      success: false,
      error: error.message || "Error interno del servidor",
    });
  }
}
