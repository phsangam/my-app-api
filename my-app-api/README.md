# my-app-api

ASP.NET Core 8 microservices, one folder per service:
- `services/orders-svc`
- `services/payments-svc`
- `services/users-svc`

Each exposes:
- `GET /health/live`  — liveness probe target
- `GET /health/ready` — readiness probe target
- `GET /api/<resource>` — sample data endpoint

## Run a service locally
```bash
cd services/orders-svc
dotnet restore
dotnet run          # http://localhost:5000
curl http://localhost:5000/health/live
```

## CI/CD note
Jenkins multibranch pipeline in this repo should path-filter on `services/<name>/**`
so a change to one microservice doesn't rebuild/redeploy the other two.
