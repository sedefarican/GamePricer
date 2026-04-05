(function () {
  const $ = (sel, root = document) => root.querySelector(sel);
  const $$ = (sel, root = document) => [...root.querySelectorAll(sel)];

  let favoriteIdSet = new Set();

  function formatTry(value) {
    return formatPrice(value, "TRY");
  }

  function formatPrice(value, currency) {
    const cur = (currency || "TRY").toUpperCase();
    if (value == null || value === "") return "—";
    const n = Number(value);
    if (Number.isNaN(n)) return "—";
    const sym = cur === "TRY" ? " ₺" : " " + cur;
    return (
      new Intl.NumberFormat("tr-TR", {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(n) + sym
    );
  }

  function discountPill(pct) {
    if (pct == null || pct <= 0) return "";
    return `<span class="discount-pill" title="İndirim">-${pct}%</span>`;
  }

  function coverDiscountHtml(pct) {
    if (pct == null || pct <= 0) return "";
    return `<span class="discount-pill discount-pill--cover" title="İndirim">-${pct}%</span>`;
  }

  function gameUrl(id) {
    return `game.html?id=${id}`;
  }

  function placeholderCover(name) {
    const enc = encodeURIComponent((name || "Oyun").slice(0, 2).toUpperCase());
    return `https://placehold.co/400x250/1e3a5f/60a5fa?text=${enc}&font=source-sans-pro`;
  }

  const placeholderThumbFav =
    "https://placehold.co/56x36/1e3a5f/60a5fa?text=GP&font=source-sans-pro";

  function heartSvg() {
    return `<svg class="icon-heart" viewBox="0 0 24 24" aria-hidden="true"><path d="M20.84 4.61a5.5 5.5 0 0 0-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 0 0-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 0 0 0-7.78z"/></svg>`;
  }

  function escapeHtml(s) {
    if (!s) return "";
    const d = document.createElement("div");
    d.textContent = s;
    return d.innerHTML;
  }

  async function fetchFavoriteIds() {
    if (!localStorage.getItem("gp_token")) {
      favoriteIdSet = new Set();
      return;
    }
    try {
      const list = await api.getFavorites();
      favoriteIdSet = new Set((list || []).map((f) => String(f.gameId)));
    } catch {
      favoriteIdSet = new Set();
    }
  }

  function syncFavoriteButtons() {
    $$(".card-fav-btn[data-game-id]").forEach((btn) => {
      const id = btn.getAttribute("data-game-id");
      if (!id) return;
      const on = favoriteIdSet.has(id);
      btn.classList.toggle("is-favorite", on);
      btn.setAttribute("aria-pressed", String(on));
      btn.setAttribute("aria-label", on ? "Favorilerden çıkar" : "Favorilere ekle");
    });
  }

  function renderDealCard(g) {
    const id = String(g.id);
    const fav = favoriteIdSet.has(id);
    const img = g.coverImageUrl || placeholderCover(g.name);
    const price = formatPrice(g.lowestPrice, g.priceCurrency);
    const ph = placeholderCover(g.name);
    const disc = discountPill(g.bestDiscountPercent);
    return `
      <article class="deal-card">
        <div class="deal-card-media">
          <button type="button" class="card-fav-btn deal-fav-btn${fav ? " is-favorite" : ""}" data-game-id="${id}" aria-label="${
      fav ? "Favorilerden çıkar" : "Favorilere ekle"
    }" aria-pressed="${fav}">
            ${heartSvg()}
          </button>
          <a href="${gameUrl(g.id)}">
            <img class="thumb" src="${img}" alt="" loading="lazy" width="220" height="138"
              onerror="this.onerror=null;this.src='${ph}'" />
          </a>
        </div>
        <a href="${gameUrl(g.id)}">
          <div class="body">
            <h3>${escapeHtml(g.name)}</h3>
            <div class="price-row">${disc}<div class="price">${price}</div></div>
          </div>
        </a>
      </article>`;
  }

  function renderGameCard(g) {
    const id = String(g.id);
    const fav = favoriteIdSet.has(id);
    const img = g.coverImageUrl || placeholderCover(g.name);
    const price = formatPrice(g.lowestPrice, g.priceCurrency);
    const ph = placeholderCover(g.name);
    return `
      <article class="game-card">
        <div class="cover-wrap">
          <span class="badge">Fiyat karşılaştır</span>
          ${coverDiscountHtml(g.bestDiscountPercent)}
          <button type="button" class="card-fav-btn${fav ? " is-favorite" : ""}" data-game-id="${id}" aria-label="${
      fav ? "Favorilerden çıkar" : "Favorilere ekle"
    }" aria-pressed="${fav}">
            ${heartSvg()}
          </button>
          <a class="card-media" href="${gameUrl(g.id)}">
            <img src="${img}" alt="" loading="lazy" width="400" height="250"
              onerror="this.onerror=null;this.src='${ph}'" />
          </a>
        </div>
        <a class="card-link" href="${gameUrl(g.id)}">
          <div class="card-body">
            <h3>${escapeHtml(g.name)}</h3>
            <div class="meta-row">
              <span>En ucuz fiyat</span>
              <span class="lowest">${price}</span>
            </div>
          </div>
        </a>
      </article>`;
  }

  async function handleFavClick(ev) {
    const btn = ev.target.closest(".card-fav-btn");
    if (!btn || (!btn.closest("#dealsRow") && !btn.closest("#gamesGrid"))) return;
    ev.preventDefault();
    ev.stopPropagation();
    if (!localStorage.getItem("gp_token")) {
      window.dispatchEvent(new CustomEvent("gp-open-login"));
      return;
    }
    const gid = btn.getAttribute("data-game-id");
    if (!gid) return;
    const was = favoriteIdSet.has(gid);
    try {
      if (was) {
        await api.removeFavorite(gid);
        favoriteIdSet.delete(gid);
      } else {
        await api.addFavorite(gid);
        favoriteIdSet.add(gid);
      }
    } catch (err) {
      const msg = (err && err.message) || "";
      if (!was && msg.toLowerCase().includes("zaten")) {
        favoriteIdSet.add(gid);
      } else if (was && (msg.includes("404") || msg.toLowerCase().includes("bulunamadı"))) {
        favoriteIdSet.delete(gid);
      } else {
        window.alert(msg || "İşlem başarısız.");
        return;
      }
    }
    const on = favoriteIdSet.has(gid);
    btn.classList.toggle("is-favorite", on);
    btn.setAttribute("aria-pressed", String(on));
    btn.setAttribute("aria-label", on ? "Favorilerden çıkar" : "Favorilere ekle");
  }

  function openFavoritesModal() {
    const el = $("#modalFavorites");
    if (el) el.classList.add("is-open");
  }

  function closeFavoritesModal() {
    const el = $("#modalFavorites");
    if (el) el.classList.remove("is-open");
  }

  function renderFavRow(f) {
    const id = String(f.gameId);
    const img = f.coverImageUrl || placeholderCover(f.gameName);
    const price = formatTry(f.lowestPrice);
    return `
      <div class="favorites-row" data-fav-game-id="${id}">
        <img class="favorites-row-thumb" src="${escapeHtml(img)}" alt="" loading="lazy"
          onerror="this.onerror=null;this.src='${placeholderThumbFav}'" />
        <div class="favorites-row-info">
          <p class="favorites-row-title"><a href="${gameUrl(id)}">${escapeHtml(f.gameName)}</a></p>
          <div class="favorites-row-meta">En ucuz: ${price}</div>
        </div>
        <div class="favorites-row-actions">
          <a class="btn btn-primary" href="${gameUrl(id)}">Oyuna git</a>
          <button type="button" class="btn btn-ghost" data-remove-fav="${id}">Kaldır</button>
        </div>
      </div>`;
  }

  async function loadFavoritesModal() {
    const body = $("#favoritesListBody");
    const flash = $("#favoritesModalFlash");
    if (!body) return;
    flash.textContent = "";
    flash.className = "flash-msg";
    body.innerHTML = '<p class="loading-pulse">Yükleniyor…</p>';
    try {
      const list = await api.getFavorites();
      favoriteIdSet = new Set((list || []).map((f) => String(f.gameId)));
      syncFavoriteButtons();
      if (!list || !list.length) {
        body.innerHTML =
          '<p class="empty-state" style="padding:1.25rem;margin:0;">Henüz favori oyun yok. Kartlardaki kalbe tıklayarak ekleyin.</p>';
        return;
      }
      body.innerHTML = list.map(renderFavRow).join("");
      $$("[data-remove-fav]", body).forEach((b) =>
        b.addEventListener("click", onRemoveFromModal)
      );
    } catch (e) {
      body.innerHTML = "";
      flash.textContent = e.message || "Liste alınamadı.";
      flash.classList.add("is-error");
    }
  }

  async function onRemoveFromModal(ev) {
    const btn = ev.currentTarget;
    const id = btn.getAttribute("data-remove-fav");
    if (!id) return;
    btn.disabled = true;
    try {
      await api.removeFavorite(id);
      favoriteIdSet.delete(id);
      syncFavoriteButtons();
      const row = btn.closest(".favorites-row");
      row?.remove();
      const body = $("#favoritesListBody");
      if (body && !body.querySelector(".favorites-row")) {
        body.innerHTML =
          '<p class="empty-state" style="padding:1.25rem;margin:0;">Henüz favori oyun yok. Kartlardaki kalbe tıklayarak ekleyin.</p>';
      }
    } catch (e) {
      window.alert(e.message || "Kaldırılamadı.");
      btn.disabled = false;
    }
  }

  async function loadDealsAndGrid() {
    const dealsEl = $("#dealsRow");
    const gridEl = $("#gamesGrid");
    const errDeals = $("#dealsError");
    const errGrid = $("#gridError");

    dealsEl.innerHTML = '<p class="loading-pulse">Yükleniyor…</p>';
    gridEl.innerHTML = '<p class="loading-pulse">Yükleniyor…</p>';
    errDeals.textContent = "";
    errGrid.textContent = "";

    try {
      await fetchFavoriteIds();
      const list = await api.getPopular(1, 12);
      if (!list || !list.length) {
        dealsEl.innerHTML = "";
        gridEl.innerHTML =
          '<p class="empty-state">Henüz listelenecek oyun yok.</p>';
        return;
      }
      dealsEl.innerHTML = list.map(renderDealCard).join("");
      gridEl.innerHTML = list.map(renderGameCard).join("");
      syncFavoriteButtons();
    } catch (e) {
      dealsEl.innerHTML = "";
      gridEl.innerHTML = "";
      const msg = e.message || "Veri alınamadı.";
      errDeals.textContent = msg;
      errGrid.textContent = msg;
    }
  }

  async function runSearch(q) {
    const gridEl = $("#gamesGrid");
    const errGrid = $("#gridError");
    const sectionTitle = $("#mainSectionTitle");
    const sectionSub = $("#mainSectionSub");

    if (!q.trim()) {
      sectionTitle.textContent = "Oyun fiyatları";
      sectionSub.textContent =
        "En ucuz oyun fiyatlarını inceleyin; platformları tek ekranda karşılaştırın.";
      await loadDealsAndGrid();
      return;
    }

    sectionTitle.textContent = "Arama sonuçları";
    sectionSub.textContent = `“${escapeHtml(q)}” için sonuçlar`;
    gridEl.innerHTML = '<p class="loading-pulse">Aranıyor…</p>';
    errGrid.textContent = "";

    try {
      await fetchFavoriteIds();
      const list = await api.search(q, 1, 20);
      if (!list || !list.length) {
        gridEl.innerHTML =
          '<p class="empty-state">Bu aramaya uygun oyun bulunamadı.</p>';
        return;
      }
      gridEl.innerHTML = list.map(renderGameCard).join("");
      syncFavoriteButtons();
    } catch (e) {
      gridEl.innerHTML = "";
      errGrid.textContent = e.message || "Arama başarısız.";
    }
  }

  $("#dealsRow").addEventListener("click", handleFavClick);
  $("#gamesGrid").addEventListener("click", handleFavClick);

  $("#btnFavoritesHeader")?.addEventListener("click", async () => {
    if (!localStorage.getItem("gp_token")) {
      window.dispatchEvent(new CustomEvent("gp-open-login"));
      return;
    }
    openFavoritesModal();
    await loadFavoritesModal();
  });

  $("#modalFavorites")?.addEventListener("click", (e) => {
    if (e.target.id === "modalFavorites") closeFavoritesModal();
  });

  window.addEventListener("gp-auth-change", async () => {
    await fetchFavoriteIds();
    syncFavoriteButtons();
    const modal = $("#modalFavorites");
    if (modal && modal.classList.contains("is-open")) {
      await loadFavoritesModal();
    }
  });

  let searchTimer;
  $("#searchForm").addEventListener("submit", (ev) => {
    ev.preventDefault();
    const q = $("#searchInput").value;
    runSearch(q);
  });

  $("#searchInput").addEventListener("input", () => {
    clearTimeout(searchTimer);
    const q = $("#searchInput").value;
    searchTimer = setTimeout(() => runSearch(q), 350);
  });

  $("#navGames").classList.add("is-active");

  const startParams = new URLSearchParams(window.location.search);
  const startQ = startParams.get("q");
  if (startQ) {
    $("#searchInput").value = startQ;
    runSearch(startQ);
  } else {
    loadDealsAndGrid();
  }
})();
