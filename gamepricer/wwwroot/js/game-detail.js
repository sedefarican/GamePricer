(function () {
  function $(sel, root = document) {
    return root.querySelector(sel);
  }

  function $$(sel, root = document) {
    return [...root.querySelectorAll(sel)];
  }

  function formatTry(value, currency) {
    const cur = currency || "TRY";
    if (value == null) return "—";
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

  function escapeHtml(s) {
    if (!s) return "";
    const d = document.createElement("div");
    d.textContent = s;
    return d.innerHTML;
  }

  function safeStoreUrl(u) {
    if (!u || typeof u !== "string") return null;
    try {
      const x = new URL(u);
      if (x.protocol === "http:" || x.protocol === "https:") return x.href;
    } catch {
      /* ignore */
    }
    return null;
  }

  function placeholderCover(name) {
    const enc = encodeURIComponent((name || "Oyun").slice(0, 2).toUpperCase());
    return `https://placehold.co/320x427/1e3a5f/60a5fa?text=${enc}&font=source-sans-pro`;
  }

  function getQueryId() {
    const p = new URLSearchParams(window.location.search);
    const id = p.get("id");
    return id && /^[0-9a-fA-F-]{36}$/.test(id) ? id : null;
  }

  /** Eski önbelleğe alınmış api.js için: getItadPrices yoksa doğrudan request kullan. */
  function fetchItadLivePrices(gameId) {
    if (typeof api.getItadPrices === "function") {
      return api.getItadPrices(gameId);
    }
    return api.request(`/games/${gameId}/itad-prices`);
  }

  function readCurrentUserId() {
    try {
      const raw = localStorage.getItem("gp_user");
      const u = raw ? JSON.parse(raw) : null;
      return u && u.userId != null ? String(u.userId) : null;
    } catch {
      return null;
    }
  }

  function formatCommentDate(iso) {
    if (!iso) return "";
    try {
      return new Date(iso).toLocaleString("tr-TR", {
        dateStyle: "medium",
        timeStyle: "short",
      });
    } catch {
      return "";
    }
  }

  function commentsPanelHtml() {
    return `
      <section class="comments-panel" id="commentsPanel">
        <h2>Yorumlar</h2>
        <div id="commentComposerWrap"></div>
        <p id="commentsFlash" class="flash-msg"></p>
        <ul class="comments-list" id="commentsList" aria-live="polite"></ul>
      </section>`;
  }

  function renderComposer() {
    const wrap = $("#commentComposerWrap");
    if (!wrap) return;
    const uid = readCurrentUserId();
    if (uid) {
      wrap.innerHTML = `
        <form id="formNewComment" class="comment-composer">
          <label class="sr-only" for="newCommentText">Yorumunuz</label>
          <textarea id="newCommentText" class="comment-textarea" rows="3" maxlength="1000" minlength="2" required placeholder="Bu oyun hakkında düşüncelerinizi yazın…"></textarea>
          <div class="comment-composer-actions">
            <button type="submit" class="btn btn-primary">Yorum yap</button>
          </div>
        </form>`;
    } else {
      wrap.innerHTML = `
        <p class="comments-login-hint">
          Yorum yapmak için
          <button type="button" class="link-like" id="btnCommentLogin">giriş yapın</button>.
        </p>`;
      $("#btnCommentLogin")?.addEventListener("click", () => {
        window.dispatchEvent(new CustomEvent("gp-open-login"));
      });
    }
  }

  function renderCommentItem(c) {
    const cid = String(c.id);
    const mine = readCurrentUserId() === String(c.userId);
    const dateStr = formatCommentDate(c.createdAt);
    const updated =
      c.updatedAt && c.updatedAt !== c.createdAt
        ? ` · düzenlendi: ${formatCommentDate(c.updatedAt)}`
        : "";
    const actions = mine
      ? `<div class="comment-owner-actions">
          <button type="button" class="link-like comment-edit-open" data-comment-edit="${cid}">Düzenle</button>
          <button type="button" class="link-like comment-delete" data-comment-delete="${cid}">Sil</button>
        </div>`
      : "";
    return `
      <li class="comment-item" data-comment-id="${cid}">
        <div class="comment-view">
          <div class="comment-head">
            <span class="comment-author">${escapeHtml(c.username || "")}</span>
            <span class="comment-date">${escapeHtml(dateStr)}${escapeHtml(updated)}</span>
          </div>
          <p class="comment-body">${escapeHtml(c.content || "")}</p>
          <div class="comment-toolbar">
            <button type="button" class="btn btn-ghost comment-like-btn" data-comment-like="${cid}" aria-label="Beğen">
              <span class="comment-like-icon" aria-hidden="true">👍</span>
              <span>Beğen</span>
              <span class="comment-like-count">(${Number(c.likeCount) || 0})</span>
            </button>
            ${actions}
          </div>
        </div>
        <div class="comment-edit-wrap" hidden>
          <label class="sr-only" for="edit-ta-${cid}">Yorumu düzenle</label>
          <textarea id="edit-ta-${cid}" class="comment-textarea" rows="3" maxlength="1000" minlength="2"></textarea>
          <div class="comment-edit-actions">
            <button type="button" class="btn btn-primary comment-save-edit" data-comment-save="${cid}">Kaydet</button>
            <button type="button" class="btn btn-ghost comment-cancel-edit" data-comment-cancel="${cid}">İptal</button>
          </div>
        </div>
      </li>`;
  }

  async function loadCommentsList(gameId) {
    const listEl = $("#commentsList");
    const flash = $("#commentsFlash");
    if (!listEl) return;
    flash.textContent = "";
    flash.className = "flash-msg";
    listEl.innerHTML = '<li class="comments-loading">Yorumlar yükleniyor…</li>';
    try {
      const items = await api.getComments(gameId);
      if (!items || !items.length) {
        listEl.innerHTML = '<li class="comments-empty">Henüz yorum yok. İlk yorumu siz yazın.</li>';
        return;
      }
      listEl.innerHTML = items.map(renderCommentItem).join("");
    } catch (e) {
      listEl.innerHTML = "";
      flash.textContent = e.message || "Yorumlar yüklenemedi.";
      flash.classList.add("is-error");
    }
  }

  function bindCommentsEvents(gameId) {
    const root = $("#detailRoot");
    if (!root || root.dataset.commentsBound === "1") return;
    root.dataset.commentsBound = "1";

    root.addEventListener("submit", async (ev) => {
      const form = ev.target;
      if (form.id !== "formNewComment") return;
      ev.preventDefault();
      const flash = $("#commentsFlash");
      const ta = $("#newCommentText");
      const text = (ta && ta.value.trim()) || "";
      if (text.length < 2) return;
      flash.textContent = "";
      flash.className = "flash-msg";
      try {
        await api.createComment(gameId, text);
        ta.value = "";
        await loadCommentsList(gameId);
      } catch (e) {
        flash.textContent = e.message || "Yorum gönderilemedi.";
        flash.classList.add("is-error");
      }
    });

    root.addEventListener("click", async (ev) => {
      const likeBtn = ev.target.closest("[data-comment-like]");
      if (likeBtn) {
        ev.preventDefault();
        if (!localStorage.getItem("gp_token")) {
          window.dispatchEvent(new CustomEvent("gp-open-login"));
          return;
        }
        const cid = likeBtn.getAttribute("data-comment-like");
        try {
          await api.likeComment(cid);
          await loadCommentsList(gameId);
        } catch (e) {
          window.alert(e.message || "Beğeni eklenemedi.");
        }
        return;
      }

      const delBtn = ev.target.closest("[data-comment-delete]");
      if (delBtn) {
        const cid = delBtn.getAttribute("data-comment-delete");
        if (!window.confirm("Bu yorumu silmek istediğinize emin misiniz?")) return;
        try {
          await api.deleteComment(cid);
          await loadCommentsList(gameId);
        } catch (e) {
          window.alert(e.message || "Silinemedi.");
        }
        return;
      }

      const editOpen = ev.target.closest("[data-comment-edit]");
      if (editOpen) {
        const cid = editOpen.getAttribute("data-comment-edit");
        const item = root.querySelector(`[data-comment-id="${cid}"]`);
        if (!item) return;
        const view = item.querySelector(".comment-view");
        const editWrap = item.querySelector(".comment-edit-wrap");
        const body = item.querySelector(".comment-body");
        const ta = item.querySelector(`#edit-ta-${cid}`);
        if (ta && body) ta.value = body.textContent || "";
        if (view) view.hidden = true;
        if (editWrap) editWrap.hidden = false;
        ta?.focus();
        return;
      }

      const cancelBtn = ev.target.closest("[data-comment-cancel]");
      if (cancelBtn) {
        const cid = cancelBtn.getAttribute("data-comment-cancel");
        const item = root.querySelector(`[data-comment-id="${cid}"]`);
        if (!item) return;
        item.querySelector(".comment-view").hidden = false;
        item.querySelector(".comment-edit-wrap").hidden = true;
        return;
      }

      const saveBtn = ev.target.closest("[data-comment-save]");
      if (saveBtn) {
        const cid = saveBtn.getAttribute("data-comment-save");
        const item = root.querySelector(`[data-comment-id="${cid}"]`);
        if (!item) return;
        const ta = item.querySelector(`#edit-ta-${cid}`);
        const text = (ta && ta.value.trim()) || "";
        if (text.length < 2) {
          window.alert("Yorum en az 2 karakter olmalı.");
          return;
        }
        saveBtn.disabled = true;
        try {
          await api.updateComment(cid, text);
          item.querySelector(".comment-view").hidden = false;
          item.querySelector(".comment-edit-wrap").hidden = true;
          await loadCommentsList(gameId);
        } catch (e) {
          window.alert(e.message || "Güncellenemedi.");
        } finally {
          saveBtn.disabled = false;
        }
      }
    });
  }

  function initCommentsSection(gameId) {
    renderComposer();
    loadCommentsList(gameId);
    bindCommentsEvents(gameId);
  }

  window.addEventListener("gp-auth-change", () => {
    const id = getQueryId();
    if (!id || !$("#commentsPanel")) return;
    renderComposer();
    loadCommentsList(id);
  });

  async function load() {
    const id = getQueryId();
    const root = $("#detailRoot");
    if (!id) {
      root.innerHTML =
        '<p class="error-state">Geçersiz veya eksik oyun bağlantısı.</p>';
      return;
    }

    root.innerHTML = '<p class="loading-pulse">Oyun yükleniyor…</p>';

    try {
      const [g, itad] = await Promise.all([
        api.getGame(id),
        fetchItadLivePrices(id).catch(() => null),
      ]);
      const img = g.coverImageUrl || placeholderCover(g.name);
      const dbPrices = (g.prices || []).slice().sort((a, b) => a.price - b.price);
      const itadDeals = (itad && itad.deals) || [];
      const useItad = itadDeals.length > 0;

      function rowHtmlStore(name, price, currency, discPct, productUrl) {
        const href = safeStoreUrl(productUrl);
        const link = href
          ? `<a href="${escapeHtml(href)}" target="_blank" rel="noopener noreferrer">Mağazaya git</a>`
          : "—";
        const disc =
          discPct != null && discPct > 0 ? "%" + discPct : "—";
        return `
        <tr>
          <td><strong>${escapeHtml(name || "")}</strong></td>
          <td class="num">${formatTry(price, currency)}</td>
          <td class="discount">${disc}</td>
          <td>${link}</td>
        </tr>`;
      }

      let priceTbody = "";
      let priceLead = "";
      let priceSub = "";
      let storeCountLabel = "";

      if (useItad) {
        const sorted = itadDeals.slice().sort((a, b) => a.price - b.price);
        priceTbody = sorted
          .map((p) =>
            rowHtmlStore(p.shopName, p.price, p.currency, p.cutPercent, p.productUrl)
          )
          .join("");
        priceLead =
          "Tek tabloda gösterilen fiyatlar <a href=\"https://isthereanydeal.com/\" target=\"_blank\" rel=\"noopener noreferrer\">IsThereAnyDeal</a> API ile güncellenir; bölge ayarınız sunucudaki ITAD ülke koduna bağlıdır.";
        priceSub = `Eşleşen oyun: ${escapeHtml(itad.matchedTitle || g.name)}`;
        storeCountLabel = `${itadDeals.length} mağaza`;
      } else {
        priceTbody = dbPrices
          .map((p) =>
            rowHtmlStore(
              p.platformName,
              p.price,
              p.currency,
              p.discountRate,
              p.productUrl
            )
          )
          .join("");
        if (dbPrices.length) {
          priceLead =
            "Bu oyun için canlı mağaza verisi alınamadı. Aşağıdaki satırlar yalnızca uygulama veritabanındaki kayıtlardır.";
          storeCountLabel = `${dbPrices.length} kayıtlı fiyat`;
        } else {
          priceLead =
            "Ne canlı mağaza fiyatı ne de veritabanında gösterilecek geçerli fiyat bulundu. Oyun adının IsThereAnyDeal ile eşleştiğinden emin olun veya daha sonra tekrar deneyin.";
          storeCountLabel = "Fiyat yok";
        }
      }

      const priceTableBlock = priceTbody
        ? `<div class="price-table-wrap">
            <table class="price-table">
              <thead><tr><th>Mağaza / platform</th><th>Fiyat</th><th>İndirim</th><th>Bağlantı</th></tr></thead>
              <tbody>${priceTbody}</tbody>
            </table>
          </div>`
        : `<p class="empty-state">Gösterilecek fiyat satırı yok.</p>`;

      root.innerHTML = `
        <a class="back-link" href="index.html">← Ana sayfaya dön</a>
        <div class="detail-hero">
          <div class="detail-cover">
            <img src="${img}" alt="" width="320" height="427"
              onerror="this.onerror=null;this.src='${placeholderCover(g.name)}'" />
          </div>
          <div class="detail-info">
            <h1>${escapeHtml(g.name)}</h1>
            <div class="detail-meta">
              <span>${escapeHtml(storeCountLabel)}</span>
              ${g.developer ? `<span>Geliştirici: ${escapeHtml(g.developer)}</span>` : ""}
              ${g.publisher ? `<span>Yayıncı: ${escapeHtml(g.publisher)}</span>` : ""}
            </div>
            <div class="chips">
              ${(g.categories || []).map((c) => `<span class="chip">${escapeHtml(c)}</span>`).join("")}
            </div>
            <p class="detail-desc">${escapeHtml(g.description || "")}</p>
          </div>
        </div>
        <section class="price-panel">
          <h2>Fiyat karşılaştırması</h2>
          <p class="price-panel-lead">${priceLead}</p>
          ${priceSub ? `<p class="price-panel-lead subtle">${priceSub}</p>` : ""}
          ${priceTableBlock}
        </section>
        ${commentsPanelHtml()}`;

      root.dataset.commentsBound = "0";
      initCommentsSection(id);
    } catch (e) {
      root.innerHTML = `<p class="error-state">${escapeHtml(e.message || "Oyun yüklenemedi.")}</p>
        <p style="text-align:center;margin-top:1rem;"><a href="index.html">Ana sayfa</a></p>`;
    }
  }

  document.addEventListener("DOMContentLoaded", load);
})();
