# PRD â€“ Há»‡ thá»‘ng VinhKhanhStreet (VKFoodTour)

> **Product Requirements Document**
> Dá»± Ã¡n: Ná»n táº£ng Food Tour thÃ´ng minh vá»›i audio guide Ä‘a ngÃ´n ngá»¯ cho phá»‘ áº©m thá»±c
> PhiÃªn báº£n: 1.1
> NgÃ y cáº­p nháº­t: 27/04/2026

> **PhiÃªn báº£n cÃ³ áº£nh sÆ¡ Ä‘á»“ Ä‘Ã£ render:** xem `docs/diagrams/PRD.Rendered.md` (Ä‘Ã£ thay táº¥t cáº£ khá»‘i Mermaid báº±ng PNG/SVG).
> **Render láº¡i khi sá»­a file nÃ y:** cháº¡y `pwsh -File docs/render-prd.ps1` tá»« thÆ° má»¥c gá»‘c.

---

## 1. Tá»•ng quan Ä‘á»“ Ã¡n

### 1.1. Bá»‘i cáº£nh
Phá»‘ áº©m thá»±c táº¡i Viá»‡t Nam ngÃ y cÃ ng phÃ¡t triá»ƒn vÃ  thu hÃºt lÆ°á»£ng lá»›n du khÃ¡ch trong vÃ  ngoÃ i nÆ°á»›c. Tuy nhiÃªn, du khÃ¡ch thÆ°á»ng gáº·p cÃ¡c váº¥n Ä‘á»:
- KhÃ´ng hiá»ƒu Ä‘Æ°á»£c cÃ¢u chuyá»‡n, vÄƒn hÃ³a, lá»‹ch sá»­ cá»§a tá»«ng quÃ¡n.
- RÃ o cáº£n ngÃ´n ngá»¯ khi Ä‘á»c menu, biá»ƒn hiá»‡u, há»i thÃ´ng tin.
- KhÃ³ lá»±a chá»n quÃ¡n phÃ¹ há»£p do thiáº¿u thÃ´ng tin thá»‘ng nháº¥t.
- Tráº£i nghiá»‡m rá»i ráº¡c, khÃ´ng cÃ³ "sá»£i dÃ¢y" dáº«n dáº¯t xuyÃªn suá»‘t tour.

CÃ¡c giáº£i phÃ¡p hiá»‡n cÃ³ nhÆ° Google Maps, TripAdvisor chá»‰ cung cáº¥p thÃ´ng tin tÄ©nh, khÃ´ng cÃ³ tráº£i nghiá»‡m audio theo vá»‹ trÃ­ (location-based audio guide) vÃ  khÃ´ng há»— trá»£ Ä‘a ngÃ´n ngá»¯ tá»± Ä‘á»™ng cho tá»«ng quÃ¡n nhá» láº».

### 1.2. Má»¥c tiÃªu sáº£n pháº©m
**VinhKhanhStreet (VKFoodTour)** lÃ  há»‡ thá»‘ng tráº£i nghiá»‡m áº©m thá»±c thÃ´ng minh, bao gá»“m:
- **Web Admin**: nÆ¡i quáº£n trá»‹ viÃªn váº­n hÃ nh toÃ n bá»™ phá»‘ áº©m thá»±c (POI, ngÃ´n ngá»¯, audio, QR, thá»‘ng kÃª).
- **Web Vendor**: nÆ¡i chá»§ quÃ¡n tá»± quáº£n lÃ½ gian hÃ ng, thá»±c Ä‘Æ¡n, media.
- **Mobile App**: á»©ng dá»¥ng dÃ nh cho du khÃ¡ch, quÃ©t QR Ä‘áº§u phá»‘ Ä‘á»ƒ báº¯t Ä‘áº§u tour audio Ä‘a ngÃ´n ngá»¯, tá»± Ä‘á»™ng thuyáº¿t minh theo vá»‹ trÃ­ GPS.

Má»¥c tiÃªu cá»‘t lÃµi:
1. Táº¡o tráº£i nghiá»‡m **audio tour tá»± Ä‘á»™ng** theo geofence cho du khÃ¡ch.
2. Há»— trá»£ **Ä‘a ngÃ´n ngá»¯ (i18n)** vá»›i dá»‹ch + TTS (Text-to-Speech) hÃ ng loáº¡t.
3. Há»‡ thá»‘ng **QR-first**: má»™t QR Ä‘áº§u phá»‘ khá»Ÿi táº¡o toÃ n bá»™ tour, tá»«ng QR quÃ¡n Ä‘á»ƒ xem chi tiáº¿t nhanh.
4. Cung cáº¥p **dashboard thá»‘ng kÃª realtime** phá»¥c vá»¥ quáº£n lÃ½ vÃ  quyáº¿t Ä‘á»‹nh kinh doanh.

### 1.3. Pháº¡m vi & Ä‘á»‘i tÆ°á»£ng sá»­ dá»¥ng

| Äá»‘i tÆ°á»£ng | Ná»n táº£ng | Vai trÃ² chÃ­nh |
|---|---|---|
| Admin | Web Admin (Blazor Server) | Quáº£n trá»‹ toÃ n há»‡ thá»‘ng, duyá»‡t POI, cáº¥u hÃ¬nh ngÃ´n ngá»¯, táº¡o audio, quáº£n lÃ½ QR, dá»‹ch ná»™i dung |
| Vendor (chá»§ quÃ¡n) | Web Vendor (cÃ¹ng app Blazor, role `Vendor`) | Cáº­p nháº­t thÃ´ng tin quÃ¡n, quáº£n lÃ½ thá»±c Ä‘Æ¡n, xem thá»‘ng kÃª |
| Du khÃ¡ch (End-user) | Mobile App (.NET MAUI) | QuÃ©t QR, chá»n ngÃ´n ngá»¯, nghe audio theo vá»‹ trÃ­, Ä‘Ã¡nh giÃ¡ |

### 1.4. Kiáº¿n trÃºc tá»•ng thá»ƒ

![diagram](./PRD.Rendered-1.png)

> **Ghi chÃº kiáº¿n trÃºc thá»±c táº¿**
> - Nghiá»‡p vá»¥ Ä‘Æ°á»£c Ä‘áº·t trá»±c tiáº¿p trong Controller cá»§a API vÃ  trong `Admin/Services/` (khÃ´ng dÃ¹ng CQRS/MediatR).
> - Google Translate vÃ  Edge TTS Ä‘Æ°á»£c gá»i tá»« Web Admin (khi Admin soáº¡n ná»™i dung), khÃ´ng pháº£i tá»« API runtime.
> - KhÃ´ng cÃ³ SignalR hub nghiá»‡p vá»¥ â€“ dashboard realtime dá»±a trÃªn polling tracking log.

---

## 2. Use-case tá»•ng quan

**MÃ´ táº£:** SÆ¡ Ä‘á»“ use-case gom toÃ n bá»™ chá»©c nÄƒng cá»§a há»‡ thá»‘ng theo **3 actor** chÃ­nh: Admin, Vendor, Du khÃ¡ch. ÄÃ¢y lÃ  cÃ¡i nhÃ¬n tá»•ng quan vá» pháº¡m vi Ä‘á»“ Ã¡n trÆ°á»›c khi Ä‘i vÃ o chi tiáº¿t tá»«ng module.

![diagram](./PRD.Rendered-2.png)

**Ghi chÃº:**
- Admin vÃ  Vendor **dÃ¹ng chung há»‡ thá»‘ng Ä‘Äƒng nháº­p** (UA1) â€“ phÃ¢n quyá»n theo role.
- Chá»©c nÄƒng "Quáº£n lÃ½ POI" (UA3) **include** use-case "Duyá»‡t POI" (Pending â†’ Approved/Rejected) â€“ chi tiáº¿t tráº¡ng thÃ¡i xem á»Ÿ sÆ¡ Ä‘á»“ State Lifecycle (STATE-01).
- CÃ¡c use-case **dáº¡ng CRUD** (UA3, UA8, UA11, UV1, UV2) Ä‘á»u tuÃ¢n theo pattern chung â€“ xem sÆ¡ Ä‘á»“ SEQ-08 (CRUD generic).

---

## 3. Chá»©c nÄƒng ná»•i báº­t

