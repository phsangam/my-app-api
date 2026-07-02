# my-app-ui

ReactJS frontend. Calls backend microservices via `/api/orders`, `/api/payments`, `/api/users`
(paths routed by the Ingress/ALB to the `my-app-api` services in EKS).

## Run locally
```bash
npm install
npm start        # http://localhost:3000
```

## Build
```bash
npm run build     # outputs to ./build, served via nginx in Docker
```
