1.
- Przygotowano i uruchomiono build deweloperski i produkcyjny Windows
- Zaimplementowano licznik FPS i czasu klatki (ms), aktualizowany co 0.5s
- Dodano logi diagnostyczne [DEV] występujące wyłącznie w buildzie deweloperskim
- Dodano ustawienie Fullscreen/Windowed z zapisem preferencji
- Przeprowadzono 30-sekundowy benchmark z wyłączonym VSync i limitem FPS:
    - Development - 1219,48 FPS, 173 MB
    - Production - 1604,77 FPS, 109 MB
    - Wersja produkcyjna osiągnęła wydajność wyższą o 31,59%
2.
    - Dodano wykrywanie kontrolerów
    - Dodano sterowanie kontrolerem
    - Podczas wykrycia kontroleru zmienia tekst w UI
3.
    - Dodano ekran startowy z wyborem profilu użytkownika
    - Zaimplementowano tworzenie nowych profili i zapisywanie ich lokalnie
    - Dodano wyświetlanie nazwy zalogowanego użytkownika w UI
    - Dodano możliwość zmiany użytkownika bez restartowania gry
    - Dodano osobne rekordy punktowe dla każdego profilu
    - Zaimplementowano przypisywanie urządzenia wejściowego do głównego użytkownika
    - Dodano symulację wylogowania użytkownika z platformy (F8)
    - Dodano symulację utraty i przywrócenia połączenia internetowego (F9)
    - Dodano obsługę podłączania i odłączania kontrolerów podczas działania gry
    - Zaimplementowano pauzowanie gry po odłączeniu głównego kontrolera i wznowienie po ponownym podłączeniu
    - Dodano możliwość dołączenia drugiego lokalnego użytkownika z osobnym profilem i urządzeniem bez zatrzymywania rozgrywki
4.
    - Zintegrowano grę z DSW Cloud API za pomocą dostarczonego CloudSave.cs
    - Przeprowadzono test połączenia z serwerem oraz zapisu i odczytu danych z chmury
    - Zaimplementowano lokalny zapis rekordu punktowego dla każdego użytkownika
    - Dodano automatyczną synchronizację zapisu z chmurą po zakończeniu rundy
    - Dodano pobieranie i porównywanie zapisu lokalnego z chmurowym po zalogowaniu
    - Zaimplementowano wersjonowanie zapisów z wykorzystaniem expectedVersion
    - Dodano obsługę zapisu offline i automatyczną synchronizację po przywróceniu połączenia
    - Zaimplementowano wykrywanie konfliktów pomiędzy zapisem lokalnym a chmurowym
    - Dodano okno konfliktu z porównaniem punktów, dat zapisu i wersji chmurowej
    - Dodano możliwość wyboru zachowania zapisu lokalnego lub chmurowego
    - Dodano opcję testową czyszczenia lokalnego zapisu i przywracania postępu z chmury
    - Przetestowano synchronizację zapisów i wykrywanie konfliktów pomiędzy różnymi wersjami danych