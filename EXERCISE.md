1.
- Przygotowano i uruchomiono build deweloperski i produkcyjny Windows
- Zaimplementowano licznik FPS i czasu klatki (ms), aktualizowany co 0.5s
- Dodano logi diagnostyczne [DEV] występujące wyłącznie w buildzie deweloperskim
- Dodano ustawienie Fullscreen/Windowed z zapisem preferencji
- Przeprowadzono 30-sekundowy benchmark z wyłączonym VSync i limitem FPS:
    - Development - 1219,48 FPS, 173 MB
    - Production - 1604,77 FPS, 109 MB
    - Wersja produkcyjna osiągnęła wydajność wyższą o 31,59%