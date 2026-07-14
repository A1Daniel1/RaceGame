# AuthService

Microservicio de autenticación y gestión de sesiones con JWT.

## Tecnologías
- **Runtime:** Node.js 20
- **Framework:** Express.js
- **Base de datos:** MongoDB Atlas (DB: `racegame_auth`)
- **Auth:** JSON Web Tokens (JWT)
- **Deploy:** AWS ECS Fargate

## Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/api/auth/register` | Registrar jugador |
| `POST` | `/api/auth/login` | Iniciar sesión |
| `POST` | `/api/logs` | Registrar log de juego (requiere JWT) |
| `GET` | `/health` | Health check |

### POST /api/auth/register
```json
// Request
{ "username": "player1", "password": "pass123" }

// Response
{
  "id": "64a...",
  "username": "player1",
  "token": "eyJhbGciOiJI...",
  "message": "Jugador registrado exitosamente"
}
```

### POST /api/auth/login
```json
// Request
{ "username": "player1", "password": "pass123" }

// Response
{
  "id": "64a...",
  "username": "player1",
  "token": "eyJhbGciOiJI..."
}
```

## Despliegue local
```bash
cp .env.example .env
# Editar .env con MONGODB_URI y JWT_SECRET
npm install
npm run dev
```

## Despliegue en AWS
1. Crear ECR repo: `auth-service`
2. Push imagen Docker al ECR
3. Configurar `terraform/terraform.tfvars` con valores reales
4. Ejecutar `terraform apply` desde `terraform/`

## Variables de entorno
- `PORT` — Puerto del servidor (default: 3000)
- `MONGODB_URI` — URI de conexión a MongoDB Atlas
- `JWT_SECRET` — Secreto para firmar tokens JWT
