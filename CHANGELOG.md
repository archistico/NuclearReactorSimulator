# Changelog

La cronologia registra le modifiche e gli esiti al momento della loro verifica.
Lo [stato corrente](docs/PROJECT.md), l'[handoff](docs/PROJECT.md#handoff) e la
[roadmap](docs/ROADMAP.md) mantengono rispettivamente checkpoint, ripresa e passi futuri.

## 2026-11-11

### Indagine sull'equilibrio integrato R3

- Consolidati i confronti fisici ripetuti e gli esiti delle ricerche H2–H9, con contratti,
  budget, identità degli input e replay delle evidenze congelate. L'esecutore
  di batch è riutilizzato dai nuovi protocolli; la produzione resta Exact-V9.
- Confrontati minimo dei quadrati e minimo del massimo residuo; separati i
  certificati numerici dalla validità delle previsioni fisiche. Le scelte
  numeriche mantengono invariati box, scale e criteri di equilibrio.
- Ricostruiti i cambi di fase dell'hotwell lungo i quattro checkpoint. H8 ha
  portato il massimo residuo accettato a 3.337.270,068; la successiva diagnosi
  distingue qualità troncata, margine di volume e criterio energetico C4.
- Verificato il margine energetico locale sui 120 campioni dei Trial 6–8.
  Trial 9 ha confrontato sei ampiezze con 554 ricette ripetute. Trial 10 ha
  verificato una proposta favorevole al raggio 1e-5, poi usata come ingresso H9.
- H9 ha eseguito 476 ricette e accettato due aggiornamenti, arrivando a
  3.334.923,856. L'arresto conserva la colonna indisponibile della massa di
  control-out: le sonde ammesse cambiano fase all'ultimo checkpoint.
- Ricostruiti gli accumuli di energia dai trasferimenti registrati. I replay
  passano, ma nessuna radice o nuovo seed è qualificato: R3 resta RED e M10 aperto.

