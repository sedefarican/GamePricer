# GamePricer REST API Metotları

<br>

**REST API Adresi:** [Link daha sonra buraya eklenecek](https://example.com)

**API Test Videosu:** [Link daha sonra buraya eklenecek](https://example.com)

<br>

## 1. Ana Sayfada Popüler Oyunları Listeleme
- **Endpoint:** `GET /games/popular`
- **Query Parameters:**
  - `page` (integer, optional) - Sayfa numarası
  - `limit` (integer, optional) - Sayfa başına gösterilecek oyun sayısı
- **Authentication:** Gerekli değil
- **Response:** `200 OK` - Popüler oyunlar başarıyla listelendi

## 2. Oyun Sayfasını Görüntüleme
- **Endpoint:** `GET /games/{gameId}`
- **Path Parameters:**
  - `gameId` (string, required) - Oyun ID'si
- **Authentication:** Gerekli değil
- **Response:** `200 OK` - Oyun detayları başarıyla getirildi

## 3. Üye Olma
- **Endpoint:** `POST /auth/register`
- **Request Body:**
  ```json
  {
    "email": "kullanici@example.com",
    "password": "ruhi123",
    "firstName": "Sedef",
    "lastName": "Arıcan"
  }
  ```
- **Response:** `201 Created` - Kullanıcı başarıyla oluşturuldu

## 4. Giriş Yapma
- **Endpoint:** `POST /auth/login`
- **Request Body:**
```json
{
  "email": "kullanici@example.com",
  "password": "ruhi123"
}
  ```
- **Response:** `200 OK` - Giriş başarılı, erişim bilgileri döndürüldü

## 5. Şifremi Unuttum
- **Endpoint:** `POST /auth/forgot-password`
- **Request Body:**
```json
{
  "email": "kullanici@example.com"
}
  ```
- **Response:** `200 OK` - Şifre sıfırlama bağlantısı başarıyla gönderildi

## 6. Hesabı Güncelleme
- **Endpoint:** `PUT /users/{userId}`
- **Path Parameters:**
  - `userId` (string, required) - Kullanıcı ID'si
- **Request Body:**
```json
{
  "email": "yeniemail@example.com",
  "username": "yenikullaniciadi",
  "password": "ruhi123456"
}
  ```
- **Authentication:** Bearer Token gerekli
- **Response:** `200 OK` - Kullanıcı bilgileri başarıyla güncellendi

## 7. Hesabı Silme
- **Endpoint:** `DELETE /users/{userId}`
- **Path Parameters:** 
  - `userId` (string, required) - Kullanıcı ID'si
- **Authentication:** Bearer Token gerekli
- **Response:** `204 No Content` - Kullanıcı hesabı başarıyla silindi

## 8. Hesaptan Çıkış Yapma
- **Endpoint:** `POST /auth/logout`
- **Authentication:** Bearer Token gerekli
- **Response:** `200 OK` - Kullanıcı başarıyla çıkış yaptı

## 9. Oyun Arama
- **Endpoint:** `GET /games/search`
- **Query Parameters:** 
  - `q` (string, required) - Aranacak oyun adı
  - `page` (integer, optional) - Sayfa numarası
  - `limit` (integer, optional) - Sayfa başına gösterilecek sonuç sayısı
- **Authentication:** Gerekli değil
- **Response:** `200 OK` - Arama sonuçları başarıyla listelendi

## 10. Favorilere Ekleme
- **Endpoint:** `POST /favorites`
- **Request Body:** 
  ```json
  {
    "gameId": "12345"
  }
  ```
- **Authentication:** Bearer Token gerekli
- **Response:** `201 Created` - Oyun favorilere başarıyla eklendi

## 11. Favorilerden Kaldırma
- **Endpoint:** `DELETE /favorites/{gameId}`
- **Path Parameters:** 
  - `gameId` (string, required) - Favorilerden kaldırılacak oyun ID'si
- **Authentication:** Bearer Token gerekli
- **Response:** `204 No Content` - Oyun favorilerden başarıyla kaldırıldı

## 12. Favorileri Listeleme
- **Endpoint:** `GET /favorites`
- **Authentication:** Bearer Token gerekli
- **Response:** `200 OK` - Favori oyunlar başarıyla listelendi

## 13. Yorum Yapma
- **Endpoint:** `POST /games/{gameId}/comments`
- **Path Parameters:** 
  - `gameId` (string, required) - Yorum yapılacak oyun ID'si
- **Request Body:** 
  ```json
  {
    "content": "Bu oyun gerçekten çok başarılı ve keyifli."
  }
  ```
- **Authentication:** Bearer Token gerekli  
- **Response:** `201 Created` - Yorum başarıyla oluşturuldu

## 14. Yorum Silme
- **Endpoint:** `DELETE /comments/{commentId}`
- **Path Parameters:** 
  - `commentId` (string, required) - Silinecek yorum ID'si
- **Authentication:** Bearer Token gerekli
- **Response:** `204 No Content` - Yorum başarıyla silindi

## 15. Yorum Güncelleme
- **Endpoint:** `PUT /comments/{commentId}`
- **Path Parameters:** 
  - `commentId` (string, required) - Güncellenecek yorum ID'si
- **Request Body:** 
  ```json
  {
    "content": "Yorumumu güncelledim, oyun hâlâ oldukça başarılı."
  }
  ```
- **Authentication:** Bearer Token gerekli
- **Response:** `200 OK` - Yorum başarıyla güncellendi

## 16. Yorum Beğen
- **Endpoint:** `POST /comments/{commentId}/like`
- **Path Parameters:** 
  - `commentId` (string, required) - Beğenilecek yorum ID'si
- **Authentication:** Bearer Token gerekli
- **Response:** `200 OK` - Yorum başarıyla beğenildi
