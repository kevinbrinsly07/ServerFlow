# ServeFlow

ServeFlow is a full-stack application built to demonstrate modern .NET
backend development, PostgreSQL integration, and a Next.js frontend. The
current implementation includes a product API and a frontend for viewing
and creating products.

> **Project status:** Early development. Features listed as planned are
> not represented as completed functionality.

## Tech Stack

### Backend

-   C#
-   .NET 10 / ASP.NET Core Web API
-   Entity Framework Core
-   PostgreSQL
-   Npgsql PostgreSQL provider

### Frontend

-   Next.js
-   React
-   TypeScript
-   Bun

### Development tools

-   Docker Compose
-   Git
-   `dotnet-ef` for Entity Framework migrations

## Project Structure

``` text
ServerFlow/
├── ServeFlow.sln
├── docker-compose.yml
├── backend/
│   ├── ServeFlow.Api/
│   │   ├── Controllers/
│   │   ├── Properties/
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   ├── ServeFlow.Application/
│   ├── ServeFlow.Domain/
│   │   └── Entities/
│   │       └── Product.cs
│   └── ServeFlow.Infrastructure/
│       └── Data/
│           └── ServeFlowDbContext.cs
└── frontend/
    ├── src/
    │   └── app/
    │       └── page.tsx
    └── .env.local
```

Your local folder is currently named `ServerFlow`; the application and
project names use `ServeFlow`. Adjust paths if your actual folder
structure differs.

## Current Features

-   ASP.NET Core Web API
-   Product entity with a UUID, name, description, price, availability,
    and creation timestamp
-   Product endpoints to list, retrieve, and create products
-   Entity Framework Core database context
-   PostgreSQL persistence
-   Next.js frontend that loads and creates products through the API
-   Development CORS policy for `http://localhost:3000`
-   Docker Compose configuration for PostgreSQL

## Prerequisites

Install the following before starting:

-   [.NET SDK](https://dotnet.microsoft.com/download) compatible with
    your project (targeting .NET 10)
-   [Docker Desktop](https://www.docker.com/products/docker-desktop/)
-   [Bun](https://bun.sh/)
-   Git

Check your installations:

``` bash
dotnet --version
docker --version
docker compose version
bun --version
```

## Getting Started

Run commands from the repository root (the folder containing
`docker-compose.yml` and `ServeFlow.sln`).

### 1. Start PostgreSQL

Start the database container:

``` bash
docker compose up -d
docker compose ps
```

Wait until the PostgreSQL service reports healthy.

**Port note:** The current local Docker setup maps host port `5433` to
PostgreSQL's container port `5432`. If your `docker-compose.yml` uses a
different mapping, update the connection string accordingly.

### 2. Configure the connection string

In `backend/ServeFlow.Api/appsettings.Development.json`, configure the
database connection:

``` json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=serveflow;Username=serveflow;Password=local_dev_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

These values are for local development only. Do not commit real
production credentials. If your local database uses another port,
username, password, or database name, use the values configured for your
environment.

### 3. Install or update the Entity Framework CLI

If `dotnet-ef` is not installed:

``` bash
dotnet tool install --global dotnet-ef
```

If it is already installed, check its version:

``` bash
dotnet ef --version
```

Use an EF CLI version compatible with the Entity Framework Core packages
in the project.

### 4. Apply database migrations

From the repository root:

``` bash
ASPNETCORE_ENVIRONMENT=Development \
dotnet ef database update \
  --project backend/ServeFlow.Infrastructure \
  --startup-project backend/ServeFlow.Api
```

If the project does not yet contain an initial migration, create it
first:

``` bash
ASPNETCORE_ENVIRONMENT=Development \
dotnet ef migrations add InitialCreate \
  --project backend/ServeFlow.Infrastructure \
  --startup-project backend/ServeFlow.Api \
  --output-dir Data/Migrations
```

Then run `dotnet ef database update` again.

### 5. Start the backend API

From the repository root:

``` bash
ASPNETCORE_ENVIRONMENT=Development \
dotnet run --no-launch-profile \
  --project backend/ServeFlow.Api \
  --urls http://localhost:5080
```

The API will be available at:

-   API base URL: `http://localhost:5080`
-   Products endpoint: `http://localhost:5080/api/products`
-   OpenAPI document in Development:
    `http://localhost:5080/openapi/v1.json` (if OpenAPI is enabled in
    the current API configuration)

Keep this terminal running.

### 6. Configure and start the frontend

In `frontend/.env.local`, set:

``` dotenv
NEXT_PUBLIC_API_URL=http://localhost:5080
```

Install dependencies and start the frontend:

``` bash
cd frontend
bun install
bun dev
```

Open <http://localhost:3000>.

Keep the API and frontend running in separate terminals.

## API Endpoints

The currently implemented product endpoints are:

  Method   Endpoint               Description
  -------- ---------------------- ----------------------------
  `GET`    `/api/products`        Retrieve the product list
  `GET`    `/api/products/{id}`   Retrieve a product by UUID
  `POST`   `/api/products`        Create a product

Example request to create a product:

``` bash
curl -i -X POST http://localhost:5080/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "House Blend Coffee",
    "description": "Medium-roast coffee",
    "price": 12.50,
    "isAvailable": true
  }'
```

Example list request:

``` bash
curl -i http://localhost:5080/api/products
```

The POST request body should match the fields accepted by the current
`ProductsController` implementation.

## Troubleshooting

### `role "serveflow" does not exist`

The PostgreSQL instance you connected to does not have a login role
named `serveflow`. Check which database instance and port the API is
using, then verify the role in that instance. Docker Compose may be
reusing an existing database volume, so changing `POSTGRES_USER` in the
Compose file does not automatically create a new role in an already
initialized data directory.

### `password authentication failed for user "serveflow"`

Verify the username and password against the existing database instance.
Check the host port mapping with:

``` bash
docker compose ps
```

The current local setup maps host port `5433` to container port `5432`.

### `The ConnectionString property has not been initialized`

Confirm that `DefaultConnection` exists in
`appsettings.Development.json` and start the API with:

``` bash
ASPNETCORE_ENVIRONMENT=Development \
dotnet run --no-launch-profile \
  --project backend/ServeFlow.Api \
  --urls http://localhost:5080
```

### Browser reports a CORS error

Confirm that the API is running and that its CORS policy allows
`http://localhost:3000`. A backend exception can also cause the browser
to report a CORS error because the expected response headers may be
missing. Check the API terminal for the underlying exception.

### Migration or table errors

Ensure PostgreSQL is healthy and the connection string points to the
correct database. Apply migrations using the command in the database
setup section. Do not delete the database volume as a first
troubleshooting step; it may contain data you want to keep.

## Development Notes

-   Keep local credentials in local development configuration or
    environment variables. Never commit production secrets.
-   Keep the backend's CORS allowlist limited to trusted frontend
    origins.
-   Create a migration whenever you make a database schema change,
    review it, and apply it to the intended database.
-   Use separate configuration values for local development, testing,
    and production.

## Roadmap

Potential next steps for the project:

-   Add request validation and consistent API error responses
-   Add unit and integration tests
-   Add structured logging and health checks
-   Add authentication and authorization
-   Add Docker configuration for the full application
-   Add CI checks with GitHub Actions
-   Improve API documentation and frontend loading/error states

These are proposed improvements, not claims about currently implemented
features.

## License

No license has been specified yet. Add a `LICENSE` file before
distributing the project under an open-source license.
