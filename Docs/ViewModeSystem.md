# View Mode System - Dokumentace

## Pøehled
Systém pro pøepínání mezi tøemi módy zobrazení:
1. **AR Walkthrough** - standardní AR režim s passthrough
2. **In-Room** - pohled zevnitø místností s viditelnými obrysy stìn (jako LayoutXR)
3. **Doll House** - miniaturní pohled shora na celý dùm

## Komponenty

### 1. ViewModeController.cs
Hlavní kontrolér pro pøepínání mezi módy.

**Módy:**
- `ARWalkthrough` - Normální AR s passthrough kamerou
- `InRoom` - Full-scale vizualizace stìn, uživatel vidí obrysy jako by byl uvnitø
- `DollHouse` - Zmenšený model vidìný shora

**Veøejné metody:**
- `ToggleMode()` - Cykluje mezi módy: AR ? InRoom ? DollHouse ? AR
- `SetMode(Mode mode)` - Nastaví konkrétní mód

### 2. InRoomWallVisualizer.cs
Vizualizuje obrysy stìn v plném mìøítku.

**Nastavení:**
- `wallColor` - barva stìn (výchozí: svìtle modrá)
- `wallLineWidth` - tlouška èar stìn v metrech
- `wallHeight` - výchozí výška stìn (pokud není detekována)
- `showFloor` - zobrazit prùhlednou podlahu
- `showCeiling` - zobrazit prùhledný strop

**Veøejné metody:**
- `GenerateWallOutlines()` - Vytvoøí vizualizaci stìn ze MRUK dat
- `ClearWalls()` - Odstraní všechny vizualizované stìny

### 3. DollHouseVisualizer.cs
Vizualizuje místnosti jako miniaturní model.

**Nastavení:**
- `scale` - mìøítko modelu (výchozí: 0.1 = 1:10)
- `floorSpacing` - vertikální rozestup mezi podlažími
- `roomMaterial` - materiál pro místnosti

### 4. ControllerInputExporter.cs
Ovládání pomocí Quest controllerù.

**Tlaèítka:**
- **Primary Button (A/X)** - Export všech místností
- **Secondary Button (B/Y)** - Pøepínání mezi view módy

## Setup v Unity

### Automatický setup:
1. V Unity menu: `Tools > Setup View Mode Controller`
2. Tento nástroj automaticky vytvoøí:
   - ViewModeController GameObject
   - DollHouseRoot s kamerou
   - InRoomWallsRoot
   - Propojení s OVRCameraRig (pokud existuje)

### Manuální setup:
1. Pøidej `ViewModeController` komponentu do scény
2. Vytvoø prázdný GameObject "DollHouseRoot" a pøiøaï do `dollHouseRoot`
3. Vytvoø prázdný GameObject "InRoomWallsRoot" a pøiøaï do `inRoomWallsRoot`
4. Pøiøaï OVRCameraRig do `arPassthroughRoot`
5. Vytvoø kameru pro doll house pohled a pøiøaï do `dollHouseCamera`

## Použití v runtime

### Programové pøepínání:
```csharp
ViewModeController controller = FindObjectOfType<ViewModeController>();

// Pøepnout na další mód
controller.ToggleMode();

// Nastavit konkrétní mód
controller.SetMode(ViewModeController.Mode.InRoom);
```

### Controller input:
Stiskni **Secondary Button (B nebo Y)** na Quest controlleru pro pøepnutí módu.

## Customizace

### Zmìna barev stìn v InRoom módu:
```csharp
InRoomWallVisualizer visualizer = FindObjectOfType<InRoomWallVisualizer>();
visualizer.wallColor = new Color(1f, 0f, 0f, 0.8f); // èervená
visualizer.GenerateWallOutlines();
```

### Zmìna mìøítka DollHouse:
```csharp
DollHouseVisualizer visualizer = FindObjectOfType<DollHouseVisualizer>();
visualizer.scale = 0.05f; // 1:20 mìøítko
visualizer.GenerateDollHouse();
```

## Tipy

1. **InRoom mód** je nejlepší pro pochopení prostorového uspoøádání, když jste fyzicky ve skenovaném prostoru
2. **DollHouse mód** poskytuje pøehled o celém domì/bytì najednou
3. **AR Walkthrough** je standardní režim pro skenování a pohyb

## Troubleshooting

**Problém:** Stìny se nezobrazují v InRoom módu
- Zkontroluj, že MRUK.Instance.Rooms obsahuje data
- Ujisti se, že místnosti mají FloorAnchor s PlaneBoundary2D

**Problém:** DollHouse kamera neukazuje nic
- Zkontroluj pozici a rotaci dollHouseCamera
- Ujisti se, že DollHouseVisualizer.GenerateDollHouse() byl zavolán

**Problém:** Toggle tlaèítko nefunguje
- Zkontroluj, že ControllerInputExporter má referenci na ViewModeController na stejném GameObjectu
