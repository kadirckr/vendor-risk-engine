# Vendor Risk Scoring Engine (Kural Tabanlı Risk Skorlama Motoru)

Şirketleri (vendor) **finansal**, **operasyonel** ve **güvenlik/uyumluluk** boyutlarında
puanlayan; şeffaf, **açıklanabilir** ve **deterministik** bir risk skorlama projesi.
Her skorun yanında hem sayısal bir kırılım hem de insan tarafından okunabilir bir gerekçe
(`reason`) döner; böylece *"bu şirket neden bu skoru aldı?"* her zaman yanıtlanabilir.

Çekirdek skorlama tamamen kural tabanlı ve deterministiktir; yapay zeka yalnızca bu gerekçeyi
insan-dostu bir cümleye çevirmek için **opsiyonel** olarak kullanılır ve skoru asla etkilemez.

**Stack:** ASP.NET Core (.NET 8) · PostgreSQL (EF Core) · Redis (cache) · Microsoft.Extensions.AI + OpenAI · Serilog (JSON) · xUnit + Moq · React + TypeScript (Vite)

---

## Nasıl Çalıştırılır

**Gereksinimler:** Docker Desktop · .NET 8 SDK · Node.js

```bash
# 1) Altyapıyı başlat (PostgreSQL + Redis — başka bir şeye gerek yok)
docker compose up -d

# 2) Backend — ayrı terminal
dotnet run --project src/VendorRisk.Api        # http://localhost:5242

# 3) Frontend — ayrı terminal
cd frontend
npm install
npm run dev                                    # http://localhost:5173
```

- **Uygulama (UI):** http://localhost:5173
- **API (doğrudan):** http://localhost:5242/api/vendor
- **Swagger UI (dev):** http://localhost:5242/swagger

Frontend, Vite proxy üzerinden `/api` isteklerini `localhost:5242`'ye yönlendirir; ayrı bir CORS
yapılandırması gerekmez.

İlk açılışta API, EF Core migration'larını uygular ve **15 örnek şirketi** otomatik seed eder
(yalnızca tablolar boşsa, idempotent). Redis yapılandırılmışsa skorlama sonuçları cache'lenir;
yapılandırılmamışsa motor doğrudan çalışır.

**(Opsiyonel) AI gerekçesi:** İnsan-dostu `reason` metni için bir OpenAI anahtarını user-secrets'a
girin; girilmezse sistem otomatik olarak deterministik gerekçeye düşer (uygulama yine çalışır)

---

## 1. Kullanılan Teknolojiler

