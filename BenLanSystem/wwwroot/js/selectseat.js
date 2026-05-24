// BenLan — Select Seat page wiring

const tripId = new URLSearchParams(window.location.search).get('tripId');
let trip = null;
let occupiedSeats = [];
let selectedSeats = [];

function esc(value) {
    return String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
}

async function readError(res) {
    try {
        const data = await res.json();
        return data.message || JSON.stringify(data);
    } catch {
        return await res.text();
    }
}

async function loadTrip() {
    if (!tripId) {
        document.getElementById('ss-seat-grid').innerHTML = '<div class="ss-loading">Missing trip ID.</div>';
        return;
    }

    try {
        const [tripRes, seatsRes] = await Promise.all([
            fetch(`/api/Trip/${tripId}`),
            fetch(`/api/Booking/trip/${tripId}/seats`)
        ]);

        if (!tripRes.ok) {
            document.getElementById('ss-seat-grid').innerHTML = '<div class="ss-loading">Trip not found.</div>';
            return;
        }

        trip = await tripRes.json();
        occupiedSeats = seatsRes.ok ? (await seatsRes.json()) : [];
        renderTripInfo();
        renderSeatGrid();
    } catch {
        document.getElementById('ss-seat-grid').innerHTML = '<div class="ss-loading">Unable to load trip details.</div>';
    }
}

function renderTripInfo() {
    if (!trip) return;

    document.getElementById('ss-from').textContent = trip.originName || '...';
    document.getElementById('ss-to').textContent = trip.destinationName || '...';

    const depDate = trip.departureTimeUtc ? new Date(trip.departureTimeUtc) : null;
    if (depDate) {
        const options = { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' };
        document.getElementById('ss-date').textContent = depDate.toLocaleDateString('en-GB', options);
    }

    const depTime = trip.departureTimeUtc ? new Date(trip.departureTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '-';
    const arrTime = trip.arrivalTimeUtc ? new Date(trip.arrivalTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '-';

    const vehicleName = trip.vehicleBrand && trip.vehicleModel
        ? `${esc(trip.vehicleBrand)} ${esc(trip.vehicleModel)} (${trip.vehicleSeatCapacity || trip.availableSeats || 12})`
        : esc(trip.vehiclePlateNumber) || 'BenLan Van';

    document.getElementById('ss-vehicle').textContent = vehicleName;
    document.getElementById('ss-dep').textContent = depTime;
    document.getElementById('ss-arr').textContent = arrTime;

    updateSummary();
}

function renderSeatGrid() {
    const grid = document.getElementById('ss-seat-grid');
    if (!trip) return;

    const capacity = trip.vehicleSeatCapacity || trip.availableSeats || 12;
    const cols = 3;
    const rows = Math.ceil(capacity / cols);

    let html = '';
    for (let r = 0; r < rows; r++) {
        html += '<div class="ss-seat-row">';
        for (let c = 0; c < cols; c++) {
            const seatNum = r * cols + c + 1;
            if (seatNum > capacity) {
                html += '<div class="ss-seat" style="visibility:hidden;"></div>';
                continue;
            }

            const seatId = String(seatNum);
            const isBooked = occupiedSeats.includes(seatId);
            const isSelected = selectedSeats.includes(seatId);
            let cls = 'ss-seat';
            if (isBooked) cls += ' ss-booked';
            else if (isSelected) cls += ' ss-selected';

            const onclick = isBooked ? '' : `onclick="toggleSeat('${seatId}')"`;

            html += `<div class="${cls}" ${onclick} title="Seat ${seatNum}${isBooked ? ' - Booked' : ''}">${seatNum}</div>`;
        }
        html += '</div>';
    }

    grid.innerHTML = html;
}

function toggleSeat(seatId) {
    if (occupiedSeats.includes(seatId)) return;

    const idx = selectedSeats.indexOf(seatId);
    if (idx >= 0) {
        selectedSeats.splice(idx, 1);
    } else {
        selectedSeats.push(seatId);
    }

    renderSeatGrid();
    updateSummary();
}

function updateSummary() {
    const count = selectedSeats.length;
    const price = trip ? Number(trip.basePrice || 0) : 0;
    const total = (price * count).toFixed(2);

    document.getElementById('ss-seat-count').textContent = count;
    document.getElementById('ss-total').textContent = `$${total}`;

    const btn = document.getElementById('ss-checkout-btn');
    if (btn) {
        btn.textContent = count > 0 ? `Check out, $${total}` : 'Check out, $0.00';
        btn.disabled = count === 0;
    }
}

async function checkout() {
    const firstname = document.getElementById('ss-firstname').value.trim();
    const lastname = document.getElementById('ss-lastname').value.trim();
    const phone = document.getElementById('ss-phone').value.trim();

    if (!firstname || !phone) {
        if (!firstname) shakeField('ss-firstname');
        if (!phone) shakeField('ss-phone');
        return;
    }

    if (selectedSeats.length === 0) {
        alert('Please select at least one seat.');
        return;
    }

    const btn = document.getElementById('ss-checkout-btn');
    btn.textContent = 'Processing...';
    btn.disabled = true;

    const passengerName = `${firstname} ${lastname}`.trim();
    const dto = {
        tripId: Number(tripId),
        seatsBooked: selectedSeats.length,
        unitPrice: Number(trip.basePrice || 0),
        notes: phone ? `Phone: ${phone}` : null,
        passengers: selectedSeats.map(function (seat) {
            return { passengerName, seatNumber: seat };
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
            alert('Booking failed: ' + await readError(res));
            btn.textContent = `Check out, $${(Number(trip.basePrice || 0) * selectedSeats.length).toFixed(2)}`;
            btn.disabled = false;
            return;
        }

        const booking = await res.json();
        window.location.href = `/Home/Pay?bookingId=${encodeURIComponent(booking.id)}`;
    } catch (e) {
        alert('Booking failed: ' + e.message);
        btn.textContent = `Check out, $${(Number(trip.basePrice || 0) * selectedSeats.length).toFixed(2)}`;
        btn.disabled = false;
    }
}

function shakeField(id) {
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

document.addEventListener('DOMContentLoaded', loadTrip);
