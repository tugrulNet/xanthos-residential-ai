# Xanthos Residential AI — .NET Blazor Yapım Brifi (26 Ekim 2026)

Karar: Tuğrul 26 Eki 09:54'te stack olarak **.NET Blazor, Xanthos çizgisinde devam** dedi.
Bu brif, kodu yazacak kişiye (Tuğrul / Opencode) verilecek tek sayfalık görev tanımıdır. Benim bulunduğum ortamda dotnet SDK yok, bu yüzden Blazor derlemesi burada yapılamaz; derleme Tuğrul/Opencode ortamında, test + demo + teslim metinleri bende.

## Zorunlu çerçeve
- **Hedef framework: net10.0.** Sunucuda .NET 10.0.13 runtime hazır. .NET 8 ve .NET 9, 10 Kasım 2026'da destek dışı kalıyor; yeni projeye net8/net9 ile başlanmaz.
- **Yeni public repo, sıfırdan.** Xanthos kodu repoya kopyalanmaz. Repo adı henüz belli değil — Tuğrul belirleyecek.
- **Sunucu durumu (26 Eki kontrolü):** zoning-api.service, zoning-frontend.service, zoning-agents.service üçü de `active`. Yani skor kaynağı ayakta görünüyor; yine de ilk adımda skor endpoint'i canlı doğrulanacak.
- **API anahtarları repoya girmez.** Meta Model API anahtarı environment / sunucu secret'ında tutulur, public repoda yalnızca placeholder olur.

## Mimari kararı (26 Eki 10:00, Tuğrul): İkili yürütme
**Ön plan .NET, arka plan Python.** Bu, Xanthos'un mevcut çizgisidir (zoning-frontend Blazor + zoning-api .NET + zoning-agents Python FastAPI) ve foto ön eleme planıyla aynıdır; C#'ta Python ekosistemi yeniden yazılmaz.

### Ön plan — .NET Blazor (kullanıcının gördüğü her şey)
- `XanthosResidentialAI/` — Blazor Web App (Server interactive; WASM CORS uğraşı yok, demo hızlı).
- Sayfalar: `/` tek akış sayfası. Başka modül, auth, fiyatlandırma, tenant sayfası YOK.
- .NET tarafı yapar: adres formu, skor kartı, asistan sohbet UI'ı, foto kartları, özet PDF'i (QuestPDF).
- .NET servisleri:
  - `XanthosScoreClient` — mevcut Residential skorunu salt-okunur alır. Endpoint + auth, Tuğrul/Opencode tarafından doldurulacak; bilinmiyorsa demo için 3 adresin skoru önbelleğe alınır ve README'de "demo önbelleği" diye yazılır.
  - `MetaModelClient` (ön plan, etkileşimli) — Muse Spark asistan çağrıları. Sohbet beklemez, kuyruğa girmez; .NET HttpClient ile doğrudan Meta Model API'ye gider. Endpoint, model adı, request/response şeması Meta dokümanından doğrulanmadan kodlanmaz; dokümanda yazmayan URL uydurulmaz.
  - `SummaryService` — skor + asistan özeti + foto bulgularını tek sayfalık kaynaklı özet PDF'ine döker.

### Arka plan — Python (kullanıcının görmediği işler)
- Hazır kod: `photo-triage-service/` (triage.py + app.py, FastAPI). Yeni hackathon servisi bunun üzerine kurulur, sıfırdan triage yazılmaz.
- Python tarafı yapar: fotoğraf indirme (DDF URL), OpenCV bulanık tespiti, pHash tekrar tespiti, CLIP oda tipi sınıflandırması, SAM 3.1 segmentasyon çağrıları, özellik / sorun bayrağı çıkarımı.
- İletişim: .NET ↔ Python arasında localhost HTTP (`POST /analyze-url` ve benzeri), tıpkı foto ön eleme planındaki gibi. Python servisi çökerse .NET kilitlenmez; iş `failed` yazılır, demo önbelleğiyle devam edilir.
- Kalıcı sonuçlar (demo için): `photo_analyses` mantığıyla ayrı alanda tutulur; mevcut `properties` tablosuna yazılmaz, mevcut kayıt değiştirilmez.
- SAM 3.1'in Python'da mı .NET'te mi çağrılacağı net kural: Foto boru hattının tamamı Python'da kalır (indirme → triage → SAM → bayrak tek boru hattı), .NET yalnızca sonucu alır. Böylece SAM promptları ve segment sonuçları tek yerde yönetilir.

