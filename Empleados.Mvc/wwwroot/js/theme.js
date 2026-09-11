(function () {
  const STORAGE_KEY = 'theme';
  const html = document.documentElement;

  function getPreferredTheme() {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'light' || stored === 'dark') return stored;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  function applyTheme(theme) {
    html.setAttribute('data-theme', theme);
    // Actualizar iconos del toggle
    const sunIcon = document.querySelector('[data-theme-icon="sun"]');
    const moonIcon = document.querySelector('[data-theme-icon="moon"]');
    if (sunIcon && moonIcon) {
      if (theme === 'dark') {
        sunIcon.classList.add('d-none');
        moonIcon.classList.remove('d-none');
      } else {
        moonIcon.classList.add('d-none');
        sunIcon.classList.remove('d-none');
      }
    }
    const label = document.getElementById('theme-toggle-label');
    if (label) label.textContent = theme === 'dark' ? 'Modo oscuro' : 'Modo claro';
  }

  // Aplicar al cargar, antes de pintar
  applyTheme(getPreferredTheme());

  // Escuchar cambios del sistema si no hay preferencia guardada
  window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
    if (!localStorage.getItem(STORAGE_KEY)) {
      applyTheme(e.matches ? 'dark' : 'light');
    }
  });

  // Exponer toggle global
  window.toggleTheme = function () {
    const current = html.getAttribute('data-theme') || 'light';
    const next = current === 'dark' ? 'light' : 'dark';
    localStorage.setItem(STORAGE_KEY, next);
    applyTheme(next);
  };

  // Bind cuando DOM listo
  document.addEventListener('DOMContentLoaded', () => {
    applyTheme(getPreferredTheme());
    const btn = document.getElementById('theme-toggle');
    if (btn) {
      btn.addEventListener('click', window.toggleTheme);
    }
  });
})();
