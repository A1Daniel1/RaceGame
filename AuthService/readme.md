# AuthService

Microservicio de autenticación y logs para un juego de carreras multijugador.

## Requisitos

- Node.js 18+
- MongoDB

## Instalación

```bash
npm install
```

## Configuración

Copiar `.env.example` a `.env` y ajustar variables:

| Variable       | Descripción                    | Default                                  |
|----------------|--------------------------------|------------------------------------------|
| `PORT`         | Puerto del servidor            | `3000`                                   |
| `MONGODB_URI`  | URI de conexión a MongoDB      | `mongodb+srv://<user>:<password>@cluster0.mk2gice.mongodb.net/racegame?retryWrites=true&w=majority&appName=Cluster0` |

## Uso

```bash
npm start        # producción
npm run dev      # desarrollo con --watch
```

## Docker

```bash
docker build -t gp-auth-logs-service .
docker run -p 8080:3000 --env-file .env gp-auth-logs-service
```

La imagen expone el puerto `8080` (mapear al `PORT` interno, ej. `3000`).

## Endpoints

### `POST /api/auth/register`

Registra un jugador en la colección `players`.

**Body:**
```json
{ "username": "nombre_del_jugador" }
```

**Response (201):**
```json
{ "id": "...", "message": "Jugador registrado exitosamente" }
```

**Validation:** `username` es obligatorio y no puede estar vacío.

---

### `POST /api/logs`

Guarda un registro de telemetría del juego en la colección `logs`.

**Body:**
```json
{
  "timestamp": "2025-01-01T00:00:00.000Z",
  "jugador": "nombre_del_jugador",
  "evento": "cruce_meta",
  "rttMs": 45
}
```

**Response (201):**
```json
{ "id": "...", "message": "Log registrado exitosamente" }
```

**Campos requeridos:** `timestamp`, `jugador`, `evento`, `rttMs`.

---

### `GET /health`

Health check del servicio.

**Response:**
```json
{ "status": "ok" }
```

## Estructura del proyecto

```
├── models/
│   ├── Player.js       # Schema de jugadores
│   └── Log.js          # Schema de logs
├── routes/
│   ├── auth.js         # Ruta de autenticación
│   └── logs.js         # Ruta de logs
├── app.js              # Configuración de Express
├── server.js           # Punto de entrada
├── Dockerfile          # Imagen para ECS
├── .github/workflows/
│   └── deploy.yml      # CI/CD a Amazon ECS
└── package.json
```

## Base de datos

El servicio usa MongoDB Atlas. Las credenciales de la base de datos compartida son:

| Usuario     | Contraseña          |
|-------------|---------------------|
| `app_user`  | `LHNkNEfx9VQZV9aa` |

La conexión se realiza automáticamente al iniciar el servidor con las variables de entorno configuradas.
