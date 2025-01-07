FROM node:20.18.1-alpine

RUN npm install -g http-server

WORKDIR /app

COPY package*.json ./

RUN npm install

COPY . .

ARG VITE_BACKEND

RUN echo $VITE_BACKEND

RUN npm run build

EXPOSE 8080
CMD [ "http-server", "dist", "-p", "8080", "--proxy", "http://localhost:8080?" ]
