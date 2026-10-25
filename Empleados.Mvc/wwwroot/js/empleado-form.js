// Formulario de empleado: puestos según el departamento y rango salarial del puesto elegido
(function () {
    const departamento = document.getElementById('DepartamentoId');
    const puesto = document.getElementById('PuestoId');
    const rango = document.getElementById('rangoPuesto');
    const salario = document.getElementById('Salario');
    if (!departamento || !puesto) return;
    const formato = new Intl.NumberFormat('es-DO', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    let puestos = [];

    function mostrarRango() {
        if (!rango) return;
        const elegido = puestos.find(p => String(p.id) === puesto.value);
        if (!elegido || (elegido.salarioMinimo == null && elegido.salarioMaximo == null)) {
            rango.textContent = departamento.value ? 'El puesto no tiene rango salarial' : 'Se filtra según el departamento';
            rango.classList.remove('text-danger');
            return;
        }
        const min = elegido.salarioMinimo != null ? 'RD$ ' + formato.format(elegido.salarioMinimo) : 'sin mínimo';
        const max = elegido.salarioMaximo != null ? 'RD$ ' + formato.format(elegido.salarioMaximo) : 'sin máximo';
        const valor = parseFloat(salario?.value);
        const fuera = !isNaN(valor) && ((elegido.salarioMinimo != null && valor < elegido.salarioMinimo) || (elegido.salarioMaximo != null && valor > elegido.salarioMaximo));
        rango.textContent = 'Rango del puesto: ' + min + ' – ' + max + (fuera ? ' · el salario está fuera del rango' : '');
        rango.classList.toggle('text-danger', fuera);
    }

    async function cargar(seleccionado) {
        puesto.innerHTML = '<option value="">Seleccione puesto</option>';
        puestos = [];
        if (departamento.value) {
            try {
                const res = await fetch('/Puestos/GetByDepartamento?departamentoId=' + departamento.value, { credentials: 'same-origin' });
                if (res.ok) puestos = (await res.json()).map(p => ({ id: p.id ?? p.Id, nombre: p.nombre ?? p.Nombre, salarioMinimo: p.salarioMinimo ?? null, salarioMaximo: p.salarioMaximo ?? null }));
            } catch (e) { console.error(e); }
            if (puestos.length === 0) puesto.insertAdjacentHTML('beforeend', '<option value="" disabled>No hay puestos para este departamento</option>');
            puestos.forEach(p => {
                const opcion = new Option(p.nombre, p.id, false, seleccionado != null && String(p.id) === String(seleccionado));
                puesto.appendChild(opcion);
            });
        }
        mostrarRango();
    }

    departamento.addEventListener('change', () => cargar(null));
    puesto.addEventListener('change', mostrarRango);
    salario?.addEventListener('input', mostrarRango);
    if (departamento.value) cargar(puesto.value);
})();