| Alan | Teknoloji |
|---|---|
| Backend | **.NET 8** Minimal API (C# 12) |
| Veritabanı | **PostgreSQL 16** + **EF Core 8** (snake_case isimlendirme) |
| Cache | **Redis 7** (opsiyonel, `IDistributedCache`) |
| Yapay Zeka | **Microsoft.Extensions.AI** + **OpenAI** (`IChatClient` soyutlaması) |
| Loglama | **Serilog** — yapılandırılmış JSON (CLEF), günlük dosya rotasyonu |
| Test | **xUnit** + **Moq** (58 test) |
| API Dokümantasyonu | **Swagger / OpenAPI** (Swashbuckle) |
| Hata yönetimi | **ErrorOr** (success-or-errors deseni) |
| Frontend | **React 19** + **Vite** + **TypeScript** (`frontend/`) |
| Çalışma ortamı | **Docker Compose** (PostgreSQL + Redis) |

---

## 2. Mimari

Proje, bağımlılıkların içe doğru aktığı katmanlı bir **Clean Architecture** yapısındadır.

```
src/
  VendorRisk.Domain          # Varlıklar, enum'lar ve saf skorlama motoru (I/O yok)
  VendorRisk.Application      # Use-case'ler (dispatcher + handler'lar), soyutlamalar, DTO'lar
  VendorRisk.Infrastructure   # EF Core (PostgreSQL), repository'ler, AI humanizer, matris sağlayıcı
  VendorRisk.Api              # Minimal API uçları, Serilog, Swagger, composition root
tests/
  VendorRisk.Tests            # xUnit + Moq
frontend/                     # React 19 + Vite arayüzü
```

### İstek akışı

`GET /api/vendor/23/risk` çağrıldığında akış şu şekildedir (oklar isteğin gidiş yönüdür):

```
┌──────────────┐   HTTP (JSON)   ┌─────────────────────────┐
│   React UI   │ ──────────────► │   Api   →   Handler     │
└──────────────┘                 └────────────┬────────────┘
                                              │ risk hesapla
                                              ▼
                                  ┌─────────────────────────┐
                                  │  Cache (Redis):          │
                                  │  bu vendor zaten var mı? │
                                  └────┬────────────────┬────┘
                                  VAR  │                │  YOK
                            (cache hit)│                │ (ilk istek)
                                       │                ▼
                                       │     ┌─────────────────────────┐
                                       │     │ RiskScoreCalculator      │
                                       │     │ → skor + ham reason      │
                                       │     └────────────┬────────────┘
                                       │                  ▼
                                       │     ┌─────────────────────────┐
                                       │     │  AI (OpenAI) anahtarı?   │
                                       │     └────┬───────────────┬────┘
                                       │     VAR  │               │ YOK / hata
                                       │ (AI humanize eder)        │ (ham reason kalır)
                                       │          └───────┬───────┘
                                       │                  ▼
                                       │     ┌─────────────────────────┐
                                       │     │ sonucu cache'e yaz       │
                                       │     └────────────┬────────────┘
                                       │                  │
                                       └─────────┬────────┘
                                                 ▼
                                  ┌─────────────────────────┐
                                  │  Response → kullanıcı    │
                                  └─────────────────────────┘
```

- **Cache VAR** → sonuç doğrudan cache'ten döner; yeniden hesap yok, **AI'a gidilmez**.
- **Cache YOK** → hesap yapılır, ham reason üretilir, (varsa) AI ile iyileştirilir, sonuç
  cache'e yazılır.
- **AI VAR** → ham reason insan-dostu bir cümleye çevrilir.
- **AI YOK / hata** → ham reason olduğu gibi kullanılır (sistem yine çalışır).

- **`RiskScoreCalculator`**: Domain içinde **saf bir fonksiyondur** — DB, saat veya rastgelelik
  yoktur. Aynı girdi her zaman aynı çıktıyı verir; bu yüzden tutarlı ve kolayca test edilebilir.

---

## 3. Risk Hesaplama Motoru

Bir şirketin riski dört adımda, baştan sona deterministik olarak hesaplanır:

1. **Kurallar faktör üretir.** Şirketin verileri (finansal sağlık, SLA, olaylar, sertifikalar,
   dokümanlar) kurallardan geçer; tetiklenen her sorun, 0–1 arası bir severity'ye (önem derecesi)
   sahip bir "risk faktörü" olur.
2. **Faktörler ilişkili risklerle biraz büyür.** Risk matrisi, bir riskin başka hangi risklerle
   ilişkili olduğunu bilir; bu ilişkiler faktörün severity'sini bir miktar yukarı çeker.
3. **Faktörler boyut skoruna indirgenir.** Her boyutta (finansal / operasyonel / güvenlik) o
   boyutun faktörleri tek bir 0–1 skora birleştirilir.
4. **Üç boyut ağırlıkla toplanır** → final skor ve risk seviyesi çıkar.

Aşağıdaki bölümler bu adımları tek tek açıyor.

### Genel formül (Adım 4)

```
FinalScore = 0.40 · FinansalRisk
           + 0.30 · OperasyonelRisk
           + 0.30 · Güvenlik/UyumlulukRiski        (her terim ∈ [0,1])
```

### Adım 1 — Hangi veri hangi faktörü tetikler?

Her ham alan bir veya birden fazla risk faktörü üretir; her faktörün 0–1 arası bir base severity
(temel önem derecesi) değeri olur:

| Tetikleyici koşul | Faktör | Base severity |
|---|---|---|
| `pentestReportValid == false` | `failedPenTest` | **1.00** (en kritik; güvenlik boyutunu maksimuma çeker) |
| `securityCerts` içinde ISO27001 yok | `missingISO27001` | 0.60 |
| `contractValid == false` | `expiredContract` | 0.50 |
| `privacyPolicyValid == false` | `expiredPrivacyPolicy` | 0.40 |
| `slaUptime < 95` | `slaDrop` | `0.30 + (95 − sla)·0.05` (üst sınır 0.90) |
| `majorIncidents` 4+/3/2/1 | `majorIncident` | 0.90 / 0.80 / 0.55 / 0.30 |
| `financialHealth` <50 / 50–64 / 65–80 / >80 | finansal band | 0.85 / 0.50 / 0.30 / 0.10 |

> **Tri-state doküman mantığı:** dokümanlar üç durumludur — `true` geçerli, `false` geçersiz/başarısız
> (risk), **`null` = değerlendirilmemiş** (tek başına risk tetiklemez).

### Adım 2 — İlişkili risklerle büyütme (matris)

Tetiklenen bir faktör tek başına değildir; risk matrisi onu ilişkili risklere bağlar. Motor bu
ilişkili risklerin ağırlıklarının ortalamasını alıp faktörün severity'sini bir miktar yukarı
çeker — ama sonuç asla 1'i geçmez:

```
yeni severity = base severity + (1 − base severity) · 0.30 · (ilişkili risklerin ortalama ağırlığı)
```

`(1 − base severity)` çarpanı artışı sınırlar: severity zaten yüksekse az artar, düşükse daha çok
artar, ve sonuç hep 0–1 arasında kalır.

**Örnek —** `missingISO27001` (base severity 0.60): ilişkili riskler `weakAccessControl` (0.84),
`noEncryptionPolicy` (0.79), `failedAudit` (0.76) … ortalama ≈ 0.80 →
`0.60 + 0.40 · 0.30 · 0.80 ≈ 0.70`.

### Adım 3 — Boyut skoru (faktörleri birleştirme)

Bir boyuttaki faktörler "olasılıksal VEYA" ile birleşir: tek bir güçlü faktör bile boyutu riskli
yapar; faktör sayısı arttıkça skor 1'e doğru yaklaşır ama 1'i geçmez.

```
BoyutRiski = 1 − Π(1 − sᵢ)
```

### Risk seviyesi

Final skor doğrudan bir banda düşer; skor ve seviye asla çelişmez:

```
Low < 0.35 ≤ Medium < 0.60 ≤ High < 0.85 ≤ Critical
```

### Kalibrasyon (testler ve çalışan API tarafından doğrulanmış)

| Şirket | Finansal | Operasyonel | Güvenlik | **Final** | Seviye |
|---|---|---|---|---|---|
| SecurePay (3) | 0.10 | 0.00 | 0.00 | **0.04** | Low |
| TechPlus (1) | 0.30 | 0.76 | 0.54 | **0.51** | Medium |
| AlphaCloud (4) | 0.50 | 0.90 | 1.00 | **0.77** | High |
| NovaLog (2) | 0.50 | 0.96 | 1.00 | **0.79** | High |
| TrustCom (5) | 0.89 | 0.97 | 1.00 | **0.95** | Critical |

### Gerekçe (reason) metni

Skorla birlikte, base severity'si eşiği (`ReasonThreshold = 0.40`) geçen faktörlerin etiketleri
birleştirilerek deterministik bir gerekçe üretilir:

- Faktör varsa: `Overall Critical risk, driven by: Failed penetration test + Missing ISO27001 + ...`
- Faktör yoksa: `No significant risk factors identified; the vendor meets financial, operational, and security expectations.`

Bu ham gerekçe, opsiyonel olarak AI ile insan-dostu bir cümleye çevrilir (bkz. §6).

### Kullanılan / esinlenilen algoritmalar

Motor sıfırdan icat edilmedi; bilinen birkaç klasik yöntem bir araya getirildi. Yukarıdaki dört
adımın her birinin literatürdeki karşılığı:

| Teknik | Nerede (adım) | Ne için |
|---|---|---|
| **Kural tabanlı sistem** (rule-based / expert system) | Adım 1 | Ham veriyi deterministik kurallarla risk faktörlerine eşler — ML değil, açıklanabilir sabit kurallar. |
| **Graf + BFS** (yönlü ağırlıklı graf, genişlik öncelikli arama) | Adım 2 | Risk matrisi bir komşuluk listesidir; ilişkili riskler bir kuyruk + ziyaret kümesiyle gezilir. Ziyaret kümesi döngülerde sonsuz döngüyü engeller (cycle-safe traversal). |
| **Yayılım / etki yayma** (spreading activation, influence propagation) | Adım 2 | Bir faktörün önemini ilişkili düğümlerin ağırlıklarıyla bir miktar büyütme fikri. |
| **Doygunlaşan dönüşüm** (bounded saturation / azalan getiri) | Adım 2 | `base + (1 − base)·β·avg(...)` formülündeki `(1 − base)` headroom terimi sonucu `[0,1]`'de tutar ve büyük değerlerde artışı azaltır. |
| **Noisy-OR** (olasılıksal OR — Bayesçi ağlardan) | Adım 3 | `1 − Π(1 − sᵢ)`: tek güçlü faktör bile boyutu riskli yapar, faktör sayısı arttıkça 1'e doygunlaşır. |
| **Ağırlıklı doğrusal kombinasyon** (weighted linear / sum model) | Adım 4 | Boyutları sabit ağırlıklarla (0.40 / 0.30 / 0.30) tek final skora indirger. |
| **Eşik tabanlı sınıflandırma** (threshold banding) | Risk seviyesi | Sürekli skoru ayrık seviyelere (Low / Medium / High / Critical) çevirir. |
| **Normalizasyon / clamping** | Her adım | Tüm ara değerler `[0,1]` aralığına sıkıştırılır. |

---

## 4. Veritabanı / Tablo Yapıları

Dört tablo vardır: kullanıcıların kaydettiği vendor'ları tutan bir tablo ve risk matrisini
tutan üç tablo.

### `vendors` — kullanıcıların kaydettiği vendor'lar

Kullanıcının girdiği ve skorlamaya giren ham veriler:

| Kolon | Açıklama |
|---|---|
| `id` | Birincil anahtar |
| `name` | Şirket adı |
| `financial_health` | Finansal sağlık (0–100) |
| `sla_uptime` | SLA uptime yüzdesi (0–100) |
| `major_incidents` | Son 12 aydaki büyük olay sayısı |
| `security_certs` | Sertifika listesi |
| `contract_valid`, `privacy_policy_valid`, `pentest_report_valid` | Doküman geçerlilikleri (üç durumlu: geçerli / geçersiz / belirsiz) |

### Risk matrisi — 3 tablo

Risk matrisi, "hangi risk hangi risklerle ilişkili" bilgisini bir **graf** olarak tutar ve üç
tabloya bölünür:

| Tablo | Ne tutar | Örnek |
|---|---|---|
| `risk_categories` | Risk grupları | `securityRisk`, `financialRisk` |
| `risk_factors` | Tekil risk düğümleri | `missingISO27001`, `weakAccessControl` |
| `risk_factor_edges` | İki risk arasındaki ilişki ve ağırlığı | `missingISO27001 → weakAccessControl (0.84)` |

Yani bir risk (düğüm) bir kategoriye aittir ve diğer risklere ağırlıklı bağlarla (kenarlarla)
bağlanır. Bir faktör tetiklendiğinde motor bu graf üzerinde ilişkili riskleri gezerek toplar ve
ağırlıklarını skor hesabında kullanır (detay için bkz. §3 — benzerlik yayılımı).

### Seed (ilk veriler)

Uygulama ilk açılışta iki JSON dosyasından besler:

- `Data/SampleVendorData.json` → 15 örnek şirket (`vendors`)
- `Data/RiskFactorMatrix.json` → risk matrisi (üç tablo)

Besleme yalnızca tablolar boşsa yapılır, yani yeniden başlatmalar veriyi çoğaltmaz. Sonrasında
**veritabanı tek doğruluk kaynağıdır**; matris bellek içine yüklenip hızlıca okunur.

---

## 5. Cache Yapısı

Aynı şirket için aynı sonucu tekrar tekrar hesaplamamak (ve AI'ı boşa çağırmamak) için
Redis tabanlı bir cache kullanılır. Cache yalnızca `Redis` bağlantı dizesi yapılandırıldığında
devreye girer; yoksa motor doğrudan çalışır.

Nasıl çalışır:

- **İlk istek:** sonuç hesaplanır, AI ile gerekçe iyileştirilir ve **tüm sonuç (iyileştirilmiş
  gerekçe dahil)** Redis'e yazılıp kullanıcıya döner.
- **İkinci istek:** sonuç Redis'ten döner; yeniden hesap yapılmaz ve **AI'a gidilmez**.
- Kayıtlar 10 dakika sonra düşer (TTL).

---

## 6. Yapay Zeka Kullanımı (Reason İyileştirme)

Amaç: deterministik üretilen ham gerekçeyi tek, akıcı, insan-dostu bir cümleye çevirmek.
Örnek ham gerekçe:

```text
Overall Critical risk, driven by: Failed penetration test + Missing ISO27001 + ...
```

### Tasarım

Kod, AI'ın detayını tek bir arayüzün arkasına saklar: **`IReasonHumanizer`**. "Ham gerekçeyi al,
insan-dostu hale getir" der; nasıl yapıldığını bilmez. Bu arayüzün iki uygulaması vardır:

- **`ChatClientReasonHumanizer`** — anahtar varsa kullanılır; OpenAI'a istek atar.
- **`PassthroughReasonHumanizer`** — anahtar yoksa kullanılır; metni hiç değiştirmeden döndürür.

Böylece uygulamanın geri kalanı "AI var mı yok mu" bilmek zorunda kalmaz. Ayrıca AI çağrısı saf
domain'e değil, bir ara katmana (`HumanizingRuleEngineService`) konduğu için `RiskScoreCalculator`
saf ve test edilebilir kalır.

### Güvenli ve dayanıklı (resilient) davranış

```
API key yok            → PassthroughReasonHumanizer → ham reason
API key var, hata olur → try/catch → ham reason (fallback)
API key var, başarılı  → AI'ın insan-dostu metni
```

AI hiçbir zaman skorlamayı kıramaz: hata durumunda deterministik metne düşülür. `Temperature`
düşük (`0.2`) tutulur; amaç yaratıcılık değil, faktörleri ve seviyeyi koruyan **tutarlı, sadık**
bir yeniden yazımdır.

### Yapılandırma (sır yönetimi)

API anahtarı **koda veya repoya yazılmaz**. `appsettings.json`'daki `OpenAI:ApiKey` boş bırakılır;
gerçek değer **user-secrets** veya ortam değişkeninden okunur:

Model `OpenAI:Model` ile değiştirilebilir (varsayılan `gpt-4o-mini`). Anahtar girilmezse
uygulama yine sorunsuz çalışır ve deterministik gerekçeyi gösterir.

---

## 7. API Uçları

| Metot & rota | Açıklama |
|---|---|
| `POST /api/vendor` | Yeni vendor kaydeder → üretilen id ile `201`. |
| `GET /api/vendor` | Tüm şirketleri riskleriyle listeler (en riskli önce). |
| `GET /api/vendor/{id}/risk` | Bir şirket için tam, açıklanabilir risk değerlendirmesi. |

---

## 8. Testler

```bash
dotnet test        # 58 test
```

Skorlama kritik yol olduğu için sıkı şekilde sabitlenmiştir:

- **Golden regression** — 15 örnek şirketin tam skoru ve seviyesi assert edilir; bir
  ağırlık/band/sabit değişirse build kırılır.
- **Determinizm** — her şirket defalarca skorlanır; tüm değerlendirme (skor, seviye, reason,
  boyut skorları, faktör severity'leri) birebir aynı olmalıdır.
- **Sınırlar** — her skor `[0,1]` aralığında kalır.

Bunlara ek olarak davranışsal kurallar, graf yürüyüşü, `RuleEngineService` orkestrasyonu
(mock'lu `IRiskMatrixProvider`) ve caching dekoratörü (cache hit'te AI'a gidilmediği dahil)
test edilir. AI için gerçek ağ çağrısı yapılmaz; `IReasonHumanizer` mock'lanır.

---

## 9. Loglama

Serilog, yapılandırılmış JSON (CLEF) olarak günlük rotasyonlu dosyaya yazar
(`logs/log-<tarih>.json`, 7 gün saklama; ELK/Filebeat-ready). HTTP istek (`UseSerilogRequestLogging`),
use-case ve repository exception loglaması yapılır; saf domain (`RiskScoreCalculator`) I/O'dan
arındırılmıştır.