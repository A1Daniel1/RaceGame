import mongoose from "mongoose";

const playerSchema = new mongoose.Schema(
  {
    username: {
      type: String,
      required: [true, "El username es obligatorio"],
      unique: true,
      trim: true,
    },
  },
  { timestamps: true }
);

export default mongoose.model("Player", playerSchema);
