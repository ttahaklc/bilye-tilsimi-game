# Bilye Tılsımı – App Store Yükleme Rehberi

SAP benzetmesiyle: Bundle ID = sistem kimliği, Build numarası = transport numarası, TestFlight = QAS, App Store yayını = PRD'ye transport.

Bu pakette olanlar: `01-Logo` (ikon, logo, Google Play görselleri), `02-Screenshots` (iPhone 6.9", 6.5", iPad 13"; tr/en/es/fr/zh), `03-Metadata` (7 dil, kopyala-yapıştır), `04-Site` (gizlilik + destek sayfaları), bu rehber.

## 0. Önce yapılacaklar (bunlar sende, ben yapamam)
1. **Destek e-postası**: `04-Site` içindeki tüm .html dosyalarında `ADD_YOUR_SUPPORT_EMAIL_HERE` yazan yeri gerçek e-postanla değiştir (6 dosya). Apple bu adresi destek/gizlilik sayfasında görmek ister.
2. **Siteyi yayınla** (ücretsiz): GitHub'da yeni public repo aç (örn. `bilye-tilsimi`) → `04-Site` içeriğini yükle → Settings > Pages > Branch: main / root. Adresin `https://KULLANICIADIN.github.io/bilye-tilsimi/` olur.
   - Privacy Policy URL: `.../privacy.html`
   - Support URL: `.../support.html`
   - Marketing URL (opsiyonel): `.../index.html`
3. Apple Developer hesabın aktif olmalı (99 $/yıl).

## 1. Xcode: imzalama ve Archive
1. `/Users/tahakilic/BilyeTilsimi-iOS/Unity-iPhone.xcodeproj` dosyasını aç.
2. Target **Unity-iPhone** > Signing & Capabilities > "Automatically manage signing" açık, Team = kendi hesabın.
3. Bundle Identifier: `com.tahakilic.bilyetilsimi` (App Store Connect'te aynısını kullanacaksın; sonradan değişmez).
4. General > Version **1.0.0**, Build **1**. (Her yeni yüklemede Build'i artır: 2, 3 …)
5. Cihaz olarak **Any iOS Device (arm64)** seç → Product > Archive.
6. Organizer açılınca Distribute App > App Store Connect > Upload. Export compliance Info.plist'te hazır (şifreleme muafiyeti), soru çıkmaz.

## 2. App Store Connect: uygulama kaydı
appstoreconnect.apple.com > Apps > + > New App
- Platform: iOS · Ad: `Bilye Tılsımı: Zincir Bulmaca` (İngilizce: `Bilye Tılsımı: Marble Shooter`) · Primary language: Turkish · Bundle ID: seç · SKU: `bilyetilsimi001`
- Kategori: **Games > Puzzle**, ikincil: Games > Casual
- Fiyat: Free (veya istediğin kademe) · Availability: tüm ülkeler
- Her dil için: `03-Metadata/<dil>/` içindeki name, subtitle, promotional_text, description, keywords, release_notes dosyalarını yapıştır (limitler kontrol edildi).
  - Dil ekleme: App Information > Localizable Information > + ile en-US, zh-Hans, hi, es-ES, ar-SA, fr-FR ekle.
- Ekran görüntüleri: Version sayfasında her dil için sırayla yükle (01…06 sırasıyla).
  - iPhone 6.9" → `iPhone-6.9/<dil>/` · iPhone 6.5" → `iPhone-6.5/<dil>/` · iPad 13" → `iPad-13/<dil>/`
  - Hindi (hi) ve Arapça (ar-SA) için ayrı görsel üretmedim (konteynerde bu yazı tipleri yok); bu dillerde "Use screenshots from primary language" benzeri seçenek yok, o yüzden bu iki dile **en** klasörünü yükle (ya da bu iki dili hiç ekleme).
- Sürüm notları, Promosyon metni (170 karakter, istediğin zaman değiştirilebilir) hazır.
- İkon: Build'in içinden gelir (1024 ikon projeye gömülü), ayrıca yüklemen gerekmez. Yedek: `01-Logo/AppIcon-1024.png`.

## 3. App Privacy ve diğer formlar
- **App Privacy**: "Data Not Collected" (oyun hiçbir veri toplamıyor, reklam/analitik SDK yok). Privacy Policy URL'ini gir.
- **Age Rating**: tüm sorulara "None" → 4+.
- **Content Rights**: üçüncü taraf içerik yok (görseller ve sesler kodla üretildi; yazı tipleri açık lisans: Cinzel, Manrope, Noto – OFL). Bunu "Hayır, üçüncü taraf içerik içermiyor" olarak işaretlemek yeterli.
- **Copyright**: `2026 Taha Kılıç`
- **Export Compliance**: Info.plist'te `ITSAppUsesNonExemptEncryption = false` ayarlı.
- **EU DSA Trader Status** (AB'de yayın için zorunlu): Account > Business > "Trader / Non-trader" seç. Ticari amaçla (para kazanmak üzere) yayınlıyorsan *Trader* seçmen ve adres/telefon/e-posta vermen gerekir; bu bilgiler AB mağazasında herkese görünür. Vermek istemezsen uygulamayı AB ülkelerinden çıkarabilirsin. Hangisinin sana uyduğu hukuki bir karar; ben tavsiye veremem.
- **App Review Information**: Giriş yok, hesap yok. Not: "No login, fully offline, no in-app purchases, no ads. Game starts directly on the menu." İletişim: ad, telefon, e-posta.

## 4. TestFlight (QAS) → App Store (PRD)
1. Build yüklendikten sonra 10-30 dk "Processing" olur; mail gelir.
2. TestFlight > Internal Testing: kendini ekle, iPhone'a TestFlight'tan kur, **gerçek cihazda** en az şunları dene: titreşim, kilit/ana ekran dönüşünde otomatik duraklatma, dil değiştirme (özellikle Arapça/Hindi), Dial/Direct Aim, ses ve müzik, düşük pil modu.
3. Version sayfasında Build'i seç → Add for Review → Submit. İnceleme genelde 24-48 saat.
4. Release: "Manually" seçersen onay sonrası sen basarsın; "Phased release" ilk 7 günde kademeli dağıtır.

## 5. Red sebepleri kontrol listesi
- Privacy URL / Support URL açılmıyor → siteyi yayınladığından emin ol.
- Ekran görüntüsü yanlış boyut → `02-Screenshots` boyutları Apple'ın zorunlu boyutlarıdır.
- Metinde başka marka adı (Zuma vb.) → metinlerde yok; keywords'e de ekleme.
- Çökme → TestFlight'ta cihazda test et.
- "Minimum functionality" → oyun 100 bölüm + 3 sonsuz harita içerir, sorun beklenmez.

## 6. Android / Google Play (sonra)
- `01-Logo/Android-GooglePlay/`: play-icon-512.png, feature-graphic-1024x500.png, adaptive-foreground/background-432.png (uygulama içinde ikon olarak Unity zaten ayarlı değil: Player Settings > Icons > Adaptive ile bu iki dosyayı ata).
- Build: File > Build Profiles > Android > **Build App Bundle (AAB)** işaretle, keystore oluştur ve **sakla** (kaybedersen güncelleme atamazsın).
- Play Console: Data safety = veri toplanmıyor/paylaşılmıyor; Content rating anketi (şiddet yok → Everyone); Telefon ekran görüntüleri için `02-Screenshots/iPhone-6.5` kullanılabilir (9:16'ya yakın).
- Yeni kişisel hesaplar için Google, yayından önce 12 test kullanıcısıyla 14 günlük kapalı test ister.

## 7. Sürüm bilgisi ve doğrulama durumu
Bkz. teslim raporu: Editörde derleme, testler, ekran görüntüleri ve iki iOS derlemesi doğrulandı; gerçek cihaz, titreşim, performans, Hindi/Arapça ayrıntıları ve Android derlemesi doğrulanmadı.
