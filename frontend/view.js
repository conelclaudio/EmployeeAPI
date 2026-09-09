// Capa de presentación: solo lee/escribe el DOM a partir de datos que le pasan desde
// afuera. No hace fetch (no importa api-client.js), no conoce el token ni la sesión
// (no importa session.js). Si mañana cambia el framework de UI, solo este archivo
// se toca.

const el = (id) => document.getElementById(id);

function esc(str) {
  if (str === null || str === undefined) return "";
  return String(str).replace(/[&<>"']/g, (c) =>
    ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c])
  );
}

export const View = {
  showLogin() {
    el("loginView").style.display = "block";
    el("appView").classList.remove("is-active");
  },

  showApp() {
    el("loginView").style.display = "none";
    el("appView").classList.add("is-active");
  },

  setSessionInfo(text) {
    el("sessionInfo").textContent = text;
  },

  setLoginBanner(html) {
    el("loginBanner").innerHTML = html;
  },

  setLoginBusy(isBusy) {
    el("loginBtn").disabled = isBusy;
  },

  setActiveTab(tab) {
    el("tabEmployeesBtn").classList.toggle("is-active", tab === "employees");
    el("tabPunchesBtn").classList.toggle("is-active", tab === "punches");
    el("employeeFilters").style.display = tab === "employees" ? "block" : "none";
    el("punchFilters").style.display = tab === "punches" ? "block" : "none";
    el("resultsTitle").textContent = tab === "employees" ? "Empleados" : "Marcaciones";
  },

  setResultsBanner(html) {
    el("resultsBanner").innerHTML = html;
  },

  clearTable() {
    el("resultsHead").innerHTML = "";
    el("resultsBody").innerHTML = "";
  },

  renderDepartments(departments) {
    const select = el("filterDepartment");
    select.innerHTML = '<option value="">Todos</option>';
    departments.forEach((d) => {
      const opt = document.createElement("option");
      opt.value = d.name; opt.dataset.id = d.id; opt.textContent = d.name;
      select.appendChild(opt);
    });
  },

  renderPositions(positions) {
    const select = el("filterPosition");
    select.innerHTML = '<option value="">Todas</option>';
    positions.forEach((p) => {
      const opt = document.createElement("option");
      opt.value = p.name; opt.textContent = p.name;
      select.appendChild(opt);
    });
  },

  renderEmployeeOptions(selectId, employees) {
    const select = el(selectId);
    select.innerHTML = '<option value="">Todos</option>';
    employees.forEach((e) => {
      const opt = document.createElement("option");
      opt.value = e.id; opt.textContent = e.name;
      select.appendChild(opt);
    });
  },

  renderDeviceOptions(devices) {
    const select = el("filterDevice");
    select.innerHTML = '<option value="">Todos</option>';
    devices.forEach((d) => {
      const opt = document.createElement("option");
      opt.value = d.id; opt.textContent = d.name;
      select.appendChild(opt);
    });
  },

  renderPunchTypeOptions(punchTypes) {
    const select = el("filterPunchType");
    select.innerHTML = '<option value="">Todos</option>';
    punchTypes.forEach((pt) => {
      const opt = document.createElement("option");
      opt.value = pt.code; opt.textContent = pt.name + " (" + pt.code + ")";
      select.appendChild(opt);
    });
  },

  renderEmployeesTable(employees) {
    el("resultsHead").innerHTML =
      '<tr><th>Nombre</th><th>Email</th><th class="mono">DNI</th><th>Departamento</th><th>Posición</th></tr>';
    el("resultsBody").innerHTML = employees.map((e) =>
      "<tr><td>" + esc(e.name) + "</td><td>" + esc(e.email) + "</td>" +
      '<td class="mono">' + esc(e.dni || "—") + "</td><td>" + esc(e.department) + "</td><td>" + esc(e.position) + "</td></tr>"
    ).join("");
  },

  renderPunchesTable(punches, employeeNameOf, deviceNameOf) {
    el("resultsHead").innerHTML =
      '<tr><th class="mono">Fecha/hora</th><th>Empleado</th><th>Dispositivo</th><th>Tipo</th><th>Estado</th></tr>';
    el("resultsBody").innerHTML = punches.map((p) =>
      '<tr><td class="mono">' + esc(new Date(p.punch_Dtm).toLocaleString()) + "</td>" +
      "<td>" + esc(employeeNameOf(p.employee_Id)) + "</td>" +
      "<td>" + esc(deviceNameOf(p.device_Id)) + "</td>" +
      "<td>" + esc(p.punchType) + "</td><td>" + esc(p.status) + "</td></tr>"
    ).join("");
  },

  getFilters() {
    const departmentSelect = el("filterDepartment");
    const selectedOption = departmentSelect.options[departmentSelect.selectedIndex];
    return {
      apiBase: el("apiBase").value.trim().replace(/\/+$/, ""),
      username: el("username").value.trim(),
      password: el("password").value,
      department: departmentSelect.value,
      selectedDepartmentId: selectedOption ? selectedOption.dataset.id : null,
      position: el("filterPosition").value,
      search: el("searchEmployee").value.trim().toLowerCase(),
      employee: el("filterEmployee").value,
      device: el("filterDevice").value,
      punchType: el("filterPunchType").value,
      from: el("filterFrom").value,
      to: el("filterTo").value
    };
  }
};