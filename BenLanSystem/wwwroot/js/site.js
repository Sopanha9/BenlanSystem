/**
 * BENLAN LUXURY TRANSIT — Master Script (site.js)
 * Lucide Icons Init, AOS Animation, Theme Engine & Global Utilities
 */

(function () {
  'use strict';

  // 1. Theme Engine (Dark Mode / Light Mode with localStorage persistence)
  const THEME_KEY = 'benlan_theme';

  function initTheme() {
    const savedTheme = localStorage.getItem(THEME_KEY) || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);
    updateFlatpickrTheme(savedTheme);

    const toggleBtn = document.getElementById('theme-toggle-btn');
    if (toggleBtn) {
      toggleBtn.addEventListener('click', toggleTheme);
    }
  }

  function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
    const nextTheme = currentTheme === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', nextTheme);
    localStorage.setItem(THEME_KEY, nextTheme);
    updateFlatpickrTheme(nextTheme);

    // Re-render icons if needed
    if (window.lucide) {
      window.lucide.createIcons();
    }
  }

  function updateFlatpickrTheme(theme) {
    const darkLink = document.getElementById('flatpickr-dark-theme');
    if (darkLink) {
      darkLink.disabled = (theme === 'light');
    }
  }

  // 2. Mobile Drawer
  function initMobileDrawer() {
    const toggleBtn = document.getElementById('mobile-menu-btn');
    const drawer = document.getElementById('mobile-drawer');
    if (!toggleBtn || !drawer) return;

    toggleBtn.addEventListener('click', function () {
      drawer.classList.toggle('open');
      const openIcon = toggleBtn.querySelector('.menu-open-icon');
      const closeIcon = toggleBtn.querySelector('.menu-close-icon');
      if (openIcon && closeIcon) {
        openIcon.classList.toggle('hidden');
        closeIcon.classList.toggle('hidden');
      }
    });
  }

  // 3. Lucide Icons & AOS Init
  function initPlugins() {
    if (window.lucide) {
      window.lucide.createIcons();
    }

    if (window.AOS) {
      window.AOS.init({
        duration: 700,
        easing: 'ease-out-cubic',
        once: true,
        offset: 50
      });
    }
  }

  // 4. SweetAlert2 Custom Toast Helpers
  window.BenLanToast = {
    success: function (title, message) {
      if (window.Swal) {
        window.Swal.fire({
          icon: 'success',
          title: title || 'Success',
          text: message || '',
          timer: 3000,
          timerProgressBar: true,
          showConfirmButton: false,
          background: document.documentElement.getAttribute('data-theme') === 'light' ? '#ffffff' : '#163026',
          color: document.documentElement.getAttribute('data-theme') === 'light' ? '#18201C' : '#F2EFE6'
        });
      } else {
        alert(title + (message ? ': ' + message : ''));
      }
    },
    error: function (title, message) {
      if (window.Swal) {
        window.Swal.fire({
          icon: 'error',
          title: title || 'Oops!',
          text: message || 'Something went wrong.',
          background: document.documentElement.getAttribute('data-theme') === 'light' ? '#ffffff' : '#163026',
          color: document.documentElement.getAttribute('data-theme') === 'light' ? '#18201C' : '#F2EFE6'
        });
      } else {
        alert(title + (message ? ': ' + message : ''));
      }
    },
    confirm: async function (title, text, confirmBtnText) {
      if (window.Swal) {
        const result = await window.Swal.fire({
          title: title,
          text: text,
          icon: 'warning',
          showCancelButton: true,
          confirmButtonColor: '#163D32',
          cancelButtonColor: '#B3402A',
          confirmButtonText: confirmBtnText || 'Yes, proceed',
          background: document.documentElement.getAttribute('data-theme') === 'light' ? '#ffffff' : '#163026',
          color: document.documentElement.getAttribute('data-theme') === 'light' ? '#18201C' : '#F2EFE6'
        });
        return result.isConfirmed;
      }
      return confirm(text || title);
    }
  };

  // Run on DOM Ready
  document.addEventListener('DOMContentLoaded', function () {
    initTheme();
    initMobileDrawer();
    initPlugins();
  });

  // Export refresh icons helper
  window.refreshIcons = function () {
    if (window.lucide) {
      window.lucide.createIcons();
    }
  };
})();
