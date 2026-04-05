# 🎮 GamePricer: Front-end Geliştirme Görev Listesi

[gamepricer.net](https://gamepricer.net/)

[📺 GamePricer Test Videosunu İzlemek İçin Tıklayın](https://youtu.be/-ahKVJXhZWI)

Bu döküman, GamePricer platformunun kullanıcı yönetimi ve profil sistemine ait front-end gereksinimlerini ve teknik detaylarını içerir.



## 1. Ana Sayfa ve Popüler Oyunlar (Discovery)
*Kullanıcıyı karşılayan ve en iyi fırsatları sunan vitrin.*

- **API Endpoint:** `GET /games/popular`, `GET /games/deals`
- **Görev:** Dinamik, hızlı ve görsel odaklı bir keşif ana sayfası.
- **UI Bileşenleri:**
  - **Hero Section:** Öne çıkan büyük indirim veya yeni çıkan popüler bir oyunun banner'ı.
  - **Game Card Component:** - Oyun kapak resmi (Poster/Thumbnail).
    - Oyun adı, türü (Tag) ve platform ikonları (Steam, Epic, PS, Xbox).
    - **Fiyat Alanı:** Eski fiyat (üzeri çizili) ve yeni indirimli fiyat (belirgin renk).
    - İndirim yüzdesi badge'i (Örn: -%75).
  - **Grid Layout:** Responsive 4-column (desktop) to 1-column (mobile) dizilimi.
- **Kullanıcı Deneyimi:**
  - **Infinite Scroll** veya **"Daha Fazla Yükle"** butonu.
  - Fiyata veya popülerliğe göre anlık sıralama (Sorting) butonları.
  - Skeleton screens (yükleme sırasında kart taslakları).

---

## 2. Oyun Detay Sayfası
*Oyun hakkındaki tüm teknik verilerin ve fiyat geçmişinin merkezi.*

- **API Endpoint:** `GET /games/{gameId}`
- **Görev:** Detaylı veri sunumu ve fiyat takip aksiyonları.
- **UI Bileşenleri:**
  - **Galeri/Media:** Oyun içi ekran görüntüleri veya trailer (Slider/Carousel).
  - **Fiyat Karşılaştırma Tablosu:** Farklı mağazalardaki (Steam vs Epic) anlık fiyatların listesi ve "Mağazaya Git" butonları.
  - **Fiyat Takip (Price Alert):** "Hedef fiyatıma düşünce haber ver" butonu ve modalı.
  - **Sistem Gereksinimleri:** Tab yapısında Minimum ve Önerilen gereksinimler.
  - **Oyun Açıklaması:** "Devamını Oku" özellikli metin alanı.
- **Teknik Detay:**
  - **SEO:** Oyun ismine özel dinamik `Meta Tags` ve `Structured Data` (Schema.org) kullanımı.
  - Dinamik routing (`/game/cyberpunk-2077` gibi slug yapısı).

---

## 3. Üye Olma ve Giriş (Auth)
- **API Endpoints:** `POST /auth/register`, `POST /auth/login`
- **UI Bileşenleri:**
  - Responsive kayıt ve giriş formları.
  - Şifre gücü göstergesi ve gerçek zamanlı validasyon.
  - Form submission sırasında loading state ve double-click koruması.
- **UX:** Hata durumlarında kullanıcı dostu mesajlar (Örn: "Şifre hatalı" veya "Bu email sistemde kayıtlı").

---

## 4. Kullanıcı Profil ve Favoriler Dashboard
- **API Endpoints:** `GET /users/{userId}`, `GET /users/{userId}/favorites`
- **UI Bileşenleri:**
  - **Takip Listesi:** Kullanıcının "Takip Et" dediği oyunların liste/grid görünümü.
  - **Profil Bilgileri:** Ad, Soyad, Email ve profil fotoğrafı düzenleme alanı.
  - **Bildirim Ayarları:** "İndirim olduğunda email al" toggle butonu.
- **Teknik:** `localStorage` üzerinde JWT yönetimi ve private route koruması.

---

## 5. Güvenli Hesap Silme Akışı
- **API Endpoint:** `DELETE /users/{userId}`
- **Görev:** Destructive action yönetimi.
- **Akış:**
  - "Hesabı Sil" butonu -> Onay Modalı -> Şifre Doğrulama -> Kalıcı Silme ve Logout.

---

## 🛠️ Genel Teknik Standartlar ve Optimizasyon
- **Framework:** React / TypeScript (Önerilen).
- **State Management:** Oyun listesi ve kullanıcı oturumu için `Redux Toolkit` veya `Zustand`.
- **Image Optimization:** Oyun görselleri için `next/image` benzeri lazy loading ve WebP desteği.
- **Caching:** API istekleri için `TanStack Query` (React Query) ile veri önbelleğe alma.
- **Responsive:** Tüm sayfalar için Mobile-First yaklaşımı.
- **Accessibility:** Erişilebilir formlar, doğru Heading (H1-H6) hiyerarşisi ve ARIA etiketleri.
