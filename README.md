# MUD Metro Hra

Textový multiplayer MUD (Multi-User Dungeon) zasazený do postapokalyptického metra. Hráči se připojují přes vlastního TCP klienta, pohybují se mezi stanicemi, sbírají předměty a plní úkoly.

---

## Požadavky

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows / Linux / macOS
- JetBrains Rider nebo Visual Studio (nebo jen terminál)

---

## Struktura projektu

```
MUD_MetroHra/
├── MUD_MetroHra/        ← server
│   ├── world.json       ← herní svět (místnosti, předměty, NPC, questy)
│   └── *.cs
├── MudClient/           ← klientská aplikace
│   └── *.cs
└── MUD_MetroHra.sln
```

---

## Spuštění

### 1. Spusť server

```bash
cd MUD_MetroHra
dotnet run
```

Server naslouchá na portu **5000** (výchozí). Jiný port:

```bash
dotnet run -- 6000
```

Úspěšné spuštění vypadá takto:
```
2026-xx-xx [INFO] Server bezi
```

### 2. Spusť klienta

V **novém terminálu**:

```bash
cd MudClient
dotnet run
```

Připojení na jiný server:

```bash
dotnet run -- 192.168.1.5 5000
```

---

## Přihlášení

Po připojení klient zobrazí:
```
Vitej v MUD Metro Hra!
Zadej 'login' nebo 'register':
```

- `register` — vytvoří nový účet
- `login` — přihlásí existujícího hráče

---

## Herní příkazy

| Příkaz | Popis |
|---|---|
| `pomoc` | Seznam všech příkazů |
| `prozkoumej` | Zobrazí místnost, východy, předměty, NPC, hráče |
| `jdi <smer>` | Pohyb (sever, jih, vychod, zapad) |
| `vezmi <predmet>` | Vezme předmět do inventáře |
| `odloz <predmet>` | Odloží předmět do místnosti |
| `inventar` | Zobrazí inventář a kapacitu |
| `mluv <npc>` | Promluví s NPC |
| `rekni <zprava>` | Zpráva hráčům ve stejné místnosti |
| `krik <zprava>` | Zpráva všem připojeným hráčům |
| `pouzij <predmet>` | Použije předmět z inventáře |
| `quest` | Zobrazí stav úkolů |
| `stav` | Zobrazí zdraví a aktivní efekty |
| `zebricek` | Zobrazí hráče kteří dokončili hru |
| `konec` | Odpojí hráče |

---

## Cíl hry

1. Promluv se **strážcem** na základně → získáš quest
2. Najdi **klíčovou kartu** v Opuštěné stanici
3. Projdi **Karanténním sektorem** (pozor na otrávenost — použij filtr do masky)
4. Dostaň se na **Poslední stanici** s kartou i artefaktem
5. Hra se dokončí a tvoje jméno se zapíše do žebříčku

---

## Testovací účty

Pro testování vytvoř tyto účty příkazem `register`:

| Uživatelské jméno | Heslo |
|---|---|
| `test_player` | `Test123` |
| `saved_player` | `Save123` |

---

## Soubory generované serverem

| Soubor/Složka | Obsah |
|---|---|
| `server.log` | Logy serveru s časovými razítky |
| `accounts.json` | Uživatelské účty (hesla jako hash) |
| `players/` | Uložené stavy hráčů (JSON) |

---

## Spuštění v JetBrains Rider

1. Otevři `MUD_MetroHra.sln`
2. V Run/Debug Configurations vyber **MUD** (Compound konfigurace)
3. Klikni ▶️ — spustí se server i klient najednou