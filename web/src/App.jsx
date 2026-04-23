import { useMemo, useState } from "react";

const API_BASE = import.meta.env.VITE_API_BASE ?? "http://localhost:5000";
const credentials = { username: "test", password: "test123" };

function App() {
  const [jwt, setJwt] = useState(localStorage.getItem("demo_jwt") ?? "");
  const [result, setResult] = useState("Sonuc burada gorunecek.");
  const replayId = useMemo(() => "same-request-id-for-demo", []);

  async function callApi(path, options = {}) {
    const response = await fetch(`${API_BASE}${path}`, options);
    const text = await response.text();

    try {
      return JSON.stringify(JSON.parse(text), null, 2);
    } catch {
      return text || `${response.status} ${response.statusText}`;
    }
  }

  async function loginSession() {
    const body = JSON.stringify(credentials);
    const output = await callApi("/login-session", {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
      body,
    });
    setResult(output);
  }

  async function loginJwt() {
    const output = await callApi("/login-jwt", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(credentials),
    });

    const parsed = JSON.parse(output);
    localStorage.setItem("demo_jwt", parsed.token);
    setJwt(parsed.token);
    setResult(output);
  }

  async function callSessionProtected() {
    const output = await callApi("/protected-session", {
      credentials: "include",
    });
    setResult(output);
  }

  async function callJwtProtected() {
    const output = await callApi("/protected-jwt", {
      headers: { Authorization: `Bearer ${jwt}` },
    });
    setResult(output);
  }

  async function simulateCsrf() {
    const output = await callApi("/simulate-csrf-state-change", {
      credentials: "include",
    });
    setResult(output);
  }

  async function simulateTokenTheft() {
    const output = await callApi("/simulate-token-theft", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ token: localStorage.getItem("demo_jwt") ?? "" }),
    });
    setResult(output);
  }

  async function simulateReplay() {
    const output = await callApi("/simulate-replay", {
      method: "POST",
      headers: {
        Authorization: `Bearer ${jwt}`,
        "x-demo-request-id": replayId,
      },
    });
    setResult(output);
  }

  function clearJwt() {
    localStorage.removeItem("demo_jwt");
    setJwt("");
    setResult("JWT localStorage'dan silindi.");
  }

  return (
    <main className="page">
      <section className="hero">
        <h1>Authentication Demo</h1>
        <p>Session, JWT, API key, Basic Auth ve basit zafiyet davranislari.</p>
      </section>

      <section className="panel">
        <h2>Login</h2>
        <div className="actions">
          <button onClick={loginSession}>Session login</button>
          <button onClick={loginJwt}>JWT login</button>
          <button onClick={clearJwt}>JWT temizle</button>
        </div>
      </section>

      <section className="panel">
        <h2>Protected Endpointler</h2>
        <div className="actions">
          <button onClick={callSessionProtected}>Session protected cagir</button>
          <button onClick={callJwtProtected} disabled={!jwt}>
            JWT protected cagir
          </button>
        </div>
      </section>

      <section className="panel">
        <h2>Zafiyet Simulasyonlari</h2>
        <div className="actions">
          <button onClick={simulateCsrf}>CSRF demo</button>
          <button onClick={simulateTokenTheft} disabled={!jwt}>
            Token theft demo
          </button>
          <button onClick={simulateReplay} disabled={!jwt}>
            Replay demo
          </button>
        </div>
      </section>

      <section className="panel">
        <h2>XSS Etkisi</h2>
        <p>
          JWT localStorage icinde tutuldugu icin sayfadaki JavaScript tarafindan
          okunabilir.
        </p>
        <pre>{localStorage.getItem("demo_jwt") || "localStorage demo_jwt bos"}</pre>
      </section>

      <section className="panel">
        <h2>Sonuc</h2>
        <pre>{result}</pre>
      </section>
    </main>
  );
}

export default App;
