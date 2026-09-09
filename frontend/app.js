// Capa de orquestación/aplicación (la más "externa"): es la única pieza que conoce
// a la vez a api-client.js, session.js y view.js, y decide qué pasa cuando el
// usuario hace algo. Las otras tres capas nunca se importan entre sí — solo esta
// las conecta. Esa es la regla de dependencia que estamos respetando: los detalles
// (HTTP, DOM) no imponen nada hacia adentro; la orquestación depende de ellos, no
// al revés.

import { ApiClient, ApiError } from "./api-client.js";
import { Session } from "./session.js";
import { View } from "./view.js";

const el = (id) => document.getElementById(id);

// employeesCache/devicesCache: SIEMPRE la lista completa sin filtro, usada solo para
// traducir employee_Id/device_Id a nombres y para poblar esos selects.
let employeesCache = [];
let devicesCache = [];

// currentEmployeeResults/currentPunchResults: el último resultado que devolvió el
// servidor para los filtros server-side (departamento/posición, o empleado/dispositivo/
// fechas). "Buscar" y "Tipo de marca" se aplican en el cliente, sobre estos resultados,
// sin volver a golpear la API — por eso viven separados de employeesCache/devicesCache.
let currentEmployeeResults = [];
let currentPunchResults = [];

function nameFromCache(cache, id) {
  const found = cache.find((item) => item.id === id);
  return found ? found.name : id;
}

// Envuelve cualquier llamada a la API: revisa expiración en el cliente antes de
// llamar (evita gastar una llamada de red con un token que ya sabemos vencido),
// y si el servidor de todas formas responde 401, cierra la sesión local.
async function withSession(apiBase, fn) {
  if (Session.token && Session.isExpired()) {
    Session.end();
    View.showLogin();
    View.setLoginBanner('<div class="banner banner--error">Tu sesión expiró, inicia sesión de nuevo.</div>');
    throw new Error("session-expired");
  }
  try {
    return await fn();
  } catch (err) {
    if (err instanceof ApiError && err.status === 401) {
      Session.end();
      View.showLogin();
      View.setLoginBanner('<div class="banner banner--error">Tu sesión venció o no es válida. Inicia sesión de nuevo.</div>');
    }
    throw err;
  }
}

function refreshSessionInfo() {
  if (!Session.token) { View.setSessionInfo(""); return; }
  const exp = new Date(Session.expiresAtUtc);
  View.setSessionInfo(Session.username + " · sesión activa hasta " + exp.toLocaleString());
}

async function loadDepartments(apiBase) {
  try {
    const departments = await withSession(apiBase, () => ApiClient.getDepartments(apiBase, Session.token));
    View.renderDepartments(departments);
  } catch (_) { /* el error principal ya se muestra al cargar empleados */ }
}

// ---------- Empleados ----------

function applySearchFilter(employees, search) {
  if (!search) return employees;
  return employees.filter((e) =>
    (e.name || "").toLowerCase().includes(search) ||
    (e.email || "").toLowerCase().includes(search) ||
    (e.dni || "").toLowerCase().includes(search)
  );
}

// Re-renderiza la tabla de empleados aplicando "Buscar" sobre el último resultado
// del servidor (currentEmployeeResults), sin hacer una nueva llamada a la API.
function renderEmployeeResults() {
  const f = View.getFilters();
  const filtered = applySearchFilter(currentEmployeeResults, f.search);
  View.clearTable();
  if (!filtered.length) {
    View.setResultsBanner('<div class="banner banner--empty">No hay empleados con estos filtros todavía.</div>');
    return;
  }
  View.setResultsBanner("");
  View.renderEmployeesTable(filtered);
}

async function loadEmployees(apiBase) {
  View.clearTable();
  View.setResultsBanner('<div class="banner banner--loading">Cargando empleados…</div>');
  const f = View.getFilters();
  const params = new URLSearchParams();
  if (f.department) params.set("departmentName", f.department);
  if (f.position) params.set("positionName", f.position);

  try {
    currentEmployeeResults = await withSession(apiBase, () => ApiClient.getEmployees(apiBase, Session.token, params));
    renderEmployeeResults();
  } catch (err) {
    if (err.message !== "session-expired") {
      View.setResultsBanner('<div class="banner banner--error">No se pudo cargar la lista de empleados.<br><span class="hint">' + err.message + '</span></div>');
    }
  }
}

// ---------- Marcaciones ----------

async function loadPunchFilterData(apiBase) {
  try {
    employeesCache = await withSession(apiBase, () => ApiClient.getEmployees(apiBase, Session.token));
    devicesCache = await withSession(apiBase, () => ApiClient.getDevices(apiBase, Session.token));
    const punchTypes = await withSession(apiBase, () => ApiClient.getPunchTypes(apiBase, Session.token));
    View.renderEmployeeOptions("filterEmployee", employeesCache);
    View.renderDeviceOptions(devicesCache);
    View.renderPunchTypeOptions(punchTypes);
  } catch (err) {
    if (err.message !== "session-expired") {
      View.setResultsBanner('<div class="banner banner--error">No se pudieron cargar los datos de los filtros.<br><span class="hint">' + err.message + '</span></div>');
    }
  }
}

