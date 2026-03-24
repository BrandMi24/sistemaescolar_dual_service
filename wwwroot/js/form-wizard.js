// ============================================================
// form-wizard.js — Wizard multi-paso para Preinscripciones
// ============================================================
// Extraído de Preinscripciones/Create.cshtml
// ============================================================
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        var form = document.getElementById('preinscripcionForm');
        if (!form) return; // Solo ejecutar si existe el formulario de preinscripción

        var currentStep = 1;
        var totalSteps = document.querySelectorAll('.wizard-step').length;
        if (totalSteps === 0) return;

        function showStep(n) {
            document.querySelectorAll('.wizard-step').forEach(function (s) {
                s.classList.remove('active');
            });
            document.querySelectorAll('.step-badge').forEach(function (b) {
                b.classList.remove('active');
            });

            var step = document.querySelector('.wizard-step[data-step="' + n + '"]');
            var badge = document.querySelector('.step-badge[data-step="' + n + '"]');
            if (step) step.classList.add('active');
            if (badge) badge.classList.add('active');

            // Scroll to top of form
            form.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }

        function validateStep(n) {
            var step = document.querySelector('.wizard-step[data-step="' + n + '"]');
            if (!step) return true;
            var inputs = step.querySelectorAll('[required]');
            var valid = true;
            inputs.forEach(function (input) {
                if (!input.reportValidity()) {
                    valid = false;
                }
            });
            return valid;
        }

        // Navigation buttons
        document.querySelectorAll('.btn-next-step').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                if (validateStep(currentStep)) {
                    currentStep++;
                    showStep(currentStep);
                }
            });
        });

        document.querySelectorAll('.btn-prev-step').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                if (currentStep > 1) {
                    currentStep--;
                    showStep(currentStep);
                }
            });
        });

        // Step badges (clickable)
        document.querySelectorAll('.step-badge').forEach(function (badge) {
            badge.addEventListener('click', function () {
                var target = parseInt(this.getAttribute('data-step'));
                if (target < currentStep) {
                    currentStep = target;
                    showStep(currentStep);
                } else if (target === currentStep + 1) {
                    if (validateStep(currentStep)) {
                        currentStep = target;
                        showStep(currentStep);
                    }
                }
            });
        });

        // Age calculator
        var fechaNacInput = document.querySelector('input[name="FechaNacimiento"]');
        var edadDisplay = document.getElementById('edadCalculada');
        if (fechaNacInput && edadDisplay) {
            fechaNacInput.addEventListener('change', function () {
                if (this.value) {
                    var birth = new Date(this.value);
                    var today = new Date();
                    var age = today.getFullYear() - birth.getFullYear();
                    var m = today.getMonth() - birth.getMonth();
                    if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) {
                        age--;
                    }
                    edadDisplay.value = age + ' años';
                } else {
                    edadDisplay.value = '0 años';
                }
            });
        }

        // Discapacidad sync
        var discSelect = document.getElementById('discapacidadSelect');
        var discHidden = document.getElementById('tieneDiscapacidad');
        if (discSelect && discHidden) {
            function syncDiscapacidad() {
                discHidden.value = (discSelect.value !== 'Ninguna' && discSelect.value !== '').toString();
            }
            discSelect.addEventListener('change', syncDiscapacidad);
            syncDiscapacidad();
        }

        showStep(1);
    });
})();
