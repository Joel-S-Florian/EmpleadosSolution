// Landing: aparición al hacer scroll, sombra del encabezado, sección activa, recibo que se inclina y neto que cuenta.
// Los estilos están en css/publico.css; sin este script la página se ve completa, solo sin movimiento.
(function () {
    const quieto = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    const encabezado = document.querySelector('.pub-header');
    const sombra = () => encabezado && encabezado.classList.toggle('con-sombra', window.scrollY > 8);
    sombra();
    window.addEventListener('scroll', sombra, { passive: true });

    if (!('IntersectionObserver' in window)) return;

    // Sección activa en la navegación
    const enlaces = Array.from(document.querySelectorAll('.pub-nav a[href^="/#"]'));
    const espia = new IntersectionObserver((entradas) => entradas.forEach((e) => {
        if (e.isIntersecting) enlaces.forEach((a) => a.classList.toggle('activo', a.hash === '#' + e.target.id));
    }), { rootMargin: '-45% 0px -50% 0px' });
    enlaces.forEach((a) => { const seccion = document.getElementById(a.hash.slice(1)); if (seccion) espia.observe(seccion); });

    if (quieto) return;

    // Aparición escalonada: cada hijo de [data-escalonar] espera un poco más que el anterior
    document.querySelectorAll('[data-escalonar]').forEach((grupo) =>
        Array.from(grupo.children).forEach((hijo, i) => hijo.style.setProperty('--i', i)));
    document.documentElement.classList.add('lp-anima');
    const revelar = new IntersectionObserver((entradas) => entradas.forEach((e) => {
        if (!e.isIntersecting) return;
        const el = e.target;
        el.classList.add('visible');
        revelar.unobserve(el);
        // Después de aparecer, el hover responde sin el retraso del escalonado
        setTimeout(() => el.classList.add('revelado'), 700 + (Number(el.style.getPropertyValue('--i')) || 0) * 150);
    }), { rootMargin: '0px 0px -8% 0px', threshold: 0.1 });
    document.querySelectorAll('.lp-revelar').forEach((el) => revelar.observe(el));

    // Neto del recibo: cuenta desde cero cuando aparece la línea del total
    const neto = document.querySelector('[data-contar]');
    if (neto) {
        const final = Number(neto.dataset.contar);
        const formato = new Intl.NumberFormat('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        neto.textContent = formato.format(0);
        setTimeout(() => {
            const inicio = performance.now();
            const paso = (ahora) => {
                const t = Math.min((ahora - inicio) / 1100, 1);
                neto.textContent = formato.format(final * (1 - Math.pow(1 - t, 3)));
                if (t < 1) requestAnimationFrame(paso);
            };
            requestAnimationFrame(paso);
        }, 1200);
    }

    // Solo con mouse: el recibo se inclina hacia el puntero y las tarjetas siguen su brillo
    if (!window.matchMedia('(hover: hover) and (pointer: fine)').matches) return;
    const marco = document.querySelector('.lp-recibo-marco');
    const recibo = marco && marco.querySelector('.lp-recibo');
    if (recibo) {
        marco.addEventListener('pointermove', (e) => {
            const r = marco.getBoundingClientRect();
            const x = (e.clientX - r.left) / r.width - 0.5;
            const y = (e.clientY - r.top) / r.height - 0.5;
            recibo.classList.add('inclinado');
            recibo.style.setProperty('--ry', (x * 10).toFixed(2) + 'deg');
            recibo.style.setProperty('--rx', (-y * 8).toFixed(2) + 'deg');
        });
        marco.addEventListener('pointerleave', () => {
            recibo.classList.remove('inclinado');
            recibo.style.setProperty('--rx', '0deg');
            recibo.style.setProperty('--ry', '0deg');
        });
    }
    document.querySelectorAll('.lp-tarjeta').forEach((tarjeta) => tarjeta.addEventListener('pointermove', (e) => {
        const r = tarjeta.getBoundingClientRect();
        tarjeta.style.setProperty('--mx', (e.clientX - r.left) + 'px');
        tarjeta.style.setProperty('--my', (e.clientY - r.top) + 'px');
    }));
})();
