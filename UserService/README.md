# UserService

Microservicio de gestión de perfiles de jugadores.

## Tecnologías
- **Runtime:** Node.js 20
- **Framework:** Express.js
- **Base de datos:** MongoDB Atlas (DB: `racegame_users`)
- **Deploy:** AWS ECS Fargate

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/users/register` | Crear perfil de jugador |
| `GET` | `/api/users/players` | Listar todos los jugadores |
| `GET` | `/api/users/players/:id` | Obtener jugador por ID |
| `DELETE` | `/api/users/players/:id` | Eliminar jugador |
| `GET` | `/health` | Health check |

### POST /api/users/register
```json
// Request
{ "username": "player1" }

// Response
{ "success": true, "data": { "_id": "64a...", "username": "player1" } }
```

### GET /api/users/players
```json
{
  "success": true,
  "data": [
    { "_id": "64a...", "username": "player1" },
    { "_id": "64b...", "username": "player2" }
  ]
}
```

## Despliegue local
```bash
cp .env.example .env
# Editar .env con MONGODB_URI
npm install
npm run dev
```

## Despliegue en AWS
1. Crear ECR repo: `user-service`
2. Push imagen Docker al ECR
3. Configurar `terraform.tfvars` en `terraform/`
4. Ejecutar `terraform apply` desde `terraform/`

## Variables de entorno
- `PORT` — Puerto del servidor (default: 3000)
- `MONGODB_URI` — URI de conexión a MongoDB Atlas
