# MatchmakingService

Microservicio que gestiona la cola de emparejamiento de jugadores para iniciar carreras.

## Tecnologías
- **Runtime:** Node.js 20
- **Framework:** Express.js
- **Base de datos:** En memoria (sin persistencia)
- **Deploy:** AWS ECS Fargate

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/matchmaking/join` | Unirse a la cola |
| `DELETE` | `/api/matchmaking/leave` | Salir de la cola |
| `GET` | `/api/matchmaking/status/:playerId` | Estado de búsqueda |
| `GET` | `/health` | Health check |

### POST /api/matchmaking/join
```json
// Request
{ "playerId": "uuid-1", "username": "player1" }

// Response (en cola)
{ "success": true, "data": { "status": "queued", "position": 1 } }

// Response (match encontrado)
{
  "success": true,
  "data": {
    "status": "ready",
    "lobbyId": "5",
    "gameServerUrl": "ws://ecs-xxx.us-east-1.elb.amazonaws.com",
    "players": [...]
  }
}
```

### GET /api/matchmaking/status/:playerId
```json
// Esperando
{ "success": true, "data": { "status": "waiting", "position": 2 } }

// Listo
{ "success": true, "data": { "status": "ready", "lobbyId": "5", "gameServerUrl": "ws://..." } }
```

## Flujo
1. Jugador llama `POST /join` → entra en cola
2. Cliente hace polling cada 2s con `GET /status/:playerId`
3. Cuando hay 4 jugadores, MatchmakingService llama al GameServer para crear el lobby
4. Jugador recibe `status: "ready"` con `lobbyId` y `gameServerUrl`
5. Jugador abre WebSocket al GameServer con ese `lobbyId`

## Despliegue local
```bash
cp .env.example .env
# Editar .env con GAME_SERVER_URL
npm install
npm run dev
```

## Despliegue en AWS
1. Crear ECR repo: `matchmaking-service`
2. Push imagen Docker al ECR
3. Configurar `terraform.tfvars` en `terraform/`
4. Ejecutar `terraform apply` desde `terraform/`

## Variables de entorno
- `PORT` — Puerto del servidor (default: 3000)
- `GAME_SERVER_URL` — URL del ALB para comunicarse con GameServer
- `MAX_PLAYERS_PER_LOBBY` — Jugadores por lobby (default: 4)
