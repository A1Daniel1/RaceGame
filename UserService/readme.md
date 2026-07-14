# UserService

Microservicio de autenticación y gestión de usuarios para RaceGame.

## Stack

- **Runtime:** Node.js (ES Modules)
- **Framework:** Express 4
- **ODM:** Mongoose 8
- **Base de datos:** MongoDB

## Estructura

```
UserService/
├── app.js                    # Entry point, middlewares, conexión MongoDB
├── package.json
├── .env.example
├── models/
│   ├── Player.js             # Esquema de jugador (username + timestamps)
│   └── Log.js                # Esquema de log de acciones
├── services/
│   └── userService.js        # Lógica de negocio (UserService class)
├── controllers/
│   └── userController.js     # Handlers con try/catch
└── routes/
    └── authRoutes.js         # Definición de rutas
```

## Instalación

```bash
npm install
```

## Configuración

Copiar `.env.example` a `.env` y ajustar variables:

| Variable      | Descripción                    | Valor por defecto                  |
| ------------- | ------------------------------ | ---------------------------------- |
| `PORT`        | Puerto del servidor            | `3000`                             |
| `MONGODB_URI` | URI de conexión a MongoDB      | `mongodb://localhost:27017/racegame |

## Uso

```bash
# Desarrollo (con watch)
npm run dev

# Producción
npm start
```

## Endpoints

### `POST /api/auth/register`

Registra un nuevo jugador.

**Body:**
```json
{
  "username": "player1"
}
```

**Respuesta exitosa (201):**
```json
{
  "success": true,
  "data": {
    "_id": "664f...",
    "username": "player1",
    "createdAt": "2025-01-01T00:00:00.000Z",
    "updatedAt": "2025-01-01T00:00:00.000Z"
  }
}
```

**Errores:**
| Código | Causa                        |
| ------ | ---------------------------- |
| 400    | Username vacío o ya existente |
| 500    | Error interno del servidor   |

**Respuesta error (400):**
```json
{
  "success": false,
  "error": "El jugador ya está registrado"
}
```

### `GET /api/auth/players`

Lista todos los jugadores registrados (ordenados por fecha de creación descendente).

**Respuesta exitosa (200):**
```json
{
  "success": true,
  "data": [
    { "_id": "...", "username": "player1", "createdAt": "...", "updatedAt": "..." }
  ]
}
```

### `GET /api/auth/players/:id`

Obtiene un jugador por su ID.

**Respuesta exitosa (200):**
```json
{
  "success": true,
  "data": { "_id": "...", "username": "player1", "createdAt": "...", "updatedAt": "..." }
}
```

**Error (404):**
```json
{
  "success": false,
  "error": "Jugador no encontrado"
}
```

### `DELETE /api/auth/players/:id`

Elimina un jugador por su ID.

**Respuesta exitosa (200):**
```json
{
  "success": true,
  "data": { "_id": "...", "username": "player1", "createdAt": "...", "updatedAt": "..." }
}
```

### `GET /health`

Health check del servicio.

```json
{
  "status": "ok",
  "service": "user-service"
}
```

## Logs

Cada operación de registro queda persistida en la colección `logs` con los campos:

| Campo      | Tipo   | Descripción                        |
| ---------- | ------ | ---------------------------------- |
| `action`   | String | `REGISTER`, `LOGIN`, `LOGOUT`, `ERROR` |
| `username` | String | Nombre del usuario involucrado     |
| `status`   | String | `SUCCESS` o `FAILURE`              |
| `message`  | String | Mensaje descriptivo                |
| `ip`       | String | Dirección IP (opcional)            |
