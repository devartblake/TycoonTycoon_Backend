/* Operator dashboard progressive-enhancement layer. */

(function () {
  'use strict';

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

  function clearFieldState(input) {
    input.classList.remove('invalid', 'valid');
    var closestField = input.closest('.form-field');
    var errorEl = closestField
      ? closestField.querySelector('.field-error')
      : document.querySelector('[data-error-for="' + input.id + '"]');
    if (errorEl) {
      errorEl.textContent = '';
      errorEl.classList.remove('visible');
    }
  }

  function markFieldInvalid(input, message) {
    input.classList.add('invalid');
    input.classList.remove('valid');
    var closestField = input.closest('.form-field');
    var errorEl = closestField
      ? closestField.querySelector('.field-error')
      : (input.id ? document.querySelector('[data-error-for="' + input.id + '"]') : null);
    if (errorEl) {
      errorEl.textContent = message || 'This field is required.';
      errorEl.classList.add('visible');
    }
  }

  function markFieldValid(input) {
    input.classList.add('valid');
    input.classList.remove('invalid');
    var closestField = input.closest('.form-field');
    var errorEl = closestField
      ? closestField.querySelector('.field-error')
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
        markFieldInvalid(input, input.dataset.hint || 'This field is required.');
        valid = false;
      } else {
        markFieldValid(input);
      }
    });
    return valid;
  }

  function initFormValidation() {
    document.querySelectorAll('form[data-validate]').forEach(function (form) {
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

  function copyText(text, button) {
    if (!text) return;
    function done() {
      if (!button) return;
      var original = button.textContent;
      button.textContent = 'Copied';
      window.setTimeout(function () {
        button.textContent = original;
      }, 1400);
    }

    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(done).catch(function () {});
      return;
    }

    var ta = document.createElement('textarea');
    ta.value = text;
    ta.setAttribute('readonly', '');
    ta.style.position = 'absolute';
    ta.style.left = '-9999px';
    document.body.appendChild(ta);
    ta.select();
    try {
      document.execCommand('copy');
      done();
    } finally {
      document.body.removeChild(ta);
    }
  }

  function initCopyControls() {
    document.querySelectorAll('[data-copy-target]').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var target = document.querySelector(btn.dataset.copyTarget);
        copyText(target ? target.textContent : '', btn);
      });
    });

    document.querySelectorAll('.surface-note pre').forEach(function (pre, idx) {
      var wrapper = pre.closest('.surface-note');
      if (!wrapper || wrapper.querySelector('.copy-inline')) return;
      wrapper.classList.add('copyable');
      if (!pre.id) pre.id = 'copyable-payload-' + idx;
      var btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'button secondary copy-inline';
      btn.textContent = 'Copy';
      btn.setAttribute('data-copy-target', '#' + pre.id);
      btn.addEventListener('click', function () {
        copyText(pre.textContent, btn);
      });
      wrapper.appendChild(btn);
    });
  }

  document.addEventListener('DOMContentLoaded', function () {
    initDensityBars();
    initFormValidation();
    initCopyControls();
  });
})();
