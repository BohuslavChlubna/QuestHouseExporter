# VR Control Panel & Enhanced Export System

## Pøehled
Nový systém nahrazuje staré 2D UI profesionálním 3D ovládacím panelem pøipojeným pøímo k VR ovladaèi, plus vylepšený export systém s kompletními daty.

## ?? VR Control Panel

### Co je to?
- **3D tlaèítka** pøipojená fyzicky k Quest ovladaèi
- Viditelná ve VR prostoru
- Interakce pomocí trigger tlaèítka
- Haptická odezva pøi stisknutí

### Tlaèítka:
1. **Export Full House** - Exportuje celý dùm jako unified GLTF model s pospojovanými stìnami
2. **Toggle View Mode** - Pøepíná mezi AR ? InRoom ? DollHouse režimy
3. **Export SVG Plans** - Generuje SVG pùdorysy všech pater s rozmìry
4. **Export Excel** - Vytváøí detailní Excel soubory s místnostmi, stìnami, okny, dveømi
5. **Upload to Drive** - Nahraje exportované soubory na Google Drive

### Setup:
```
Unity Menu: Tools > Setup VR Control Panel
```

Automaticky:
- Najde a pøipojí panel k pravému ovladaèi
- Propojí s MRUKRoomExporter a ViewModeController
- Vypne staré 2D UI

### Manuální setup:
1. Pøidej `VRControlPanel` komponentu na `RightControllerAnchor` nebo `LeftControllerAnchor`
2. Pøiøaï odkazy na `MRUKRoomExporter` a `ViewModeController`
3. Panel se automaticky vytvoøí pøi startu

## ?? Vylepšený Export Systém

### 1. Unified GLTF House Export (`GLTFHouseExporter`)
**Co dìlá:**
- Spojuje všechny místnosti do jednoho modelu
- Správnì zarovnává multifloor (patra nad sebou)
- Vytváøí souvislé stìny mezi místnostmi
- Exportuje podlahy, stìny, stropy

**Výstup:**
- `UnifiedHouse.obj` - kompletní 3D model domu

**Použití:**
```csharp
GLTFHouseExporter.ExportUnifiedHouse(rooms, outputPath);
```

### 2. SVG Floor Plans s rozmìry (`SVGFloorPlanGenerator`)
**Co obsahuje:**
- Pùdorysy všech pater
- **Rozmìry stìn** (kóty) u každé stìny
- **Okna** (modré obdélníky) s rozmìry
- **Dveøe** (hnìdé obdélníky) s rozmìry
- Názvy místností z Quest scanù

**Výstup:**
- `Floor_0_plan.svg`
- `Floor_1_plan.svg`
- atd.

**Funkce:**
- Automatické mìøítko podle skuteèné velikosti
- Rotované texty rozmìrù (èitelné)
- Oznaèení W=šíøka okna, D=šíøka dveøí

### 3. Detailní Excel Export (`DetailedExcelExporter`)
**Co exportuje:**

#### `rooms_summary.csv`
| Room Name | Floor Level | Floor Area (m?) | Ceiling Height (m) | Num Walls | Num Windows | Num Doors |
|-----------|-------------|-----------------|-------------------|-----------|-------------|-----------|
| Ložnice   | 1           | 18.5            | 2.7               | 4         | 2           | 1         |

#### `walls_details.csv`
| Room Name | Wall Number | Length (m) | Height (m) | Start X | Start Y | Start Z | End X | End Y | End Z |
|-----------|-------------|------------|------------|---------|---------|---------|-------|-------|-------|
| Ložnice   | 1           | 4.20       | 2.70       | ...     | ...     | ...     | ...   | ...   | ...   |

#### `openings_details.csv`
| Room Name | Type   | Width (m) | Height (m) | Wall Side | Position X | Position Y | Position Z |
|-----------|--------|-----------|------------|-----------|------------|------------|------------|
| Ložnice   | Window | 1.20      | 1.50       | Wall_1    | ...        | ...        | ...        |
| Ložnice   | Door   | 0.90      | 2.10       | Wall_3    | ...        | ...        | ...        |

**Použití:**
```csharp
DetailedExcelExporter.ExportToExcel(rooms, outputPath);
```

## ?? Názvy místností z Quest

Systém automaticky používá názvy místností, které jsi zadal v Quest pøi skenování:
- Quest umožòuje pojmenovat až 10-12 místností
- Tyto názvy se ukládají do `MRUKRoom.name`
- Exportér je automaticky ète a používá v SVG a Excel souborech

