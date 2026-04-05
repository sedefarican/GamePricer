const api = {
  base: "",

  async request(path, options = {}) {
    const headers = { Accept: "application/json", ...options.headers };
    if (options.body && !(options.body instanceof FormData)) {
      headers["Content-Type"] = "application/json";
    }
    const token = localStorage.getItem("gp_token");
    if (token) headers.Authorization = `Bearer ${token}`;

    const res = await fetch(`${this.base}${path}`, { ...options, headers });

    if (res.status === 204 || res.status === 205) {
      if (!res.ok) {
        throw new Error(`İstek başarısız (${res.status})`);
      }
      return null;
    }

    const text = await res.text();
    let data = null;
    if (text) {
      try {
        data = JSON.parse(text);
      } catch {
        data = text;
      }
    }

    if (!res.ok) {
      const msg =
        data && typeof data === "object" && data.message
          ? data.message
          : `İstek başarısız (${res.status})`;
      throw new Error(msg);
    }
    return data;
  },

  getPopular(page = 1, limit = 12) {
    return this.request(
      `/games/popular?page=${page}&limit=${limit}&live=true`
    );
  },

  search(q, page = 1, limit = 12) {
    const p = encodeURIComponent(q.trim());
    return this.request(
      `/games/search?q=${p}&page=${page}&limit=${limit}&live=true`
    );
  },

  getGame(id) {
    return this.request(`/games/${id}?live=true`);
  },

  getItadPrices(gameId) {
    return this.request(`/games/${gameId}/itad-prices`);
  },

  getComments(gameId) {
    return this.request(`/games/${gameId}/comments`);
  },

  createComment(gameId, content) {
    return this.request(`/games/${gameId}/comments`, {
      method: "POST",
      body: JSON.stringify({ content }),
    });
  },

  updateComment(commentId, content) {
    return this.request(`/comments/${commentId}`, {
      method: "PUT",
      body: JSON.stringify({ content }),
    });
  },

  deleteComment(commentId) {
    return this.request(`/comments/${commentId}`, { method: "DELETE" });
  },

  likeComment(commentId) {
    return this.request(`/comments/${commentId}/like`, { method: "POST" });
  },

  login(emailOrUsername, password) {
    return this.request("/auth/login", {
      method: "POST",
      body: JSON.stringify({ emailOrUsername, password }),
    });
  },

  register(payload) {
    return this.request("/auth/register", {
      method: "POST",
      body: JSON.stringify(payload),
    });
  },

  logout() {
    return this.request("/auth/logout", { method: "POST" });
  },

  forgotPassword(email) {
    return this.request("/auth/forgot-password", {
      method: "POST",
      body: JSON.stringify({ email }),
    });
  },

  getFavorites() {
    return this.request("/favorites");
  },

  addFavorite(gameId) {
    return this.request("/favorites", {
      method: "POST",
      body: JSON.stringify({ gameId }),
    });
  },

  removeFavorite(gameId) {
    return this.request(`/favorites/${gameId}`, { method: "DELETE" });
  },

  updateUser(userId, payload) {
    return this.request(`/users/${userId}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    });
  },

  deleteUser(userId) {
    return this.request(`/users/${userId}`, { method: "DELETE" });
  },
};

window.api = api;