function applyPunchTypeFilter(punches, punchType) {
  if (!punchType) return punches;
  return punches.filter((p) => p.punchType === punchType);
}

// Re-renderiza la tabla de marcaciones aplicando "Tipo de marca" sobre el último
// resultado del servidor (currentPunchResults), sin hacer una nueva llamada a la API.
function renderPunchResults() {
  const f = View.getFilters();
  const filtered = applyPunchTypeFilter(currentPunchResults, f.punchType);
  View.clearTable();
  if (!filtered.length) {
    View.setResultsBanner('<div class="banner banner--empty">No hay marcaciones con estos filtros.</div>');
    return;
  }
  View.setResultsBanner("");
  View.renderPunchesTable(
    filtered,
    (id) => nameFromCache(employeesCache, id),
    (id) => nameFromCache(devicesCache, id)
  );
}

async function loadPunches(apiBase) {
  View.clearTable();
  View.setResultsBanner('<div class="banner banner--loading">Cargando marcaciones…</div>');
  const f = View.getFilters();
  const params = new URLSearchParams();
  if (f.employee) params.set("employeeId", f.employee);
  if (f.device) params.set("deviceId", f.device);
  if (f.from) params.set("from", f.from + "T00:00:00Z");
  if (f.to) params.set("to", f.to + "T23:59:59Z");

  try {
    currentPunchResults = await withSession(apiBase, () => ApiClient.getPunches(apiBase, Session.token, params));
    renderPunchResults();
  } catch (err) {
    if (err.message !== "session-expired") {
      View.setResultsBanner('<div class="banner banner--error">No se pudieron cargar las marcaciones.<br><span class="hint">' + err.message + '</span></div>');
    }
  }
}

// ---------- navegación entre pestañas ----------

function switchTab(tab) {
  View.setActiveTab(tab);
  const apiBase = View.getFilters().apiBase;
  if (tab === "employees") loadEmployees(apiBase);
  else loadPunchFilterData(apiBase).then(() => loadPunches(apiBase));
}

function enterApp(apiBase) {
  View.showApp();
  refreshSessionInfo();
  loadDepartments(apiBase);
  loadEmployees(apiBase);
}

// ---------- eventos ----------

el("loginForm").addEventListener("submit", async (ev) => {
  ev.preventDefault();
  const f = View.getFilters();
  View.setLoginBanner('<div class="banner banner--loading">Iniciando sesión…</div>');
  View.setLoginBusy(true);
  try {
    const data = await ApiClient.login(f.apiBase, f.username, f.password);
    Session.start(data.username, data.token, data.expiresAtUtc);
    View.setLoginBanner("");
    enterApp(f.apiBase);
  } catch (err) {
    View.setLoginBanner('<div class="banner banner--error">No se pudo iniciar sesión. Revisa usuario, contraseña, y que la API en "' +
      f.apiBase + '" esté corriendo.<br><span class="hint">' + err.message + '</span></div>');
  } finally {
    View.setLoginBusy(false);
  }
});

el("logoutBtn").addEventListener("click", () => {
  Session.end();
  View.showLogin();
});

el("tabEmployeesBtn").addEventListener("click", () => switchTab("employees"));
el("tabPunchesBtn").addEventListener("click", () => switchTab("punches"));

// Empleados: departamento/posición van al servidor (cambian qué se pide);
// "Buscar" es client-side, se re-renderiza al instante sin llamar a la API.
el("filterDepartment").addEventListener("change", async () => {
  const apiBase = View.getFilters().apiBase;
  const departmentId = View.getFilters().selectedDepartmentId;
  View.renderPositions([]);
  if (departmentId) {
    try {
      const positions = await withSession(apiBase, () => ApiClient.getPositions(apiBase, Session.token, departmentId));
      View.renderPositions(positions);
    } catch (_) { /* deja "Todas" si falla */ }
  }
  loadEmployees(apiBase);
});

el("filterPosition").addEventListener("change", () => loadEmployees(View.getFilters().apiBase));
el("searchEmployee").addEventListener("input", renderEmployeeResults);

// Marcaciones: empleado/dispositivo/fechas van al servidor; "Tipo de marca" es
// client-side, igual que "Buscar" en Empleados.
["filterEmployee", "filterDevice", "filterFrom", "filterTo"].forEach((id) => {
  el(id).addEventListener("change", () => loadPunches(View.getFilters().apiBase));
});
el("filterPunchType").addEventListener("change", renderPunchResults);

// ---------- init ----------

if (Session.isActive()) {
  enterApp(el("apiBase").value.trim().replace(/\/+$/, ""));
} else {
  if (Session.token) Session.end();
  View.showLogin();
}