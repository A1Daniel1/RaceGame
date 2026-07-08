# RaceGame — Conexión con AWS

## Arquitectura

```
Unity (AuthManager)  ──HTTP POST──>  ALB ──> Auth Service  (puerto 80)
Unity (NetworkManager) ──WebSocket──> ALB ──> Game Server  (puerto 3000)
```

## URLs del ALB

| Servicio | URL |
|---|---|
| Auth Service | `http://ecs-express-gateway-alb-18642d98-656595321.us-east-1.elb.amazonaws.com/api/auth/login` |
| Game Server | `ws://ecs-express-gateway-alb-18642d98-656595321.us-east-1.elb.amazonaws.com:3000` |

## Estructura de archivos

```
Assets/Scripts/
├── Core/
│   └── NetworkConfig.cs        — URLs de AWS
├── Network/
│   ├── AuthManager.cs           — Login UI + HTTP POST
│   └── NetworkManager.cs        — Menú UI + WebSocket (NativeWebSocket)
└── UI/
    └── (vacío — la UI vive dentro de AuthManager y NetworkManager)
```

## Dependencias

- **NativeWebSocket** — instalarlo vía Package Manager:
  ```
  https://github.com/endel/NativeWebSocket.git#upm
  ```
- **TextMeshPro** — viene incluido en Unity (si no, usar Window → TextMeshPro → Import TMP Essentials)

## Jerarquía del Canvas

```
Canvas (Render Mode: Screen Space – Overlay)
├── LoginPanel (Panel) — activo al inicio
│   ├── TitleText        (TextMeshPro - Text)   "Iniciar Sesión"
│   ├── UsernameInput    (TMP_InputField)       placeholder: "Usuario"
│   ├── PasswordInput    (TMP_InputField)       placeholder: "Contraseña", Content Type: Password
│   ├── LoginButton      (Button)               texto: "Iniciar Sesión"
│   └── ErrorText        (TextMeshPro - Text)   vacío (mostrará errores)
│
└── MenuPanel (Panel) — inicialmente desactivado
    ├── StatusText       (TextMeshPro - Text)   "Desconectado"
    └── FindMatchButton  (Button)               texto: "Buscar Partida"
```

## GameObject raíz

```
[GameManager] (GameObject vacío, DontDestroyOnLoad)
 ├─ GameManager.cs       (singleton: JWT, Username, PlayerId)
 ├─ AuthManager.cs       (singleton: login panel + HTTP)
 └─ NetworkManager.cs    (singleton: menú + WebSocket)
```

### Asignación en el Inspector (AuthManager)

| Campo | Arrastrar |
|---|---|
| Login Panel | `LoginPanel` |
| Username Input | `UsernameInput` |
| Password Input | `PasswordInput` |
| Login Button | `LoginButton` |
| Error Text | `ErrorText` |
| Menu Panel | `MenuPanel` |

### Asignación en el Inspector (NetworkManager)

| Campo | Arrastrar |
|---|---|
| Status Text | `StatusText` |
| Find Match Button | `FindMatchButton` |

## Flujo de ejecución

1. **LoginPanel** visible. Usuario ingresa usuario/contraseña y presiona "Iniciar Sesión".
2. `AuthManager.Login()` envía **POST** con JSON `{"username":"...","password":"..."}` a `/api/auth/login`.
3. Si la respuesta es **200**:
   - Extrae `token` del JSON.
   - Almacena token en `PlayerPrefs` y en `GameManager.Instance.JWT`.
   - Guarda `GameManager.Instance.Username`.
   - Oculta `LoginPanel`, muestra `MenuPanel`.
   - Llama a `NetworkManager.Instance.Connect()`.
4. `NetworkManager.Connect()` abre **WebSocket** hacia el Game Server en puerto 3000.
   - **OnOpen**: estado → "Conectado", botón "Buscar Partida" habilitado.
   - **OnError**: estado → "Error de conexión".
   - **OnClose**: estado → "Desconectado", botón deshabilitado.
5. Usuario presiona "Buscar Partida".
   - Estado → "Buscando rivales...".
   - Envía `{"eventType":"join_match"}` por WebSocket.
6. Al recibir mensaje del servidor:
   - `match_found` / `opponent_found` → "¡Partida encontrada!"
   - `joined` / `waiting` → "Esperando rival..."
   - `error` → "Error del servidor", botón rehabilitado.
