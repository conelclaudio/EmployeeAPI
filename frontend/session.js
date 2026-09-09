// session.js
// Capa de dominio: encapsula qué es una sesión válida y dónde vive (sessionStorage,
// no localStorage — ver nota de manejo de token más abajo). No importa api-client.js
// ni view.js: no sabe que existen, ni le importa cómo se obtuvo el token ni cómo se
// muestra en pantalla.
//
// Manejo del token:
// - sessionStorage en vez de localStorage: se pierde al cerrar la pestaña/navegador,
//   reduce la ventana de exposición si el equipo es compartido.
// - isExpired() se puede consultar ANTES de llamar a la API, para no gastar una
//   llamada de red con un token que el cliente ya sabe que está vencido.

const STORAGE_KEYS = { token: "er_token", username: "er_username", expires: "er_expires" };

export const Session = {
  get token() {
    return sessionStorage.getItem(STORAGE_KEYS.token) || "";
  },
  get username() {
    return sessionStorage.getItem(STORAGE_KEYS.username) || "";
  },
  get expiresAtUtc() {
    return sessionStorage.getItem(STORAGE_KEYS.expires) || "";
  },

  isExpired() {
    const exp = this.expiresAtUtc;
    if (!exp) return true;
    return new Date(exp).getTime() <= Date.now();
  },

  isActive() {
    return Boolean(this.token) && !this.isExpired();
  },

  start(username, token, expiresAtUtc) {
    sessionStorage.setItem(STORAGE_KEYS.username, username);
    sessionStorage.setItem(STORAGE_KEYS.token, token);
    sessionStorage.setItem(STORAGE_KEYS.expires, expiresAtUtc);
  },

  end() {
    sessionStorage.removeItem(STORAGE_KEYS.username);
    sessionStorage.removeItem(STORAGE_KEYS.token);
    sessionStorage.removeItem(STORAGE_KEYS.expires);
  }
};