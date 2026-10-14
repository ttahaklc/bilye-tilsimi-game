# Bilye Tılsımı – Proje (Unity 6000.5.3f1)

Bu klasör eksiksiz Unity projesidir. `AppStore/` içinde mağaza görselleri, metinler ve site vardır (ayrıntı: `AppStore/05-Guide/APP_STORE_YUKLEME_REHBERI.md`).

## iOS (Mac + Xcode gerekir)
1. Unity Hub ile projeyi aç (Unity **6000.5.3f1**, iOS Build Support kurulu olmalı).
2. File > Build Profiles > iOS > Build → çıktı klasörünü seç.
3. Oluşan `Unity-iPhone.xcodeproj` dosyasını Xcode'da aç. Signing: kendi Team'in. Bundle ID: `com.tahakilic.bilyetilsimi`. Version 1.0.0, Build 1.
4. Product > Archive > Distribute App > App Store Connect.

## Android (Google Play)
1. Projeyi ilk açışta Android ayarları otomatik uygulanır (AAB, IL2CPP, ARM64). Gerekirse menü: Bilye > Android hazırla.
2. Player Settings > Publishing Settings'ten kendi keystore'unu oluştur ve **sakla**.
3. File > Build Profiles > Android > Build (çıktı .aab).
