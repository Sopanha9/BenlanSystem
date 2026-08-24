/**
 * BENLAN LUXURY TRANSIT — BookTicket Script (bookticket.js)
 * Flatpickr search widget, real-time trip rendering, Lucide icons & seat redirection
 */

(function () {
  'use strict';

  let currentTrips = [];
  let locations = [];

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
      btn.innerHTML = '<div class="spinner-border spinner-border-sm text-dark" role="status"></div> <span>Searching...</span>';
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
        <div class="bt-loading-state luxe-card">
          <i data-lucide="compass" style="width:40px;height:40px;opacity:0.4;"></i>
          <p>${escapeHtml(message)}</p>
          <button type="button" class="btn-luxe-outline" onclick="window.location.href='/Home/BookTicket'">Clear Search Filters</button>
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
      const vehicleName = t.vehicleBrand ? `${t.vehicleBrand} ${t.vehicleModel || ''}` : 'VIP Transit Coach';
      const isLowSeat = availableSeats <= 4;

      return `
        <div class="bt-ticket-card luxe-card" data-aos="fade-up" data-aos-delay="${(idx % 4) * 60}">
          <!-- Vehicle Column -->
          <div class="bt-ticket-vehicle">
            <span class="bt-vehicle-pill"><i data-lucide="award"></i> BenLan VIP</span>
            <h3 class="bt-vehicle-name">${escapeHtml(vehicleName)}</h3>
            <span class="bt-vehicle-plate">${escapeHtml(t.vehiclePlateNumber || 'Certified Coach')}</span>
          </div>

          <!-- Timeline Schedule Column -->
          <div class="bt-ticket-schedule">
            <div class="bt-station-block">
              <span class="bt-station-time">${depTime}</span>
              <span class="bt-station-city">${escapeHtml(t.originName)}</span>
            </div>

            <div class="bt-duration-block">
              <span class="bt-duration-val">${duration}</span>
              <div class="bt-route-line">
                <span class="bt-dot"></span>
                <span class="bt-line"></span>
                <i data-lucide="chevron-right" style="width:14px;height:14px;"></i>
              </div>
            </div>

            <div class="bt-station-block right">
              <span class="bt-station-time">${arrTime}</span>
              <span class="bt-station-city">${escapeHtml(t.destinationName)}</span>
            </div>
          </div>

          <!-- Price & Action Column -->
          <div class="bt-ticket-action">
            <div class="bt-ticket-price-box">
              <span class="bt-price-sub">Starting from</span>
              <span class="bt-price-main">${price}</span>
            </div>

            <span class="bt-seats-avail ${isLowSeat ? 'low' : ''}">
              <i data-lucide="user-check"></i> ${availableSeats} seats remaining
            </span>

            <a href="/Home/SelectSeat?tripId=${t.id}" class="btn-luxe-primary btn-select-seat">
              <span>Select Seat</span>
              <i data-lucide="arrow-right"></i>
            </a>
          </div>
        </div>
      `;
    }).join('');

    if (window.lucide) {
      window.lucide.createIcons();
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
    await loadLocations();
    prefillFromUrl();

    document.getElementById('bt-search-btn')?.addEventListener('click', searchTrips);
    document.getElementById('bt-sort-select')?.addEventListener('change', function () {
      sortTrips(this.value);
    });
  });
})();
