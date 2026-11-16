# Kayıp Ada — Model ve Ses Seçimleri

> Bu dosya **Tools > Kayıp Ada > Prototip Sahnesini Kur** komutu tarafından yeniden yazılır.
> Modeli değiştirmek için: `Assets/_Game/Scripts/Editor/ModelCatalog.cs` içindeki dosya adını değiştir, komutu çalıştır.
> Ana stil **Quaternius Pirate** (low-poly). Kenney paketleri sadece Quaternius'ta olmayan eşyalar için.

## Karakterler

| Nesne | Prefab adı | Paket | Model dosyası | Boyut | Durum |
|---|---|---|---|---|---|
| Oyuncu (kaptan) | Player | Quaternius | Characters_Captain_Barbarossa.fbx | 1,9 m (yükseklik) | ✅ |
| Oduncu işçi | Worker_Lumberjack | Quaternius | Characters_Henry.fbx | 1,75 m (yükseklik) | ✅ |
| Taşıyıcı işçi | Worker_Carrier | Quaternius | Characters_Anne.fbx | 1,7 m (yükseklik) | ✅ |
| Lifçi işçi (lif toplar, Halat Atölyesi'ne götürür) | Worker_Fiber | Quaternius | Characters_Mako.fbx | 1,75 m (yükseklik) | ✅ |

## Kullanılmayan karakterler (prefab hazır, sahnede yok)

| Nesne | Prefab adı | Paket | Model dosyası | Boyut | Durum |
|---|---|---|---|---|---|
| Düşman: iskelet | Enemy_Skeleton | Quaternius | Characters_Skeleton.fbx | 1,8 m (yükseklik) | ✅ |
| Düşman: başsız iskelet | Enemy_SkeletonHeadless | Quaternius | Characters_Skeleton_Headless.fbx | 1,5 m (yükseklik) | ✅ |
| Düşman: köpek balığı korsan | Enemy_Sharky | Quaternius | Characters_Sharky.fbx | 1,9 m (yükseklik) | ✅ |
| Düşman: köpek balığı (yüzer) | Enemy_Shark | Quaternius | Characters_Shark.fbx | 3,5 m (en büyük kenar) | ✅ |
| Düşman: dokunaç | Enemy_Tentacle | Quaternius | Characters_Tentacle.fbx | 3 m (yükseklik) | ✅ |

## Taşınan kaynaklar ve para

| Nesne | Prefab adı | Paket | Model dosyası | Boyut | Durum |
|---|---|---|---|---|---|
| Odun | Wood | Quaternius | UI_Wood.fbx | 0,7 m (en büyük kenar) | ✅ |
| Taş | Stone | Quaternius | UI_Rocks.fbx | 0,6 m (en büyük kenar) | ✅ |
| Meyve — kullanılmıyor: meyve kodla üretilir (kırmızı/turuncu, yapraklı) | Fruit | KenneyNature | crop_melon.fbx | 0,5 m (en büyük kenar) | ✅ |
| Kalas — Quaternius'ta yok | Plank | KenneySurvival | resource-planks.fbx | 0,75 m (en büyük kenar) | ✅ |
| Tuğla (kesme taş) — Quaternius'ta yok | Brick | KenneySurvival | resource-stone.fbx | 0,6 m (en büyük kenar) | ✅ |
| Lif (sarı keten demeti) | Fiber | Quaternius | UI_Wheat.fbx | 0,6 m (en büyük kenar) | ✅ |
| Yelken bezi (sarılı bez) — Quaternius'ta yok | Cloth | KenneySurvival | bedroll-packed.fbx | 0,7 m (en büyük kenar) | ✅ |
| Uçan/zıplayan para | Coin | Quaternius | UI_Gold.fbx | 0,45 m (en büyük kenar) | ✅ |

## Kaynak noktaları

| Nesne | Prefab adı | Paket | Model dosyası | Boyut | Durum |
|---|---|---|---|---|---|
| Odun veren palmiye | Node_Palm_1 | Quaternius | Environment_PalmTree_1.fbx | 5,5 m (yükseklik) | ✅ |
| Odun veren palmiye | Node_Palm_2 | Quaternius | Environment_PalmTree_1.fbx | 5,2 m (yükseklik) | ✅ |
| Odun veren palmiye | Node_Palm_3 | Quaternius | Environment_PalmTree_1.fbx | 5,8 m (yükseklik) | ✅ |
| Taş veren kaya | Node_Rock_1 | Quaternius | Environment_Rock_1.fbx | 1,7 m (yükseklik) | ✅ |
| Taş veren kaya | Node_Rock_2 | Quaternius | Environment_Rock_2.fbx | 1,6 m (yükseklik) | ✅ |
| Taş veren kaya | Node_Rock_3 | Quaternius | Environment_Rock_3.fbx | 1,8 m (yükseklik) | ✅ |
| Meyve ağacı (yuvarlak taçlı; üstünde kodla üretilen kırmızı/turuncu meyveler) — Quaternius'ta yok | Node_FruitTree | KenneyNature | tree_default.fbx | 2,6 m (yükseklik) | ✅ |
| Keten/saz bitkisi (lif tarlası) — sarımsı-yeşile boyanır | FlaxClump | KenneyNature | crops_wheatStageB.fbx | 1,1 m (yükseklik) | ✅ |
| Kesilen ağacın kütüğü (tükenince) | Stump | KenneyNature | stump_round.fbx | 0,55 m (yükseklik) | ✅ |
| Tükenen kayanın yerinde kalan taşlar | Rubble | KenneyPirate | rocks-sand-a.fbx | 1,1 m (en büyük kenar) | ✅ |

## Binalar ve alanlar

| Nesne | Prefab adı | Paket | Model dosyası | Boyut | Durum |
|---|---|---|---|---|---|
| Bıçkıhane | Sawmill | Quaternius | Environment_Sawmill.fbx | 3,8 m (en büyük kenar) | ✅ |
| Taş Atölyesi (ev modeli) | StoneWorkshop | Quaternius | Environment_House3.fbx | 3,8 m (en büyük kenar) | ✅ |
| Halat Atölyesi — kodla kurulan halat çarkı (MachineBuilder), model kullanılmaz | RopeWorkshop | KenneySurvival | structure.fbx | 3,6 m (en büyük kenar) | ✅ |
| Dokuma Tezgahı — kodla kurulan el tezgahı (MachineBuilder), model kullanılmaz | Loom | KenneySurvival | structure-canvas.fbx | 3,4 m (en büyük kenar) | ✅ |
| Tayfa Barakası (korsan evi; işçiler kapısından çıkar) | Barracks | Quaternius | Environment_House1.fbx | 5 m (en büyük kenar) | ✅ |
| Pazar satıcısı (tezgahın arkasında el sallar) | Merchant | Quaternius | Characters_Sharky.fbx | 1,8 m (yükseklik) | ✅ |
| Kamp çadırı — Quaternius'ta yok | CampTent | KenneySurvival | tent-canvas.fbx | 2,6 m (en büyük kenar) | ✅ |
| Kamp ateşi | Campfire | KenneySurvival | campfire-pit.fbx | 1,1 m (en büyük kenar) | ✅ |
| Kapalı bölge çiti — Quaternius'ta yok | Fence | KenneySurvival | fence.fbx | 2 m (en büyük kenar) | ✅ |
| Bölge tabelası (ahşap) — korsan paketlerinde tabela yok | Sign | KenneyNature | sign.fbx | 3 m (en büyük kenar) | ✅ |
| Erzak aşamasında direğin tepesine çıkan korsan bayrağı | ShipFlag | KenneyPirate | flag-pirate-high.fbx | 2,2 m (yükseklik) | ✅ |
| İskele | Dock | Quaternius | Environment_Dock.fbx | 7 m (en büyük kenar) | ✅ |
| İskele direği / inşa iskelesi | DockPole | Quaternius | Environment_Dock_Pole.fbx | 2,6 m (en büyük kenar) | ✅ |
| Yarım kalmış gemi iskeleti (dekor) | ShipWreck | KenneyPirate | ship-wreck.fbx | 10 m (en büyük kenar) | ✅ |
| Geliştirme tezgahı (kampta) — Quaternius'ta yok | UpgradeBench | KenneySurvival | workbench.fbx | 1,9 m (en büyük kenar) | ✅ |
| İnşa edilen gemi (kızakta aşağıdan yukarı belirir, törende suya kayar) | Ship | Quaternius | Ship_Large.fbx | 9 m (en büyük kenar) | ✅ |

## Süsler

| Nesne | Prefab adı | Paket | Model dosyası | Boyut | Durum |
|---|---|---|---|---|---|
| Varil | Barrel | Quaternius | Prop_Barrel.fbx | 0,95 m (yükseklik) | ✅ |
| Sandık | Chest | Quaternius | Prop_Chest_Closed.fbx | 0,85 m (en büyük kenar) | ✅ |
| Çapa (kumda yatık) | Anchor | Quaternius | Prop_Anchor.fbx | 1,7 m (en büyük kenar) | ✅ |
| Kova | Bucket | Quaternius | Prop_Bucket.fbx | 0,5 m (yükseklik) | ✅ |
| Balık kovası | BucketFish | Quaternius | Prop_Bucket_Fishes.fbx | 0,55 m (yükseklik) | ✅ |
| Top | Cannon | Quaternius | Prop_Cannon.fbx | 1,7 m (en büyük kenar) | ✅ |
| Gülle | CannonBall | Quaternius | Prop_CannonBall.fbx | 0,3 m (en büyük kenar) | ✅ |
| Şişe | Bottle | Quaternius | Prop_Bottle_1.fbx | 0,35 m (yükseklik) | ✅ |
| Kafatasları | Skulls | Quaternius | Environment_Skulls.fbx | 0,7 m (en büyük kenar) | ✅ |
| Büyük kemikler (kumda) | Bones | Quaternius | Environment_LargeBones.fbx | 2,6 m (en büyük kenar) | ✅ |
| Kasa | Crate | KenneyPirate | crate.fbx | 0,8 m (yükseklik) | ✅ |
| Şişe kasası | CrateBottles | KenneyPirate | crate-bottles.fbx | 0,8 m (yükseklik) | ✅ |
| Kumda taşlar | SandRocks_A | KenneyPirate | rocks-sand-a.fbx | 1,1 m (en büyük kenar) | ✅ |
| Kumda taşlar | SandRocks_B | KenneyPirate | rocks-sand-b.fbx | 1 m (en büyük kenar) | ✅ |
| Kumda taşlar | SandRocks_C | KenneyPirate | rocks-sand-c.fbx | 0,9 m (en büyük kenar) | ✅ |
| Küçük çakıl (kabuk yerine) | Pebble_A | KenneyNature | stone_smallFlatA.fbx | 0,35 m (en büyük kenar) | ✅ |
| Küçük çakıl (kabuk yerine) | Pebble_B | KenneyNature | stone_smallFlatB.fbx | 0,3 m (en büyük kenar) | ✅ |
| Ot öbeği | GrassTuft | KenneyNature | grass_leafs.fbx | 0,6 m (yükseklik) | ✅ |
| Büyük ot öbeği | GrassPatch | KenneyNature | grass_leafsLarge.fbx | 0,9 m (en büyük kenar) | ✅ |
| Uzun ot | Grass | KenneyNature | grass_large.fbx | 0,6 m (yükseklik) | ✅ |
| Sarı çiçek | FlowerYellow | KenneyNature | flower_yellowA.fbx | 0,45 m (yükseklik) | ✅ |
| Kırmızı çiçek | FlowerRed | KenneyNature | flower_redA.fbx | 0,45 m (yükseklik) | ✅ |
| Küçük çalı | Bush | KenneyNature | plant_bushSmall.fbx | 0,9 m (yükseklik) | ✅ |
| Mor çiçek | FlowerPurple | KenneyNature | flower_purpleA.fbx | 0,45 m (yükseklik) | ✅ |
| Sarı çiçek öbeği | FlowerYellowB | KenneyNature | flower_yellowB.fbx | 0,5 m (yükseklik) | ✅ |
| Küçük kaya (doğal öbeklerde) | RockTiny | KenneyNature | rock_smallA.fbx | 0,4 m (en büyük kenar) | ✅ |
| Yuvarlak çalı (doğal öbeklerde) | BushRound | KenneyNature | plant_bushDetailed.fbx | 1 m (yükseklik) | ✅ |
| Süs palmiyesi (kenarlarda) | DecorPalm | Quaternius | Environment_PalmTree_1.fbx | 5 m (yükseklik) | ✅ |
| Kayalık arkasında yamaç | Cliff | Quaternius | Environment_Cliff1.fbx | 3,2 m (yükseklik) | ✅ |
| Süs kayası | BigRock | Quaternius | Environment_Rock_4.fbx | 1,3 m (yükseklik) | ✅ |
| Küçük süs kayası | SmallRock | Quaternius | Environment_Rock_5.fbx | 0,7 m (yükseklik) | ✅ |
| Kırık gemi/iskele parçası | BrokenDock | Quaternius | Environment_Dock_Broken.fbx | 3 m (en büyük kenar) | ✅ |
| Tabela | Signpost | KenneySurvival | signpost.fbx | 1,5 m (yükseklik) | ✅ |
| Örs (atölye süsü) | Workbench | KenneySurvival | workbench-anvil.fbx | 1,1 m (en büyük kenar) | ✅ |
| Kütük yığını (bıçkıhane süsü) | LogPile | KenneyNature | log_stack.fbx | 1,4 m (en büyük kenar) | ✅ |

## Bulunamayan / içe aktarılamayan modeller

Yok — tüm modeller yüklendi.

## Paketlerde olmayanlar ve çözümü

- Kumda deniz kabuğu: hiçbir pakette yok → küçük yassı çakıllar kullanıldı (Pebble_A/B).
- Taşıma animasyonu: Quaternius karakterlerinde yok → kodla taşıma pozu (CharacterVisual).
- Meyve: Quaternius'ta yok → Kenney Nature kavun (crop_melon).
- Tuğla: hiçbir pakette tuğla yok → Kenney Survival kesme taş (resource-stone).
- Halat: hiçbir pakette yok → kodla üretilen halat halkası (ArtGenerator.BuildRopeMesh).
- Havai fişek ve su sıçraması: partikül sistemiyle kodla üretildi (PrefabFactory).
- Dalga/deniz ortam sesi: Audio klasöründe yok → AudioManager yumuşak dalga sesini kendisi üretir.
- Quaternius renk dokusu (Atlas_Pirate.png): indirilen FBX klasöründe yoktu → Quaternius'un resmi paylaşımından eklendi (CC0).

## Font
- **Kayip Display** = Google Fonts'taki **Lilita One** (SIL Open Font License 1.1) + eklenen Türkçe harfler (İ, Ğ, ğ, Ş, ş; fontun kendi harf ve işaretlerinden birleştirildi). OFL gereği değiştirilmiş sürüm "Lilita" adını taşıyamaz. Dosya: `Assets/_Game/Art/Fonts/KayipDisplay-Regular.ttf`, lisans: `OFL.txt`, açıklama: `BENIOKU.txt`.

## Kodla üretilenler
- Kaynak ikonları (`Art/Generated/Icon_*.png`, IconGenerator), meyve modeli (FruitMesh), omurga ve kaburgalar,
  gemi parçaları (gövde/direkler/yelkenler Quaternius gemisinden bölünür: `Art/Generated/Ship_*.asset`), kızak,
  pop ve vuuş sesleri (`Assets/_Game/Audio/Generated`), kuş ve dalga ortam sesleri (AudioManager).

## Sesler (`Assets/_Game/Data/SoundLibrary.asset`)

Modern, 8-bit olmayan, hepsi CC0. Her sesin kaynağı, yazarı ve lisansı: **Docs/Lisanslar.md**. Adaylar:
`Assets/_Game/ThirdParty/Audio/Adaylar/<ihtiyaç>/`; yedek: `ThirdParty/Audio/Claude_Uretim`. Toplama sesleri kısık ve
3-4 çeşitli (perde ±%8), para ve kutlamalar öne çıkar. Arka plan müziği kutlamalarda kısılır, finalde zafer müziğine geçer.

