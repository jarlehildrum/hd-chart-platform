# Spec: AI-drevet HD-tolkningsapp + utvidelse av testdata

Statusdokument for å kunne plukke opp arbeidet på en annen maskin. To uavhengige
delprosjekter beskrevet under: (1) utvidelse av golden-data-tester via et
eksternt API, (2) arkitektur for en app som tar imot fødselsdata, beregner
chart, og bruker en LLM til å generere en norsk tolkning basert på innholdet
i `hd-chat-docs/`.

## Status ved skriving (2026-09-13)

**Ferdig:**
- `HdPlatform` bygger og kjører (docker-compose), reell Swiss Ephemeris-basert
  chart-beregning via `HumanDesignService` (`src/HdPlatform/Services/`).
- `tests/HdPlatform.Tests/FamousChartsTests.cs`: 62 golden-data-tester, kryssjekket
  mot publiserte HD-lesninger for kjente personer fra `tests/the-famous-rave-collection.mmi`.
- `hd-chat-docs/CHATGTP-POD-HUMANDESIGN.7z` kartlagt: 132 filer, ren
  maskinlesbar norsk tekst (ikke skannet), organisert nøyaktig langs
  HD-begrepene (typer, profiler, sentre, porter, kanaler gruppert per krets,
  nodeakser/192 kors, assimileringer/splitter).

**Ikke startet:**
- Faktisk tekstuttrekk/chunking av `hd-chat-docs`-innholdet til en spørrbar kilde.
- Ny endpoint/tjeneste som kombinerer chart-beregning + innholdsoppslag + LLM-kall.
- API-nøkkel og uttrekk fra Total Human Design (se del 1).

---

## Del 1: Utvidelse av testdata via Total Human Design API

**Mål:** Utvide `FamousChartsTests.cs` (i dag 62 fixtures, alle fra manuelle
websøk mot `.mmi`-filens 100 personer) raskere og i større skala, ved å bruke
et API i stedet for enkeltvise websøk.

