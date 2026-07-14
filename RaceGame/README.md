# RaceGame

Juego de carreras multijugador con cliente Unity WebGL y backend en AWS ECS Fargate.

## Arquitectura

```
Unity WebGL
  │
  ├──HTTP──► ALB ──► AuthService        (/api/auth/*)
  ├──HTTP──► ALB ──► UserService        (/api/users/*)
  ├──HTTP──► ALB ──► HistoryService     (/api/history/*)
  ├──HTTP──► ALB ──► MatchmakingService (/api/matchmaking/*)
  │
  └──WebSocket──► ALB ──► GameServer    (default)

MatchmakingService ──HTTP──► GameServer  (POST /api/lobby/create)
GameServer ──HTTP──► HistoryService      (POST /api/history/races)
```

## Servicios

| Servicio | Puerto | Tecnología | Estado |
|----------|--------|------------|--------|
| GameServer | 3000 | Node.js + WebSocket | Desplegado |
| AuthService | 3000 | Node.js + JWT + MongoDB | Pendiente deploy |
| UserService | 3000 | Node.js + MongoDB | Pendiente deploy |
| HistoryService | 3000 | Node.js + MongoDB | Nuevo |
| MatchmakingService | 3000 | Node.js (en memoria) | Nuevo |

## URLs de Producción

| Servicio | URL |
|----------|-----|
| ALB | `http://ecs-express-gateway-alb-18642d98-656595321.us-east-1.elb.amazonaws.com` |
| Auth | `http://<ALB>/api/auth/login` |
| Users | `http://<ALB>/api/users/players` |
| History | `http://<ALB>/api/history/races` |
| Matchmaking | `http://<ALB>/api/matchmaking/join` |
| GameServer (WS) | `ws://ecs-express-gateway-alb-18642d98-656595321.us-east-1.elb.amazonaws.com` |

## Endpoints

### AuthService
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/auth/register` | Registrar jugador |
| POST | `/api/auth/login` | Login → JWT |
| GET | `/health` | Health check |

### UserService
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/users/register` | Crear perfil |
| GET | `/api/users/players` | Listar jugadores |
| GET | `/api/users/players/:id` | Obtener jugador |
| DELETE | `/api/users/players/:id` | Eliminar jugador |

### HistoryService
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/history/races` | Guardar carrera |
| GET | `/api/history/races` | Listar carreras |
| GET | `/api/history/races/:id` | Detalle carrera |
| GET | `/api/history/players/:id/stats` | Stats jugador |

### MatchmakingService
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/matchmaking/join` | Unirse a cola |
| DELETE | `/api/matchmaking/leave` | Salir de cola |
| GET | `/api/matchmaking/status/:playerId` | Estado búsqueda |

### GameServer
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/health` | Health check |
| POST | `/api/lobby/create` | Crear lobby (interno) |

**WebSocket Events:**
| Evento | Dirección | Descripción |
|--------|-----------|-------------|
| `JOIN_LOBBY` | both | Unirse/confirmar lobby |
| `START_RACE` | S→C | Carrera iniciada |
| `GAME_STATE` | S→C | Posiciones (~30fps) |
| `RACE_FINISHED` | S→C | Carrera terminada |
| `UPDATE_INPUT` | C→S | Enviar posición |

## Estructura del Proyecto

```
RaceGame/
├── AuthService/           # Microservicio de autenticación
│   ├── terraform/         # Infraestructura AWS
│   └── .github/workflows/ # CI/CD
├── UserService/           # Microservicio de usuarios
│   ├── terraform/
│   └── .github/workflows/
├── HistoryService/        # Microservicio de historial
│   ├── terraform/
│   └── .github/workflows/
├── MatchmakingService/    # Microservicio de matchmaking
│   ├── terraform/
│   └── .github/workflows/
├── GameServer/            # Servidor de juego en tiempo real
│   ├── terraform/
│   └── .github/workflows/
└── RaceGame/              # Cliente Unity
    └── Assets/Scripts/Network/
        ├── ApiClient.cs
        ├── WebSocketClient.cs
        ├── AuthManager.cs
        ├── MatchmakingManager.cs
        └── GameNetworkManager.cs
```

## Despliegue

### GitHub Secrets (configurar en el repo)
| Secret | Descripción |
|--------|-------------|
| `AWS_ACCESS_KEY_ID` | Credencial AWS |
| `AWS_SECRET_ACCESS_KEY` | Secreto AWS |
| `AWS_SESSION_TOKEN` | Token temporal AWS |

### Pasos
1. Configurar GitHub secrets
2. Crear ECR repos: `auth-service`, `user-service`, `history-service`, `matchmaking-service`
3. Push a `main` → GitHub Actions buildea y sube imágenes a ECR
4. Ejecutar `terraform apply` en cada servicio
5. Verificar con `curl http://<ALB>/health`

### Terraform (desde la carpeta de cada servicio)
```bash
cd AuthService/terraform
cp terraform.tfvars.example terraform.tfvars
# Editar con valores reales
terraform init
terraform apply
```

## Scripts Unity (C#)

Los scripts de red están en `RaceGame/Assets/Scripts/Network/`:

- **ApiClient.cs** — Wrapper HTTP genérico
- **AuthManager.cs** — Login/Register con JWT
- **MatchmakingManager.cs** — Cola de emparejamiento
- **GameNetworkManager.cs** — WebSocket + game loop
- **WebSocketClient.cs** — Conexión WebSocket

## Base de datos

Todos los servicios usan el mismo cluster de MongoDB Atlas con bases de datos separadas:

| Servicio | DB Name |
|----------|---------|
| AuthService | `racegame_auth` |
| UserService | `racegame_users` |
| HistoryService | `racegame_history` |
