import { Router } from "express";
import {
  register,
  getPlayer,
  listPlayers,
  removePlayer,
} from "../controllers/userController.js";

const router = Router();

router.post("/register", register);
router.get("/players", listPlayers);
router.get("/players/:id", getPlayer);
router.delete("/players/:id", removePlayer);

export default router;
