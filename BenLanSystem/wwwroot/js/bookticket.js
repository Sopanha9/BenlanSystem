// BenLan book-ticket page wiring

window.__locations = [];
window.__tripCache = new Map();

let currentTrips = [];
let seatCount = 1;
let selectedTripId = null;
let selectedTripPrice = 0;
let selectedTripAvailableSeats = 1;

function escapeHtml(value) {
    return String(value ?? '').replace(/[&<>"']/g, function (c) {
        return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
    });
}

async function readResponseError(res) {
    try {
        const data = await res.json();
        return data.message || JSON.stringify(data);
    } catch {
        return await res.text();
    }
}

async function loadLocations() {
    try {
        const res = await fetch('/api/Location');
        if (!res.ok) return;
        window.__locations = await res.json();
        const list = document.getElementById('bt-location-list');
        if (list) {
            list.innerHTML = window.__locations.map(l => `<option value="${escapeHtml(l.name)}">`).join('');
        }
    } catch {
    }
}

function prefillFromUrl() {
    const params = new URLSearchParams(window.location.search);
    const from = params.get('from');
    const to = params.get('to');
    const date = params.get('date');
    if (from) document.getElementById('bt-input-from').value = from;
    if (to) document.getElementById('bt-input-to').value = to;
    if (date) document.getElementById('bt-input-date').value = date;

    if (from && to) {
        searchTrips();
    } else {
        renderEmptyState('Choose a route and date to search available trips.');
    }
}

function renderEmptyState(message) {
    const list = document.getElementById('bt-ticket-list');
    const countEl = document.getElementById('bt-results-count');
    if (list) {
        list.innerHTML = `<div style="padding:40px;text-align:center;color:#6b7280;">${escapeHtml(message)}</div>`;
    }
    if (countEl) countEl.innerHTML = 'Showing <strong>0</strong> results';
}

function findLocationByName(name) {
    return window.__locations.find(l => l.name.toLowerCase() === name.toLowerCase());
}

async function searchTrips() {
    const btn = document.getElementById('bt-search-btn');
    const from = document.getElementById('bt-input-from').value.trim();
    const to = document.getElementById('bt-input-to').value.trim();

    if (!from || !to) {
        if (!from) shakeField('bt-field-from');
        if (!to) shakeField('bt-field-to');
        return;
    }

    const fromLoc = findLocationByName(from);
    const toLoc = findLocationByName(to);
    if (!fromLoc || !toLoc) {
        renderEmptyState('Please choose locations from the list.');
        return;
    }

    btn.textContent = 'Searching...';
    btn.disabled = true;

    const params = new URLSearchParams();
    params.set('OriginId', fromLoc.id);
    params.set('DestinationId', toLoc.id);
    const date = document.getElementById('bt-input-date').value;
    if (date) params.set('DepartureDate', date);
    params.set('PageSize', '20');

    try {
        const res = await fetch(`/api/Trip?${params}`);
        if (!res.ok) {
            renderEmptyState('Unable to load trips right now.');
        } else {
            const data = await res.json();
            renderTrips(data.items || []);
        }
    } catch {
        renderEmptyState('Unable to load trips right now.');
    } finally {
        btn.innerHTML = '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/></svg> Search';
        btn.disabled = false;
    }
}