> Má»—i má»¥c dÆ°á»›i Ä‘Ã¢y liá»‡t kÃª **route**, **file Razor / Service**, vÃ  **danh sÃ¡ch phÆ°Æ¡ng thá»©c** thá»±c sá»± Ä‘Æ°á»£c gá»i khi váº­n hÃ nh tab. Phá»¥c vá»¥ tra cá»©u nhanh khi maintain.

### 3.1. Web Admin (role `Admin`)

#### A1 â€” Dashboard tá»•ng quan `/` (`Pages/Home.razor`)
| Khu vá»±c | MÃ´ táº£ | PhÆ°Æ¡ng thá»©c / nguá»“n dá»¯ liá»‡u |
|---|---|---|
| Realtime thiáº¿t bá»‹ | Äáº¿m device Ä‘ang dÃ¹ng app trong cá»­a sá»• vÃ i chá»¥c giÃ¢y | `ActiveDevicesWidget.LoadAsync()` â†’ Ä‘á»c `Db.TrackingLogs` (poll má»—i 3s) |
| Thá»‘ng kÃª há»‡ thá»‘ng | Tá»•ng POI / POI Ä‘ang hoáº¡t Ä‘á»™ng / tá»•ng thuyáº¿t minh / sá»‘ ngÃ´n ngá»¯ active / tá»•ng user / sá»‘ vendor / lÆ°á»£t QR hÃ´m nay | `Home.LoadAdminStats()` |
| HÃ nh vi ngÆ°á»i dÃ¹ng | Tá»•ng tÆ°Æ¡ng tÃ¡c hÃ´m nay (loáº¡i trá»« heartbeat `move`), thá»i gian nghe TB, tá»•ng Ä‘Ã¡nh giÃ¡ | `Home.LoadAdminStats()` |
| Top gian hÃ ng | Top 5 POI cÃ³ nhiá»u lÆ°á»£t `enter` + `qr_scan` | Group `TrackingLogs` theo `PoiId` |
| Top thuyáº¿t minh | Top 5 POI cÃ³ tá»•ng phÃºt nghe lá»›n nháº¥t (`listen_end`) | Sum `ListenedDurationSec` |
| NgÃ´n ngá»¯ sá»­ dá»¥ng | Äáº¿m **sá»‘ thiáº¿t bá»‹ duy nháº¥t** theo ngÃ´n ngá»¯ Má»šI NHáº¤T cá»§a há» trong 30 ngÃ y, chuáº©n hoÃ¡ bá» `anon:` / region | `Home.LoadAdminStats()` + `NormalizeLangCode()` |
| PhÃ¢n bá»• Ä‘Ã¡nh giÃ¡ | Histogram 1-5 sao | `Db.Reviews.GroupBy(Rating)` |

#### A2 â€” Quáº£n lÃ½ gian hÃ ng `/admin/pois` (`Pages/PoiList.razor` + `Services/PoiService.cs`)
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| TÃ¬m kiáº¿m theo tÃªn + lá»c tráº¡ng thÃ¡i + lá»c phÃª duyá»‡t | `FilteredPois` (LINQ in-memory) |
| Táº£i danh sÃ¡ch POI vÃ  Vendor | `PoiService.GetAllAsync()`, `AuthService.GetAllUsersAsync()` |
| Má»Ÿ modal sá»­a / lÆ°u | `ShowEditModal()`, `SavePoi()` â†’ `PoiService.UpdateAsync(Poi)` (radius bá»‹ Ã©p vá» `DefaultRadius=20` máº·c Ä‘á»‹nh, runtime cá»™ng theo gÃ³i) |
| Duyá»‡t POI | `ApprovePoiAction()` â†’ `PoiService.ApprovePoiAsync(poiId)` |
| Tá»« chá»‘i POI | `RejectPoiAction()` â†’ `PoiService.RejectPoiAsync(poiId, note)` |
| KhoÃ¡ / má»Ÿ khoÃ¡ gian hÃ ng | `HidePoiAsync()` â†’ `PoiService.HideStallAsync(id)` ; `ToggleActive()` â†’ `ToggleActiveAsync(id)` |
| Hiá»ƒn thá»‹ geofence hiá»‡u dá»¥ng theo gÃ³i | `GetOwnerTier(ownerId)` + `GetTierBonus(tier)` (`+0/+5/+10/+15`) |

#### A3 â€” Báº£n Ä‘á»“ POI + Heatmap `/admin/ban-do` (`Pages/Admin/BanDoPoi.razor`)
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| Khá»Ÿi táº¡o Leaflet + render markers | `InitMapAsync()` â†’ JS interop `initAdminMap` |
| Báº­t/táº¯t heatmap | `OnToggleHeatmap()`, `ReloadHeatmapAsync()` |
| Heatmap data | API `GET /api/Tracking/heatmap?hours=...` |

#### A4 â€” Quáº£n lÃ½ ngÃ´n ngá»¯ & dá»‹ch `/quan-ly-ngon-ngu` (`Pages/Admin/QuanLyNgonNgu.razor` + `LanguageProvisionJobService`, `GoogleTranslateService`, `EdgeTtsService`)
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| Táº£i danh sÃ¡ch ngÃ´n ngá»¯ + voice gá»£i Ã½ | `LoadData()` |
| Kiá»ƒm tra mÃ£ ngÃ´n ngá»¯ Google há»— trá»£ | `CheckLanguageCode()` â†’ `GoogleTranslateService.IsLanguageSupportedAsync()` |
| ThÃªm ngÃ´n ngá»¯ + cháº¡y auto-provision audio cho má»i POI Approved | `AddLanguage()` â†’ `LanguageProvisionJobService.StartAsync()` (job ná»n) |
| Theo dÃµi tiáº¿n Ä‘á»™ job | `RefreshCurrentJob()`, `StartJobPolling()` |
| Audit dá»‹ch â€” phÃ¡t hiá»‡n POI thiáº¿u hoáº·c lá»‡ch ná»™i dung | `RunTranslationAudit()` â†’ so sÃ¡nh `Narrations` theo `LanguageId` |
| Sá»­a lá»—i tá»«ng POI | `RetryIssue(issue)` â†’ `PoiService.SyncPoiLanguageAsync(poiId, languageId)` |
| Äá»“ng bá»™ láº¡i toÃ n ngÃ´n ngá»¯ | `StartResyncForLanguage(id)` |
| Báº­t/táº¯t ngÃ´n ngá»¯ | `ToggleLanguage(row)` |

#### A5 â€” Audio Intro Phá»‘ `/admin/intro-audio` (`Pages/Admin/IntroAudio.razor`)
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| Táº£i ná»™i dung intro hiá»‡n táº¡i theo ngÃ´n ngá»¯ | `LoadCurrentSettingAsync()` (Ä‘á»c báº£ng `APP_SETTINGS`) |
| Äá»•i ngÃ´n ngá»¯ Ä‘ang sá»­a | `SelectLang(code)` |
| Tá»± Ä‘á»™ng dá»‹ch intro tá»« tiáº¿ng Viá»‡t | `AutoTranslateIntroAsync()` â†’ `GoogleTranslateService.TranslateAsync()` |
| Sinh audio Edge TTS | `GenerateIntroTtsAsync()` â†’ `EdgeTtsService.SynthesizeAsync(text, voice)` (output `UploadsData/intro/intro_{lang}.mp3`) |
| XoÃ¡ audio intro | `DeleteIntroAudioAsync()` |
| LÆ°u setting | `UpsertSettingAsync(key, value)` |

#### A6 â€” Quáº£n lÃ½ thuyáº¿t minh POI `/thuyet-minh` (`Pages/Admin/ThuyetMinh.razor`) vÃ  Audio tá»•ng `/admin/audio-management` (`Pages/Admin/AudioManagement.razor`)
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| Táº£i POI + tÃ¬nh tráº¡ng audio Ä‘a ngÃ´n ngá»¯ | `LoadDataAsync()` (group `Narrations` theo `LanguageId` & `IsActive`) |
| Sinh audio Edge TTS cho 1 POI | `GenerateTtsForPoiAsync(poi)` â†’ `PoiService.UpdateMultilingualNarrationsAsync(poiId, forceRefresh)` |
| Sinh audio cho **toÃ n bá»™** POI | `GenerateAllTtsAsync()` |
| Táº¡o QR cho POI | `CreateQrForPoiAsync(poi)` â†’ `PoiService.EnsurePrimaryQrForVendorAsync()` |
| Nghe thá»­ audio trÃªn trÃ¬nh duyá»‡t | `PlayAudio(url)` / `StopAudio()` (JS interop `<audio>`) |

