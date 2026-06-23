# Finance Tracker

Aplicación web para gestión de finanzas personales. Permite registrar ingresos y gastos manualmente o importando el estado de cuenta PDF de BBVA México, con categorización automática, presupuestos por categoría y alertas de gasto.

## Tech Stack

**Backend:** .NET 10 · ASP.NET Core Minimal API · Entity Framework Core · SQL Server  
**Frontend:** React · TypeScript · Vite  
**Librerías:** MediatR · FluentValidation · Hangfire · PdfPig · BCrypt

### ¿Por qué Vite y no Next.js?

Next.js es un framework encima de React que agrega SSR (Server Side Rendering) y SSG (Static Site Generation). Su ventaja principal es el SEO y el tiempo de carga inicial para páginas públicas. Para este proyecto no aplica por dos razones:

- **La app vive detrás de login.** No hay páginas públicas que Google necesite indexar, así que el SSR no aporta nada.
- **Vite + React puro enseña React.** Next.js abstrae el modelo mental con Server Components, App Router y la distinción server/client. Aprender eso antes de dominar React genera confusión. Primero React, después Next.

Si este proyecto fuera una app pública (landing, blog, e-commerce), Next.js sería la elección correcta.

### ¿Por qué Vite y no Create React App?

Create React App está abandonado desde 2023 y ya no recibe mantenimiento. Vite es el estándar actual para proyectos React por tres razones concretas:

- **Arranque instantáneo:** Vite no empaqueta el código al iniciar el servidor de desarrollo. Sirve los módulos directamente al navegador usando ES Modules nativos, así el servidor arranca en menos de 300ms sin importar el tamaño del proyecto. CRA compilaba todo antes de mostrar algo.
- **Hot reload real:** Cuando modificas un archivo, Vite solo recarga ese módulo específico. CRA reconstruía todo el bundle y tardaba varios segundos.
- **Build de producción rápido:** Usa Rollup internamente, que genera bundles más pequeños y optimizados que el Webpack de CRA.

## Arquitectura

El backend usa **Vertical Slices** — cada feature vive en su propio folder con todo lo que necesita (endpoint, handler, validación). No hay capas horizontales compartidas. Esto hace que agregar o modificar un feature no toque código de otros.

```
Features/
├── Auth/
│   ├── Register/   ← RegisterCommand · RegisterHandler · RegisterValidator · RegisterEndpoint
│   └── Login/      ← LoginCommand · LoginHandler · LoginValidator · LoginEndpoint
├── Transactions/
├── Categories/
├── Budgets/
├── Dashboard/
└── Import/         ← Parser de PDF de BBVA con PdfPig
```

Cada slice sigue el mismo patrón:

```
POST /api/auth/register
  → Endpoint valida con FluentValidation
  → Manda Command a MediatR
  → Handler ejecuta lógica y persiste
  → Devuelve Response
```

### ¿Por qué Vertical Slices y no Clean Architecture?

Clean Architecture agrega valor cuando el dominio es complejo y hay múltiples puntos de entrada (API, workers, CLI). Para este proyecto, el overhead de 4 capas con interfaces por todos lados no aporta — solo agrega burocracia. Vertical Slices da la misma separación de responsabilidades con menos fricción.

## Funcionalidades

- [x] Autenticación con JWT (Register / Login)
- [x] Categorías (CRUD)
- [x] Transacciones manuales (CRUD)
- [x] Importar estado de cuenta PDF de BBVA
- [x] Auto-categorización por descripción del movimiento
- [x] Presupuestos por categoría y mes
- [x] Dashboard con balance, gastos por categoría y % de presupuesto
- [x] Alertas automáticas al superar el 80% del presupuesto (Hangfire)

## Cómo correr el proyecto

### Requisitos

- .NET 10 SDK
- SQL Server o LocalDB (viene incluido con Visual Studio)
- Node 18+

### Backend

```bash
cd backend/FinanceTracker.API
```

Crea un archivo `appsettings.Development.json` con tu connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=FinanceTracker;Trusted_Connection=True;"
  },
  "Jwt": {
    "Key": "tu_clave_secreta_minimo_32_caracteres",
    "Issuer": "FinanceTracker",
    "Audience": "FinanceTrackerUsers",
    "ExpiresInHours": 24
  }
}
```

Corre las migraciones y levanta el servidor:

```bash
dotnet ef database update
dotnet run
```

API disponible en `https://localhost:7xxx` · Swagger en `/openapi`

### Frontend

```bash
cd frontend
npm install
npm run dev
```

App disponible en `http://localhost:5173`

## Endpoints

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/auth/register` | No | Crear cuenta |
| POST | `/api/auth/login` | No | Iniciar sesión |
| GET | `/api/categories` | Sí | Listar categorías |
| POST | `/api/categories` | Sí | Crear categoría |
| GET | `/api/transactions` | Sí | Listar transacciones |
| POST | `/api/transactions` | Sí | Crear transacción |
| POST | `/api/import/bbva` | Sí | Importar PDF de BBVA |
| GET | `/api/budgets` | Sí | Listar presupuestos |
| POST | `/api/budgets` | Sí | Crear presupuesto |
| GET | `/api/dashboard` | Sí | Resumen del mes actual |