### Jak pojmenovat místnosti v Quest:
1. Otevøi **Quest Settings ? Space Setup**
2. V každé naskenované místnosti klikni na **Edit Room**
3. Zadej název (napø. "Ložnice", "Obývák", "Kuchynì")
4. Názvy se automaticky objeví v exportech

## ?? Kompletní workflow

### 1. Pøíprava ve Unity:
```
Tools > Setup VR Control Panel
```

### 2. Build a nahraj na Quest

### 3. Ve VR:
1. **Stiskni B/Y** - pøepni na InRoom režim (vidíš obrysy stìn)
2. **Zkontroluj** - procházej místnosti, ovìø správnost scanu
3. **Stiskni trigger** na tlaèítko **Export Full House**
4. **Poèkej** - export zabere cca 5-30 sekund (podle poètu místností)
5. **Upload to Drive** (volitelné) - nahraj na Google Drive

### 4. Výstupní soubory:
```
/sdcard/Android/data/com.yourcompany.app/files/QuestHouseDesign/
??? UnifiedHouse.obj                    # Kompletní 3D model
??? Floor_0_plan.svg                    # Pùdorys pøízemí
??? Floor_1_plan.svg                    # Pùdorys 1. patra
??? rooms_summary.csv                   # Pøehled místností
??? walls_details.csv                   # Detaily stìn
??? openings_details.csv                # Okna a dveøe
??? export_log.txt                      # Log exportu
```

## ?? Customizace VR panelu

### Zmìna pozice panelu:
```csharp
vrPanel.panelOffset = new Vector3(0.05f, 0.02f, 0.08f);
vrPanel.panelRotation = new Vector3(-45f, 0f, 0f);
```

### Zmìna velikosti:
```csharp
vrPanel.panelSize = new Vector2(0.12f, 0.16f);
```

### Zmìna barev tlaèítek:
```csharp
vrPanel.buttonNormalColor = new Color(0.2f, 0.3f, 0.4f);
vrPanel.buttonHoverColor = new Color(0.3f, 0.5f, 0.7f);
vrPanel.buttonPressColor = new Color(0.5f, 0.7f, 1.0f);
```

## ?? Technické detaily

### Detekce oken a dveøí:
Systém hledá MRUK anchory s labels:
- `WINDOW`, `Window` ? okno
- `DOOR`, `DOOR_FRAME`, `Door` ? dveøe

### Výpoèet rozmìrù:
- **Stìny**: Distance mezi body boundary
- **Okna/Dveøe**: `MRUKAnchor.VolumeBounds.size`
- **Plocha místnosti**: Shoelace algorithm na 2D polygon
- **Výška stropu**: `CeilingAnchor.Y - FloorAnchor.Y`

### Podlažía:
Automaticky groupuje podle Y pozice:
```
FloorLevel = Round(FloorY / 3.0)  // pøedpoklad ~3m per floor
```

## ?? Troubleshooting

**Panel se nezobrazuje:**
- Zkontroluj, že `VRControlPanel` je na správném GameObject
- Ovìø, že controller je detected (check console log)
- Zkus restartovat aplikaci

**Export neobsahuje okna/dveøe:**
- Zkontroluj Quest scan - musíš oznaèit okna a dveøe pøi skenování
- Ovìø, že MRUK má naètené anchory
- Check export_log.txt

**Špatné názvy místností:**
- Pojmenuj místnosti v Quest Settings ? Space Setup
- Re-scan místnosti pokud potøeba
- Resetni MRUK cache

**Excel soubory se neotvírají:**
- Jsou to CSV soubory - otevøi v Excel jako "Import Data"
- Použij "Text Import" s delimiter = comma
- Kódování: UTF-8

## ?? Pøíklad výstupu

Pro dùm s 8 místnostmi (2 patra):
- **UnifiedHouse.obj**: ~50 MB, 15000 vertices
- **SVG plány**: 2 soubory, každý ~20 KB
- **Excel soubory**: 3 CSV, celkem ~15 KB
- **Export trvá**: ~15 sekund

## ? Checklist pøed exportem

- [ ] Quest má naskenovaných všechny místnosti
- [ ] Místnosti jsou pojmenované
- [ ] Oznaèena okna a dveøe pøi skenu
- [ ] VR Control Panel je aktivní
- [ ] MRUK je inicializovaný
- [ ] Dostatek místa na zaøízení (~100 MB free)

---

**Vytvoøeno:** 2026-01-05  
**Verze:** 2.0  
**Pro:** Quest 2/3/Pro s MRUK