#### A7 â€” Quáº£n lÃ½ QR `/ma-qr` (`Pages/Admin/MaQR.razor`)
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| Táº£i danh sÃ¡ch QR (POI + master tour) | `LoadData()` |
| Táº¡o/Ä‘áº£m báº£o QR master cho phá»‘ | `CreateMasterQrAsync()` â†’ `PoiService.EnsureMasterTourQrAsync()` |
| Báº­t/táº¯t 1 QR | `ToggleQr(qrId)` |
| TÃ¡i táº¡o QR theo ná»™i dung quÃ¡n | `RegenerateByDescription(qr)` â†’ `PoiService.RegenerateQrFromDescriptionAsync()` |
| Táº£i PNG QR | `DownloadQr(token)` â†’ JS endpoint sinh PNG |
| XoÃ¡ táº¥t cáº£ QR | `DeleteAllQrAsync()` â†’ `PoiService.DeleteAllQrAsync()` |
| Resolve token (mobile) | API `GET /api/Qr/resolve/{token}` |

#### A8 â€” Quáº£n lÃ½ gian hÃ ng (ná»™i bá»™) `/gian-hang` (`Pages/Admin/GianHang.razor`)
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| Tá»•ng há»£p POI + chá»§ + email + áº£nh + ngÃ´n ngá»¯ TM + sá»‘ mÃ³n | `OnInitializedAsync()` (join `Pois`, `Users`, `Narrations`, `Foods`, `Images`) |
| Má»Ÿ quick-edit (chá»‰ priority â€” radius láº¥y máº·c Ä‘á»‹nh + bonus theo gÃ³i) | `OpenQuickEdit(row)` |
| LÆ°u priority | `SaveQuickEditAsync()` (Ã©p `poi.Radius = DefaultRadius`) |

#### A9 â€” Quáº£n lÃ½ nhÃ¢n sá»± & vendor `/nguoi-dung` (`Pages/Admin/NguoiDung.razor`)
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| Táº£i danh sÃ¡ch user (Admin + Vendor) | `LoadUsers()` â†’ `AuthService.GetAllUsersAsync()` |
| Báº­t/khoÃ¡ tÃ i khoáº£n | `ToggleUserAsync(user)` â†’ `AuthService.ToggleActiveAsync(userId)` |
| Táº¡o / sá»­a user (má»Ÿ modal) | `ShowModal()`, `SaveUser()` â†’ `AuthService.RegisterAdminAsync()` / `RegisterVendorAsync()` (auto-táº¡o POI placeholder cho vendor) |

#### A10 â€” Pháº£n há»“i App `/admin/phan-hoi-app` (`Pages/Admin/AppFeedback.razor`)
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| Táº£i feedback + lá»c / phÃ¢n trang | `LoadDataAsync()`, `SetRatingFilter(star)`, `ApplyFilter()`, `PrevPage()`, `NextPage()` |
| XoÃ¡ pháº£n há»“i | `DeleteFeedbackAsync(id)` |

#### A11 â€” Auth `/login`, `/logout`
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| ÄÄƒng nháº­p | `Login.HandleLogin()` â†’ `AuthService.AuthenticateAsync(email, password)` (BCrypt verify) â†’ `SignInAsync` cookie scheme |
| ÄÄƒng xuáº¥t | `Logout.OnInitializedAsync()` â†’ `SignOutAsync` |

---

### 3.2. Web Vendor (cÃ¹ng app Blazor, role `Vendor`)

#### V1 â€” Dashboard quÃ¡n `/` (`Pages/Home.razor` â€” branch Vendor)
| Khu vá»±c | PhÆ°Æ¡ng thá»©c |
|---|---|
| Tá»•ng quan KPI (QR scan hÃ´m nay, audio play hÃ´m nay, Ä‘Ã¡nh giÃ¡ TB, gÃ³i thÃ nh viÃªn) | `Home.LoadVendorStats()` â†’ `DashboardMetricsService.GetVendorMetricsAsync(userId)` |
| Checklist váº­n hÃ nh (mÃ´ táº£ / Ä‘á»‹a chá»‰-báº£n Ä‘á»“ / áº£nh bÃ¬a / audio / mÃ³n) | flag tá»« `VendorMetrics` (`HasDescription`, `HasAddress`, `HasValidMap`, `HasCover`, `HasAudio`, `MenuCount`) |
| Quick action | link sang `/vendor/thong-tin`, `/vendor/thuc-don`, `/vendor/goi-thanh-vien` |

#### V2 â€” ThÃ´ng tin gian hÃ ng `/vendor/thong-tin` (`Pages/Vendor/ThongTinQuan.razor`)
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| Táº£i POI cá»§a vendor (auto-táº¡o náº¿u chÆ°a cÃ³) | `LoadDataAsync()` â†’ `PoiService.GetPoiByOwnerIdAsync(userId)` / `EnsureVendorPoiAsync()` |
| Khá»Ÿi táº¡o báº£n Ä‘á»“ tÆ°Æ¡ng tÃ¡c + Ã´ tÃ¬m Ä‘á»‹a chá»‰ | JS `initMapPicker(...)` (callback `OnMapLocationChanged`, `OnAddressSearchSelected`) |
| TÃ¬m kiáº¿m Ä‘á»‹a chá»‰ | `HandleSearchClick()` / `HandleSearchKeyUp(e)` â†’ JS `triggerMapSearch` |
| LÆ°u thÃ´ng tin chung (tÃªn / mÃ´ táº£ / Ä‘á»‹a chá»‰ / toáº¡ Ä‘á»™ / áº£nh) | `SaveChanges()` â†’ `PoiService.UpdateStallInfoAsync(...)` + sau Ä‘Ã³ `UpdateMultilingualNarrationsAsync` Ä‘á»ƒ re-sinh audio |
| Gá»­i yÃªu cáº§u duyá»‡t | `RequestApproval()` â†’ `PoiService.RequestApprovalAsync(poiId)` |
| Upload áº£nh bÃ¬a / gallery | `HandleCoverUpload(e)`, `HandleGalleryUpload(e)` â†’ ghi `UploadsData/poi/...` + `PoiService.AddStallGalleryImageAsync` |
| XoÃ¡ áº£nh gallery | `RemoveGalleryAsync(id)` â†’ `PoiService.RemoveStallGalleryImageAsync` |
| LÃ m má»›i audio Ä‘a ngÃ´n ngá»¯ | `SyncAudioAsync()` â†’ `PoiService.UpdateMultilingualNarrationsAsync(poiId, forceRefresh: true)` |

#### V3 â€” Thá»±c Ä‘Æ¡n `/vendor/thuc-don` (`Pages/Vendor/ThucDon.razor` + `MenuService`)
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| Táº£i mÃ³n cá»§a POI | `OnInitializedAsync()` â†’ `MenuService.GetByPoiAsync(poiId)` |
| Má»Ÿ modal thÃªm / sá»­a mÃ³n | `ShowAddModal()`, `ShowEditModal(item)`, `HideModal()` |
| Upload áº£nh mÃ³n | `HandleMenuImageUpload(e)` (lÆ°u vÃ o `UploadsData/menu/...`) |
| LÆ°u mÃ³n (insert/update) | `SaveItem()` â†’ `MenuService.UpsertAsync(MenuItem)` |
| Báº­t/táº¯t mÃ³n (sold-out / available) | `ToggleStatus(item)` â†’ `MenuService.ToggleAvailabilityAsync(id)` |

