# Guia de Despliegue — RaceGame

## Arquitectura Final

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

---

## Paso 1: Configurar GitHub Secrets

Ir a tu repo de GitHub → Settings → Secrets and variables → Actions → New repository secret.

Crear los siguientes secrets:

| Nombre | Valor |
|--------|-------|
| `AWS_ACCESS_KEY_ID` | Tu access key de AWS |
| `AWS_SECRET_ACCESS_KEY` | Tu secret key de AWS |
| `AWS_SESSION_TOKEN` | Tu session token temporal (si usas roles) |

---

## Paso 2: Crear ECR Repositories

En la consola de AWS o con AWS CLI:

```bash
aws ecr create-repository --repository-name auth-service --region us-east-1
aws ecr create-repository --repository-name user-service --region us-east-1
aws ecr create-repository --repository-name history-service --region us-east-1
aws ecr create-repository --repository-name matchmaking-service --region us-east-1
```

> Si el `game-server` ya existe en ECR, no es necesario crearlo de nuevo.

---

## Paso 3: Push de imagenes a ECR

Opcion A — **GitHub Actions** (automatico):
Hacer push a la rama `main` y cada servicio buildea su imagen automaticamente.

Opcion B — **Manual** desde tu maquina:
```bash
# Login a ECR
aws ecr get-login-password --region us-east-1 | docker login --username AWS --password-stdin 042076316445.dkr.ecr.us-east-1.amazonaws.com

# AuthService
cd AuthService
docker build -t auth-service .
docker tag auth-service:latest 042076316445.dkr.ecr.us-east-1.amazonaws.com/auth-service:latest
docker push 042076316445.dkr.ecr.us-east-1.amazonaws.com/auth-service:latest

# UserService
cd ../UserService
docker build -t user-service .
docker tag user-service:latest 042076316445.dkr.ecr.us-east-1.amazonaws.com/user-service:latest
docker push 042076316445.dkr.ecr.us-east-1.amazonaws.com/user-service:latest

# HistoryService
cd ../HistoryService
docker build -t history-service .
docker tag history-service:latest 042076316445.dkr.ecr.us-east-1.amazonaws.com/history-service:latest
docker push 042076316445.dkr.ecr.us-east-1.amazonaws.com/history-service:latest

# MatchmakingService
cd ../MatchmakingService
docker build -t matchmaking-service .
docker tag matchmaking-service:latest 042076316445.dkr.ecr.us-east-1.amazonaws.com/matchmaking-service:latest
docker push 042076316445.dkr.ecr.us-east-1.amazonaws.com/matchmaking-service:latest
```

---

## Paso 4: Configurar Terraform Variables

### AuthService
```bash
cd AuthService/terraform
cp terraform.tfvars.example terraform.tfvars
```

Editar `terraform.tfvars`:
```hcl
aws_region     = "us-east-1"
vpc_id         = "vpc-00ec11dca0610ee1c"
subnet_ids     = ["subnet-0271cfa9c9e590320", "subnet-03b5e829969f8fea7"]
listener_arn   = "arn:aws:elasticloadbalancing:us-east-1:042076316445:listener/app/ecs-express-gateway-alb-18642d98/77aae85f27cf11be/29a713eddf4596cd"
ecr_image_uri  = "042076316445.dkr.ecr.us-east-1.amazonaws.com/auth-service:latest"
mongodb_uri    = "mongodb+srv://app_user:LHNkNEfx9VQZV9aa@cluster0.mk2gice.mongodb.net/racegame_auth"
```

### UserService
```bash
cd UserService/terraform
cp terraform.tfvars.example terraform.tfvars
```

Editar `terraform.tfvars`:
```hcl
aws_region     = "us-east-1"
vpc_id         = "vpc-00ec11dca0610ee1c"
subnet_ids     = ["subnet-0271cfa9c9e590320", "subnet-03b5e829969f8fea7"]
listener_arn   = "arn:aws:elasticloadbalancing:us-east-1:042076316445:listener/app/ecs-express-gateway-alb-18642d98/77aae85f27cf11be/29a713eddf4596cd"
ecr_image_uri  = "042076316445.dkr.ecr.us-east-1.amazonaws.com/user-service:latest"
mongodb_uri    = "mongodb+srv://app_user:LHNkNEfx9VQZV9aa@cluster0.mk2gice.mongodb.net/racegame_users"
```

