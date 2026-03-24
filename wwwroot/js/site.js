// ========================================================
// site.js – Flatpickr date pickers + Estado → Municipio cascada
// ========================================================
(function () {
    'use strict';

    // --------------------------------------------------
    // 1. Flatpickr: inicializar todos los date pickers
    // --------------------------------------------------
    function initFlatpickr() {
        if (typeof flatpickr === 'undefined') return;

        flatpickr.localize(flatpickr.l10ns.es);

        document.querySelectorAll('input[type="date"].date-quick').forEach(function (input) {
            flatpickr(input, {
                dateFormat: 'Y-m-d',
                altInput: true,
                altFormat: 'd/m/Y',
                allowInput: true,
                locale: 'es',
                monthSelectorType: 'dropdown',
                // Rango razonable para fechas académicas
                minDate: '1950-01-01',
                maxDate: 'today',
            });
        });
    }

    // --------------------------------------------------
    // 2. Estado → Municipio: carga dinámica
    // --------------------------------------------------
    var estadosMunicipios = null;

    function loadEstadosMunicipios() {
        return fetch('/data/estados-municipios.json')
            .then(function (res) { return res.json(); })
            .then(function (data) {
                estadosMunicipios = data;
                return data;
            });
    }

    function populateEstadoSelect(select, currentValue) {
        if (!estadosMunicipios) return;

        // Conservar la primera opción vacía
        var placeholder = select.querySelector('option[value=""]');
        select.innerHTML = '';
        if (placeholder) {
            select.appendChild(placeholder);
        } else {
            var opt = document.createElement('option');
            opt.value = '';
            opt.textContent = 'Seleccione un estado';
            select.appendChild(opt);
        }

        Object.keys(estadosMunicipios).sort().forEach(function (estado) {
            var opt = document.createElement('option');
            opt.value = estado;
            opt.textContent = estado;
            if (currentValue && estado === currentValue) {
                opt.selected = true;
            }
            select.appendChild(opt);
        });
    }

    function populateMunicipioSelect(select, estado, currentValue) {
        if (!estadosMunicipios) return;

        var placeholder = select.querySelector('option[value=""]');
        select.innerHTML = '';
        if (!placeholder) {
            placeholder = document.createElement('option');
            placeholder.value = '';
            placeholder.textContent = 'Seleccione un municipio';
        }
        select.appendChild(placeholder);

        if (estado && estadosMunicipios[estado]) {
            estadosMunicipios[estado].forEach(function (mun) {
                var opt = document.createElement('option');
                opt.value = mun;
                opt.textContent = mun;
                if (currentValue && mun === currentValue) {
                    opt.selected = true;
                }
                select.appendChild(opt);
            });
        }
    }

    function initEstadoMunicipio() {
        var estadoSelects = document.querySelectorAll('select[data-estado]');
        if (!estadoSelects.length) return;

        loadEstadosMunicipios().then(function () {
            estadoSelects.forEach(function (estadoSelect) {
                var currentEstado = estadoSelect.getAttribute('data-current-value') || '';
                populateEstadoSelect(estadoSelect, currentEstado);

                var targetName = estadoSelect.getAttribute('data-municipio-target');
                if (targetName) {
                    var munSelect = document.querySelector('select[data-municipio="' + targetName + '"]');
                    if (munSelect) {
                        var currentMun = munSelect.getAttribute('data-current-value') || '';
                        // Poblar municipios si ya hay un estado seleccionado
                        if (currentEstado) {
                            populateMunicipioSelect(munSelect, currentEstado, currentMun);
                        }

                        // Evento change
                        estadoSelect.addEventListener('change', function () {
                            populateMunicipioSelect(munSelect, estadoSelect.value, '');
                        });
                    }
                }
            });
        });
    }

    // --------------------------------------------------
    // Inicializar todo al cargar el DOM
    // --------------------------------------------------
    document.addEventListener('DOMContentLoaded', function () {
        initFlatpickr();
        initEstadoMunicipio();
    });
})();