#### V4 â€” GÃ³i thÃ nh viÃªn `/vendor/goi-thanh-vien` (`Pages/Vendor/GoiThanhVien.razor`) â­ má»›i khÃ´i phá»¥c
| TÃ¡c vá»¥ | PhÆ°Æ¡ng thá»©c |
|---|---|
| Táº£i gÃ³i hiá»‡n táº¡i | `OnInitializedAsync()` â†’ `AuthService.GetUserByIdAsync(userId)` Ä‘á»c `User.MembershipTier` |
| ÄÄƒng kÃ½ gÃ³i (Standard / Silver / Gold / Diamond) | `RegisterTierAsync(newTier)` â†’ `AuthService.UpdateMembershipTierAsync(userId, newTier)` |
| Quyá»n lá»£i theo gÃ³i | bonus geofence `+0/+5/+10/+15`, Æ°u tiÃªn audio trong queue, huy hiá»‡u trÃªn map mobile (gáº¯n vÃ o `Poi.MembershipTier` qua join `Owner`) |

#### V5 â€” Trang doanh thu redirect `/vendor/thu-nhap` (`Pages/Vendor/ThuNhap.razor`)
Trang chá»‰ Ä‘iá»u hÆ°á»›ng vá» Home Dashboard tá»•ng há»£p (Ä‘Ã£ merge sá»‘ liá»‡u doanh thu/Ä‘Ã¡nh giÃ¡ vÃ o Home Vendor Ä‘á»ƒ gá»n UI).

---

### 3.3. Mobile App (.NET MAUI) â€” `VKFoodTour.Mobile/`

#### M1 â€” Onboarding & chá»n ngÃ´n ngá»¯
- `Views/WelcomePage.xaml.cs` â€” landing cÃ³ nÃºt "Báº¯t Ä‘áº§u", ghi event `move` lÃºc khá»Ÿi Ä‘á»™ng qua `IDataService.TrackEventAsync()`.
- `Views/LanguagePickerPage.xaml.cs` â€” `OnLanguageSelected()` â†’ `ILocalizationService.SetLanguageCode(code)` â†’ ghi `ISettingsService.SelectedLanguageCode` + `HasPickedLanguage = true`.

#### M2 â€” ÄÄƒng nháº­p / Ä‘Äƒng kÃ½ / khÃ¡ch vÃ£ng lai
- `Views/LoginPage.xaml.cs`:
  - `OnSubmit()` â†’ `IDataService.LoginAsync(email,pwd)` hoáº·c `RegisterAsync(name,email,pwd)` â†’ `IAuthSessionService.SetUser(...)`.
  - `OnContinueAsGuest()` â†’ `IAuthSessionService.EnterAnonymous()` + log event `move` kÃ¨m `languageCode` thuáº§n (khÃ´ng cÃ²n prefix `anon:`).

#### M3 â€” QuÃ©t QR
- `Views/QrScanPage.xaml.cs` (ZXing) â†’ `OnQrDetected(token)` â†’ `IDataService.ResolveQrAsync(token, lang)` (`GET /api/Qr/resolve/{token}`).
- Náº¿u káº¿t quáº£ type = `tour` â†’ má»Ÿ `TourPlayerPage`. Náº¿u `poi` â†’ má»Ÿ `StallDetailPage(poiId)`.

#### M4 â€” Tour player / audio queue
- `Views/TourPlayerPage.xaml.cs` + `ViewModels/TourPlayerViewModel`:
  - `StartAsync()` â†’ `TourService.StartTourAsync(tourId, lang)` (`POST /api/Tour/start`).
  - `LoadQueueAsync()` â†’ `TourService.GetAudioQueueAsync(tourId, lang)` (`GET /api/Tour/audio-queue`).
  - Push tá»«ng item vÃ o `AudioQueueService`.

#### M5 â€” PhÃ¡t audio
- `Services/AudioPlaybackService.cs` (Plugin.Maui.Audio): `PlayAsync(url)`, `Pause()`, `Resume()`, `Stop()`, `SeekTo(sec)`, sá»± kiá»‡n `PlaybackEnded`.
- `Services/AudioQueueService.cs` â€” orchestrator chÃ­nh:
  - `EnqueueAsync(item)`, `HandlePoiEnteredAsync(poi)`, `PlayNextFromQueueAsync()`, `InsertNext(item)`, `InterruptAndPlay(item)`.
  - Quy táº¯c 60% (`TierValue` so sÃ¡nh gÃ³i + tiáº¿n Ä‘á»™ `_player.PositionRatio`) Ä‘á»ƒ quyáº¿t Ä‘á»‹nh `InsertNext` vs `InterruptAndPlay`.
  - `MarkPoiPlayed(poi)` bÃ¡o cho `GeofenceMonitorService` khÃ´ng re-trigger.

#### M6 â€” Geofence
- `Services/GeofenceMonitorService.cs`:
  - `StartAsync()` poll `Geolocation.Default.GetLastKnownLocationAsync()` má»—i 3s.
  - `EvaluateAsync(lat,lng)` tÃ­nh khoáº£ng cÃ¡ch tá»›i má»i POI: `threshold = clamp(PoiRadiusMeters,5,200) + TierBonusMeters + GpsBufferMeters(10m)`.
  - **Dwell 8s**: `_pendingEnter[poiId] = now`, sau 8s á»Ÿ trong vÃ¹ng â†’ `_confirmedIn.Add(poiId)` + báº¯n event `PoiEntered`.
  - **Exit debounce 10s**: pháº£i ngoÃ i vÃ¹ng Ä‘á»§ 10s má»›i `PoiExited`.
  - Heartbeat `move` má»—i `MoveHeartbeatSeconds` qua `IDataService.TrackEventAsync(eventType:"move", lat, lng)`.

#### M7 â€” Danh sÃ¡ch / chi tiáº¿t quÃ¡n
- `Views/StallListPage.xaml.cs` + `ViewModels/HomeViewModel.cs`: `LoadAsync()` â†’ `IDataService.GetPoisAsync(lang)` (`GET /api/Poi`). Sort theo `MembershipTier` rá»“i `Name`.
- `Views/StallDetailPage.xaml.cs` + `ViewModels/StallDetailViewModel`: `LoadAsync(poiId)` â†’ `IDataService.GetPoiDetailAsync(poiId, lang)` (`GET /api/Poi/{id}/detail`).
- `Views/FullMapPage.xaml.cs` `LoadPoisAsync()` â€” váº½ pin + vÃ²ng trÃ²n geofence (`base + tierBonus + 10m`) + popup chi tiáº¿t khi tap pin.

#### M8 â€” ÄÃ¡nh giÃ¡ quÃ¡n
- `ViewModels/StallDetailViewModel.SubmitReviewAsync()` â†’ `IDataService.PostReviewAsync(CreateReviewDto { PoiId, Rating, Comment, LanguageCode })` (`POST /api/Reviews`).

#### M9 â€” YÃªu thÃ­ch (offline-only)
- `Services/FavoriteService.cs`: `Toggle(poiId)`, `IsFavorite(poiId)`, `GetAll()` lÆ°u trong `Preferences` cá»§a MAUI.

#### M10 â€” Tracking log + offline queue
- `Services/DataService.TrackEventAsync(poiId, eventType, listenedDurationSec?, languageCode?, lat?, lng?)`:
  - Tá»± gáº¯n `LanguageCode` tá»« `SettingsService.SelectedLanguageCode` náº¿u caller khÃ´ng truyá»n.
  - `NormalizeLanguageCode()` bá» prefix `xxx:`, lower-case.
  - Náº¿u API lá»—i â†’ `EnqueueTrackingAsync()` lÆ°u vÃ o SQLite (`ILocalStore.EnqueueEventAsync`).
  - `FlushPendingEventsAsync()` xáº£ queue khi máº¡ng quay láº¡i.

#### M11 â€” Äá»“ng bá»™ offline (má»›i thÃªm)
- `Services/Offline/SyncService.cs`: `SyncAsync()` gá»i `GET /api/Sync/bootstrap` â†’ ghi `LocalStore` (POI, áº£nh, audio URL, ngÃ´n ngá»¯).
- `Services/Offline/MediaCacheService.cs`: `EnsureCachedAsync(url)` táº£i áº£nh/audio vá» `FileSystem.AppDataDirectory` â†’ tráº£ `file://` cho UI.
- Cháº¡y má»—i láº§n `App.Resumed` Ä‘á»ƒ cáº­p nháº­t ngáº§m.

