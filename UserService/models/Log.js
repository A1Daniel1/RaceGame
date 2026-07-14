import mongoose from "mongoose";

const logSchema = new mongoose.Schema(
  {
    action: {
      type: String,
      required: true,
      enum: ["REGISTER", "LOGIN", "LOGOUT", "ERROR"],
    },
    username: {
      type: String,
      required: true,
    },
    status: {
      type: String,
      enum: ["SUCCESS", "FAILURE"],
      required: true,
    },
    message: {
      type: String,
      default: "",
    },
    ip: {
      type: String,
      default: "",
    },
  },
  { timestamps: true }
);

export default mongoose.model("Log", logSchema);
