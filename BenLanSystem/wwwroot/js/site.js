// BenLan System - Site JavaScript

// Mobile menu toggle
document.addEventListener('DOMContentLoaded', function () {
    const hamburger = document.getElementById('navbar-hamburger');
    const navLinks = document.getElementById('navbar-links');

    if (hamburger && navLinks) {
        hamburger.addEventListener('click', function () {
            navLinks.classList.toggle('open');

            // Animate hamburger to X
            const spans = hamburger.querySelectorAll('span');
            hamburger.classList.toggle('active');

            if (navLinks.classList.contains('open')) {
                spans[0].style.transform = 'rotate(45deg) translate(5px, 5px)';
                spans[1].style.opacity = '0';
                spans[2].style.transform = 'rotate(-45deg) translate(5px, -5px)';
            } else {
                spans[0].style.transform = 'none';
                spans[1].style.opacity = '1';
                spans[2].style.transform = 'none';
            }
        });
    }

    // Populate location datalist
    fetch('/api/Location')
        .then(r => r.ok ? r.json() : [])
        .then(locations => {
            const list = document.getElementById('location-list');
            if (list) {
                list.innerHTML = locations.map(l => `<option value="${l.name}">`).join('');
            }
        });

    // Homepage search form
    const bookingForm = document.getElementById('booking-form');
    if (bookingForm) {
        bookingForm.addEventListener('submit', function (e) {
            e.preventDefault();
            const from = document.getElementById('input-departing').value.trim();
            const to = document.getElementById('input-goingto').value.trim();
            const date = document.getElementById('input-goingdate').value;
            if (from && to) {
                window.location.href = `/Home/BookTicket?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}&date=${encodeURIComponent(date || '')}`;
            }
        });
    }
});
