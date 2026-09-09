// Capa de infraestructura (la más "interna"): su única responsabilidad es hablar
// HTTP con EmployeeAPI. No importa session.js ni view.js, no toca sessionStorage,
// no toca el DOM. Recibe el token como parámetro en vez de leerlo de algún lado
// global, para no acoplarse a cómo se guarda la sesión.

export class ApiError extends Error {
  constructor(status, message) {
    super(message);
    this.status = status;
  }
}

async function request(apiBase, path, { method = "GET", token, body } = {}) {
  const headers = { "Content-Type": "application/json" };
  if (token) headers["Authorization"] = "Bearer " + token;

  const res = await fetch(apiBase + path, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined
  });

  if (!res.ok) {
    let detail = "Error " + res.status;
    try {
      const text = await res.text();
      if (text) detail += ": " + text;
    } catch (_) { /* sin body */ }
    throw new ApiError(res.status, detail);
  }

  const contentType = res.headers.get("content-type") || "";
  return contentType.includes("application/json") ? res.json() : null;
}

export const ApiClient = {
  login(apiBase, username, password) {
    return request(apiBase, "/api/auth/login", { method: "POST", body: { username, password } });
  },
  getDepartments(apiBase, token) {
    return request(apiBase, "/api/employee/departments", { token });
  },
  getPositions(apiBase, token, departmentId) {
    return request(apiBase, "/api/employee/departments/" + departmentId + "/positions", { token });
  },
  getEmployees(apiBase, token, query) {
    const qs = query && query.toString() ? "?" + query.toString() : "";
    return request(apiBase, "/api/employee" + qs, { token });
  },
  getDevices(apiBase, token) {
    return request(apiBase, "/api/device", { token });
  },
  getPunchTypes(apiBase, token) {
    return request(apiBase, "/api/punch/types", { token });
  },
  getPunches(apiBase, token, query) {
    const qs = query && query.toString() ? "?" + query.toString() : "";
    return request(apiBase, "/api/punch" + qs, { token });
  }
};