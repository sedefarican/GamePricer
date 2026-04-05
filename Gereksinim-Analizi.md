# Gereksinim Analizi 

Bu bölüm, proje gereksinimlerini ve her gereksinime karşılık gelen REST API endpointlerini içerir.

---

### 1) Ana Sayfada Popüler Oyunları Listeleme
**API Metodu:** `GET /games/popular`  
**Açıklama:** Ana sayfada popülerliğine göre sıralanmış oyunları listeler. Popülerlik; yorum sayısı, yorum beğenileri ve favori sayısı gibi metriklerden hesaplanabilir. Sonuçlar sayfalama ile dönebilir (`page`, `limit`). Giriş zorunlu değildir.

---

### 2) Oyun Sayfasını Görüntüleme (Oyun Detayı)
**API Metodu:** `GET /games/{gameId}`  
**Açıklama:** Seçilen oyunun detay sayfasını görüntüler. Oyun adı, açıklama, kategori/platform bilgileri, görseller ve ilgili istatistikler (yorum sayısı, beğeni sayısı vb.) dönebilir. Giriş zorunlu değildir.

---

### 3) Üye Olma
**API Metodu:** `POST /auth/register`  
**Açıklama:** Kullanıcıların yeni hesap oluşturarak sisteme kayıt olmasını sağlar. Kullanıcı email (veya kullanıcı adı) ve şifre belirleyerek hesap oluşturur. Başarılı kayıt sonrası kullanıcı bilgileri döndürülür veya otomatik giriş akışı uygulanabilir.

---

### 4) Giriş Yapma
**API Metodu:** `POST /auth/login`  
**Açıklama:** Kullanıcının email/kullanıcı adı ve şifre ile sisteme giriş yapmasını sağlar. Başarılı girişte JWT access token (ve isteğe bağlı refresh token) döndürülür. Güvenlik için hatalı denemeler sınırlandırılabilir.

---

### 5) Şifremi Unuttum
**API Metodu:** `POST /auth/forgot-password`  
**Açıklama:** Kullanıcının şifre sıfırlama talebi oluşturmasını sağlar. Kullanıcı email adresini girer; sistem bir şifre sıfırlama bağlantısı/tek kullanımlık kod üretir ve e-posta ile gönderir (ders projesinde e-posta yerine loglama veya “reset token döndürme” şeklinde simüle edilebilir).

---

### 6) Hesabı Güncelle (Profil Güncelleme)
**API Metodu:** `PUT /users/{userId}`  
**Açıklama:** Kullanıcının profil bilgilerini güncellemesini sağlar. Kullanıcı adı, email gibi alanları değiştirebilir. Güvenlik için giriş yapmış olmak gerekir ve kullanıcı yalnızca kendi hesabını güncelleyebilir (admin rolü hariç).

---

### 7) Hesabı Sil
**API Metodu:** `DELETE /users/{userId}`  
**Açıklama:** Kullanıcının hesabını sistemden silmesini sağlar. İşlem geri alınamaz. Güvenlik için giriş yapmak gerekir ve kullanıcı sadece kendi hesabını silebilir (admin rolü hariç). Silme işlemi “soft delete” veya “hard delete” olarak tasarlanabilir.

---

### 8) Hesaptan Çıkış Yap
**API Metodu:** `POST /auth/logout`  
**Açıklama:** Kullanıcının oturumunu sonlandırmasını sağlar. JWT kullanılıyorsa istemci token’ı siler; refresh token kullanılıyorsa sistem ilgili refresh token’ı geçersiz kılar/blacklist’e alır. Giriş yapmış olmak gerekir.

---

### 9) Oyun Arama
**API Metodu:** `GET /games/search?q={query}`  
**Açıklama:** Kullanıcının oyun adı üzerinden arama yapmasını sağlar. Arama metni (`q`) ile kısmi eşleşmeye izin verir. Sonuçlar sayfalama ile dönebilir (`page`, `limit`). Giriş zorunlu değildir.  
> Alternatif: `GET /games?q=&category=&platform=` gibi tek endpoint ile birleştirilebilir.

---

### 10) Favorilere Ekle
**API Metodu:** `POST /favorites`  
**Açıklama:** Kullanıcının seçtiği oyunu favorilerine eklemesini sağlar. İstek gövdesinde `gameId` gönderilir. Aynı oyun ikinci kez favoriye eklenemez. Güvenlik için giriş yapmak gerekir.

---

### 11) Favorilerden Kaldır
**API Metodu:** `DELETE /favorites/{gameId}`  
**Açıklama:** Kullanıcının favorilerine eklediği oyunu favorilerden kaldırmasını sağlar. Güvenlik için giriş yapmak gerekir. Kullanıcı sadece kendi favori listesini yönetebilir.

---

### 12) Favorileri Listele
**API Metodu:** `GET /favorites`  
**Açıklama:** Kullanıcının favoriye eklediği oyunları listeler. Oyunların temel bilgileri (ad, görsel, platform vb.) dönebilir. Güvenlik için giriş yapmak gerekir. Sonuçlar sayfalama ile dönebilir.

---

### 13) Yorum Yap
**API Metodu:** `POST /games/{gameId}/comments`  
**Açıklama:** Kullanıcının seçtiği oyun için yorum yazmasını sağlar. Yorum metni istek gövdesinde gönderilir. Güvenlik için giriş yapmak gerekir. Başarılı işlemde oluşturulan yorum döndürülür.

---

### 14) Yorum Sil
**API Metodu:** `DELETE /comments/{commentId}`  
**Açıklama:** Kullanıcının yazdığı yorumu silmesini sağlar. Kullanıcı yalnızca kendi yorumunu silebilir (admin/moderatör hariç). Güvenlik için giriş yapmak gerekir. Silme işlemi soft delete olarak yapılabilir (yorum “silindi” görünsün).

---

### 15) Yorum Güncelle
**API Metodu:** `PUT /comments/{commentId}`  
**Açıklama:** Kullanıcının yazdığı yorumu düzenlemesini sağlar. Yeni yorum içeriği istek gövdesinde gönderilir. Kullanıcı yalnızca kendi yorumunu güncelleyebilir (admin/moderatör hariç). Güvenlik için giriş yapmak gerekir.

---

### 16) Yorum Beğen
**API Metodu:** `POST /comments/{commentId}/like`  
**Açıklama:** Kullanıcının bir yorumu beğenmesini sağlar. Aynı kullanıcı aynı yorumu yalnızca bir kez beğenebilir. Güvenlik için giriş yapmak gerekir.  
> Opsiyonel: tekrar gönderilirse “beğeniyi kaldır” (toggle) mantığı uygulanabilir.
