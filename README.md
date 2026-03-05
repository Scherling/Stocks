# Stocks

A monorepo containing three services:

| Service  | Technology              | Dev URL                   |
|----------|-------------------------|---------------------------|
| Backend  | .NET 10 Minimal API     | http://localhost:5000     |
| Frontend | React + Vite + TypeScript | http://localhost:5173   |
| Wiki     | Docusaurus v3           | http://localhost:3000     |

---

## Getting Started

### Backend

```bash
cd backend/Stocks.Api
dotnet run
```

Or open `backend/Stocks.sln` in Visual Studio and press F5.

Verify: `curl http://localhost:5000/api/hello`

### Frontend

```bash
cd frontend
npm install
npm run dev
```

Open http://localhost:5173 — the page fetches `/api/hello` from the backend and displays the response.

> The backend must be running for the frontend to show data.

### Wiki

```bash
cd wiki
npm install
npm run start
```

Open http://localhost:3000.

---

## Project Structure

```
Stocks/
├── backend/          .NET 10 Minimal API
├── frontend/         React + Vite + TypeScript
└── wiki/             Docusaurus v3 documentation site
```