#### M12 â€” Äa ngÃ´n ngá»¯ giao diá»‡n
- `Services/LocalizationService.cs`: `SetLanguageCode(code)`, sá»± kiá»‡n `LanguageChanged`.
- `Localization/TranslationStrings.cs` chá»©a map vi/en/ja/ko/zh + helper `NormalizeCultureCode(code)` (cáº¯t region: `zh-CN â†’ zh`).

#### M13 â€” Gá»­i feedback app
- `IDataService.SendAppFeedbackAsync(rating, comment)` â†’ `POST /api/Feedback/app`.

#### M14 â€” Cache áº£nh
- `Services/HttpImageService.cs`: `GetImageStreamAsync(url)` cache trong `MemoryCache` vá»›i TTL 10 phÃºt, fallback nguá»“n gá»‘c khi miss.

### 3.4. Äiá»ƒm ná»•i báº­t cá»§a Ä‘á»“ Ã¡n
- **Location-based audio guide**: audio thuyáº¿t minh tá»± Ä‘á»™ng phÃ¡t theo GPS/geofence, khÃ¡c biá»‡t vá»›i cÃ¡c app du lá»‹ch báº¥m-má»›i-nghe.
- **Pipeline dá»‹ch + TTS khÃ©p kÃ­n trÃªn Web Admin**: admin nháº­p tiáº¿ng Viá»‡t â†’ Google Translate sang ngÃ´n ngá»¯ Ä‘Ã­ch â†’ Edge TTS sinh audio â†’ lÆ°u vÃ o `UploadsData` vÃ  liÃªn káº¿t vá»›i POI.
- **Smart Audio Queue** (`AudioQueueService`): xá»­ lÃ½ hÃ ng Ä‘á»£i audio khi Ä‘i qua nhiá»u POI hoáº·c Ä‘á»©ng giá»¯a cÃ¡c vÃ¹ng geofence chá»“ng láº¥n â€“ Æ°u tiÃªn theo **tiáº¿n Ä‘á»™ track (60%)** thay vÃ¬ khoáº£ng cÃ¡ch, káº¿t há»£p **dwell 8s + exit debounce 10s** á»Ÿ `GeofenceMonitorService` Ä‘á»ƒ trÃ¡nh "nhÃ¡y" do GPS nhiá»…u.
- **QR-first onboarding**: má»™t QR Ä‘áº§u phá»‘ Ä‘á»§ Ä‘á»ƒ khá»Ÿi táº¡o toÃ n bá»™ tour, khÃ´ng cáº§n Ä‘Äƒng kÃ½ trÆ°á»›c.
- **Tracking Ä‘áº§y Ä‘á»§ hÃ nh vi** (`Tracking/log`) phá»¥c vá»¥ heatmap, Ä‘áº¿m thiáº¿t bá»‹ online, thá»‘ng kÃª tá»‰ lá»‡ hoÃ n thÃ nh tour.

---

## 4. SÆ¡ Ä‘á»“ Sequence (tuáº§n tá»±)

### 4.1. SEQ-01 â€“ Du khÃ¡ch quÃ©t QR vÃ  báº¯t Ä‘áº§u tour

**MÃ´ táº£:** MÃ´ táº£ luá»“ng tá»« khi du khÃ¡ch quÃ©t QR táº¡i cá»•ng phá»‘ áº©m thá»±c Ä‘áº¿n khi audio intro Ä‘Æ°á»£c phÃ¡t. ÄÃ¢y lÃ  luá»“ng "onboarding" quan trá»ng nháº¥t quyáº¿t Ä‘á»‹nh tráº£i nghiá»‡m Ä‘áº§u tiÃªn.

![diagram](./PRD.Rendered-3.png)

**Äiá»ƒm quan trá»ng:**
- QR token luÃ´n Ä‘i qua `Qr/resolve` Ä‘á»ƒ trÃ¡nh lá»™ dá»¯ liá»‡u POI.
- Audio url lÃ  Ä‘Æ°á»ng dáº«n tÄ©nh `/uploads/...` (ASP.NET `UseStaticFiles`).
- `Tour/track-listen` ghi láº¡i viá»‡c báº¯t Ä‘áº§u phÃ¡t intro.

---

### 4.2. SEQ-02 â€“ Tá»± Ä‘á»™ng phÃ¡t audio khi vÃ o vÃ¹ng geofence POI

**MÃ´ táº£:** CÆ¡ cháº¿ geofence trigger tá»± Ä‘á»™ng â€“ mobile poll GPS má»—i 3s, pháº£i á»Ÿ trong vÃ¹ng POI **liÃªn tá»¥c 8 giÃ¢y** (dwell) má»›i phÃ¡t audio Ä‘á»ƒ trÃ¡nh giáº­t do GPS nhiá»…u. Khi káº¿t thÃºc tá»± nhiÃªn, audio káº¿ tiáº¿p Ä‘Æ°á»£c láº¥y tá»« queue.

![diagram](./PRD.Rendered-4.png)

**Äiá»ƒm quan trá»ng:**
- **Dwell 8s** ngÄƒn false-positive khi du khÃ¡ch Ä‘i ngang qua quÃ¡n mÃ  khÃ´ng dá»«ng.
- POI Ä‘Ã£ phÃ¡t xong Ä‘Æ°á»£c Ä‘Ã¡nh dáº¥u `_playedPois` â†’ khÃ´ng re-trigger dÃ¹ du khÃ¡ch quay láº¡i.
- TrÆ°á»ng há»£p **Ä‘á»©ng giá»¯a 2 geofence** Ä‘Æ°á»£c mÃ´ táº£ riÃªng á»Ÿ SEQ-07.

---

### 4.3. SEQ-03 â€“ Admin dá»‹ch ná»™i dung POI + sinh audio báº±ng Edge TTS

**MÃ´ táº£:** TrÃªn Web Admin, tá»« trang `ThuyetMinh.razor`, admin soáº¡n ná»™i dung tiáº¿ng Viá»‡t cá»§a 1 POI, dá»‹ch sang cÃ¡c ngÃ´n ngá»¯ Ä‘Ã­ch (Google Translate), sau Ä‘Ã³ sinh audio (Edge TTS) vÃ  lÆ°u file vÃ o thÆ° má»¥c `UploadsData/` Ä‘á»ƒ API phá»¥c vá»¥ qua `/uploads`.

![diagram](./PRD.Rendered-5.png)

**Äiá»ƒm quan trá»ng:**
- Hoáº¡t Ä‘á»™ng **trong Blazor Server** (khÃ´ng qua API runtime) â€“ táº­n dá»¥ng server-side Ä‘á»ƒ gá»i Google Translate vÃ  Edge TTS.
- Má»—i ngÃ´n ngá»¯ tÆ°Æ¡ng á»©ng má»™t **voice** Ä‘Æ°á»£c cáº¥u hÃ¬nh á»Ÿ `QuanLyNgonNgu.razor`.
- File audio lÆ°u vÃ o `UploadsData/` (shared vá»›i API Ä‘á»ƒ phá»¥c vá»¥ qua `/uploads`).

---

### 4.4. SEQ-04 â€“ Vendor cáº­p nháº­t gian hÃ ng & Admin duyá»‡t

**MÃ´ táº£:** Vendor Ä‘Äƒng nháº­p cÃ¹ng Blazor app, sá»­a thÃ´ng tin quÃ¡n vÃ  thá»±c Ä‘Æ¡n. Admin duyá»‡t trong `PoiList.razor`. Mobile app chá»‰ nháº­n POI á»Ÿ tráº¡ng thÃ¡i Approved qua `Poi` API.

![diagram](./PRD.Rendered-6.png)

---

### 4.5. SEQ-05 â€“ Mobile ghi log & Dashboard Ä‘á»c cÃ¹ng database

**MÃ´ táº£:** Du khÃ¡ch gá»­i sá»± kiá»‡n qua **VKFoodTour.API** (`TrackingController`). Trang Dashboard (`Home.razor`, Blazor Server) **khÃ´ng gá»i HTTP ná»™i bá»™ tá»›i API** Ä‘á»ƒ láº¥y thá»‘ng kÃª: nÃ³ vÃ  cÃ¡c widget con dÃ¹ng **EF Core** (`ApplicationDbContext` / `IDbContextFactory`) truy váº¥n trá»±c tiáº¿p **cÃ¹ng SQL Server** mÃ  API ghi vÃ o. LÃ m tÆ°Æ¡i â€œrealtimeâ€ nhá» **Timer** trong widget (khÃ´ng dÃ¹ng SignalR nghiá»‡p vá»¥).

