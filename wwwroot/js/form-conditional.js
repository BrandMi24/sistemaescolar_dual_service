// ============================================================
// form-conditional.js — Toggles condicionales genéricos
// ============================================================
// En lugar de repetir el patrón de toggle 7+ veces en cada vista,
// este archivo usa atributos data-* para declarar comportamiento.
//
// Uso:
//   <input type="radio" name="CAMPO" value="true"
//          data-toggle-target="#grupoId" data-toggle-value="true" />
//
// O para <select>:
//   <select data-toggle-target="#grupoId"
//           data-toggle-hide-value="Ninguna">
//
// ============================================================
(function () {
    'use strict';

    function initConditionalToggles() {
        // --- Radio / Checkbox toggles ---
        document.querySelectorAll('[data-toggle-target]').forEach(function (el) {
            var targetSelector = el.getAttribute('data-toggle-target');
            var triggerValue = el.getAttribute('data-toggle-value');
            var hideValue = el.getAttribute('data-toggle-hide-value');
            var target = document.querySelector(targetSelector);
            if (!target) return;

            var tagName = el.tagName.toLowerCase();

            if (tagName === 'select') {
                // Select-based toggles
                function handleSelectChange() {
                    var show = hideValue
                        ? el.value !== hideValue
                        : el.value === triggerValue;
                    $(target).slideToggle(300, function () { }).stop(true, false);
                    if (show) {
                        $(target).slideDown(300);
                    } else {
                        $(target).slideUp(300);
                    }
                }
                el.addEventListener('change', handleSelectChange);
                // Run on init
                handleSelectChange();
            } else if (el.type === 'radio' || el.type === 'checkbox') {
                // For radio buttons, listen on all radios with same name
                var name = el.name;
                if (!name) return;

                var radios = document.querySelectorAll('input[name="' + name + '"]');
                radios.forEach(function (radio) {
                    radio.addEventListener('change', function () {
                        var checked = document.querySelector('input[name="' + name + '"]:checked');
                        if (!checked) return;
                        var show = triggerValue
                            ? (checked.value === triggerValue || checked.value === 'True' || checked.value === 'true')
                            : false;
                        if (show) {
                            $(target).slideDown(300);
                        } else {
                            $(target).slideUp(300);
                        }
                    });
                });
            }
        });
    }

    // --- CURP uppercase helper ---
    function initCurpUppercase() {
        var curpInputs = document.querySelectorAll('input[name="CURP"]');
        curpInputs.forEach(function (input) {
            input.style.textTransform = 'uppercase';
            input.addEventListener('input', function () {
                var pos = this.selectionStart;
                this.value = this.value.toUpperCase();
                this.setSelectionRange(pos, pos);
            });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        initConditionalToggles();
        initCurpUppercase();
    });
})();