### HistoryService
```bash
cd HistoryService/terraform
cp terraform.tfvars.example terraform.tfvars
```

Editar `terraform.tfvars`:
```hcl
aws_region     = "us-east-1"
vpc_id         = "vpc-00ec11dca0610ee1c"
subnet_ids     = ["subnet-0271cfa9c9e590320", "subnet-03b5e829969f8fea7"]
listener_arn   = "arn:aws:elasticloadbalancing:us-east-1:042076316445:listener/app/ecs-express-gateway-alb-18642d98/77aae85f27cf11be/29a713eddf4596cd"
ecr_image_uri  = "042076316445.dkr.ecr.us-east-1.amazonaws.com/history-service:latest"
mongodb_uri    = "mongodb+srv://app_user:LHNkNEfx9VQZV9aa@cluster0.mk2gice.mongodb.net/racegame_history"
```

### MatchmakingService
```bash
cd MatchmakingService/terraform
cp terraform.tfvars.example terraform.tfvars
```

Editar `terraform.tfvars`:
```hcl
aws_region       = "us-east-1"
vpc_id           = "vpc-00ec11dca0610ee1c"
subnet_ids       = ["subnet-0271cfa9c9e590320", "subnet-03b5e829969f8fea7"]
listener_arn     = "arn:aws:elasticloadbalancing:us-east-1:042076316445:listener/app/ecs-express-gateway-alb-18642d98/77aae85f27cf11be/29a713eddf4596cd"
ecr_image_uri    = "042076316445.dkr.ecr.us-east-1.amazonaws.com/matchmaking-service:latest"
game_server_url  = "http://ecs-express-gateway-alb-18642d98-656595321.us-east-1.elb.amazonaws.com"
```

---

## Paso 5: Ejecutar Terraform

Ejecutar en orden (cada servicio es independiente pero el GameServer depende del ALB que ya existe):

```bash
# 1. AuthService
cd AuthService/terraform
terraform init
terraform apply -auto-approve

# 2. UserService
cd ../../UserService/terraform
terraform init
terraform apply -auto-approve

# 3. HistoryService
cd ../../HistoryService/terraform
terraform init
terraform apply -auto-approve

# 4. MatchmakingService
cd ../../MatchmakingService/terraform
terraform init
terraform apply -auto-approve

# 5. GameServer (ya desplegado, solo actualizar si hay cambios)
cd ../../GameServer
terraform apply -auto-approve
```

---

## Paso 6: Verificar

### Health checks
```bash
ALB="http://ecs-express-gateway-alb-18642d98-656595321.us-east-1.elb.amazonaws.com"

curl $ALB/health                                    # GameServer
curl $ALB/api/auth/health                           # AuthService
curl $ALB/api/users/health                          # UserService
curl $ALB/api/history/health                        # HistoryService
curl $ALB/api/matchmaking/health                    # MatchmakingService
```

### Test de registro y login
```bash
# Register
curl -X POST $ALB/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testplayer","password":"test123"}'

# Login
curl -X POST $ALB/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testplayer","password":"test123"}'
```

### Test de WebSocket
```bash
# Usar un cliente WebSocket para conectarse a:
ws://ecs-express-gateway-alb-18642d98-656595321.us-east-1.elb.amazonaws.com

# Enviar:
{"event":"JOIN_LOBBY","data":{"username":"testplayer"}}
```

---

## Paso 7: Integrar en Unity

Los scripts de red estan en `RaceGame/Assets/Scripts/Network/`.

### 7.1 — Crear NetworkBootstrap

1. En la escena `MainMenu`, crear un GameObject vacio llamado `[NetworkBootstrap]`
2. Agregarle el componente `NetworkBootstrap.cs`
3. Este script crea automaticamente los singletons: `ApiClient`, `AuthManager`, `WebSocketClient`, `GameNetworkManager`, `MatchmakingManager`
4. Marcarlo como `DontDestroyOnLoad` (ya lo hace el script)

### 7.2 — Configurar MatchmakingManager

