# ---- build stage ----
FROM node:20-alpine AS build
WORKDIR /app
ARG REACT_APP_ORDERS_URL=""
ARG REACT_APP_PAYMENTS_URL=""
ARG REACT_APP_USERS_URL=""
ENV REACT_APP_ORDERS_URL=$REACT_APP_ORDERS_URL
ENV REACT_APP_PAYMENTS_URL=$REACT_APP_PAYMENTS_URL
ENV REACT_APP_USERS_URL=$REACT_APP_USERS_URL
COPY package*.json ./
RUN npm ci
COPY . .
RUN npm run build

# ---- runtime stage ----
FROM nginx:1.27-alpine
COPY --from=build /app/build /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
