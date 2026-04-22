// BenLan — bookticket.js

// ── Swap button ──
document.getElementById('bt-swap-btn')?.addEventListener('click', function () {
    var fromInput = document.getElementById('bt-input-from');
    var toInput = document.getElementById('bt-input-to');
    var tmp = fromInput.value;
    fromInput.value = toInput.value;
    toInput.value = tmp;

    // Spin animation
    this.style.transform = 'rotate(180deg)';
    setTimeout(() => { this.style.transform = ''; }, 300);
});

// ── Search button ──
document.getElementById('bt-search-btn')?.addEventListener('click', function () {
    var btn = this;
    var from = document.getElementById('bt-input-from').value.trim();
    var to = document.getElementById('bt-input-to').value.trim();

    if (!from || !to) {
        // Shake the empty fields
        if (!from) shakeField('bt-field-from');
        if (!to)   shakeField('bt-field-to');
        return;
    }

    btn.textContent = 'Searching…';
    btn.disabled = true;

    // Simulate search delay
    setTimeout(function () {
        btn.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg> Search';
        btn.disabled = false;

        // Update count label
        var count = document.querySelectorAll('.bt-ticket').length;
        var el = document.getElementById('bt-results-count');
        if (el) el.innerHTML = 'Showing <strong>' + count + '</strong> results for <strong>' + from + ' → ' + to + '</strong>';

        // Re-animate tickets
        document.querySelectorAll('.bt-ticket').forEach(function (t, i) {
            t.style.opacity = '0';
            t.style.transform = 'translateY(14px)';
            setTimeout(function () {
                t.style.transition = 'opacity 0.35s ease, transform 0.35s ease';
                t.style.opacity = '1';
                t.style.transform = '';
            }, i * 80);
        });
    }, 900);
});

function shakeField(id) {
    var el = document.getElementById(id);
    if (!el) return;
    el.style.borderColor = 'rgba(239, 68, 68, 0.6)';
    el.style.boxShadow = '0 0 0 3px rgba(239, 68, 68, 0.15)';
    el.animate([
        { transform: 'translateX(0)' },
        { transform: 'translateX(-5px)' },
        { transform: 'translateX(5px)' },
        { transform: 'translateX(-4px)' },
        { transform: 'translateX(4px)' },
        { transform: 'translateX(0)' }
    ], { duration: 320, easing: 'ease-in-out' });
    setTimeout(function () {
        el.style.borderColor = '';
        el.style.boxShadow = '';
    }, 1400);
}

// ── Booking Modal ──
var seatCount = 1;

function openBookingModal(from, to, dep, arr, price) {
    document.getElementById('bt-modal-from').textContent  = from;
    document.getElementById('bt-modal-to').textContent    = to;
    document.getElementById('bt-modal-dep').textContent   = dep;
    document.getElementById('bt-modal-arr').textContent   = arr;
    document.getElementById('bt-modal-price').textContent = price;

    seatCount = 1;
    document.getElementById('bt-seat-count').textContent = '1';

    var confirmBtn = document.getElementById('bt-modal-confirm-btn');
    if (confirmBtn) {
        confirmBtn.textContent = 'Confirm Booking';
        confirmBtn.classList.remove('success');
        confirmBtn.disabled = false;
    }

    document.getElementById('bt-modal-backdrop').classList.add('open');
    document.getElementById('bt-modal').classList.add('open');
    document.body.style.overflow = 'hidden';
}

function closeBookingModal() {
    document.getElementById('bt-modal-backdrop').classList.remove('open');
    document.getElementById('bt-modal').classList.remove('open');
    document.body.style.overflow = '';
}

function changeSeat(delta) {
    seatCount = Math.max(1, Math.min(12, seatCount + delta));
    document.getElementById('bt-seat-count').textContent = seatCount;
}

function confirmBooking() {
    var firstname = document.getElementById('bt-modal-firstname').value.trim();
    var phone     = document.getElementById('bt-modal-phone').value.trim();

    if (!firstname || !phone) {
        if (!firstname) shakeModal('bt-modal-firstname');
        if (!phone)     shakeModal('bt-modal-phone');
        return;
    }

    var btn = document.getElementById('bt-modal-confirm-btn');
    btn.textContent = 'Processing…';
    btn.disabled = true;

    setTimeout(function () {
        btn.textContent = '✓ Booked!';
        btn.classList.add('success');
        setTimeout(closeBookingModal, 1400);
    }, 1000);
}

function shakeModal(id) {
    var el = document.getElementById(id);
    if (!el) return;
    el.style.borderColor = 'rgba(239, 68, 68, 0.6)';
    el.animate([
        { transform: 'translateX(0)' },
        { transform: 'translateX(-4px)' },
        { transform: 'translateX(4px)' },
        { transform: 'translateX(-3px)' },
        { transform: 'translateX(0)' }
    ], { duration: 280 });
    setTimeout(function () { el.style.borderColor = ''; }, 1200);
}

// Close modal on Escape key
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeBookingModal();
});

// ── Sort select ──
document.getElementById('bt-sort-select')?.addEventListener('change', function () {
    var tickets = Array.from(document.querySelectorAll('.bt-ticket'));
    var list = document.getElementById('bt-ticket-list');

    tickets.sort(function (a, b) {
        switch (this.value) {
            case 'price-asc':
                return parseFloat(getPriceOf(a)) - parseFloat(getPriceOf(b));
            case 'price-desc':
                return parseFloat(getPriceOf(b)) - parseFloat(getPriceOf(a));
            case 'departure':
                return getDepOf(a).localeCompare(getDepOf(b));
            default:
                return 0;
        }
    }.bind(this));

    tickets.forEach(function (t) { list.appendChild(t); });
});

function getPriceOf(ticketEl) {
    var el = ticketEl.querySelector('.bt-price');
    return el ? el.textContent.replace('$', '') : '0';
}

function getDepOf(ticketEl) {
    var vals = ticketEl.querySelectorAll('.bt-time-value');
    return vals.length ? vals[0].textContent : '00:00';
}