En el Inspector del GameObject que tenga `MatchmakingManager`:
- No requiere configuracion manual — usa `NetworkConfig` internamente

### 7.3 — Configurar GameNetworkManager

En el Inspector del GameObject que tenga `GameNetworkManager`:
- No requiere configuracion — usa `WebSocketClient` interno

### 7.4 — Crear escena de Race multijugador

1. Duplicar la escena `Race` (de TopDownRace) como `RaceMultiplayer`
2. Eliminar de la escena: `GameControl`, `Rivals`, `AIScript`
3. Agregar: `MultiplayerGameController`, `MultiplayerSync`
4. Configurar en `MultiplayerGameController`:
   - `player Car Prefab`: el prefab del auto del jugador (el de TopDownRace)
   - `rival Car Prefab`: un prefab de auto rival (con `CarPhysics` + collider, sin IA)
   - `total Laps`: 3

### 7.5 — Crear escena de Lobby multijugador

1. Usar la escena `LobbyWaitting` existente
2. Asegurarse de que tenga un boton "Ready" que llame a:
   ```csharp
   MatchmakingManager.Instance.JoinQueue(AuthManager.Instance.PlayerId, AuthManager.Instance.Username);
   ```
3. Cuando `OnMatchFound` se dispare, hacer:
   ```csharp
   GameNetworkManager.Instance.ConnectToGameServer(MatchmakingManager.Instance.GameServerUrl);
   GameNetworkManager.Instance.JoinLobby(
       MatchmakingManager.Instance.LobbyId,
       AuthManager.Instance.PlayerId,
       AuthManager.Instance.Username
   );
   SceneManager.LoadScene("RaceMultiplayer");
   ```

### 7.6 — Build Settings

En `File > Build Settings`:
1. Agregar escenas en orden:
   - `MainMenu` (index 0)
   - `LobbyBrowser` (index 1)
   - `LobbyWaitting` (index 2)
   - `RaceMultiplayer` (index 3)
   - `RaceResults` (index 4)
2. Eliminar `SampleScene` si no se usa
3. Platform: WebGL

### 7.7 — Player Settings (WebGL)

En `Edit > Project Settings > Player > WebGL`:
- Compression: Brotli (mejor tamano)
- Color Space: Linear (ya configurado con URP)
- IL2CPP si es necesario

---

## Flujo completo del juego multijugador

```
MainMenu → Login → LobbyBrowser → Buscar partida (Matchmaking)
  → Match found → LobbyWaitting (esperar 4 jugadores)
  → Race start → RaceMultiplayer (TopDownRace + NetworkSync)
  → Race finish → RaceResults (mostrar ganador)
  → Volver a LobbyWaitting
```

---

## Estructura de MongoDB Atlas

Usar el mismo cluster, bases de datos separadas:

| Base de datos | Servicio | Colecciones |
|---------------|----------|-------------|
| `racegame_auth` | AuthService | `players`, `logs` |
| `racegame_users` | UserService | `players`, `logs` |
| `racegame_history` | HistoryService | `races` |

---

## URLs de Produccion

| Servicio | URL |
|----------|-----|
| ALB | `http://ecs-express-gateway-alb-18642d98-656595321.us-east-1.elb.amazonaws.com` |
| GameServer WS | `ws://ecs-express-gateway-alb-18642d98-656595321.us-east-1.elb.amazonaws.com` |
| Auth Register | `POST /api/auth/register` |
| Auth Login | `POST /api/auth/login` |
| Users | `GET /api/users/players` |
| History | `GET /api/history/races` |
| Matchmaking | `POST /api/matchmaking/join` |

---

## Troubleshooting

### El servicio no responde
1. Verificar que el contenedor esta corriendo en ECS Console
2. Verificar CloudWatch Logs en `/ecs/<nombre-servicio>`
3. Verificar que el health check del Target Group esta Healthy

### Terrafall falla
1. Verificar que las credenciales AWS son validas: `aws sts get-caller-identity`
2. Verificar que los ECR repos existen
3. Verificar que el listener ARN es correcto

### WebSocket no conecta
1. Verificar que el ALB soporta WebSocket (deberia, ya que esta configurado)
2. Verificar que el GameServer esta Healthy en el Target Group
3. Probar con un cliente WebSocket simple primero
