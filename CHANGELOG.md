# Zmiany

## 0.3.0 - 2026-09-28

- Dodano stale widoczny stan akwizycji `START`, `STOP`, `NIEZNANY` lub `OFFLINE`, odczytywany poleceniem `SAST?`.
- Polecenia Start, Stop i Auto są kolejkowane za trwającym odczytem, dlatego pierwsze kliknięcie nie ginie podczas aktywnego podglądu.
- Oczekujące polecenie blokuje rozpoczęcie kolejnego automatycznego odświeżenia.
- Pasek tytułu, normalne menu i standardowe kontrolki dziedziczą motyw Windows. Dodano obsługę ciemnego paska DWM i menu na Windows 10.
- Dodano test kolejności operacji, test odpowiedzi `SAST` i kontrolę ciemnego paska tytułu.
- Połączenie LAN, odczyt CH1/CH2, CSV oraz Start/Stop zweryfikowano na fizycznym SDS1102CML+ z firmware 6.01.01.25.

## 0.2.1 - 2026-09-28

- Przycisk połączenia pokazuje wyłącznie status Offline albo Online.
- Usunięto komunikat o USB z głównego okna.
- Zastąpiono niestandardowy wygląd menu standardowym paskiem systemowym Windows.
- Usunięto grafiki, ikony paska tytułu i przyciski Zamknij z okien Autor i Licencja.
- Okna informacyjne zamyka się standardowym przyciskiem X na pasku tytułu.

## 0.2.0 - 2026-09-28

- Dodano przekazane ikony PNG i ICO do aplikacji oraz okien informacyjnych.
- Przeniesiono status Offline/Online do przycisku Połącz/Rozłącz.
- Usunięto osobny napis Offline i opis podglądu.
- Dodano pasek `O Aplikacji` z pozycjami Autor i Licencja.
- Dodano okno autora: Mateusz Skipor, Inżynier Technik Elektroniki,
  mskiporsklep@op.pl.
- Dodano okno z pełnym tekstem PolyForm Noncommercial License 1.0.0.
- Zablokowano wybór USB i oznaczono tę metodę jako nietestowaną oraz niewdrożoną.
- Rozszerzono test UI o nowe zachowania i dwa rozmiary okna.

## 0.1.0 - 2026-09-27

Pierwsze wydanie aplikacji Windows:

- Szare okno WinForms, Consolas i wykres CH1/CH2.
- Połączenie VXI-11 przez Ethernet i USBTMC przez WinUSB bez NI.
- Pobieranie bloków WAVEDESC i eksport CSV.
- Jawne Start/Stop i Auto Setup.
- Oddzielenie zapisanego przebiegu od aktualizowanego podglądu.
- Testy dekodowania, transportu, poleceń i interfejsu.

Wersja zweryfikowana programowo, bez testu na fizycznym oscyloskopie.
