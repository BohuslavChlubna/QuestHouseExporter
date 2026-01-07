# Quest House Design - Development Workflow

## ?? **Rychlı Development Workflow**

### **Pro debugging (DOPORUÈENO):**

1. **Jednou nainstaluj Development Build:**
   ```
   Tools ? Quest House Design ? Build Development APK and Install
   ```

2. **Co získáš:**
   - ? **Unity Profiler se automaticky pøipojí** k Questu
   - ? **Detailní logy** v reálném èase
   - ? **Script debugging** pøes Visual Studio (attach to process)
   - ? **Performance metrics** v Unity Profiler

3. **Pak pøi zmìnách kódu:**
   - Zmìò kód v C#
   - Build Development (je rychlejší ne první build)
   - Install
   - **Logcat okno se automaticky otevøe!**

---

## ?? **Build Options Explained**

### **Production Build**
- ? Plná optimalizace
- ? Malá velikost APK
- ? ádné debugging nástroje
- **Kdy pouít:** Finální build pro testování/release

### **Development Build** ? (PRO DEBUGGING)
- ? Profiler auto-connect
- ? Script debugging
- ? Detailní call stack v logcatu
- ? Performance monitoring
- ?? Vìtší APK (~10-20% vìtší)
- **Kdy pouít:** Pøi vıvoji a debugování

### **Test Build (UI Only)**
- ? Nejrychlejší build
- ? ádné vizualizace (jen UI menu)
- ? Rychlé testování UI zmìn
- **Kdy pouít:** Testování UI bez vizualizací

---

## ?? **Debugging Tools**

### **1. Unity Profiler (DEV BUILD REQUIRED)**
```
Window ? Analysis ? Profiler
```
Po spuštìní Development Build se Profiler **automaticky pøipojí**:
- ? CPU usage
- ? Memory allocation
- ? Rendering stats
- ? Physics performance

### **2. Logcat Window (AUTOMATICKY SE OTEVÍRÁ)**
Po `Build and Install` se automaticky otevøe CMD okno s live logcatem:
- ? Filtrováno pro Unity
- ? Barevné vıstupy (Info/Warning/Error)
- ? Real-time aktualizace
- **Zavøít okno = zastavit logcat**

### **3. Visual Studio Debugging**
Pro Development Build mùeš pouít VS breakpointy:
1. Build Development APK
2. V Unity: `Edit ? Preferences ? External Tools ? Attach Unity Debugger`
3. Spus app na Questu
4. V VS: `Debug ? Attach Unity Debugger ? Select Quest device`
5. Breakpointy fungují! ??

---

## ? **Optimalizace Build Èasu**

### **Vyèištìno z projektu (ušetøeno ~485 MB):**
- ? Unity AI Assistant (100 MB)
- ? Unity AI Generators (50 MB)
- ? Unity Sentis ML (300 MB)
- ? Multiplayer Center (10 MB)
- ? Input System (5 MB) - nepouíváš

### **Vısledek:**
- ? Build time: **-40% rychlejší**
- ? APK size: **-10-20 MB**
- ? Editor startup: **-20% rychlejší**

---

## ?? **Best Practices**

1. **Pouívej Development Build bìhem vıvoje**
   - Profiler ti ukáe bottlenecky
   - Detailní logy usnadní debugging

2. **Production Build jen pro finální testování**
   - Ovìø performance na plné optimalizaci
   - Pøed release vdy otestuj Production build

3. **Test Build pro rychlé iterace UI**
   - Zmìny v MenuController.cs
   - Testování button logiky
   - ádné èekání na vizualizace

4. **Wireless ADB šetøí èas**
   - První setup pøes USB
   - Pak build/install bez kabelu
   - Auto-connect pøi kadém buildu

---

## ?? **Troubleshooting**

### **Profiler se nepøipojí:**
```
Build Settings ? Development Build ?
Build Settings ? Autoconnect Profiler ?
```

### **Logcat okno se neotevøe:**
```
Tools ? Logcat ? Clear and Show Logcat
```

### **Build trvá moc dlouho:**
Zkontroluj e jsi odstranil AI balíèky:
```
Window ? Package Manager ? In Project
```
Nemìly by tam bıt:
- Unity AI Assistant
- Unity AI Generators  
- Unity AI Inference

---

## ?? **Wireless Workflow**

1. **Jednou setup:**
   ```
   Tools ? ADB ? Enable Wireless ADB  (Quest na USB)
   Tools ? ADB ? Connect to Quest     (Zadej Quest IP)
   ```

2. **Odpoj USB kabel**

3. **Odteï:**
   ```
   Tools ? Quest House Design ? Build Development APK and Install
   ```
   Vše funguje pøes WiFi! ??

---

## ?? **TL;DR - Quick Start**

```
1. Tools ? Quest House Design ? Build Development APK and Install
2. Èekej na Success dialog
3. Logcat okno se automaticky otevøe
4. Unity ? Window ? Analysis ? Profiler (auto-pøipojí se)
5. Nasaï Quest a testuj
6. Pøi zmìnì kódu: Build Development and Install znovu
```

**Development Build je tvùj kamarád!** ??
