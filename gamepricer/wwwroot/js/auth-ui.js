(function () {
  const tokenKey = "gp_token";
  const userKey = "gp_user";

  function $(sel) {
    return document.querySelector(sel);
  }

  function $$(sel) {
    return [...document.querySelectorAll(sel)];
  }

  function readUser() {
    try {
      const raw = localStorage.getItem(userKey);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }

  function setSession(token, user) {
    if (token) localStorage.setItem(tokenKey, token);
    else localStorage.removeItem(tokenKey);
    if (user) localStorage.setItem(userKey, JSON.stringify(user));
    else localStorage.removeItem(userKey);
    updateHeader();
    closeUserMenu();
    window.dispatchEvent(new CustomEvent("gp-auth-change"));
  }

  function updateHeader() {
    const user = readUser();
    const guest = $("#authGuest");
    const userEl = $("#authUser");
    const nameEl = $("#authUsername");
    if (!guest || !userEl) return;
    if (user && user.username) {
      guest.hidden = true;
      userEl.hidden = false;
      if (nameEl) nameEl.textContent = user.username;
    } else {
      guest.hidden = false;
      userEl.hidden = true;
    }
  }

  function closeUserMenu() {
    const dd = $("#userMenuDropdown");
    const bt = $("#btnUserMenu");
    if (dd) dd.classList.remove("is-open");
    if (bt) bt.setAttribute("aria-expanded", "false");
  }

  function toggleUserMenu() {
    const dd = $("#userMenuDropdown");
    const bt = $("#btnUserMenu");
    if (!dd || !bt) return;
    const willOpen = !dd.classList.contains("is-open");
    dd.classList.toggle("is-open", willOpen);
    bt.setAttribute("aria-expanded", String(willOpen));
  }

  function openModal(id) {
    const el = $(id);
    if (el) {
      el.classList.add("is-open");
      el.querySelectorAll(".flash-msg").forEach((n) => {
        n.textContent = "";
        n.className = "flash-msg";
      });
    }
  }

  function closeModal(id) {
    const el = $(id);
    if (el) el.classList.remove("is-open");
  }

  function requireUserId() {
    const u = readUser();
    const id = u && u.userId;
    if (!id) return null;
    return id;
  }

  document.addEventListener("DOMContentLoaded", () => {
    updateHeader();

    window.addEventListener("gp-open-login", () => {
      closeUserMenu();
      openModal("#modalLogin");
    });

    $("#btnUserMenu")?.addEventListener("click", (e) => {
      e.stopPropagation();
      toggleUserMenu();
    });

    document.addEventListener("click", () => {
      closeUserMenu();
    });

    $("#userMenuWrap")?.addEventListener("click", (e) => {
      e.stopPropagation();
    });

    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape") closeUserMenu();
    });

    $("#menuEditProfile")?.addEventListener("click", () => {
      closeUserMenu();
      const uid = requireUserId();
      const u = readUser();
      if (!uid || !u) {
        openModal("#modalLogin");
        return;
      }
      $("#editEmail").value = u.email || "";
      $("#editUsername").value = u.username || "";
      $("#editPassword").value = "";
      openModal("#modalEditProfile");
    });

    $("#menuDeleteAccount")?.addEventListener("click", () => {
      closeUserMenu();
      if (!requireUserId()) {
        openModal("#modalLogin");
        return;
      }
      openModal("#modalDeleteAccount");
    });

    $("#btnLogin")?.addEventListener("click", () => openModal("#modalLogin"));
    $("#btnRegister")?.addEventListener("click", () => openModal("#modalRegister"));

    $("#btnOpenForgotPassword")?.addEventListener("click", () => {
      closeModal("#modalLogin");
      $("#forgotEmail").value = "";
      openModal("#modalForgotPassword");
    });

    $("#btnForgotBackLogin")?.addEventListener("click", () => {
      closeModal("#modalForgotPassword");
      openModal("#modalLogin");
    });

    $("#btnLogout")?.addEventListener("click", async () => {
      closeUserMenu();
      try {
        if (localStorage.getItem(tokenKey)) await api.logout();
      } catch {
        /* ignore */
      }
      setSession(null, null);
    });

    $$("[data-close-modal]").forEach((btn) => {
      btn.addEventListener("click", () => {
        const id = btn.getAttribute("data-close-modal");
        if (id) closeModal(id);
      });
    });

    function bindOverlayClose(modalId) {
      const overlay = $(modalId);
      overlay?.addEventListener("click", (e) => {
        if (e.target === overlay) closeModal(modalId);
      });
    }

    bindOverlayClose("#modalLogin");
    bindOverlayClose("#modalRegister");
    bindOverlayClose("#modalForgotPassword");
    bindOverlayClose("#modalEditProfile");
    bindOverlayClose("#modalDeleteAccount");

    $("#formLogin")?.addEventListener("submit", async (e) => {
      e.preventDefault();
      const flash = $("#loginFlash");
      flash.textContent = "";
      flash.className = "flash-msg";
      const u = $("#loginUser").value;
      const p = $("#loginPass").value;
      try {
        const res = await api.login(u, p);
        setSession(res.token, {
          userId: res.userId,
          username: res.username,
          email: res.email,
        });
        closeModal("#modalLogin");
      } catch (err) {
        flash.textContent = err.message || "Giriş başarısız.";
        flash.classList.add("is-error");
      }
    });

    $("#formForgotPassword")?.addEventListener("submit", async (e) => {
      e.preventDefault();
      const flash = $("#forgotFlash");
      flash.textContent = "";
      flash.className = "flash-msg";
      const email = $("#forgotEmail").value.trim();
      try {
        const res = await api.forgotPassword(email);
        const msg =
          (res && res.message) ||
          "İsteğiniz alındı. E-postanızı kontrol edin.";
        flash.textContent = msg;
        flash.classList.add("is-ok");
      } catch (err) {
        flash.textContent = err.message || "İşlem başarısız.";
        flash.classList.add("is-error");
      }
    });

    $("#formEditProfile")?.addEventListener("submit", async (e) => {
      e.preventDefault();
      const flash = $("#editProfileFlash");
      flash.textContent = "";
      flash.className = "flash-msg";
      const uid = requireUserId();
      if (!uid) {
        flash.textContent = "Oturum bilgisi eksik. Çıkış yapıp tekrar giriş yapın.";
        flash.classList.add("is-error");
        return;
      }
      const payload = {
        email: $("#editEmail").value.trim(),
        username: $("#editUsername").value.trim(),
      };
      const pw = $("#editPassword").value;
      if (pw.length > 0) {
        if (pw.length < 6) {
          flash.textContent = "Yeni şifre en az 6 karakter olmalı.";
          flash.classList.add("is-error");
          return;
        }
        payload.password = pw;
      }
      try {
        const res = await api.updateUser(uid, payload);
        const token = localStorage.getItem(tokenKey);
        setSession(token, {
          userId: res.userId != null ? res.userId : uid,
          username: res.username,
          email: res.email,
        });
        flash.textContent = (res && res.message) || "Bilgiler güncellendi.";
        flash.classList.add("is-ok");
        $("#editPassword").value = "";
        setTimeout(() => closeModal("#modalEditProfile"), 900);
      } catch (err) {
        flash.textContent = err.message || "Güncelleme başarısız.";
        flash.classList.add("is-error");
      }
    });

    $("#btnConfirmDeleteAccount")?.addEventListener("click", async () => {
      const flash = $("#deleteAccountFlash");
      const btn = $("#btnConfirmDeleteAccount");
      flash.textContent = "";
      flash.className = "flash-msg";
      const uid = requireUserId();
      if (!uid) {
        flash.textContent = "Oturum bilgisi eksik. Çıkış yapıp tekrar giriş yapın.";
        flash.classList.add("is-error");
        return;
      }
      btn.disabled = true;
      try {
        await api.deleteUser(uid);
        closeModal("#modalDeleteAccount");
        setSession(null, null);
      } catch (err) {
        flash.textContent = err.message || "Hesap silinemedi.";
        flash.classList.add("is-error");
      } finally {
        btn.disabled = false;
      }
    });

    $("#formRegister")?.addEventListener("submit", async (e) => {
      e.preventDefault();
      const flash = $("#registerFlash");
      flash.textContent = "";
      flash.className = "flash-msg";
      const payload = {
        firstName: $("#regFirst").value.trim(),
        lastName: $("#regLast").value.trim(),
        username: $("#regUser").value.trim(),
        email: $("#regEmail").value.trim(),
        password: $("#regPass").value,
      };
      try {
        const res = await api.register(payload);
        setSession(res.token, {
          userId: res.userId,
          username: res.username,
          email: res.email,
        });
        closeModal("#modalRegister");
      } catch (err) {
        flash.textContent = err.message || "Kayıt başarısız.";
        flash.classList.add("is-error");
      }
    });
  });
})();