function renderTrips(trips) {
    currentTrips = [...trips];
    window.__tripCache = new Map(currentTrips.map(t => [Number(t.id), t]));

    const list = document.getElementById('bt-ticket-list');
    const countEl = document.getElementById('bt-results-count');

    if (!currentTrips.length) {
        renderEmptyState('No trips found for this route.');
        return;
    }

    const from = document.getElementById('bt-input-from').value.trim();
    const to = document.getElementById('bt-input-to').value.trim();
    if (countEl) {
        countEl.innerHTML = `Showing <strong>${currentTrips.length}</strong> results for <strong>${escapeHtml(from)} to ${escapeHtml(to)}</strong>`;
    }

    list.innerHTML = currentTrips.map(function (t, i) {
        const dep = t.departureTimeUtc ? new Date(t.departureTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '';
        const arr = t.arrivalTimeUtc ? new Date(t.arrivalTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '';
        let duration = '';
        if (t.departureTimeUtc && t.arrivalTimeUtc) {
            const mins = Math.max(0, Math.round((new Date(t.arrivalTimeUtc) - new Date(t.departureTimeUtc)) / 60000));
            const hours = Math.floor(mins / 60);
            const rest = mins % 60;
            duration = `${hours ? hours + 'h ' : ''}${rest ? rest + 'm' : ''}`.trim();
        }
        const availableSeats = Number(t.availableSeats || 0);
        const price = `$${Number(t.basePrice || 0).toFixed(2)}`;
        const disabled = availableSeats <= 0 ? 'disabled' : '';

        return `
            <div class="bt-ticket" id="bt-ticket-${i}" style="opacity:0;transform:translateY(14px);">
                <span class="bt-ticket-seats">${availableSeats} seats</span>
                <div class="bt-ticket-img">
                    <img src="/designs/bookticket/car.png" alt="Van" class="bt-ticket-car" />
                </div>
                <div class="bt-ticket-content">
                    <div class="bt-ticket-info">
                        <img src="/designs/image.png" alt="BenLan" class="bt-operator-logo" onerror="this.style.display='none'" />
                        <span class="bt-operator-name">${escapeHtml(t.vehicleInfo || 'BenLan Van')}</span>
                        <span class="bt-operator-route">${escapeHtml(t.originName)} to ${escapeHtml(t.destinationName)}</span>
                    </div>
                    <div class="bt-ticket-times">
                        <div class="bt-time-col">
                            <span class="bt-time-label">Departure</span>
                            <span class="bt-time-value">${escapeHtml(dep)}</span>
                        </div>
                        <div class="bt-duration-col">
                            <span class="bt-time-label">Duration</span>
                            <div class="bt-duration-display">
                                <div class="bt-duration-line"></div>
                                <span class="bt-time-value">${escapeHtml(duration || 'Direct')}</span>
                                <div class="bt-duration-line"></div>
                            </div>
                        </div>
                        <div class="bt-time-col">
                            <span class="bt-time-label">Arrival</span>
                            <span class="bt-time-value">${escapeHtml(arr || '-')}</span>
                        </div>
                    </div>
                    <div class="bt-ticket-right">
                        <span class="bt-price">${price}</span>
                        <button class="bt-book-btn" type="button" ${disabled} onclick="openBookingModal(${Number(t.id)})">Book now</button>
                    </div>
                </div>
            </div>`;
    }).join('');

    document.querySelectorAll('.bt-ticket').forEach(function (ticket, i) {
        setTimeout(function () {
            ticket.style.transition = 'opacity 0.35s ease, transform 0.35s ease';
            ticket.style.opacity = '1';
            ticket.style.transform = '';
        }, i * 80);
    });
}

function shakeField(id) {
    const el = document.getElementById(id);
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

function openBookingModal(tripId) {
    const trip = window.__tripCache.get(Number(tripId));
    if (!trip) return;

    selectedTripId = Number(tripId);
    selectedTripPrice = Number(trip.basePrice || 0);
    selectedTripAvailableSeats = Math.max(1, Number(trip.availableSeats || 1));
    seatCount = 1;

    const dep = trip.departureTimeUtc ? new Date(trip.departureTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '';
    const arr = trip.arrivalTimeUtc ? new Date(trip.arrivalTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '';

    document.getElementById('bt-modal-from').textContent = trip.originName || '';
    document.getElementById('bt-modal-to').textContent = trip.destinationName || '';
    document.getElementById('bt-modal-dep').textContent = dep;
    document.getElementById('bt-modal-arr').textContent = arr || '-';
    document.getElementById('bt-modal-price').textContent = `$${selectedTripPrice.toFixed(2)}`;
    document.getElementById('bt-seat-count').textContent = '1';

    const confirmBtn = document.getElementById('bt-modal-confirm-btn');
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
    seatCount = Math.max(1, Math.min(selectedTripAvailableSeats, seatCount + delta));
    document.getElementById('bt-seat-count').textContent = seatCount;
}

async function confirmBooking() {
    const firstname = document.getElementById('bt-modal-firstname').value.trim();
    const lastname = document.getElementById('bt-modal-lastname').value.trim();
    const phone = document.getElementById('bt-modal-phone').value.trim();

    if (!firstname || !phone) {
        if (!firstname) shakeModal('bt-modal-firstname');
        if (!phone) shakeModal('bt-modal-phone');
        return;
    }

    const btn = document.getElementById('bt-modal-confirm-btn');
    btn.textContent = 'Processing...';
    btn.disabled = true;

    const passengerName = `${firstname} ${lastname}`.trim();
    const dto = {
        tripId: selectedTripId,
        seatsBooked: seatCount,
        unitPrice: selectedTripPrice,
        notes: phone ? `Phone: ${phone}` : null,
        passengers: Array.from({ length: seatCount }, function (_, i) {
            return { passengerName, seatNumber: `A${i + 1}` };
        })
    };

    try {
        const res = await fetch('/api/Booking', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });

        if (res.status === 401) {
            window.location = '/Account/Login';
            return;
        }
        if (!res.ok) {
            alert('Booking failed: ' + await readResponseError(res));
            btn.textContent = 'Confirm Booking';
            btn.disabled = false;
            return;
        }

        const booking = await res.json();
        btn.textContent = 'Booked';
        btn.classList.add('success');
        setTimeout(function () {
            window.location.href = `/Home/Pay?bookingId=${encodeURIComponent(booking.id)}`;
        }, 800);
    } catch (e) {
        alert('Booking failed: ' + e.message);
        btn.textContent = 'Confirm Booking';
        btn.disabled = false;
    }
}

function shakeModal(id) {
    const el = document.getElementById(id);
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

function sortTrips(mode) {
    const sorted = [...currentTrips];
    sorted.sort(function (a, b) {
        if (mode === 'price-asc') return Number(a.basePrice || 0) - Number(b.basePrice || 0);
        if (mode === 'price-desc') return Number(b.basePrice || 0) - Number(a.basePrice || 0);
        if (mode === 'duration') return Number(a.estimatedMinutes || 0) - Number(b.estimatedMinutes || 0);
        return new Date(a.departureTimeUtc) - new Date(b.departureTimeUtc);
    });
    renderTrips(sorted);
}

document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') closeBookingModal();
});

document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('bt-search-btn')?.addEventListener('click', searchTrips);
    document.getElementById('bt-sort-select')?.addEventListener('change', function () {
        sortTrips(this.value);
    });
    loadLocations().then(prefillFromUrl);
});
