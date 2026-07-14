# GameServer

Servidor de juego en tiempo real para carreras multijugador. Maneja conexiones WebSocket y el loop del juego.

## Tecnologías
- **Runtime:** Node.js 24
- **WebSocket:** ws
- **Estado:** En memoria (sin Redis)
- **Deploy:** AWS ECS Fargate

## Endpoints HTTP

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/health` | Health check |
| `POST` | `/api/lobby/create` | Crear lobby (usado por MatchmakingService) |

## WebSocket Events

### Client → Server
| Event | Data | Descripción |
|-------|------|-------------|
| `JOIN_LOBBY` | `{ playerId, lobbyId, username }` | Unirse a un lobby |
| `UPDATE_INPUT` | `{ playerId, posicionX, posicionY, posicionZ }` | Enviar posición |
| `UPDATE_POSITION` | `{ playerId, posicionX, posicionY, posicionZ }` | Actualizar posición |
| `pong` | `{ playerId }` | Respuesta a heartbeat |

### Server → Client
| Event | Data | Descripción |
|-------|------|-------------|
| `JOIN_LOBBY` | `{ playerId, lobbyId, username }` | Confirmación de unión |
| `START_RACE` | `{ lobbyId }` | Carrera iniciada (4 jugadores) |
| `GAME_STATE` | `{ [playerId]: { posicionX, posicionY, posicionZ } }` | Estado del juego (~30fps) |
| `RACE_FINISHED` | `{ winnerId, winnerUsername }` | Carrera terminada |
| `ERROR` | `{ message }` | Error |

## Flujo del juego
1. MatchmakingService llama `POST /api/lobby/create` con los 4 jugadores
2. GameServer crea el lobby con IDs predefinidos
3. Cada jugador conecta WebSocket y envía `JOIN_LOBBY` con su `playerId` y `lobbyId`
4. Cuando todos están conectados, se envía `START_RACE`
5. Loop del juego a 30fps: recibe inputs, calcula posiciones, envía `GAME_STATE`
6. Cuando un jugador cruza la línea de meta (posicionX >= 500):
   - Se envía `RACE_FINISHED` a todos
   - Se envían resultados a HistoryService (`POST /api/history/races`)
   - Se limpia el lobby

## POST /api/lobby/create
```json
// Request (desde MatchmakingService)
{
  "players": [
    { "playerId": "mm-uuid-1", "username": "player1" },
    { "playerId": "mm-uuid-2", "username": "player2" },
    { "playerId": "mm-uuid-3", "username": "player3" },
    { "playerId": "mm-uuid-4", "username": "player4" }
  ]
}

// Response
{
  "success": true,
  "data": {
    "lobbyId": "1",
    "players": [
      { "playerId": "mm-uuid-1", "username": "player1" },
      ...
    ],
    "gameServerUrl": "ws://ecs-xxx.us-east-1.elb.amazonaws.com"
  }
}
```

## Despliegue local
```bash
cp .env.example .env
npm install
npm start
```

## Variables de entorno
- `PORT` — Puerto del servidor (default: 3000)
- `HISTORY_SERVICE_URL` — URL del ALB para enviar resultados a HistoryService