**Kilde:** [totalhumandesign.com/design](https://totalhumandesign.com/design)
— 56 405 personer, fødselsdata fra Astro-Databank med Rodden-pålitelighetsgradering
(AA/A/B/C/X). Søkbar/filtrerbar på Type, Profil, Autoritet, Inkarnasjonskors,
yrke. API på `totalhumandesign.com/api`, nøkkel via `dev.totalhumandesign.com`.
Gratis nivå: 100 chart-forespørsler/måned, ingen kredittkort.

**Viktig forbehold (uendret fra tidligere vurdering):** Dette er en annen
*beregnet* database — ikke en håndverifisert fasit. Verdien ligger i
konvergens mellom uavhengige implementasjoner (stikkprøve viste at deres
Steve Jobs-verdi, Generator 6/3 Emotional, er identisk med vår egen
uavhengig bekreftede fixture), ikke i "offisiell sannhet". Prioriter derfor
personer med høy Rodden-rating (AA/A) — det fjerner mye av
fødselstid-usikkerheten som var vår største feilkilde forrige runde
(Madonna, delvis Jim Morrison).

### Steg

1. **Manuelt engangssteg (bruker):** Opprett gratis API-nøkkel på
   `dev.totalhumandesign.com`. (Ikke gjort — krever kontoopprettelse med din
   identitet, så dette gjøres ikke uten et eksplisitt "gå videre".)
2. **Uttrekk med god spredning:**
   - Alle 5 typer representert, med ekstra vekt på Reflector (sjeldnest,
     ~1% av befolkningen — vi har i dag kun H.G. Wells som Reflector-fixture).
   - Kun AA/A Rodden-rating der API-et eksponerer dette feltet.
   - Maks ~90 personer per runde for å holde seg innenfor gratis kvote
     (100/måned).
3. **Feltmapping — bekreft eksakt strengformat før sammenligning, ikke anta:**
   - Type: deres sannsynlige `"Mani Gen"` ↔ vårt `ManifestingGenerator`
     (enum `.ToString()` i `ChartResponse.Type`).
   - Autoritet: de kan bruke `"Splenic"` der vi bruker `"Spleen"` — sjekk
     faktisk API-respons, ikke gjett.
   - Profil: bekreft `"3/5"`-format matcher vårt eksakt (ingen mellomrom-varianter).
4. **Sammenligning:** Gjenbruk python-mønsteret fra forrige runde (skriptet
   som kalte `/api/chart/utc` mot den kjørende containeren) — men hent
   fødsels-UTC fra Total HD API i stedet for `.mmi`-filen.
5. **Klassifiser og legg til** i samme kategori-mønster som allerede finnes
   i `FamousChartsTests.cs` (full match / type+autoritet / type+profil / kun
   profil / kun type) — nye `[InlineData(...)]`-rader i tilhørende
   `[Theory]`-metoder.
6. **Avvik → triage, ikke automatisk fixture.** Undersøk om det er en reell
   bug i vår motor eller bare en fødselsdata-/tidssone-differanse før noe
   legges inn som test.

### Ikke gjort ennå

- API-nøkkel-opprettelse (venter på bruker).
- Bekreftelse av eksakt feltformat fra et reelt API-svar.

---

## Del 2: AI-drevet HD-tolkningsapp

Bygger videre på arkitekturanbefalingen fra tidligere i sesjonen: **strukturert
nøkkeloppslag, ikke vektor-RAG**, for hoveduttrekket — chart-APIet gir
eksplisitt hvilke porter/kanaler/type/profil som er aktive, og hvert av disse
har et dokument med nøyaktig samme nøkkel i `hd-chat-docs`. Vektorsøk er
forbeholdt en eventuell senere fri-tekst chat-oppfølging, ikke selve
hovedtolkningen.

### Analyseprompt (gitt av bruker, brukes ordrett som kjerneinstruks til LLM)

```
Bruk kun Data fra fra filene her. Begynn med:
• Type, strategi og indre autoritet
• Profil og definisjon
• Inkarnasjonskors
    • Nodeaksene bevisst og ubevisst
• Porter, linjer og kanaler både bevisst og ubevisst
• Definerte og åpne sentre
• Planetaktiveringer på personlighets- og designsiden
• De 9 energisentre

Regn ut prosentvis hvor mange kanaler det er i integrasjon, sentrering,
kollektiv abstrakte, kollektive logiske, tribale osv.
Til slutt et konsentrert sammendrag av det viktigste for personen.
```

### Feltmapping: promptkrav → eksisterende data → dokkilde

| Promptkrav | Kilde i `ChartResponse` (finnes allerede) | Kilde i `hd-chat-docs` |
|---|---|---|
| Type, strategi, autoritet | `Type`, `Strategy`, `Authority` | `DE 5 TYPENE/` |
| Profil og definisjon | `Profile`, `SplitDefinition` | `12 PROFILER/` |
| Inkarnasjonskors | `IncarnationCross` (ferdig formatert streng) | `32-NODEAKSER/`, `192 kors/` |
| Nodeaksene bevisst/ubevisst | **Ingen ny beregning nødvendig** — de 4 gatene korset består av (Sol+Jord på personlighets- og designsiden) ligger allerede i `PersonalityActivation`/`DesignActivation`, bare filtrer ut `Planet == Sun \|\| Planet == Earth` fra hver liste | samme mapper som over |
| Porter/linjer/kanaler bevisst+ubevisst | `PersonalityActivation`, `DesignActivation` (planet, port, linje per rad), `ActiveChannels` | 64-porter-docx (én seksjon per port) + per-kanal-filer i krets-mappene |
| Definerte/åpne sentre | `Centers` (dict, bool per senter) | `DE 9 SENTRENE/` |
| Planetaktiveringer | `PersonalityActivation`, `DesignActivation` | (samme som portene over) |
| Prosentandel kanaler per krets | **Finnes ikke i dag** — se under | mappestrukturen selv ER kilden |

**Nøkkelinnsikt — ingen ekstern data trengs for krets-prosentene:**
mappestrukturen i `hd-chat-docs` er i praksis selve `kanal → krets`-tabellen
(`TRIBALE STAMME KRETS/19-49-ressurser-...pdf` = kanal `19-49` hører til
Tribal-kretsen, osv.). Ved forbehandling bygges denne tabellen direkte fra
mappe-/filnavn under 7z-utpakkingen — ingen egen kilde å vedlikeholde.

```
for hver krets K:
    kanaler_i_K = alle kanaler i tabellen som tilhører K
    aktive_i_K  = ActiveChannels ∩ kanaler_i_K
    prosent_K   = aktive_i_K.Count / kanaler_i_K.Count * 100
```

### Pipeline

1. **Forbehandling (engangsjobb):**
   - Pakk ut 7z, ekstraher tekst (docx: XML-parsing; PDF: selekterbar tekst
     bekreftet i stikkprøve — ingen OCR nødvendig).
   - Del opp per konsept: én fil = ett konsept for de fleste (kanal-, profil-,
     senter-filene); de store samlefilene (`ALLE ...pdf`, `DE 64 ... .docx`,
     23-24MB) splittes på interne overskrifter i stedet for å brukes hele —
     de er duplikater av de små filene og gir bare støy/dobbel-innhold hvis
     begge brukes.
   - Bygg `kanal → krets`-tabellen fra mappestrukturen samtidig.
   - Lagre i en Postgres-tabell (samme database som `HdPlatform` allerede
     bruker, eller egen — se åpne spørsmål): `concept_key`, `title`, `body`,
     `circuit` (for kanal-rader).
2. **Kjøretid:**
   - Fødselsdata → eksisterende `/api/chart` → `ChartResponse`.
   - Map chart-feltene til `concept_key`-er (type, profil, hver aktiv port
     på begge sider, hver aktiv kanal, hvert senter).
   - Hent tilhørende tekstbiter fra DB via `WHERE concept_key IN (...)`.
   - Regn ut krets-prosentene fra `ActiveChannels` + krets-tabellen.
   - Bygg prompt: brukerens instruks ordrett + strukturert chart-JSON + de
     hentede tekstbitene → send til Claude.
   - Returner sammenhengende norsk tolkning til bruker.

### Åpne spørsmål — avklar før implementasjon

1. **Prosent-definisjon er tvetydig i prompten:** "andel av kretsens totale
   kanaler som er aktivert hos personen" (f.eks. 3 av 7 abstrakte kanaler =
   43%) vs. "andel av personens aktive kanaler som tilhører denne kretsen"
   (f.eks. 3 av personens 12 totale aktive kanaler tilhører abstrakt = 25%).
   Spec over antar det første tolkningen, men bør bekreftes.
2. Skal innholdet lagres i samme Postgres-database som `HdPlatform` allerede
   bruker (enklere drift), eller en egen database/tjeneste?
3. Ny endpoint i `HdPlatform` (f.eks. `/api/chart/reading`), eller egen
   separat tjeneste/prosjekt?
4. Skal LLM-kallet gå via Anthropic API direkte fra backend, eller
   strømmes til en frontend?
