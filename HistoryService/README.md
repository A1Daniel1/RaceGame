# HistoryService

Microservicio que almacena el historial y estadísticas de las carreras completadas.

## Tecnologías
- **Runtime:** Node.js 20
- **Framework:** Express.js
- **Base de datos:** MongoDB Atlas (DB: `racegame_history`)
- **Deploy:** AWS ECS Fargate

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/history/races` | Guardar resultado de carrera |
| `GET` | `/api/history/races` | Listar carreras (paginado) |
| `GET` | `/api/history/races/:id` | Detalle de una carrera |
| `GET` | `/api/history/players/:id/stats` | Estadísticas de un jugador |
| `GET` | `/health` | Health check |

### POST /api/history/races
```json
{
  "lobbyId": "1",
  "players": [
    { "playerId": "1", "username": "player1", "position": 1 },
    { "playerId": "2", "username": "player2", "position": 2 }
  ],
  "winner": { "playerId": "1", "username": "player1" },
  "durationMs": 45000,
  "startedAt": "2026-07-10T20:00:00Z"
}
```

### GET /api/history/players/:id/stats
```json
{
  "playerId": "1",
  "totalRaces": 10,
  "wins": 3,
  "winRate": 30,
  "averagePosition": 2.1
}
```

## Despliegue local
```bash
cp .env.example .env
# Editar .env con tu MONGODB_URI
npm install
npm run dev
```

## Despliegue en AWS
1. Crear ECR repo: `history-service`
2. Push imagen Docker al ECR
3. Configurar `terraform.tfvars` en `terraform/`
4. Ejecutar `terraform apply` desde `terraform/`

## Variables de entorno
- `PORT` — Puerto del servidor (default: 3000)
- `MONGODB_URI` — URI de conexión a MongoDB Atlas
