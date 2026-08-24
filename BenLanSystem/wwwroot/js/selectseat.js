/**
 * BENLAN LUXURY TRANSIT — Select Seat Logic (selectseat.js)
 * Interactive seat deck mapping, live price calculation & SweetAlert2 validation
 */

(function () {
  'use strict';

  const tripId = new URLSearchParams(window.location.search).get('tripId');
  let trip = null;
  let occupiedSeats = [];
  let selectedSeats = [];

  function esc(value) {
    return String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);
  }

  async function loadTrip() {
    if (!tripId) {
      document.getElementById('ss-seat-grid').innerHTML = '<div class="ss-loading"><p>Missing trip parameter.</p></div>';
      return;
    }

    try {
      const [tripRes, seatsRes] = await Promise.all([
        fetch(`/api/Trip/${tripId}`),
        fetch(`/api/Booking/trip/${tripId}/seats`)
      ]);

      if (!tripRes.ok) {
        document.getElementById('ss-seat-grid').innerHTML = '<div class="ss-loading"><p>Trip not found.</p></div>';
        return;
      }

      trip = await tripRes.json();
      occupiedSeats = seatsRes.ok ? (await seatsRes.json()) : [];
      renderTripHeader();
      renderSeatDeck();
    } catch (e) {
      document.getElementById('ss-seat-grid').innerHTML = '<div class="ss-loading"><p>Unable to load live seat layout.</p></div>';
    }
  }

  function renderTripHeader() {
    if (!trip) return;

    document.getElementById('ss-from').textContent = trip.originName || 'Phnom Penh';
    document.getElementById('ss-to').textContent = trip.destinationName || 'Siem Reap';

    const depDate = trip.departureTimeUtc ? new Date(trip.departureTimeUtc) : null;
    if (depDate) {
      const options = { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' };
      document.getElementById('ss-date').textContent = depDate.toLocaleDateString('en-GB', options);
    }

    const depTime = trip.departureTimeUtc ? new Date(trip.departureTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '08:00 AM';
    const arrTime = trip.arrivalTimeUtc ? new Date(trip.arrivalTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '02:00 PM';

    document.getElementById('ss-time-range').textContent = `${depTime} – ${arrTime}`;
    
    const vName = trip.vehicleBrand ? `${trip.vehicleBrand} ${trip.vehicleModel || ''}` : 'VIP Luxury Coach';
    document.getElementById('ss-vehicle-type').textContent = vName;
    document.getElementById('ss-vehicle').textContent = vName;
    document.getElementById('ss-dep').textContent = depTime;
    document.getElementById('ss-arr').textContent = arrTime;

    updateSummary();
    if (window.lucide) window.lucide.createIcons();
  }

  function renderSeatDeck() {
    const grid = document.getElementById('ss-seat-grid');
    if (!trip || !grid) return;

    const capacity = Number(trip.vehicleSeatCapacity || trip.availableSeats || 12);
    // 2x2 layout with aisle (4 seats per row)
    const seatsPerRow = 4;
    const totalRows = Math.ceil(capacity / seatsPerRow);

    let html = '';
    for (let r = 0; r < totalRows; r++) {
      html += '<div class="ss-seat-row">';
      for (let c = 0; c < seatsPerRow; c++) {
        // Add aisle in middle (between seat 1 & 2)
        if (c === 2) {
          html += '<div class="ss-seat-aisle"><span class="aisle-line"></span></div>';
        }

        const seatNum = r * seatsPerRow + c + 1;
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

        const clickHandler = isBooked ? '' : `onclick="toggleSeat('${seatId}')"`;
        html += `<div class="${cls}" ${clickHandler} title="Seat ${seatNum}${isBooked ? ' (Booked)' : ''}">${seatNum}</div>`;
      }
      html += '</div>';
    }

    grid.innerHTML = html;
  }

  window.toggleSeat = function (seatId) {
    if (occupiedSeats.includes(seatId)) return;

    const idx = selectedSeats.indexOf(seatId);
    if (idx >= 0) {
      selectedSeats.splice(idx, 1);
    } else {
      selectedSeats.push(seatId);
    }

    renderSeatDeck();
    updateSummary();
  };

  function updateSummary() {
    const count = selectedSeats.length;
    const price = trip ? Number(trip.basePrice || 0) : 0;
    const total = (price * count).toFixed(2);

    document.getElementById('ss-seat-count').textContent = count;
    document.getElementById('ss-selected-list').textContent = selectedSeats.length > 0 ? selectedSeats.sort((a,b)=>Number(a)-Number(b)).join(', ') : 'None';
    document.getElementById('ss-total').textContent = `$${total}`;

    const btn = document.getElementById('ss-checkout-btn');
    if (btn) {
      btn.disabled = count === 0;
    }
  }

  window.checkout = async function () {
    const firstname = document.getElementById('ss-firstname')?.value.trim();
    const lastname = document.getElementById('ss-lastname')?.value.trim();
    const phone = document.getElementById('ss-phone')?.value.trim();

    if (!firstname || !phone) {
      window.BenLanToast?.error('Required Information', 'Please provide your name and contact phone number.');
      return;
    }

    if (selectedSeats.length === 0) {
      window.BenLanToast?.error('No Seats Selected', 'Please click on at least one available seat in the cabin.');
      return;
    }

    const btn = document.getElementById('ss-checkout-btn');
    btn.innerHTML = '<div class="spinner-border spinner-border-sm text-dark" role="status"></div> <span>Reserving seats...</span>';
    btn.disabled = true;

    const passengerName = `${firstname} ${lastname}`.trim();
    const dto = {
      tripId: Number(tripId),
      seatsBooked: selectedSeats.length,
      unitPrice: Number(trip.basePrice || 0),
      notes: phone ? `Phone: ${phone}` : null,
      passengers: selectedSeats.map(seat => ({ passengerName, seatNumber: seat }))
    };

    try {
      const res = await fetch('/api/Booking', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dto)
      });

      if (res.status === 401) {
        window.location.href = '/Account/Login';
        return;
      }

      if (!res.ok) {
        let errMessage = 'Booking submission failed.';
        try {
          const errData = await res.json();
          errMessage = errData.message || JSON.stringify(errData);
        } catch { }
        window.BenLanToast?.error('Reservation Issue', errMessage);
        btn.innerHTML = '<span>Proceed to Payment</span> <i data-lucide="credit-card"></i>';
        btn.disabled = false;
        if (window.lucide) window.lucide.createIcons();
        return;
      }

      const booking = await res.json();
      window.location.href = `/Home/Pay?bookingId=${encodeURIComponent(booking.id)}`;
    } catch (e) {
      window.BenLanToast?.error('Error', e.message);
      btn.innerHTML = '<span>Proceed to Payment</span> <i data-lucide="credit-card"></i>';
      btn.disabled = false;
      if (window.lucide) window.lucide.createIcons();
    }
  };

  document.addEventListener('DOMContentLoaded', loadTrip);
})();
