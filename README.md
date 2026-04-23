# Çoklu İstemci Türlerinde Kimlik Doğrulama ve Yetkilendirme Modelleri

## Ders Bilgileri

* **Ders Kodu:** TSBG-691-03
* **Program:** Siber Güvenlik (Tezsiz Yüksek Lisans)
* **Üniversite:** Ahmet Yesevi Üniversitesi

---

## Proje Açıklaması

Bu proje, farklı istemci türleri (web, mobil, masaüstü ve komut satırı uygulamaları) tarafından kullanılan web API’lerde kimlik doğrulama ve yetkilendirme modellerinin güvenlik açısından karşılaştırmalı analizini yapmak amacıyla geliştirilmiştir.

Çalışma kapsamında, yaygın olarak kullanılan kimlik doğrulama yöntemleri incelenmiş ve bu yöntemlerin farklı istemci türlerindeki davranışları ve güvenlik etkileri değerlendirilmiştir.

Proje bilinçli olarak basit tutulmuştur. Amaç üretim seviyesinde bir güvenlik sistemi geliştirmek değil, kimlik doğrulama yaklaşımlarının temel farklarını ve bazı güvenlik etkilerini anlaşılır şekilde göstermektir.

---

## Amaç

* Farklı kimlik doğrulama yöntemlerini karşılaştırmak:

  * Oturum tabanlı kimlik doğrulama (Session)
  * JSON Web Token (JWT)
  * API Anahtarı (API Key)
  * Basic Authentication
  * OAuth benzeri mock akış

* Güvenlik zafiyetlerini göstermek:

  * XSS (Siteler Arası Betik Çalıştırma)
  * CSRF (Siteler Arası İstek Sahteciliği)
  * Token Hırsızlığı
  * Replay Attack (Tekrar Oynatma Saldırısı)

* Farklı istemci türlerinin güvenlik üzerindeki etkilerini analiz etmek

---

## Proje Yapısı

```text
project-root/
│
├── api/       # .NET Web API (kimlik doğrulama sunucusu)
├── web/       # React web uygulaması
├── mobile/    # React Native mobil uygulama
├── desktop/   # Electron masaüstü uygulaması
├── cli/       # Rust komut satırı uygulaması
│
└── README.md
```

---

## Kullanılan Teknolojiler

* **Backend:** .NET Minimal API
* **Web:** React
* **Mobil:** React Native (Expo)
* **Masaüstü:** Electron
* **CLI:** Rust

---

## Kimlik Doğrulama Yöntemleri

Bu projede aşağıdaki kimlik doğrulama yöntemleri uygulanmıştır:

* Oturum tabanlı kimlik doğrulama (Session - Cookie)
* JWT tabanlı kimlik doğrulama
* API Key doğrulama
* Basic Authentication
* OAuth benzeri mock akış

API içinde kullanılan temel endpointler:

* `POST /login-session`
* `POST /login-jwt`
* `GET /protected-session`
* `GET /protected-jwt`
* `GET /protected-basic`
* `GET /protected-apikey`
* `GET /oauth-mock/authorize?username=test`
* `POST /oauth-mock/token`

---

## Güvenlik Senaryoları

Projede aşağıdaki güvenlik zafiyetleri basit senaryolarla gösterilmektedir:

* **XSS:** Token’ın istemci tarafında ele geçirilebilmesi
* **CSRF:** Oturum tabanlı sistemlerin kötüye kullanımı
* **Token Hırsızlığı:** Güvensiz saklama durumları
* **Replay Attack:** Aynı isteğin tekrar kullanılması

Demo endpointleri:

* `GET /simulate-csrf-state-change`
* `POST /simulate-token-theft`
* `POST /simulate-replay`

> Not: Bu senaryolar gerçek saldırı değil, akademik amaçlı basitleştirilmiş gösterimlerdir.

---

## Kurulum ve Çalıştırma

### 1. API

```bash
cd api
dotnet run
```

API varsayılan olarak aşağıdaki adreste çalışır:

```text
http://localhost:5000
```

### 2. Web Uygulaması

```bash
cd web
npm install
npm run dev
```

Web uygulaması varsayılan olarak aşağıdaki adreste çalışır:

```text
http://localhost:5173
```

### 3. Mobil Uygulama

```bash
cd mobile
npm install
npx expo start
```

Not: `mobile/App.js` içindeki `API_BASE` değeri çalıştırma ortamına göre değiştirilebilir.

Örnekler:

* Android emulator: `http://10.0.2.2:5000`
* iOS simulator: `http://localhost:5000`
* Fiziksel cihaz: `http://<bilgisayar-ip-adresi>:5000`

### 4. Masaüstü Uygulama

```bash
cd desktop
npm install
npm start
```

### 5. CLI

```bash
cd cli
cargo run
```

JWT almak için:

```bash
cargo run -- login-jwt
```

JWT ile korumalı endpoint çağırmak için:

```bash
cargo run -- call-jwt "<JWT_TOKEN>"
```

API Key ile korumalı endpoint çağırmak için:

```bash
cargo run -- call-apikey
```

---

## Test Kullanıcısı

```text
kullanıcı adı: test
şifre: test123
API key: demo-api-key-123
```

---

## Akademik Bağlam

Bu repo aşağıdaki başlıklı akademik çalışma kapsamında geliştirilmiştir:

**“Farklı İstemci Türleri Tarafından Kullanılan Web API’lerde Kimlik Doğrulama ve Yetkilendirme Modellerinin Güvenlik Açısından Karşılaştırmalı Analizi”**

Bu proje, teorik bilgilerin pratik olarak gösterilmesini amaçlamaktadır.

---

## Notlar

* Bu proje akademik amaçlıdır
* Üretim ortamı için uygun değildir
* Amaç, kavramların anlaşılması ve karşılaştırılmasıdır
* Session bilgisi bellek üzerinde tutulmaktadır
* JWT imzalama ve doğrulama demo amacıyla basit şekilde yapılmaktadır
* API key hardcoded olarak tanımlanmıştır
* CORS demo kolaylığı için açık tutulmuştur
* CSRF koruması bilerek eklenmemiştir
* Web uygulamasında JWT’nin `localStorage` içinde tutulması XSS etkisini göstermek içindir

---

## Hazırlayan

Furkan Cemal Çalışkan

---

## Lisans

Bu proje akademik kullanım amacıyla geliştirilmiştir.
