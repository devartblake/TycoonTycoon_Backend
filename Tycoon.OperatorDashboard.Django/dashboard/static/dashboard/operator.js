/* Operator dashboard — progressive-enhancement layer */

(function () {
  'use strict';

  /* ─── Table density toggle ──────────────────────────────────────────────── */

  var DENSITY_OPTIONS = ['standard', 'compact', 'comfortable'];

  function applyTableDensity(tableId, density) {
    var table = document.getElementById(tableId);
    if (!table) return;

    DENSITY_OPTIONS.forEach(function (opt) {
      table.classList.remove('density-' + opt);
    });
    if (density !== 'standard') {
      table.classList.add('density-' + density);
    }

    var bar = document.querySelector('[data-density-target="' + tableId + '"]');
    if (!bar) return;
    bar.querySelectorAll('.density-btn').forEach(function (btn) {
      btn.classList.toggle('is-active', btn.dataset.density === density);
    });
  }

  function initDensityBars() {
    document.querySelectorAll('[data-density-target]').forEach(function (bar) {
      var tableId = bar.dataset.densityTarget;
      var storageKey = 'op_density_' + tableId;
      var saved = localStorage.getItem(storageKey) || 'standard';
      applyTableDensity(tableId, saved);

      bar.querySelectorAll('.density-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
          var density = btn.dataset.density;
          localStorage.setItem(storageKey, density);
          applyTableDensity(tableId, density);
        });
      });
    });
  }

  /* ─── Inline form validation ─────────────────────────────────────────────
   *
   * Forms decorated with data-validate="1" run client-side checks on submit.
   * Required fields must have the `required` attribute set. An optional
   * data-hint attribute on the input provides the error message text.
   *
   * Markup example:
   *   <div class="form-field">
   *     <label for="f-reason">Reason</label>
   *     <input id="f-reason" name="reason" required
   *            data-hint="A reason is required for audit purposes." />
   *     <span class="field-error" data-error-for="f-reason"></span>
   *   </div>
   */

  function clearFieldState(input) {
    input.classList.remove('invalid', 'valid');
    var errorEl = input.closest('.form-field')
      ? input.closest('.form-field').querySelector('.field-error')
      : document.querySelector('[data-error-for="' + input.id + '"]');
    if (errorEl) {
      errorEl.textContent = '';
      errorEl.classList.remove('visible');
    }
  }

  function markFieldInvalid(input, message) {
    input.classList.add('invalid');
    input.classList.remove('valid');
    var errorEl = input.closest('.form-field')
      ? input.closest('.form-field').querySelector('.field-error')
      : (input.id ? document.querySelector('[data-error-for="' + input.id + '"]') : null);
    if (errorEl) {
      errorEl.textContent = message || 'This field is required.';
      errorEl.classList.add('visible');
    }
  }

  function markFieldValid(input) {
    input.classList.add('valid');
    input.classList.remove('invalid');
    var errorEl = input.closest('.form-field')
      ? input.closest('.form-field').querySelector('.field-error')
      : (input.id ? document.querySelector('[data-error-for="' + input.id + '"]') : null);
    if (errorEl) {
      errorEl.textContent = '';
      errorEl.classList.remove('visible');
    }
  }

  function validateForm(form) {
    var valid = true;
    form.querySelectorAll('[required]').forEach(function (input) {
      clearFieldState(input);
      var value = input.value.trim();
      if (!value) {
        var hint = input.dataset.hint || 'This field is required.';
        markFieldInvalid(input, hint);
        valid = false;
      } else {
        markFieldValid(input);
      }
    });
    return valid;
  }

  function initFormValidation() {
    document.querySelectorAll('form[data-validate]').forEach(function (form) {
      /* Clear state on input so errors disappear as the user types */
      form.querySelectorAll('[required]').forEach(function (input) {
        input.addEventListener('input', function () {
          clearFieldState(input);
        });
      });

      form.addEventListener('submit', function (e) {
        if (!validateForm(form)) {
          e.preventDefault();
          var firstInvalid = form.querySelector('.invalid');
          if (firstInvalid) firstInvalid.focus();
        }
      });
    });
  }

  /* ─── Boot ───────────────────────────────────────────────────────────────── */

  document.addEventListener('DOMContentLoaded', function () {
    initDensityBars();
    initFormValidation();
  });
})();