![diagram](./PRD.Rendered-7.png)

**Ghi chÃº:**

- **Heatmap** trÃªn báº£n Ä‘á»“ tá»•ng quan náº±m á»Ÿ `/admin/ban-do` (`BanDoPoi.razor`): cÅ©ng Ä‘á»c `TrackingLogs` qua EF + Ä‘áº©y JSON sang JS (`updateOverviewHeatmap`), khÃ´ng Ä‘i qua `GET /api/Tracking/heatmap` tá»« Blazor.
- Component **`OnlineUsersWidget`** cÃ¹ng pattern (Timer + `IDbContextFactory`) nhÆ°ng **chÆ°a Ä‘Æ°á»£c nhÃºng** vÃ o `Home.razor`; trang chá»§ admin hiá»‡n dÃ¹ng **`ActiveDevicesWidget`** (`WindowSeconds=45`, `RefreshSeconds=3`).

#### 4.5.1. SEQ-05a â€“ Admin Dashboard: phÆ°Æ¡ng thá»©c trong `Home.razor` & `ActiveDevicesWidget`

**MÃ´ táº£:** Chuá»—i gá»i **theo code thá»±c táº¿** khi user role **Admin** má»Ÿ `/`. File: `Admin/Components/Pages/Home.razor`, widget: `Admin/Components/Pages/Admin/ActiveDevicesWidget.razor`.

**A) `Home.razor` â€” `OnInitializedAsync` â†’ `LoadAdminStats` (inject `ApplicationDbContext Db`)**

| Thá»© tá»± | PhÆ°Æ¡ng thá»©c / truy váº¥n |
|--------|------------------------|
| 1 | `AuthenticationStateProvider.GetAuthenticationStateAsync()` |
| 2 | `LoadAdminStats()` |
| 3 | `Db.Pois.CountAsync()` |
| 4 | `Db.Pois.CountAsync(p => p.IsActive)` |
| 5 | `Db.Narrations.CountAsync()` |
| 6 | `Db.Languages.CountAsync(l => l.IsActive)` |
| 7 | `Db.Users.CountAsync()` |
| 8 | `Db.Users.CountAsync(u => u.Role == "Vendor")` |
| 9 | `Db.TrackingLogs.CountAsync(t => t.EventType == "qr_scan" && t.CreatedAt >= today)` |
| 10 | `Db.TrackingLogs.CountAsync(t => t.CreatedAt >= today && t.EventType != "move")` |
| 11 | `listenLogs.AnyAsync()` rá»“i `listenLogs.AverageAsync(t => t.ListenedDurationSec!.Value)` (`listenLogs` = `TrackingLogs` lá»c `listen_end`) |
| 12 | `Db.TrackingLogs` â€¦ `GroupBy`/`OrderByDescending`/`Take(5)` â†’ `ToListAsync()` (top POI) |
| 13 | `Db.Pois.Where(...).ToDictionaryAsync(p => p.PoiId, p => p.Name)` |
| 14 | VÃ²ng `foreach` gÃ¡n `TopPoiItem.PoiName` |
| 15 | `Db.TrackingLogs` â€¦ `GroupBy` POI `listen_end` â†’ `ToListAsync()` (top audio) |
| 16 | `foreach` gÃ¡n `TopAudioItem.PoiName` (dÃ¹ng `poiNames`) |
| 17 | `Db.TrackingLogs` group theo `LanguageCode` â†’ `ToListAsync()` |
| 18 | `Db.Languages.ToDictionaryAsync(l => l.Code, l => l.Name)` |
| 19 | Build `languageStats` (LINQ trÃªn bá»™ nhá»›) |
| 20 | `Db.Reviews.CountAsync()` |
| 21 | `Db.Reviews.GroupBy(r => (int)r.Rating).ToDictionaryAsync(...)` â†’ `ratingDistribution` |

![diagram](./PRD.Rendered-8.png)

**B) `ActiveDevicesWidget` â€” inject `IDbContextFactory<ApplicationDbContext> DbFactory`**

| Thá»© tá»± | PhÆ°Æ¡ng thá»©c |
|--------|-------------|
| 1 | `OnInitializedAsync()` |
| 2 | `Math.Clamp(WindowSeconds, 15, 300)` |
| 3 | `LoadAsync()` |
| 4 | `DbFactory.CreateDbContextAsync()` |
| 5 | `db.TrackingLogs.AsNoTracking()â€¦GroupBy(DeviceId)â€¦ToListAsync()` |
| 6 | `db.Pois.AsNoTracking()â€¦ToDictionaryAsync(PoiId, Name)` (náº¿u cÃ³ `poiIds`) |
| 7 | GÃ¡n `activeDevices` (LINQ trÃªn bá»™ nhá»›) |
| 8 | `new Timer(...)` â€” refresh má»—i `RefreshSeconds` (3s): gá»i láº¡i `LoadAsync()` + `InvokeAsync(StateHasChanged)` |
| 9 | `new Timer(...)` â€” má»—i 1s: cáº­p nháº­t `secondsAgo` + `StateHasChanged` |
| 10 | `DisposeAsync()` â€” há»§y hai timer |

![diagram](./PRD.Rendered-9.png)

**Ghi chÃº thá»© tá»± Blazor:** `Home.OnInitializedAsync` (gá»“m `LoadAdminStats`) cháº¡y trÆ°á»›c khi subtree render xong; `ActiveDevicesWidget` khá»Ÿi táº¡o sau (lifecycle con), nÃªn block **A** hoÃ n táº¥t trÆ°á»›c **B** trong cÃ¹ng láº§n táº£i trang Ä‘áº§u.

---

### 4.6. SEQ-06 â€“ Du khÃ¡ch Ä‘Ã¡nh giÃ¡ quÃ¡n

**MÃ´ táº£:** Sau khi nghe audio vÃ  xem chi tiáº¿t quÃ¡n, du khÃ¡ch cÃ³ thá»ƒ gá»­i rating + bÃ¬nh luáº­n vá» quÃ¡n.

![diagram](./PRD.Rendered-10.png)

---

### 4.7. SEQ-07 â€“ Æ¯u tiÃªn audio khi Ä‘á»©ng giá»¯a 2 geofence chá»“ng láº¥n

**MÃ´ táº£:** Ká»‹ch báº£n du khÃ¡ch Ä‘ang nghe audio cá»§a POI A thÃ¬ bÆ°á»›c vÃ o vÃ¹ng geofence cá»§a POI B (2 vÃ¹ng chá»“ng láº¥n, hoáº·c 2 quÃ¡n cáº¡nh nhau). `AudioQueueService` quyáº¿t Ä‘á»‹nh xá»­ lÃ½ dá»±a trÃªn **tiáº¿n Ä‘á»™ track A Ä‘ang phÃ¡t** so vá»›i ngÆ°á»¡ng **60%**.

![diagram](./PRD.Rendered-11.png)

**Giáº£i thÃ­ch Ã½ nghÄ©a ngÆ°á»¡ng 60%:**
- Vá»›i ngÆ°á»¡ng **â‰¥ 60%**: track Ä‘ang phÃ¡t Ä‘Ã£ gáº§n xong â†’ cho nghe trá»n váº¹n Ä‘á»ƒ giá»¯ tráº£i nghiá»‡m liá»n máº¡ch, POI má»›i xáº¿p káº¿ tiáº¿p.
- Vá»›i ngÆ°á»¡ng **< 60%**: du khÃ¡ch vá»«a má»›i bÆ°á»›c vÃ o quÃ¡n cÅ© vÃ  chÆ°a nghe Ä‘Æ°á»£c bao nhiÃªu, giá» Ä‘Ã£ á»Ÿ gáº§n quÃ¡n má»›i â€“ Æ°u tiÃªn thÃ´ng tin vá» **vá»‹ trÃ­ hiá»‡n táº¡i**, ngáº¯t track cÅ©, phÃ¡t track má»›i, vÃ  phÃ¡t láº¡i track cÅ© sau khi track má»›i káº¿t thÃºc.
- Háº±ng sá»‘ `InterruptThreshold = 0.60` Ä‘Æ°á»£c Ä‘á»‹nh nghÄ©a trong `AudioQueueService.cs` â€“ cÃ³ thá»ƒ tune Ä‘á»ƒ thay Ä‘á»•i hÃ nh vi.

