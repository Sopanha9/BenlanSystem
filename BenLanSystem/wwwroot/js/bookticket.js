/**
 * BENLAN — BookTicket Script (bookticket.js)
 * Flatpickr search widget, real-time trip rendering, Lucide icons,
 * vehicle enrichment & seat redirection
 */

(function () {
  'use strict';

  let currentTrips = [];
  let locations = [];
  let vehicles = [];
  let selectedTripId = null;

  function escapeHtml(value) {
    return String(value ?? '').replace(/[&<>"']/g, function (c) {
      return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
    });
  }

  function initFlatpickr() {
    flatpickr("#bt-input-date", {
      minDate: "today",
      dateFormat: "Y-m-d",
      defaultDate: "today",
      disableMobile: "true"
    });
  }

  async function loadLocations() {
    try {
      const res = await fetch('/api/Location');
      if (!res.ok) return;
      locations = await res.json();

      const fromList = document.getElementById('bt-location-list-from');
      const toList = document.getElementById('bt-location-list-to');
      if (fromList && toList) {
        const options = locations.map(l => `<option value="${escapeHtml(l.name)}"></option>`).join('');
        fromList.innerHTML = options;
        toList.innerHTML = options;
      }
    } catch (e) {
      console.error('Failed to load locations:', e);
    }
  }

  async function loadVehicles() {
    try {
      const res = await fetch('/api/Vehicle');
      if (!res.ok) return;
      vehicles = await res.json();
    } catch (e) {
      console.error('Failed to load vehicles:', e);
    }
  }

  function matchVehicle(trip) {
    if (!vehicles.length || !trip.vehicleInfo) return null;
    const info = String(trip.vehicleInfo).trim().toLowerCase();
    return vehicles.find(v => {
      const brandModel = v.brand && v.model ? `${v.brand} ${v.model}`.trim().toLowerCase() : '';
      const plate = (v.plateNumber || '').trim().toLowerCase();
      return (brandModel && brandModel === info) || (plate && plate === info);
    }) || null;
  }

  window.swapBtStations = function () {
    const fromInput = document.getElementById('bt-input-from');
    const toInput = document.getElementById('bt-input-to');
    if (fromInput && toInput) {
      const temp = fromInput.value;
      fromInput.value = toInput.value;
      toInput.value = temp;
    }
  };

  async function searchTrips() {
    const from = document.getElementById('bt-input-from')?.value.trim();
    const to = document.getElementById('bt-input-to')?.value.trim();
    const date = document.getElementById('bt-input-date')?.value.trim();
    const btn = document.getElementById('bt-search-btn');

    if (btn) {
      btn.innerHTML = '<div class="spinner-border spinner-border-sm" role="status"></div> <span>Searching...</span>';
      btn.disabled = true;
    }

    const params = new URLSearchParams();
    if (from) {
      const loc = locations.find(l => l.name.toLowerCase() === from.toLowerCase());
      if (loc) params.append('OriginId', loc.id);
    }
    if (to) {
      const loc = locations.find(l => l.name.toLowerCase() === to.toLowerCase());
      if (loc) params.append('DestinationId', loc.id);
    }
    if (date) params.append('DepartureDate', date);
    params.append('StatusName', 'Open');
    params.append('PageSize', '30');

    try {
      const res = await fetch(`/api/Trip?${params.toString()}`);
      if (!res.ok) {
        renderEmptyState('No trips currently found matching your search criteria.');
        return;
      }
      const data = await res.json();
      currentTrips = data.items || [];
      clearSelection();
      activateStep(2);
      renderTripList(currentTrips);
    } catch (err) {
      renderEmptyState('Failed to fetch schedule. Please try again.');
    } finally {
      if (btn) {
        btn.innerHTML = '<i data-lucide="search"></i> <span>Search Trips</span>';
        btn.disabled = false;
        if (window.lucide) window.lucide.createIcons();
      }
    }
  }

  function renderEmptyState(message) {
    const list = document.getElementById('bt-ticket-list');
    const countEl = document.getElementById('bt-results-count');
    if (list) {
      list.innerHTML = `
        <div class="cb-loading-state">
          <i data-lucide="compass" style="width:42px;height:42px;opacity:0.45;color:var(--cb-green);"></i>
          <p>${escapeHtml(message)}</p>
          <button type="button" class="cb-btn-primary" onclick="window.location.href='/Home/BookTicket'">Clear Search Filters</button>
        </div>`;
    }
    if (countEl) countEl.innerHTML = 'Showing <strong>0</strong> departures';
    if (window.lucide) window.lucide.createIcons();
  }

  function renderTripList(trips) {
    const list = document.getElementById('bt-ticket-list');
    const countEl = document.getElementById('bt-results-count');

    if (!trips.length) {
      renderEmptyState('No departures available for the selected route/date.');
      return;
    }

    if (countEl) {
      countEl.innerHTML = `Showing <strong>${trips.length}</strong> available departures`;
    }

    list.innerHTML = trips.map((t, idx) => {
      const depTime = t.departureTimeUtc ? new Date(t.departureTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '08:00 AM';
      const arrTime = t.arrivalTimeUtc ? new Date(t.arrivalTimeUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }) : '02:00 PM';

      let duration = 'Direct';
      if (t.departureTimeUtc && t.arrivalTimeUtc) {
        const mins = Math.max(0, Math.round((new Date(t.arrivalTimeUtc) - new Date(t.departureTimeUtc)) / 60000));
        const hours = Math.floor(mins / 60);
        const rest = mins % 60;
        duration = `${hours ? hours + 'h ' : ''}${rest ? rest + 'm' : ''}`.trim() || 'Direct';
      }

      const availableSeats = Number(t.availableSeats || 0);
      const price = `$${Number(t.basePrice || 0).toFixed(2)}`;
      const vehicle = matchVehicle(t);
      const vehicleName = escapeHtml(t.vehicleInfo || 'BenLan Coach');
      const imageUrl = vehicle?.imageUrl || '/designs/bookticket/car.png';
      const plate = vehicle?.plateNumber || '';
      const capacity = t.vehicleSeatCapacity ? `${t.vehicleSeatCapacity} seats` : '';
      const transmission = vehicle?.transmission || '';
      const fuelType = vehicle?.fuelType || '';
      const distance = t.distanceKm ? `${Number(t.distanceKm).toFixed(0)} km` : '';
      const isLowSeat = availableSeats <= 4;
      const isSelected = selectedTripId === t.id;

      const chips = [
        capacity ? `<span class="cb-chip"><i data-lucide="users"></i> ${escapeHtml(capacity)}</span>` : '',
        transmission ? `<span class="cb-chip"><i data-lucide="settings-2"></i> ${escapeHtml(transmission)}</span>` : '',
        fuelType ? `<span class="cb-chip"><i data-lucide="fuel"></i> ${escapeHtml(fuelType)}</span>` : '',
        distance ? `<span class="cb-chip"><i data-lucide="route"></i> ${escapeHtml(distance)}</span>` : ''
      ].filter(Boolean).join('');

      return `
        <article class="cb-car-card${isSelected ? ' cb-selected' : ''}" data-trip-id="${t.id}" data-aos="fade-up" data-aos-delay="${(idx % 4) * 60}">
          <div class="cb-car-media">
            <img src="${escapeHtml(imageUrl)}" alt="${vehicleName}" loading="lazy"
                 onerror="this.onerror=null;this.src='/designs/bookticket/car.png';" />
            ${plate ? `<span class="cb-car-plate">${escapeHtml(plate)}</span>` : ''}
            <span class="cb-check-badge"><i data-lucide="check"></i></span>
          </div>

          <div class="cb-car-body">
            <div class="cb-car-head">
              <div>
                <h3 class="cb-car-name">${vehicleName}</h3>
                <span class="cb-car-route">
                  <i data-lucide="map-pin"></i>
                  ${escapeHtml(t.originName)} &rarr; ${escapeHtml(t.destinationName)}
                </span>
              </div>
              <span class="cb-seats-pill${isLowSeat ? ' low' : ''}">
                <i data-lucide="user-check"></i> ${availableSeats} seats left
              </span>
            </div>

            ${chips ? `<div class="cb-car-chips">${chips}</div>` : ''}

            <div class="cb-car-schedule">
              <div class="cb-stop">
                <span class="cb-stop-time">${depTime}</span>
                <span class="cb-stop-city">${escapeHtml(t.originName)}</span>
              </div>

              <div class="cb-leg">
                <span class="cb-leg-duration">${duration}</span>
                <div class="cb-leg-line">
                  <span class="cb-dot"></span>
                  <span class="cb-line"></span>
                  <i data-lucide="bus-front"></i>
                  <span class="cb-line"></span>
                  <span class="cb-dot"></span>
                </div>
              </div>

              <div class="cb-stop right">
                <span class="cb-stop-time">${arrTime}</span>
                <span class="cb-stop-city">${escapeHtml(t.destinationName)}</span>
              </div>
            </div>
          </div>

          <div class="cb-car-action">
            <div class="cb-price-box">
              <span class="cb-price-label">per seat</span>
              <span class="cb-price">${price}</span>
            </div>
            <a href="/Home/SelectSeat?tripId=${t.id}" class="cb-btn-primary cb-select-btn" data-select-link>
              <span>Select Seats</span>
              <i data-lucide="arrow-right"></i>
            </a>
          </div>
        </article>
      `;
    }).join('');

    if (window.lucide) {
      window.lucide.createIcons();
    }
  }

  function selectTrip(tripId) {
    selectedTripId = selectedTripId === tripId ? null : tripId;

    document.querySelectorAll('.cb-car-card').forEach(card => {
      card.classList.toggle('cb-selected', Number(card.dataset.tripId) === selectedTripId);
    });

    updateContinueBar();
  }

  function clearSelection() {
    selectedTripId = null;
    updateContinueBar();
  }

  function updateContinueBar() {
    const bar = document.getElementById('cb-continue-bar');
    const btn = document.getElementById('cb-continue-btn');
    const routeEl = document.getElementById('cb-continue-route');
    const metaEl = document.getElementById('cb-continue-meta');
    if (!bar || !btn) return;

    const trip = currentTrips.find(t => t.id === selectedTripId);
    if (!trip) {
      bar.classList.remove('visible');
      bar.setAttribute('aria-hidden', 'true');
      return;
    }

    const vehicle = matchVehicle(trip);
    const vehicleName = trip.vehicleInfo || 'BenLan Coach';
    const depLabel = trip.departureTimeUtc
      ? new Date(trip.departureTimeUtc).toLocaleString([], { weekday: 'short', month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
      : '';

    if (routeEl) routeEl.textContent = `${vehicleName} — ${trip.originName} → ${trip.destinationName}`;
    if (metaEl) metaEl.textContent = `${depLabel} · $${Number(trip.basePrice || 0).toFixed(2)} per seat`;
    btn.href = `/Home/SelectSeat?tripId=${trip.id}`;

    bar.classList.add('visible');
    bar.setAttribute('aria-hidden', 'false');
  }

  function activateStep(step) {
    for (let i = 1; i <= 3; i++) {
      document.getElementById(`cb-step-${i}`)?.classList.toggle('active', i <= step);
    }
  }

  function sortTrips(mode) {
    const sorted = [...currentTrips];
    if (mode === 'price-asc') {
      sorted.sort((a, b) => Number(a.basePrice || 0) - Number(b.basePrice || 0));
    } else if (mode === 'price-desc') {
      sorted.sort((a, b) => Number(b.basePrice || 0) - Number(a.basePrice || 0));
    } else if (mode === 'duration') {
      sorted.sort((a, b) => Number(a.estimatedMinutes || 0) - Number(b.estimatedMinutes || 0));
    } else {
      sorted.sort((a, b) => new Date(a.departureTimeUtc) - new Date(b.departureTimeUtc));
    }
    renderTripList(sorted);
  }

  function prefillFromUrl() {
    const params = new URLSearchParams(window.location.search);
    const from = params.get('from');
    const to = params.get('to');
    const date = params.get('date');

    if (from) document.getElementById('bt-input-from').value = from;
    if (to) document.getElementById('bt-input-to').value = to;
    if (date) document.getElementById('bt-input-date').value = date;

    searchTrips();
  }

  document.addEventListener('DOMContentLoaded', async function () {
    initFlatpickr();
    await Promise.all([loadLocations(), loadVehicles()]);
    prefillFromUrl();

    document.getElementById('bt-search-btn')?.addEventListener('click', searchTrips);
    document.getElementById('bt-sort-select')?.addEventListener('change', function () {
      sortTrips(this.value);
    });

    document.getElementById('bt-ticket-list')?.addEventListener('click', function (e) {
      if (e.target.closest('[data-select-link]')) return;
      const card = e.target.closest('.cb-car-card');
      if (card) selectTrip(Number(card.dataset.tripId));
    });
  });
})();
