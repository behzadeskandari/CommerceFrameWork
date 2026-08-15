const isLocalhost = typeof window !== 'undefined' && (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1');

export const environment = {
  production: false,
  // During local dev we proxy `/api` to the .NET backend via Vite server.
  // Use a relative base when running the dev server so the proxy applies.
  // Use empty string for localhost so `${apiBaseUrl}/api` becomes `/api` (no double-slash).
  apiBaseUrl: isLocalhost ? '' : 'https://localhost:5100',
  appName: 'Commerce'
};