**CÃ¡c nhÃ¡nh an toÃ n khÃ¡c (code thá»±c táº¿):**
- Náº¿u POI B Ä‘Ã£ cÃ³ trong `_playedPois` â†’ **bá» qua** (khÃ´ng phÃ¡t láº¡i).
- Náº¿u POI B chÃ­nh lÃ  track Ä‘ang phÃ¡t (`CurrentlyPlaying.PoiId == B`) â†’ **bá» qua** (trÃ¡nh double-trigger do GPS jitter).
- Náº¿u `CurrentlyPlaying != null` nhÆ°ng `IsPlaying == false` (bootstrap/loading) â†’ **InsertNext** Ä‘á»ƒ trÃ¡nh race condition.

---

### 4.8. SEQ-08 â€“ Pattern CRUD cÃ³ duyá»‡t (generic)

**MÃ´ táº£:** SÆ¡ Ä‘á»“ **chung** cho táº¥t cáº£ chá»©c nÄƒng CRUD cÃ³ luá»“ng duyá»‡t trong há»‡ thá»‘ng â€“ Ã¡p dá»¥ng cho: quáº£n lÃ½ POI, thá»±c Ä‘Æ¡n, ngÆ°á»i dÃ¹ng, ngÃ´n ngá»¯, QR code, review, feedback, audioâ€¦ Khi xem má»™t chá»©c nÄƒng CRUD trong báº£ng liá»‡t kÃª (má»¥c 3), tham chiáº¿u sÆ¡ Ä‘á»“ nÃ y thay vÃ¬ váº½ láº¡i tá»«ng cÃ¡i.

![diagram](./PRD.Rendered-12.png)

**Ãp dá»¥ng cho cÃ¡c chá»©c nÄƒng:**

| Chá»©c nÄƒng | Actor | Service/Controller | File? |
|---|---|---|---|
| Quáº£n lÃ½ POI | Admin / Vendor | `PoiService` | CÃ³ áº£nh |
| Quáº£n lÃ½ thá»±c Ä‘Æ¡n | Vendor | `MenuService` | CÃ³ áº£nh |
| Quáº£n lÃ½ ngÃ´n ngá»¯ | Admin | (Blazor page) | KhÃ´ng |
| Quáº£n lÃ½ QR | Admin | (`QrController` resolve) | KhÃ´ng |
| Quáº£n lÃ½ ngÆ°á»i dÃ¹ng | Admin | `NguoiDung.razor` | KhÃ´ng |
| Gá»­i review | User | `ReviewsController` | KhÃ´ng |
| Gá»­i feedback app | User | `FeedbackController` | KhÃ´ng |
| Upload audio | Admin | `EdgeTtsService` + file write | CÃ³ file |

---

### 4.9. SEQ-09 â€“ Hiá»ƒn thá»‹ Heatmap tracking trÃªn Web Admin

**MÃ´ táº£:** Luá»“ng táº£i vÃ  hiá»ƒn thá»‹ heatmap thá»±c táº¿ trÃªn trang `BanDoPoi.razor`. Admin báº­t switch heatmap, chá»n má»‘c thá»i gian, UI gá»i API `Tracking/heatmap`, sau Ä‘Ã³ Ä‘áº©y dá»¯ liá»‡u sang JS interop Ä‘á»ƒ cáº­p nháº­t lá»›p heatmap trÃªn báº£n Ä‘á»“.

![diagram](./PRD.Rendered-13.png)

**Äiá»ƒm ká»¹ thuáº­t chÃ­nh (Ä‘Ãºng code hiá»‡n táº¡i):**
- API endpoint: `GET /api/Tracking/heatmap` trong `TrackingController`.
- UI xá»­ lÃ½ á»Ÿ `BanDoPoi.razor` vá»›i cÃ¡c hÃ m `OnToggleHeatmap()` vÃ  `ReloadHeatmapAsync()`.
- JS interop: `updateOverviewHeatmap()` vÃ  `toggleOverviewHeatmap()` trong `admin-interop.js`.

---

### 4.10. SEQ-10 â€“ Quáº£n lÃ½ ngÃ´n ngá»¯ vÃ  Ã¡nh xáº¡ TTS voice

**MÃ´ táº£:** Luá»“ng quáº£n lÃ½ ngÃ´n ngá»¯ trÃªn trang `QuanLyNgonNgu.razor`: Admin thÃªm ngÃ´n ngá»¯ má»›i, cáº¥u hÃ¬nh mÃ£ ngÃ´n ngá»¯ + voice, báº­t/táº¯t tráº¡ng thÃ¡i hoáº¡t Ä‘á»™ng. Cáº¥u hÃ¬nh nÃ y Ä‘Æ°á»£c dÃ¹ng láº¡i khi dá»‹ch ná»™i dung vÃ  sinh audio thuyáº¿t minh.

![diagram](./PRD.Rendered-14.png)

**Äiá»ƒm ká»¹ thuáº­t chÃ­nh:**
- Trang quáº£n lÃ½: `QuanLyNgonNgu.razor`.
- NgÃ´n ngá»¯ báº­t (`isEnabled=true`) lÃ  nguá»“n dá»¯ liá»‡u cho luá»“ng dá»‹ch/sinh audio á»Ÿ `ThuyetMinh.razor`.
- TTS sá»­ dá»¥ng `voice` Ä‘Ã£ Ã¡nh xáº¡ theo tá»«ng ngÃ´n ngá»¯ Ä‘á»ƒ Ä‘áº£m báº£o phÃ¡t Ã¢m Ä‘Ãºng.

---

## 5. SÆ¡ Ä‘á»“ Activity & State

### 5.1. ACT-01 â€“ HÃ nh trÃ¬nh du khÃ¡ch end-to-end trÃªn Mobile App

**MÃ´ táº£:** ToÃ n bá»™ luá»“ng cá»§a Mobile App tá»« khi má»Ÿ app (WelcomePage) Ä‘áº¿n khi káº¿t thÃºc tour.

![diagram](./PRD.Rendered-15.png)

---

### 5.2. ACT-02 â€“ Duyá»‡t POI cá»§a Admin trong `PoiList.razor`

![diagram](./PRD.Rendered-16.png)

---

### 5.3. ACT-03 â€“ Vendor cáº­p nháº­t gian hÃ ng

**MÃ´ táº£:** Vendor dÃ¹ng chung Blazor app, chá»‰ tháº¥y cÃ¡c trang `/vendor/*` theo role.

![diagram](./PRD.Rendered-17.png)

---

### 5.4. ACT-04 â€“ Dá»‹ch & sinh audio thuyáº¿t minh trong `ThuyetMinh.razor`

![diagram](./PRD.Rendered-18.png)

---

### 5.5. ACT-05 â€“ Logic Audio Queue vá»›i ngÆ°á»¡ng Æ°u tiÃªn 60%

**MÃ´ táº£:** Logic tháº­t cá»§a `HandlePoiEnteredAsync` trong `AudioQueueService.cs` khi nháº­n sá»± kiá»‡n `PoiEntered` tá»« `GeofenceMonitorService`.

![diagram](./PRD.Rendered-19.png)

**CÃ¡c háº±ng sá»‘ tham chiáº¿u trong code:**
- `GeofenceMonitorService.DwellThresholdSec = 8` â€“ pháº£i á»Ÿ trong zone 8 giÃ¢y má»›i trigger.
- `GeofenceMonitorService.ExitDebounceMs = 10_000` â€“ 10 giÃ¢y ngoÃ i zone má»›i confirm exit.
- `GeofenceMonitorService.GpsBufferMeters = 10` â€“ ná»›i bÃ¡n kÃ­nh thÃªm 10m Ä‘á»ƒ bÃ¹ GPS drift.
- `GeofenceMonitorService.PollIntervalMs = 3_000` â€“ polling 3 giÃ¢y.
- `AudioQueueService.InterruptThreshold = 0.60` â€“ ngÆ°á»¡ng quyáº¿t Ä‘á»‹nh InsertNext vs Interrupt.

