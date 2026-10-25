// Smart Select: reemplaza visualmente un <select data-smart-select> por un menú con buscador.
// El <select> nativo sigue en el formulario (envío, validación y eventos "change" funcionan igual).
// Sin búsqueda muestra las primeras opciones (el servidor las ordena de la más reciente a la más antigua)
// y carga más al llegar al final de la lista.
(function () {
    const POR_PAGINA = 8;
    const normalizar = (texto) => texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
    let contador = 0;

    function crear(select) {
        if (select.dataset.ssListo) return;
        select.dataset.ssListo = '1';
        const id = select.id || ('ss-' + (++contador));
        const contenedor = document.createElement('div');
        contenedor.className = 'ss';
        select.parentNode.insertBefore(contenedor, select);
        contenedor.appendChild(select);
        select.classList.add('ss-nativo');
        select.tabIndex = -1;

        const boton = document.createElement('button');
        boton.type = 'button';
        boton.className = 'form-select ss-boton';
        boton.setAttribute('role', 'combobox');
        boton.setAttribute('aria-haspopup', 'listbox');
        boton.setAttribute('aria-expanded', 'false');
        boton.setAttribute('aria-controls', id + '-lista');
        const etiqueta = document.querySelector('label[for="' + select.id + '"]');
        if (etiqueta) {
            etiqueta.htmlFor = id + '-boton';
            boton.id = id + '-boton';
        }
        contenedor.appendChild(boton);

        const panel = document.createElement('div');
        panel.className = 'ss-panel';
        panel.hidden = true;
        panel.innerHTML =
            '<div class="ss-buscar"><i class="bi bi-search" aria-hidden="true"></i><input type="search" autocomplete="off" placeholder="Buscar…" aria-label="Buscar"></div>' +
            '<ul class="ss-lista" role="listbox" id="' + id + '-lista"></ul>' +
            '<div class="ss-pie"></div>';
        contenedor.appendChild(panel);
        const buscador = panel.querySelector('input');
        const lista = panel.querySelector('ul');
        const pie = panel.querySelector('.ss-pie');

        let filtradas = [];
        let mostradas = 0;
        let activa = -1;

        const opciones = () => Array.from(select.options).filter(o => !o.disabled || o.value === '');
        const vacia = () => opciones().find(o => o.value === '');

        function pintarBoton() {
            const elegida = select.options[select.selectedIndex];
            boton.textContent = elegida ? elegida.text : '';
            boton.classList.toggle('ss-sin-valor', !elegida || elegida.value === '');
            boton.disabled = select.disabled;
        }

        function filtrar() {
            const consulta = normalizar(buscador.value);
            const todas = opciones().filter(o => o.value !== '');
            filtradas = consulta ? todas.filter(o => normalizar(o.text).includes(consulta)) : todas;
            // La opción vacía ("Todos", "Seleccione…") siempre arriba cuando no se busca
            if (!consulta && vacia()) filtradas.unshift(vacia());
            mostradas = 0;
            activa = -1;
            lista.innerHTML = '';
            agregarPagina();
        }

        function agregarPagina() {
            const hasta = Math.min(filtradas.length, mostradas + POR_PAGINA);
            for (let i = mostradas; i < hasta; i++) {
                const opcion = filtradas[i];
                const item = document.createElement('li');
                item.className = 'ss-opcion' + (opcion.value === '' ? ' ss-opcion-vacia' : '');
                item.setAttribute('role', 'option');
                item.id = id + '-op-' + i;
                item.dataset.indice = i;
                item.textContent = opcion.text;
                item.setAttribute('aria-selected', opcion.selected ? 'true' : 'false');
                lista.appendChild(item);
            }
            mostradas = hasta;
            const reales = filtradas.filter(o => o.value !== '').length;
            const vistas = Math.min(mostradas, filtradas.length) - (filtradas[0] && filtradas[0].value === '' ? 1 : 0);
            pie.textContent = filtradas.length === 0
                ? 'Sin resultados'
                : vistas < reales ? `Mostrando ${vistas} de ${reales} · escriba o baje para ver más` : `${reales} opción(es)`;
        }

        function abrir() {
            if (select.disabled) return;
            panel.hidden = false;
            boton.setAttribute('aria-expanded', 'true');
            buscador.value = '';
            filtrar();
            buscador.focus();
        }

        function cerrar(devolverFoco) {
            if (panel.hidden) return;
            panel.hidden = true;
            boton.setAttribute('aria-expanded', 'false');
            if (devolverFoco) boton.focus();
        }

        function elegir(indice) {
            const opcion = filtradas[indice];
            if (!opcion) return;
            select.value = opcion.value;
            select.dispatchEvent(new Event('change', { bubbles: true }));
            pintarBoton();
            cerrar(true);
        }

        function marcar(indice) {
            const items = lista.querySelectorAll('.ss-opcion');
            if (!items.length) return;
            activa = Math.max(0, Math.min(indice, items.length - 1));
            items.forEach((el, i) => el.classList.toggle('ss-activa', i === activa));
            items[activa].scrollIntoView({ block: 'nearest' });
            buscador.setAttribute('aria-activedescendant', items[activa].id);
        }

        boton.addEventListener('click', () => (panel.hidden ? abrir() : cerrar(true)));
        boton.addEventListener('keydown', (e) => {
            if (['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(e.key)) {
                e.preventDefault();
                abrir();
            }
        });
        buscador.addEventListener('input', filtrar);
        buscador.addEventListener('keydown', (e) => {
            if (e.key === 'ArrowDown') {
                e.preventDefault();
                if (activa >= mostradas - 1 && mostradas < filtradas.length) agregarPagina();
                marcar(activa + 1);
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                marcar(activa - 1);
            } else if (e.key === 'Enter') {
                e.preventDefault();
                elegir(activa >= 0 ? activa : (filtradas.length === 1 ? 0 : -1));
            } else if (e.key === 'Escape') {
                e.preventDefault();
                cerrar(true);
            } else if (e.key === 'Tab') {
                cerrar(false);
            }
        });
        lista.addEventListener('mousedown', (e) => e.preventDefault()); // no quitar el foco del buscador
        lista.addEventListener('click', (e) => {
            const item = e.target.closest('.ss-opcion');
            if (item) elegir(Number(item.dataset.indice));
        });
        lista.addEventListener('scroll', () => {
            if (lista.scrollTop + lista.clientHeight >= lista.scrollHeight - 8 && mostradas < filtradas.length) agregarPagina();
        });
        document.addEventListener('mousedown', (e) => {
            if (!contenedor.contains(e.target)) cerrar(false);
        });
        select.addEventListener('change', pintarBoton);
        select.addEventListener('invalid', () => boton.classList.add('is-invalid'));
        // Opciones que cambian por JavaScript (por ejemplo, los puestos según el departamento)
        new MutationObserver(() => {
            pintarBoton();
            if (!panel.hidden) filtrar();
        }).observe(select, { childList: true, attributes: true, attributeFilter: ['disabled'] });

        pintarBoton();
    }

    function iniciar(raiz) {
        (raiz || document).querySelectorAll('select[data-smart-select]').forEach(crear);
    }

    window.SmartSelect = { iniciar };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => iniciar());
    else iniciar();
})();
