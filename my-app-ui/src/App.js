import React, { useEffect, useState } from "react";

// Local dev: REACT_APP_*_URL points at the service's own port (see .env.development),
// so calls go to e.g. http://localhost:5001/health/live and http://localhost:5001/api/orders.
// Production build: these env vars are unset -> base is "", so calls go to relative paths
// like /api/orders, which the Ingress/ALB routes to the right microservice
// (same-origin, no CORS needed).
function buildService(name, envUrl, resource) {
  const base = envUrl || "";
  return {
    name,
    live: `${base}/health/live`,
    data: `${base}/api/${resource}`,
    init: `${base}/api/${resource}/init`,
  };
}

const SERVICES = [
  buildService("Orders Service", process.env.REACT_APP_ORDERS_URL, "orders"),
  buildService("Payments Service", process.env.REACT_APP_PAYMENTS_URL, "payments"),
  buildService("Users Service", process.env.REACT_APP_USERS_URL, "users"),
];

function ServiceCard({ svc }) {
  const [status, setStatus] = useState("checking...");
  const [rows, setRows] = useState([]);
  const [error, setError] = useState(null);

  const load = async () => {
    try {
      const health = await fetch(svc.live);
      setStatus(health.ok ? "UP" : `DOWN (${health.status})`);
      const dataRes = await fetch(svc.data);
      if (dataRes.ok) setRows(await dataRes.json());
      setError(null);
    } catch (e) {
      setStatus("UNREACHABLE");
      setError(e.message);
    }
  };

  const seed = async () => {
    await fetch(svc.init, { method: "POST" });
    load();
  };

  useEffect(() => { load(); /* eslint-disable-next-line */ }, []);

  const color = status === "UP" ? "green" : status === "checking..." ? "gray" : "red";

  return (
    <div style={{ border: "1px solid #ddd", borderRadius: 8, padding: 16, margin: 8, minWidth: 260 }}>
      <h3 style={{ margin: 0 }}>{svc.name}</h3>
      <p style={{ color, fontWeight: "bold" }}>{status}</p>
      {error && <p style={{ color: "red", fontSize: 12 }}>{error}</p>}
      <button onClick={seed} style={{ marginBottom: 8 }}>Seed sample data</button>
      <ul>{rows.map((r) => <li key={r.id}>{JSON.stringify(r)}</li>)}</ul>
    </div>
  );
}

export default function App() {
  return (
    <div style={{ fontFamily: "sans-serif", padding: 24 }}>
      <h1>My App — Microservices Dashboard</h1>
      <p>Each card checks its backend's health and Postgres-backed data endpoint.</p>
      <div style={{ display: "flex", flexWrap: "wrap" }}>
        {SERVICES.map((s) => <ServiceCard key={s.name} svc={s} />)}
      </div>
    </div>
  );
}
