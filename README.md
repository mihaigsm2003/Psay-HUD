# Psay HUD

Plugin CounterStrikeSharp pentru mesaje private afisate intr-un card Panorama.
Cardul intra cu slide din stanga, ramane pana la inchidere si iese spre dreapta.

<img width="831" height="480" alt="image" src="https://github.com/user-attachments/assets/03aac5d4-256e-4a75-9dfa-738c9ca24313" />

## Versiuni

- CounterStrikeSharp API: `1.0.376`
- PanoramaManager: `0.4.4`
- .NET SDK/runtime: `10.0`

## Comanda

```text
css_psay <#userid, name, @all, @ct or @t> <message>
```

Exemple:

```text
css_psay #12 Salut, mesajul tau este gata.
css_psay Mihai Buna seara!
css_psay @all Anunt pentru toata lumea!
css_psay @ct Atentie echipa CT!
css_psay @t Atentie echipa T!
```

Numele jucatorului poate fi complet sau o potrivire partiala unica. Daca mai multi
jucatori au acelasi nume ori corespund aceleiasi potriviri partiale, foloseste
`#userid`. Selectorii `@all`, `@ct` si `@t` trimit acelasi mesaj fiecarui jucator
conectat, respectiv fiecarui jucator din echipa CT sau T. Comanda este disponibila
din consola jucatorului, consola serverului sau RCON. Jucatorii care o folosesc
trebuie sa aiba permisiunea configurata in CounterStrikeSharp; consola
serverului/RCON ramane accesul administratorului serverului.

## Configurare

La prima pornire, CounterStrikeSharp genereaza fisierul:

```text
addons/counterstrikesharp/configs/plugins/PsayHud/PsayHud.json
```

Configuratia implicita:

```json
{
  "AdminPermission": "@css/generic",
  "MessageHeader": "MESAJ DE LA ADMIN"
}
```

`AdminPermission` este flag-ul CounterStrikeSharp necesar adminului care executa
comanda. Schimba-l cu flag-ul atribuit gradului dorit in configuratia de admini,
de exemplu `@css/root` pentru acces root sau un flag personalizat care exista in
`admins.json` / `admins_groups.json`. Flag-urile CounterStrikeSharp trebuie sa
inceapa cu `@`. `MessageHeader` este textul afisat in partea de sus a cardului.
Configul este reincarcat automat de CounterStrikeSharp dupa modificare.

## Build si instalare server

Din directorul proiectului:

```powershell
dotnet build .\PsayHud.csproj -c Release
```

Copiaza continutul `build\plugins\PsayHud\` in:

```text
addons/counterstrikesharp/plugins/PsayHud/
```

Directorul contine `PsayHud.dll` si `PanoramaManager.dll`. Serverul si clientii
trebuie sa foloseasca un build CounterStrikeSharp de cel putin `1.0.374`;
proiectul este compilat intentionat impotriva versiunii `1.0.376`.

## Panorama si Workshop

Sursele Panorama pentru addon sunt in `workshop\panorama\`. Pastreaza structura:

```text
panorama/layout/custom_game/psay_card.xml
panorama/styles/custom_game/psay_card.css
```

Deschide directorul `workshop\` ca addon in Counter-Strike 2 Workshop Tools,
compileaza addonul si publica-l. Include in addon resursele compilate:

```text
panorama/layout/custom_game/psay_card.vxml_c
panorama/styles/custom_game/psay_card.vcss_c
```

Monteaza addonul publicat pe server, astfel incat clientii sa primeasca resursele
Panorama inainte sa foloseasca pluginul. Calea layoutului din plugin trebuie sa
ramana `panorama/layout/custom_game/psay_card.vxml_c`.

Imaginea patrata de prezentare pentru pagina Workshop este
`workshop\psay-hud-workshop.png` (1024 x 1024).