Specifiche, esiti e comandi sono raggiungibili dalle
[evidenze del checkpoint](docs/PROJECT.md#evidenze-e-verifiche).

### Revisione di sufficienza della famiglia H

- Riletti in unità fisiche i residui del punto iniziale e dei finali H4–H9: il
  massimo scalato scende da 7,85 a 3,33 milioni mentre la somma assoluta dei
  tassi di energia sale da 2,69 a 12,87 MW e la coppia netta da −0,9 a −166 N·m.
- Attribuiti i due squilibri iniziali allo spostamento di energia specifica di
  C4 a parità di stato bersaglio, entro lo 0,07% dei valori osservati.
- Risolto il modello lineare 75×24 senza vincoli di box: passi fino al 4,6%,
  sei coordinate oltre ±2% e 300–460 kW residui sulla linea vapore.
- Esito offline PASS, replay identico su Windows e Linux. Nessuna radice,
  famiglia o allargamento del box è qualificato: R3 resta RED e M10 aperto.
  La prossima attività è la diagnostica di sensibilità ai punti operativi non fluidi.

### Diagnostica della valvola di regolazione (preparata)

- Aggiunto l'esecutore di test per i punti operativi non fluidi e reso
  parametrico l'harness su sei grandezze, con valori predefiniti invariati.
  `tests/` passa da 410 a 411 file; i replay H4–H9 restano legati al commit precedente.
- Congelati specifica, contratto e runner della diagnostica: 22 ricette, colonna
  primaria a 1e-4, tre modelli lineari e soglie di classificazione dichiarate.
  Revisione statica PASS; compilazione, suite ordinaria ed esecuzione sono da fare.

### Diagnostica della valvola di regolazione (eseguita)

- CI ordinaria PASS con i 411 file di test: 1.560 test, 1.395 superati,
  165 espliciti esclusi, zero fallimenti; baseline delle evidenze correnti PASS.
- Eseguite le 22 ricette in un batch: tutte valide, ripetizioni identiche con
  gli hash di stato, baseline del modo 2 riprodotta, fasi invariate a ogni ampiezza.
  Da ±1e-3 l'uscita del regolatore lascia l'inviluppo 29,27–29,30%.
- La valvola sposta energia fra `stop-out` e `control-out`, circa 3,7 kW per
  1e-4, e la coppia del rotore; non agisce sui 1,157 MW di `outlet`. Colonna
  primaria utilizzabile, con deviazione dell'8,6% da quella a 1e-5.
- Con i pesi fisici il modello congiunto porta il massimo tasso di energia da
  302 a 209 kW, il 69%: esito `CONTROL-VALVE-NOT-MATERIAL-IN-LINEAR-MODEL`. La
  previsione richiede +2,9% di valvola e sette coordinate oltre ±2%, e sposta il
  massimo sull'hotwell; con le scale del contratto peggiora da 464 a 526 kW.
- Congelato il pacchetto con ricevuta; replay PASS. Nessuna famiglia, radice o
  seed è qualificato: R3 resta RED e M10 aperto.

### Sensibilità alle altre grandezze non fluide

- Preparati e committati prima dell'esecuzione specifica, contratto e runner
  per pompe di condensato e alimento, popolazione neutronica e temperature
  iniziali, senza modifiche a `src/` o `tests/`. Revisione statica PASS.
- Eseguite 102 ricette in tre batch: tutte valide e nell'inviluppo, ripetizioni
  identiche, baseline uguale a quella congelata della valvola. Un solo cambio di
  fase: pompa di alimento a +1e-2, su `feedwater-inventory` e hotwell.
- Le pompe spostano massa fra hotwell, `feedwater-inventory` e drum; popolazione
  e temperature agiscono solo sull'energia di `outlet`, con colonne pesate
  collineari (coseno 1,0000). Con gli inventari nessuna grandezza scende sotto il
  98% del massimo dei soli inventari; il residuo resta su `stop-out`, 295–314 kW.
- Esito congelato `NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL`: il modello
  con tutte le colonne prevede 18 kW, il 6%, ma con passi fino a −6.077% sulla
  pompa di condensato e +2.136% sulla popolazione. È un artefatto della
  collinearità, non una motivazione fisica; replay PASS. R3 resta RED.
- Registrata la decisione di classificare i modelli lineari entro i limiti
  delle sonde, raggruppando le direzioni collineari.

### Revisione vincolata della combinazione non fluida

- Aggiunto un solutore ai minimi quadrati con limiti, a insieme attivo, con
  certificato KKT e sei casi analitici; confrontato fuori dal repository con
  il gradiente proiettato su 60 problemi casuali (eccesso massimo 1,4e-14).
- Committati specifica e contratto prima del calcolo: inventari entro ±2%,
  grandezze non fluide entro ±1e-2, nocciolo raggruppato e controllo separato.
- Con i soli inventari vincolati il massimo è 335 kW su `turbine-inlet`; con le
  grandezze non fluide sale a 357 kW, il 107%, in tutti i modelli. Certificati
  soddisfatti, raggruppamento ininfluente, nessuna chiamata all'impianto.
- Lungo la linea vapore restano 190–210 kW su `stop-out` e `control-out` e
  circa 350 kW su `turbine-inlet` qualunque sia la combinazione. Esito
  `NO-BOUNDED-NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL`, congelato con
  replay PASS: la prossima attività è la revisione della legge di energia dello stadio.

### Documentazione

- Riscritto il README intorno al simulatore e alle istruzioni d'uso; riscritto
  il manuale italiano con spiegazioni dei componenti, delle interazioni e dei diagrammi.
- Archiviati 74 documenti conclusi o superati, preservandone byte e SHA-256;
  separati 174 input esatti dei validatori dalla documentazione leggibile.
- Consolidati risultati e ripresa in `docs/PROJECT.md`, con handoff operativo.
  La roadmap mantiene consegne e condizioni future; gli indici collegano
  manuale, validazione, decisioni, evidenze e cronologia senza un nuovo file di stato.

## 2026-11-10 — Revisione numerica offline

- Ricostruito l'arresto della ricerca H limitata, usando traccia e Jacobiano congelati.
- Implementato il confronto locale scalato con casi numerici indipendenti.
  La discesa prevista dal modello non costituiva una prova di miglioramento
  fisico o di qualificazione del seed; R3 rimaneva RED.

Il [changelog precedente](docs/history/archives/README.md#snapshot-changelog-md)
è conservato integralmente nell'archivio documentale.