---

### 5.6. STATE-01 â€“ VÃ²ng Ä‘á»i (lifecycle) cá»§a POI

**MÃ´ táº£:** SÆ¡ Ä‘á»“ state diagram thá»ƒ hiá»‡n cÃ¡c tráº¡ng thÃ¡i vÃ  chuyá»ƒn Ä‘á»•i cá»§a má»™t POI tá»« lÃºc Vendor táº¡o Ä‘áº¿n khi xuáº¥t hiá»‡n trÃªn Mobile App.

![diagram](./PRD.Rendered-20.png)

**RÃ ng buá»™c chuyá»ƒn tráº¡ng thÃ¡i:**
- Má»i thay Ä‘á»•i ná»™i dung quan trá»ng cá»§a Vendor Ä‘á»u **reset vá» `Pending`** Ä‘á»ƒ Admin xem láº¡i.
- POI chá»‰ **hiá»ƒn thá»‹ cÃ´ng khai trÃªn Mobile** khi á»Ÿ tráº¡ng thÃ¡i `Approved` hoáº·c `Published`.
- Chuyá»ƒn sang `Archived` lÃ  **soft-delete** â€“ váº«n giá»¯ log tracking lá»‹ch sá»­.

---

## 6. YÃªu cáº§u phi chá»©c nÄƒng

| Háº¡ng má»¥c | YÃªu cáº§u |
|---|---|
| Hiá»‡u nÄƒng | API < 500ms cho cÃ¡c request chÃ­nh; audio trigger theo geofence trong vÃ i giÃ¢y. CÃ³ sáºµn `load_test.js` vÃ  `stress_test.js` Ä‘á»ƒ kiá»ƒm thá»­ táº£i. |
| Báº£o máº­t | Cookie auth + Google OAuth cho Admin/Vendor; JWT cho Mobile (`AuthController`); máº­t kháº©u hash; static files phá»¥c vá»¥ qua `/uploads`. |
| Kháº£ dá»¥ng | App khÃ´ng crash khi API lá»—i â€“ `DataService` cÃ³ `FallbackDemo()` Ä‘á»ƒ hiá»ƒn thá»‹ dá»¯ liá»‡u tá»‘i thiá»ƒu. |
| Äa ngÃ´n ngá»¯ | ThÃªm ngÃ´n ngá»¯ trong `QuanLyNgonNgu.razor` â†’ sinh audio lÃ  cÃ³ thá»ƒ sá»­ dá»¥ng, khÃ´ng cáº§n build láº¡i app. |
| Tracking | Má»i hÃ nh vi chÃ­nh (`qr_scan`, `enter`, `exit`, `listen_start`, `listen_end`) Ä‘á»u Ä‘Æ°á»£c ghi qua `Tracking/log` cho heatmap vÃ  thá»‘ng kÃª. |

---

## 7. Phá»¥ lá»¥c

### 7.1. Danh sÃ¡ch event tracking (Mobile â†’ `Tracking/log`)
- `qr_scan` â€“ quÃ©t QR Ä‘áº§u phá»‘ hoáº·c QR quÃ¡n.
- `enter` / `exit` â€“ vÃ o / rá»i vÃ¹ng geofence POI (sau dwell 8s / debounce 10s).
- `listen_start` / `listen_end` â€“ báº¯t Ä‘áº§u / káº¿t thÃºc phÃ¡t audio (kÃ¨m `ListenedDurationSec`).
- `move` â€“ heartbeat vá»‹ trÃ­ (phá»¥c vá»¥ heatmap & Ä‘áº¿m thiáº¿t bá»‹ online); luÃ´n kÃ¨m `LanguageCode` Ä‘Ã£ chuáº©n hoÃ¡.

### 7.2. Danh sÃ¡ch API chÃ­nh

| Controller | Endpoint | Má»¥c Ä‘Ã­ch |
|---|---|---|
| `AuthController` | `POST /api/Auth/login`, `/register` | ÄÄƒng nháº­p/Ä‘Äƒng kÃ½ du khÃ¡ch (role User), tráº£ token + user info |
| `PoiController` | `GET /api/Poi?lang=`, `GET /api/Poi/{id}?lang=`, `GET /api/Poi/{id}/detail?lang=` | Danh sÃ¡ch / chi tiáº¿t POI Ä‘Ã£ localize theo ngÃ´n ngá»¯; chi tiáº¿t kÃ¨m menu, áº£nh, narration, audio URL |
| `LanguagesController` | `GET /api/Languages` | Danh sÃ¡ch ngÃ´n ngá»¯ active Ä‘Ã£ cÃ³ TTS voice |
| `QrController` | `GET /api/Qr/resolve/{token}?lang=` | Resolve token QR sang tour hoáº·c POI |
| `TourController` | `POST /api/Tour/start`, `GET /api/Tour/audio-queue?tourId&lang`, `POST /api/Tour/track-listen` | Báº¯t Ä‘áº§u tour, táº£i audio queue, log start/end nghe audio |
| `TrackingController` | `POST /api/Tracking/log`, `GET /api/Tracking/online-count?minutes=`, `GET /api/Tracking/heatmap?hours=` | Ghi log (server normalize `LanguageCode`), Ä‘áº¿m thiáº¿t bá»‹ online, heatmap dáº¡ng bucket toáº¡ Ä‘á»™ |
| `ReviewsController` | `GET /api/Reviews/recent?take=`, `GET /api/Reviews/poi/{poiId}`, `POST /api/Reviews` | Danh sÃ¡ch review + táº¡o review |
| `FeedbackController` | `POST /api/Feedback/app` | Gá»­i feedback á»©ng dá»¥ng |
| `SyncController` â­ | `GET /api/Sync/bootstrap?since=` | Snapshot offline: ngÃ´n ngá»¯ active + POI Approved + áº£nh + audio URL cho mobile cache |

### 7.3. ThÃ nh pháº§n dá»± Ã¡n
- `Admin/` â€“ **Web Admin + Vendor** (ASP.NET Core **Blazor Server**), phÃ¢n quyá»n theo role `Admin` / `Vendor`.
- `VKFoodTour.API/` â€“ ASP.NET Core Web API (JWT), phá»¥c vá»¥ static files `UploadsData/` qua `/uploads`.
- `VKFoodTour.Application/` â€“ project dá»± phÃ²ng cho layer Application (hiá»‡n táº¡i gáº§n nhÆ° trá»‘ng, logic Ä‘áº·t trá»±c tiáº¿p trong API controller vÃ  `Admin/Services`).
- `VKFoodTour.Infrastructure/` â€“ `ApplicationDbContext`, Entities, Migrations (EF Core).
- `VKFoodTour.Shared/` â€“ DTO dÃ¹ng chung: `PoiDto`, `PoiDetailDto`, `TourDtos`, `AuthDtos`, `ReviewDtos`, `TrackingDtos`, `QrResolveDto`, `LanguageListItemDto`, `AppFeedbackDtos`.
- `VKFoodTour.Mobile/` â€“ App **.NET MAUI** (ZXing QR, Maui Maps, Plugin.Maui.Audio) vá»›i `DataService`, `AuthSessionService`, `SettingsService`, `LocalizationService`, `FavoriteService`, `HttpImageService`, `AudioPlaybackService`, `AudioQueueService`, `GeofenceMonitorService`.
- `VKFoodTour.Mobile.Core/` â€“ Core library dÃ¹ng chung (chá»©a `PoiApiService` nhÆ° báº£n thay tháº¿ tÆ°Æ¡ng lai).
- `Database/VKFoodTour.sql` â€“ script DDL + seed.
- `UploadsData/` â€“ chá»©a file áº£nh vÃ  audio sinh ra tá»« Edge TTS.

---

_TÃ i liá»‡u nÃ y lÃ  báº£n Ä‘áº·c táº£ yÃªu cáº§u sáº£n pháº©m (PRD) cho há»‡ thá»‘ng VKFoodTour, phá»¥c vá»¥ má»¥c Ä‘Ã­ch phÃ¡t triá»ƒn, nghiá»‡m thu vÃ  bÃ¡o cÃ¡o Ä‘á»“ Ã¡n._

