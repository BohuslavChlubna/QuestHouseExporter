# ADB Tools - Dokumentace

## Pøehled
Editor nástroje pro práci s Quest headsetem pøes ADB (Android Debug Bridge).

## Menu: Tools ? QuestHouseDesign ? ADB

### ?? Build & Deploy

#### **Build APK**
- Sestaví APK soubor bez instalace
- Uloí do: `Builds/Android/QuestHouseDesign.apk`
- Zobrazí velikost souboru po dokonèení

#### **Build and Install APK** ?
- Sestaví APK
- Automaticky nainstaluje na Quest
- Automaticky spustí aplikaci
- **Nejpouívanìjší funkce pro vıvoj!**

#### **Uninstall App** ???
- Odinstaluje aplikaci z Quest zaøízení
- **Uiteèné kdy:**
  - Instalace sele kvùli konfliktu verzí
  - Chcete èistou instalaci
  - Potøebujete smazat všechna data aplikace
- ?? **Varování:** Smae všechna data vèetnì exportù!

---

### ?? Wireless (WiFi pøipojení)

#### **Enable Wireless ADB**
1. Pøipojte Quest pøes USB
2. Spuste tuto funkci
3. Odpojte USB kabel
4. Quest je pøipraven pro WiFi spojení

**Co se stane:**
- Quest pøejde do wireless ADB módu (port 5555)
- Automaticky detekuje IP adresu Questu
- Uloí IP pro budoucí pouití

#### **Connect to Quest**
- Otevøe okno pro zadání IP adresy
- **Zobrazuje aktuální status:**
  - ? Connected: 192.168.1.100 (zelená)
  - ?? USB connected (modrá)
  - ? Not connected (èervená)
- Tlaèítko **Refresh** pro aktualizaci statusu
- Po pøipojení mùete buildovat a instalovat bez kabelu!

**Jak najít IP adresu Quest:**
```
Quest headset:
Settings ? WiFi ? Advanced ? IP Address
```

#### **Disconnect Wireless**
- Odpojí wireless ADB spojení
- Quest se vrátí do USB módu
- Musíte znovu pøipojit USB nebo pouít "Enable Wireless ADB"

---

### ?? Data Management

#### **Pull Exports from Device**
- Stáhne exportované soubory z Quest
- Uloí do: `PulledExports/`
- Automaticky otevøe sloku po dokonèení

**Co se stáhne:**
- OBJ modely místností
- SVG pùdorysy
- JSON metadata
- Export logy

---

## ?? Typickı Workflow

### První nastavení (jednou):
```
1. Pøipojte Quest pøes USB
2. Tools ? ADB ? Wireless ? Enable Wireless ADB
3. Zapište si IP adresu (napø. 192.168.1.100)
4. Odpojte USB kabel
5. Tools ? ADB ? Wireless ? Connect to Quest
6. Zadejte IP adresu
```

### Denní vıvoj (bez USB kabelu):
```
1. Tools ? ADB ? Build and Install APK
2. Aplikace se automaticky spustí na Quest
3. Testování...
4. Úpravy kódu...
5. Opakujte krok 1
```

### Èistá instalace:
```
1. Tools ? ADB ? Uninstall App
2. Tools ? ADB ? Build and Install APK
3. Aplikace se nainstaluje znovu od zaèátku
```

### Staení dat z Quest:
```
1. Exportujte data v aplikaci
2. Tools ? ADB ? Pull Exports from Device
3. Soubory jsou v PulledExports/
```

---

## ?? Connection Status

### ? Wireless Connected
```
Status: ? Connected: 192.168.1.100
- Build and Install funguje pøes WiFi
- Rychlost závisí na WiFi síti
- ádnı kabel není potøeba
```

### ?? USB Connected
```
Status: ?? USB connected (wireless not active)
- Build and Install funguje pøes USB
- Rychlejší ne WiFi
- Kabel je nutnı
```

### ? Not Connected
```
Status: ? Not connected
- Build and Install nefunguje
- Pøipojte Quest (USB nebo WiFi)
```

---

## ?? Troubleshooting

### "ADB Not Found"
**Øešení:**
1. Stáhnìte Android Platform Tools
2. Pøidejte do PATH nebo dejte do: `C:\platform-tools\`

### "No Device Detected"
**Øešení:**
- USB: Zkontrolujte kabel, povolte USB debugging
- WiFi: Zkontrolujte IP adresu, quest musí bıt na stejné WiFi

### "Install Failed"
**Øešení:**
1. Tools ? ADB ? Uninstall App
2. Tools ? ADB ? Build and Install APK

### Wireless nefunguje
**Øešení:**
1. Pøipojte USB
2. Tools ? ADB ? Wireless ? Enable Wireless ADB
3. Odpojte USB
4. Tools ? ADB ? Wireless ? Connect to Quest

---

## ?? Poznámky

- **Quest a PC musí bıt na stejné WiFi síti** pro wireless ADB
- **USB debugging musí bıt povolen** v Quest Developer Settings
- **Wireless ADB se resetuje** po restartování Quest (musíte znovu Enable)
- **IP adresa se mùe zmìnit** po restartování routeru
- **Build trvá nìkolik minut** - buïte trpìliví
- **Uninstall smae všechna data** - zálohujte exporty pøed odinstalací!

---

## ?? Pro-Tips

1. **Pouívejte wireless ADB** - pohodlnìjší ne kabel
2. **Nastavte static IP** pro Quest v routeru - IP se nebude mìnit
3. **Ulote IP do záloek** - rychlejší pøipojení
4. **Refresh status** v Connect oknì pøed pøipojením
5. **Uninstall pøed velkımi zmìnami** - zabrání konfliktùm
