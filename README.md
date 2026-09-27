# SIGLENT SDS1102CML+ Viewer

Prosta aplikacja Windows do podglądu CH1/CH2 i pobierania przebiegów do CSV.
C# / WinForms, szary interfejs i czcionka Consolas. Bez EasyScopeX, NI-VISA, pakietów NI i zależności NuGet.

## Pobieranie

Gotowe wydania Windows x64: [GitHub Releases](https://github.com/Wilk33/Sig.-SDS-1102CML-App/releases).
Rozpakuj archiwum i uruchom `SDS1102CML.Viewer.exe`.
Wydanie zawiera środowisko .NET; SDK nie jest potrzebne do uruchomienia.
Aplikacja nie wymaga uprawnień administratora.

## Zakres

- Połączenie LAN VXI-11 albo USBTMC przez WinUSB.
- Cykliczny podgląd przebiegów CH1 i CH2. Odznaczenie „Podgląd” zatrzymuje wyłącznie odświeżanie programu.
- „Pobierz przebieg” zachowuje pełny odebrany blok próbek w pamięci aplikacji.
- „Zapisz CSV” zapisuje ostatni ręcznie pobrany przebieg. Późniejsze odświeżenia podglądu go nie zastępują.
- Start, Stop i Auto Setup wysyłane tylko po kliknięciu.
- Program nie zmienia skali, wyzwalania, tłumienia sond ani stanu kanałów podczas łączenia i odczytu.
- Zaznaczenie CH1/CH2 wybiera kanały odczytywane. Kanał musi być włączony na oscyloskopie.

## Ethernet

1. Podłącz oscyloskop i komputer do tej samej sieci.
2. Ustaw adres IP oscyloskopu lub DHCP.
3. Wybierz LAN, wpisz IP, kliknij Połącz.

Program używa VXI-11 (portmapper TCP 111 i port przydzielony przez urządzenie).
SDS1000CML+ nie obsługuje zwykłego SCPI socket na porcie 5025.
Program nie skanuje sieci ani nie zmienia zapory.
Adres IP jest zapisywany lokalnie w `%LOCALAPPDATA%/SDS1102CML.Viewer/settings.json`.

## USB bez NI

1. Ustaw w oscyloskopie `Back USB = USBTMC`.
2. Połącz tylny port USB Device z komputerem.
3. Interfejs oscyloskopu musi mieć sterownik **WinUSB**.
4. Wybierz USB, Odśwież USB, właściwe urządzenie SIGLENT i Połącz.

Program używa systemowego WinUSB przez Windows API. Nie instaluje sterowników.
Jeżeli Windows ma przypisany inny sterownik, WinUSB można przypisać narzędziem
[Zadig](https://zadig.akeo.ie/), wybierając wyłącznie interfejs USBTMC oscyloskopu SIGLENT.
Zmiana dotyczy tego urządzenia i może zmienić jego dostępność dla innych programów.
Nie zmieniaj sterownika koncentratora, klawiatury ani innych urządzeń.
W razie potrzeby pierwotny sterownik przywraca się w Menedżerze urządzeń.
Obsługiwane wykrywanie USB: identyfikator producenta SIGLENT VID F4EC.

## Start / Stop / Auto

- Stop odczytuje dotychczasowy tryb wyzwalania, a następnie zatrzymuje akwizycję.
- Start wznawia odczytany lub zapamiętany tryb. Jeżeli urządzenie jest już zatrzymane
  i poprzedniego trybu nie udało się ustalić, Start wybiera `TRMD AUTO`.
- Auto oznacza `ASET` / Auto Setup i zmienia ustawienia pomiarowe urządzenia.
- Odczyt i zapis nie wykonują automatycznego Stop ani Auto.

Odczyty kanałów są sekwencyjne. Przy pracującym oscyloskopie CH1 i CH2 mogą
pochodzić z różnych akwizycji. Aby porównać zatrzymane przebiegi, naciśnij Stop
przed „Pobierz przebieg”. Program nie obiecuje ciągłego zapisu bez przerw.

## CSV i skalowanie

Kolumny: `channel,time_s,voltage_V,captured_utc`.
Czas w sekundach pochodzi z deskryptora przebiegu, napięcie w woltach.
`captured_utc` to czas odbioru danych przez komputer, nie sprzętowy znacznik wyzwolenia.
Przecinek oddziela kolumny, kropka jest separatorem dziesiętnym. Import do polskiego
Excela należy wykonać przez import CSV z tymi ustawieniami.

Program pobiera `C1:WF? ALL` / `C2:WF? ALL`, odczytuje blok `WAVEDESC`,
kolejność bajtów, szerokość próbek, gain, offset i interwał czasu.
Nie używa wzoru czasu z nowszych modeli X-E.
Ustawia wyłącznie parametry transferu `WFSU SP,1,NP,0,FP,0`.
Nie zakłada, że każda odpowiedź ma maksymalną głębokość pamięci urządzenia:
liczba rzeczywiście odebranych punktów jest widoczna w programie.

Wykres stosuje min/max przy ograniczaniu punktów do szerokości ekranu.
CSV zawiera wszystkie odebrane próbki, bez tego ograniczenia.
Niepełny lub nierozpoznany deskryptor jest odrzucany.

## Stan wersji 0.1.0

Zaimplementowane oba transporty, dekoder, interfejs i eksport.
Testy obejmują bloki binarne, podpisane próbki, skalowanie deskryptora, CSV,
ramki USBTMC, odczyt bez zmiany ustawień oraz sesję VXI-11 przez lokalny TCP.
Interfejs sprawdzany jest w dwóch rozmiarach okna.

**Ta wersja nie została jeszcze sprawdzona z fizycznym SDS1102CML+.**
Testy programowe nie potwierdzają zgodności konkretnego firmware,
skalowania na rzeczywistych danych, działania sterownika USB ani szybkości podglądu.
Nie ma ukrytego trybu demonstracyjnego w aplikacji.

## Budowanie

Windows i SDK .NET 10:

```powershell
./build.ps1
./build.ps1 -Publish
dotnet run --project tests/Scope.UiTests -c Release
```

Kod: tabulatory, klamry w nowej linii, bez przeformatowywania niezwiązanych fragmentów.
Każde wydanie ma tag wersji oraz archiwum w GitHub Releases.

## Licencja

[PolyForm Noncommercial License 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0).
Pełny, niezmieniony tekst w pliku LICENSE. Kod aplikacji jest dostępny do zastosowań
dozwolonych tą licencją. Środowisko .NET zachowuje własne licencje i informacje.
Consolas jest używana z systemu Windows; plik czcionki nie jest dystrybuowany.

## Źródła techniczne

- [SIGLENT CML+/DL+ User Manual](https://www.siglenteu.com/wp-content/uploads/dlm_uploads/2020/12/SDS1000CML_UserManual_UM0101A-E02B.pdf)
- [SIGLENT Remote Programming Manual dla CML/CML+](https://siglentna.com/wp-content/uploads/dlm_uploads/2017/10/ProgrammingGuide_forSDS-1-1.pdf)
- [SIGLENT Programming Guide EN02E](https://www.siglenteu.com/wp-content/uploads/dlm_uploads/2025/11/SDS1000-SeriesSDS2000XSDS2000X-E_ProgrammingGuide_EN02E.pdf)
- [SIGLENT LAN/VXI-11](https://siglentna.com/operating-tip/lan-setup-on-a-sds1000-series/)
- [SIGLENT - obsługa portów](https://siglentna.com/operating-tip/instrument-socket-and-telnet-port-information/)
- [Microsoft - WinUSB](https://learn.microsoft.com/en-us/windows-hardware/drivers/usbcon/using-winusb-api-to-communicate-with-a-usb-device)
- [VXI-11 definicje protokołu](https://github.com/python-ivi/python-vxi11/blob/master/vxi11/vxi11.py)
- [sigrok - porównanie formatu starszych SDS](https://github.com/sigrokproject/libsigrok/tree/master/src/hardware/siglent-sds)

Implementacja własna; nie skopiowano kodu sigrok ani python-vxi11.
