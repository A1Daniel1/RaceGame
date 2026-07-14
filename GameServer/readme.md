# gp-game-server

Servidor de juego en tiempo real para RaceGame, construido con Node.js y la librería nativa `ws`.

## Requisitos

- Node.js 18+ (recomendado 24 para desarrollo en contenedor)
- npm

## Instalación

```bash
npm install
```

## Configuración

Editar el archivo `.env`:

```
PORT=3000
```

## Uso

```bash
npm start
# o
node index.js
```

## Arquitectura

### Componentes

- **`index.js`** — Punto de entrada. Levanta el servidor WebSocket, maneja eventos de los clientes, ejecuta el game loop y el sistema de heartbeat.
- **`gameManager.js`** — Módulo de estado en memoria. Gestiona lobbies y jugadores con operaciones CRUD.

### Flujo de conexión

1. El cliente se conecta al WebSocket.
2. Envía `JOIN_LOBBY` con su `username`.
3. El servidor lo asigna a un lobby con cupo (< 4 jugadores).
4. Responde con `playerId` y `lobbyId`.
5. Cuando el lobby llega a 4 jugadores, se emite `START_RACE` automáticamente y arranca el game loop.

### Game Loop

- Frecuencia fija de 30 Hz (33 ms por tick).
- Cada tick emite `GAME_STATE` con las posiciones de todos los jugadores del lobby.
- Si un jugador supera `posicionX >= 500`, se emite `RACE_FINISHED` con el `winnerId` y se detiene el loop.

### Heartbeat

- El servidor envía `ping` cada 15 segundos.
- Si un cliente no responde en 5 segundos, se elimina del lobby y se cierra la conexión.

## Eventos del Protocolo WebSocket

### Cliente → Servidor

| Evento | Datos | Descripción |
|--------|-------|-------------|
| `JOIN_LOBBY` | `{ username }` | Unirse a un lobby |
| `UPDATE_POSITION` | `{ playerId, posicionX, posicionY, posicionZ }` | Actualizar posición |
| `UPDATE_INPUT` | `{ playerId, posicionX, posicionY, posicionZ }` | Actualizar posición (alias) |

### Servidor → Cliente

| Evento | Datos | Descripción |
|--------|-------|-------------|
| `JOIN_LOBBY` | `{ playerId, lobbyId, username }` | Confirmación de unión al lobby |
| `START_RACE` | `{ lobbyId }` | La carrera comienza |
| `GAME_STATE` | `{ [playerId]: { posicionX, posicionY, posicionZ } }` | Estado actual de todos los jugadores |
| `RACE_FINISHED` | `{ winnerId, winnerUsername }` | Un jugador cruzó la meta |
| `ERROR` | `{ message }` | Error ocurrido |

## Despliegue con Docker

### Build de la imagen

```bash
docker build -t gp-game-server .
```

### Ejecutar contenedor

```bash
docker run -d -p 8081:8081 -e PORT=8081 gp-game-server
```

> El servidor lee el puerto de la variable de entorno `PORT`. Si no se define, usa `3000` por defecto. El Dockerfile expone el puerto `8081`; puedes cambiarlo tanto en el `EXPOSE` como en el `-e PORT=...`.

## CI/CD — GitHub Actions

El workflow `.github/workflows/deploy.yml` automatiza el build y push a Amazon ECR:

- **Trigger**: push a la rama `main`.
- **Acciones**: `checkout@v4`, `configure-aws-credentials@v4` (con soporte para `aws-session-token`), `amazon-ecr-login@v2`.
- **Variables**: `AWS_REGION=us-east-1`, `ECR_REPOSITORY=gp-game-server`.
- **Tags**: la imagen se etiqueta como `latest`.

### Secrets requeridos en GitHub

| Secret | Descripción |
|--------|-------------|
| `AWS_ACCESS_KEY_ID` | Access Key de AWS (temporal en AWS Academy) |
| `AWS_SECRET_ACCESS_KEY` | Secret Key de AWS |
| `AWS_SESSION_TOKEN` | Session Token obligatorio para credenciales temporales |

## Estructura del proyecto

```
gp-game-server/
├── .github/
│   └── workflows/
│       └── deploy.yml
├── Dockerfile
├── main.tf
├── index.js
├── gameManager.js
├── package.json
├── .env
└── readme.md
```

## Terraform — Despliegue en ECS Fargate

El archivo `main.tf` define toda la infraestructura necesaria para correr el servidor en AWS ECS Fargate detrás de un Application Load Balancer.

### Recursos creados

| Recurso | Descripción |
|---|---|
| `aws_cloudwatch_log_group` | Logs del contenedor en CloudWatch con retención de 7 días |
| `aws_lb_target_group` | Target group HTTP en puerto 3000, tipo `ip` para Fargate |
| `aws_lb_listener` | Listener en puerto 80 que reenvía al target group |
| `aws_vpc_security_group_egress_rule` | Regla de egreso en el SG del ALB hacia el contenedor en puerto 3000 |
| `aws_ecs_cluster` | Cluster ECS serverless |
| `aws_security_group` | SG del contenedor, permite tráfico entrante en puerto 3000 |
| `aws_ecs_task_definition` | Task definition Fargate (CPU 256, RAM 512) |
| `aws_ecs_service` | Service ECS con health check grace period de 60s |

### Prerequisitos

- Terraform >= 1.5
- AWS CLI autenticado
- Application Load Balancer existente (el ARN está hardcodeado)
- Repositorio ECR con la imagen del servidor

### Pasos

```bash
terraform init
terraform plan
terraform apply
```

### Actualizar el servicio tras un push a ECR

```bash
aws ecs update-service --cluster game-server-d233 --service game-server-d233 --force-new-deployment
```

### Notas importantes

- Health check: `GET /health` en puerto 3000, intervalo 30s, 2 fallos.
- `health_check_grace_period_seconds = 60` para darle tiempo a Node.js a iniciar.
- El SG del contenedor permite ingreso desde `0.0.0.0/0` en puerto 3000.
- El SG del ALB tiene una regla de egreso explícita hacia el SG del contenedor en puerto 3000.
- Las subnets asignan IP pública (`assign_public_ip = true`) para que Fargate pueda descargar la imagen de ECR.