## Tek akış (demo senaryosu)
1. Adres gir → skor kartı: livability + investment + bileşenler (okul, güvenlik, transit vb.) + güven yüzdeleri + kaynaklar.
2. Asistan (Muse Spark): Skoru sade dille açıklar; "Kensington mı Hillhurst mü?" gibi karşılaştırmayı yalnızca eldeki skor/veriyle yapar; veri yoksa "bu veri yok" der, doldurmaz. Fiyat tahmini vermez.
3. Foto analizi: İlan fotoğrafları önce triage ile elenir (bulanık / tekrar / harita-logo-kat planı), sonra kalanlarda SAM 3.1 ile özellik + sorun bayrağı. Ekranda şeffaflık notu: "X fotoğraftan Y'si elendi (sebep dağılımıyla)".
4. Özet: Tek sayfa, kaynaklı, PDF indirilebilir.

## Kapsam dışı
- Smart Zoning, Business Match, Bylaw Chatbot, Area Analysis, auth/tenant, fiyatlandırma, admin panel, DDF toplu import.
- Muse Image sanal sahneleme: Yalnızca çekirdek 31 Eki'de biterse, "AI canlandırması, gerçek fotoğraf değil" etiketiyle.

## README zorunlu bölümü
- "Önceden var: Xanthos Residential skoru ve Calgary veri katmanı (417.791 adres vb. rakamlar yalnızca DB'den yeniden doğrulanınca yazılacak; canlı ana sayfadaki yeni sayımlar eski hafıza rakamlarıyla çelişiyor)."
- "27 Eki–4 Kas arasında yapıldı: Blazor akış sayfası, Muse Spark asistanı, SAM 3.1 foto analizi, özet PDF'i (ve varsa sahneleme)."
- Demo videosu yalnızca bu yeni parçaları gösterecek.

## Gün planı (.NET uyarlaması)
- 27 Eki: Repo + net10.0 Blazor iskeleti + skor bağlantısı canlı doğrulama.
- 28 Eki: Asistan v1 (Muse Spark) + doğrulanabilir cevap guardrail'leri.
- 29-30 Eki: Foto analizi v1 (triage + SAM 3.1).
- 31 Eki: Birleştirme, uçtan uca demo, hata yönetimi (Blazor "Rejoining" durumunda demo kilitlenmeyecek; demo verisi önbellekte).
- 1 Kas: Taşınma — yapım yok.
- 2 Kas: Stretch kararı. 3 Kas: 3 dk video + teslim metni. 4 Kas: Öğlen Calgary hedefiyle teslim.

## Başlamadan Tuğrul'dan gerekenler
1. GitHub repo adı + kim açıyor (public olacak).
2. Residential skor endpoint'i: URL, method, auth, örnek request/response (veya "demo önbelleğiyle başla" onayı).
3. Demo için 3 konut adresi + fotoğraf kaynağı (DDF photo_urls mi, manuel mi?).
4. Demo yayın adresi: sunucuda yeni subdomain mi, yoksa geçici demo mu?
5. Billing kart durumu: kredi gelince ilk API çağrısı için kart ekli mi?
6. SAM 3.1 + Muse Spark endpoint/model adları Meta dokümanından doğrulanacak — bu doğrulama koddan önce yapılacak.
