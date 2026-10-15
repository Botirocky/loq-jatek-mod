# LOQ Játék Mód

Windows-alkalmazás, ami egy **Lenovo LOQ (Intel Raptor Lake + RTX 5060 Laptop GPU)** gépet
egy kattintással játékra állít – és egy kattintással vissza is.

Más gépen is elindul, de figyelmeztet, hogy nem arra készült.

## Mit állít be

| Beállítás | Mit csinál | Visszaállítható |
|---|---|---|
| Csúcsteljesítmény energiaséma | „Ultimate Performance” séma (ha nem elérhető: „Nagy teljesítmény”) | igen, az előző séma |
| Lenovo teljesítmény-mód | A Lenovo GameZone „Performance” módja (ugyanaz, mint az Fn+Q) | igen, az előző mód |
| Windows Játék mód | A futó játék kap elsőbbséget | igen |
| Hardveres GPU-ütemezés (HAGS) | Kisebb késleltetés, DLSS 3 képkocka-generáláshoz kell (újraindítás) | igen |
| Háttérfelvétel (Game DVR) ki | Nincs folyamatos háttér-videófelvétel | igen |
| Optimalizálás ablakos játékokhoz | Ablakos/keret nélküli módban is gyors megjelenítés, VRR | igen |
| Egérgyorsítás ki | Egyenletes egérmozgás, pontosabb célzás | igen |

Ezen felül:

- **Játék hozzáadása (NVIDIA GPU):** a kiválasztott `.exe` mindig az RTX 5060-on fut, nem az Intel
  integrált GPU-n (Windows grafikus beállítás: „Nagy teljesítmény”).
- **Gépinformáció:** modell, processzor, memória, GPU-k és a pontos NVIDIA driververzió.
- **Driverek:** gyors link az NVIDIA App és a Lenovo támogatási oldal letöltéséhez.

Minden módosítás előtti értéket a `C:\ProgramData\LoqJatekMod\mentes.json` őriz meg; a
**Minden visszaállítása** gomb ebből állít vissza. Ha egy visszaállítás nem sikerül, a mentett
érték megmarad, és később újra lehet próbálni. **Eltávolításkor a telepítő először mindent
visszaállít**, csak utána törli a mentést.

A felhasználói beállítások (Játék mód, Game DVR, egér, játékonkénti GPU) a bejelentkezett
fiókra vonatkoznak. Ha a UAC-ablakban **másik** rendszergazda-fiók adatait adod meg, ezek annak
a fióknak a beállításai lesznek – ezért a saját, rendszergazdai jogú fiókodból futtasd.

Tudatosan **nem** nyúl hozzá: Windows Defender, memóriaintegritás (VBS), szolgáltatások
letiltása, telemetria – ezek biztonsági vagy stabilitási kockázattal járnak.

## Telepítés

A [Releases](https://github.com/Botirocky/loq-jatek-mod/releases) oldalról töltsd le a
`LoqJatekMod-<verzió>-telepito.exe` fájlt, és futtasd. Rendszergazdai jog kell (registry,
energiaséma, Lenovo WMI). A program önálló, .NET-et nem kell külön telepíteni.

## Fordítás

```
dotnet publish src/LoqJatekMod -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\LoqJatekMod.iss
```

A GitHub Actions minden `v*` tagre lefordítja és kiadja a telepítőt.

## Licenc

MIT
