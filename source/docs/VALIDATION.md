# Validation

Release candidate: 1.0.0, checked locally on Windows x64 on 2026-10-27.

## Automated checks

- 23 assertions cover shared-edge geometry, negative coordinates, horizontal and vertical arrangements, corner-only contact, gaps and diagonal movement around the top/bottom/left/right ends of uneven neighboring displays.
- Settings checks exercise all five slider/number pairs in both directions and English-to-Turkish switching.
- All eleven effect selections are checked for the correct color-control state. The four FPS choices are checked against the stored values.
- Closing Settings is checked to hide the window without disposing it. Explicit shutdown still disposes resources.
- Ten edge and ten arrival variants render successfully. Full-edge tests deliberately use a shared span of 443..1080 and a source span of 0..1080, with nontransparent pixels asserted near both source ends. A horizontal negative-coordinate source span is also checked.
- Rainbow breakthrough output is checked for transparent pixels on the destination half-plane: no extra outline or arrival ring is drawn there.
- GDI object count remained 25 to 25 across 240 consecutive full-edge frames in the local render test. This is a short regression check, not proof against every possible lifetime leak.
- A short animation-clock sample measured approximately 15.3, 30.3, 59.9 and 120.6 callbacks/s at the 15/30/60/120 targets. These are clock callbacks, not complete rendered frames or display refreshes.

The development checks do not enable sign-in startup or write test entries to the Windows Run key. Startup command quoting, ownership checks, opt-in behavior and access-denied handling were reviewed in the implementation; an actual sign-out/sign-in cycle has not been verified.

## Manual inspection

The English/Turkish settings layouts and generated Rainbow membrane, Gradient glow and Spark burst examples were visually inspected. Screenshots are generated from the application's controls and effects, without capturing the user's desktop. Reference media and user settings are not distributed.

## Limits

Physical mouse feel, all display scaling combinations, exclusive fullscreen games, elevated applications, secure desktops and sleep/resume need additional user testing. Corner regression tests exercise geometry decisions; they do not reproduce every mouse driver or OS clipping rule. Injected input is intentionally passed through. FPS values are targets and may be limited by rendering work, timer support, scheduling and monitor refresh rate.

The runtime does not use telemetry or make network requests. The optional sign-in setting modifies only this application's current-user Run entry. It does not install a service or change other startup entries.

## Reproduce

Close a running EdgePush instance, then run `source/test.ps1` from a writable checkout. Reports and images are written to ignored `source/dist/`. The normal executable starts in the tray; use its Exit menu to stop it.

## Türkçe

Geometri, çapraz köşe hareketleri, çift yönlü slider/sayı eşlemesi, iki dil, renk seçiminin etkinliği, dört FPS seçimi ve çarpıyla tepsiye gizlenme otomatik kontrol edildi. Tam ekran kenarının iki ucunda çizilmiş piksel bulundu; Rainbow'un karşı ekran tarafında ek kontur olmadığı doğrulandı.

Başlangıç özelliği test sırasında açılmaz. Gerçek oturum kapatma/açma, her fare sürücüsü, DPI düzeni, oyun ve UAC senaryosu doğrulanmış değildir. Zamanlayıcı hızı gerçek çizilmiş kare hızı olarak sunulmaz.

## 1.0.1 gradient correction

Gradient glow now uses a cached premultiplied pixel surface instead of repeating four-pixel brushes. Its leading edge fades continuously without a tiled brush seam. Generated local test outputs and duplicate packages were moved out of the application directory. Development files are grouped under source/.


Eight gradient phase/direction cases assert monotonically increasing alpha toward the source edge and zero destination-side pixels. All existing geometry, settings, rendering and clock tests passed after the folder reorganization.
