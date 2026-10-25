// Smart Select: estándar de desplegables del sistema (ver docs/ESTANDAR_DESPLEGABLES.md).
// Se aplica solo a todo <select class="form-select"> (y a <select data-smart-select>); para dejar uno nativo: data-nativo.
// El <select> original sigue en el formulario: envío, validación y eventos "change" funcionan igual.
// El menú se dibuja en <body> con posición fija, así que ningún contenedor con overflow lo recorta.
(function () {
    const POR_PAGINA = 8;          // opciones que se muestran al abrir; más al bajar o al buscar
    const BUSCADOR_DESDE = 9;      // con menos opciones no hace falta el buscador
    const ALTO_MAXIMO = 320;
    const normalizar = (texto) => texto.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();
    let contador = 0;
    let abierto = null;            // solo un menú abierto a la vez

    function crear(select) {
        if (select.dataset.ssListo || select.multiple || select.hasAttribute('data-nativo')) return;
        select.dataset.ssListo = '1';
        const id = select.id || ('ss-' + (++contador));
        const contenedor = document.createElement('div');
        contenedor.className = 'ss';
        select.parentNode.insertBefore(contenedor, select);
        contenedor.appendChild(select);
        select.classList.add('ss-nativo');
        select.tabIndex = -1;
        select.setAttribute('aria-hidden', 'true');

        const boton = document.createElement('button');
        boton.type = 'button';
        boton.className = 'form-select ss-boton' + (select.classList.contains('form-select-sm') ? ' form-select-sm' : '');
        if (select.getAttribute('style')) boton.setAttribute('style', select.getAttribute('style'));
        boton.setAttribute('role', 'combobox');
        boton.setAttribute('aria-haspopup', 'listbox');
        boton.setAttribute('aria-expanded', 'false');
        boton.setAttribute('aria-controls', id + '-lista');
        boton.id = id + '-boton';
        const etiqueta = select.id && document.querySelector('label[for="' + select.id + '"]');
        if (etiqueta) etiqueta.htmlFor = boton.id;
        else if (select.getAttribute('aria-label')) boton.setAttribute('aria-label', select.getAttribute('aria-label'));
        contenedor.appendChild(boton);

        const panel = document.createElement('div');
        panel.className = 'ss-panel';
        panel.hidden = true;
        panel.innerHTML =
            '<div class="ss-buscar"><i class="bi bi-search" aria-hidden="true"></i><input type="search" autocomplete="off" placeholder="Buscar…" aria-label="Buscar"></div>' +
            '<ul class="ss-lista" role="listbox" id="' + id + '-lista"></ul>' +
            '<div class="ss-pie"></div>';
        document.body.appendChild(panel);
        const buscar = panel.querySelector('.ss-buscar');
        const buscador = buscar.querySelector('input');
        const lista = panel.querySelector('ul');
        const pie = panel.querySelector('.ss-pie');

        let filtradas = [];
        let mostradas = 0;
        let activa = -1;

        const opciones = () => Array.from(select.options).filter(o => !o.disabled || o.value === '');
        const vacia = () => opciones().find(o => o.value === '');
        const conBuscador = () => opciones().filter(o => o.value !== '').length >= BUSCADOR_DESDE;

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
            const vistas = mostradas - (filtradas[0] && filtradas[0].value === '' ? 1 : 0);
            pie.hidden = !conBuscador() && filtradas.length > 0;
            pie.textContent = filtradas.length === 0
                ? 'Sin resultados'
                : vistas < reales ? `Mostrando ${vistas} de ${reales} · escriba o baje para ver más` : `${reales} opción(es)`;
        }

        // Debajo del botón, o encima si abajo no cabe; dentro de la ventana
        function posicionar() {
            const r = boton.getBoundingClientRect();
            const ancho = Math.max(r.width, 240);
            const izquierda = Math.min(Math.max(8, r.left), window.innerWidth - ancho - 8);
            panel.style.width = ancho + 'px';
            panel.style.left = izquierda + 'px';
            const abajo = window.innerHeight - r.bottom - 8;
            const arriba = r.top - 8;
            const alto = Math.min(panel.scrollHeight, ALTO_MAXIMO);
            if (abajo < alto && arriba > abajo) {
                panel.style.top = '';
                panel.style.bottom = (window.innerHeight - r.top + 4) + 'px';
                panel.style.maxHeight = Math.min(ALTO_MAXIMO + 90, arriba) + 'px';
            } else {
                panel.style.bottom = '';
                panel.style.top = (r.bottom + 4) + 'px';
                panel.style.maxHeight = Math.min(ALTO_MAXIMO + 90, abajo) + 'px';
            }
        }

        function abrir() {
            if (select.disabled) return;
            if (abierto && abierto !== cerrar) abierto(false);
            abierto = cerrar;
            buscar.hidden = !conBuscador();
            buscador.value = '';
            filtrar();
            panel.hidden = false;
            boton.setAttribute('aria-expanded', 'true');
            posicionar();
            const elegida = lista.querySelector('[aria-selected="true"]');
            if (elegida) marcar(Number(elegida.dataset.indice));
            (conBuscador() ? buscador : lista).focus({ preventScroll: true });
        }

        function cerrar(devolverFoco) {
            if (panel.hidden) return;
            panel.hidden = true;
            boton.setAttribute('aria-expanded', 'false');
            if (abierto === cerrar) abierto = null;
            if (devolverFoco) boton.focus();
        }

        function elegir(indice) {
            const opcion = filtradas[indice];
            if (!opcion) return;
            const cambio = select.value !== opcion.value;
            select.value = opcion.value;
            pintarBoton();
            cerrar(true);
            if (cambio) select.dispatchEvent(new Event('change', { bubbles: true }));
        }

        function marcar(indice) {
            const items = lista.querySelectorAll('.ss-opcion');
            if (!items.length) return;
            activa = Math.max(0, Math.min(indice, items.length - 1));
            items.forEach((el, i) => el.classList.toggle('ss-activa', i === activa));
            items[activa].scrollIntoView({ block: 'nearest' });
            buscador.setAttribute('aria-activedescendant', items[activa].id);
        }

        function teclas(e) {
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
        }

        lista.tabIndex = -1;
        boton.addEventListener('click', () => (panel.hidden ? abrir() : cerrar(true)));
        boton.addEventListener('keydown', (e) => {
            if (['ArrowDown', 'ArrowUp', 'Enter', ' '].includes(e.key)) {
                e.preventDefault();
                abrir();
            }
        });
        buscador.addEventListener('input', () => { filtrar(); posicionar(); });
        buscador.addEventListener('keydown', teclas);
        lista.addEventListener('keydown', teclas);
        lista.addEventListener('mousedown', (e) => e.preventDefault()); // no quitar el foco del buscador
        lista.addEventListener('click', (e) => {
            const item = e.target.closest('.ss-opcion');
            if (item) elegir(Number(item.dataset.indice));
        });
        lista.addEventListener('scroll', () => {
            if (lista.scrollTop + lista.clientHeight >= lista.scrollHeight - 8 && mostradas < filtradas.length) agregarPagina();
        });
        document.addEventListener('mousedown', (e) => {
            if (!panel.hidden && !contenedor.contains(e.target) && !panel.contains(e.target)) cerrar(false);
        });
        // Sigue al botón si la página o un contenedor hace scroll
        window.addEventListener('scroll', (e) => { if (!panel.hidden && !panel.contains(e.target)) posicionar(); }, true);
        window.addEventListener('resize', () => { if (!panel.hidden) posicionar(); });
        select.addEventListener('change', pintarBoton);
        // Opciones que cambian por JavaScript (por ejemplo, los puestos según el departamento)
        // y el estado de validación (jquery.validate marca el <select> con input-validation-error)
        new MutationObserver(() => {
            pintarBoton();
            boton.classList.toggle('is-invalid', select.classList.contains('input-validation-error'));
            if (!panel.hidden) filtrar();
        }).observe(select, { childList: true, subtree: true, attributes: true, attributeFilter: ['disabled', 'class'] });

        pintarBoton();
    }

    function iniciar(raiz) {
        (raiz || document).querySelectorAll('select.form-select, select[data-smart-select]').forEach(crear);
    }

    window.SmartSelect = { iniciar };
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', () => iniciar());
    else iniciar();
})();
