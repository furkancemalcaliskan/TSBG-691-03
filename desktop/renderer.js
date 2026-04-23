const API_BASE = "http://localhost:5000";
const credentials = { username: "test", password: "test123" };

let token = "";

const loginButton = document.getElementById("login");
const protectedButton = document.getElementById("protected");
const tokenOutput = document.getElementById("token");
const resultOutput = document.getElementById("result");

async function showResponse(response) {
  const text = await response.text();

  try {
    return JSON.stringify(JSON.parse(text), null, 2);
  } catch {
    return text || `${response.status} ${response.statusText}`;
  }
}

loginButton.addEventListener("click", async () => {
  const response = await fetch(`${API_BASE}/login-jwt`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(credentials),
  });

  const text = await showResponse(response);
  const data = JSON.parse(text);
  token = data.token ?? "";
  tokenOutput.textContent = token || "Token yok";
  resultOutput.textContent = text;
  protectedButton.disabled = !token;
});

protectedButton.addEventListener("click", async () => {
  const response = await fetch(`${API_BASE}/protected-jwt`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  resultOutput.textContent = await showResponse(response);
});
