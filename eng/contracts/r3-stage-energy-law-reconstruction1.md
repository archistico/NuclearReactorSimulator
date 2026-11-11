# Ricostruzione della legge di energia dello stadio

Le diagnostiche non fluide e la [revisione vincolata](r3-op-bounded-combination-review1.md)
mostrano che nessuna grandezza misurata sposta l'accumulo di energia su
`turbine-inlet`: circa 1,007 MW nel punto grezzo del modo 2. La
[revisione di sufficienza](r3-h-family-sufficiency-review1.md) lo attribuisce a
uno spostamento di 75,5 kJ/kg sulla portata di stadio. Questa ricostruzione
stabilisce quale energia specifica trasporta lo stadio e da quale chiusura la
ricava. Non modifica codice di produzione, non propone una correzione e non
cerca una radice.

## Ipotesi da verificare

Con la politica `VaporMassFractionLimitedWithMoistureDrain` lo stadio divide la
portata in vapore e condensa secondo la qualità del nodo di ingresso e assegna
a ciascuna parte un'energia di saturazione. `TurbineExpansionSolver` la ricava
da `IWaterSteamSaturationPropertyProvider.GetSaturationProperties`, che nel
modello semplificato usa sempre le correlazioni storiche. Il separatore del
drum usa invece `PhaseTransportPropertyProvider`, che sotto C4 delega al
resolver tabulato coerente con la chiusura. Se lo stadio usa la saturazione
storica mentre la portata entrante trasporta l'energia C4 del nodo, la
differenza si accumula su `turbine-inlet`.

## Superficie di test

Un nuovo file, `M10R3StageEnergyLawReconstructionTests`, senza modifiche
all'harness o all'esecutore dei punti operativi; `tests/` passa da 411 a 412
file. Due test ordinari controllano l'identità dei provider sotto la chiusura
storica e il contratto. Il test esplicito valuta una volta il punto grezzo a
coordinate nulle con `R3HFeasibilityHarness` e registra, al checkpoint 1:

- portate, qualità ed energie specifiche dello stadio e della valvola di ammissione;
- per la pressione di `turbine-inlet`, le energie di saturazione storiche e
  quelle di trasporto della chiusura attiva, nella modalità di trasporto dello stadio;
- per il condensatore, l'energia della condensa registrata e le due alternative.

## Verifiche

I residui devono coincidere con il record iniziale di H feasibility 1. La
portata di ammissione deve chiudere la massa dello stadio e la differenza fra
energia ammessa ed energia uscita dallo stadio deve riprodurre l'accumulo
osservato su `turbine-inlet`, entro 1e-9 relativo. Lo stesso criterio decide
quale provider ha prodotto le energie del vapore e della condensa.

Il controfattuale ricalcola, a parità di stato e di portate, l'uscita dello
stadio con le energie della chiusura attiva. È un calcolo contabile, non
un'osservazione dell'impianto.

## Esiti

- `STAGE-ADMISSION-CLOSURE-INCOHERENCE-DEMONSTRATED`: lo stadio usa la
  saturazione storica e non quella della chiusura attiva; il controfattuale
  lascia al più il 10% dell'accumulo osservato.
- `STAGE-ADMISSION-CLOSURE-INCOHERENCE-PARTIAL`: come sopra, fra 10% e 50%.
- `STAGE-ADMISSION-HISTORICAL-SATURATION-NOT-EXPLANATORY`: saturazione
  storica, ma il controfattuale lascia più del 50%.
- `NO-STAGE-ADMISSION-CLOSURE-INCOHERENCE`: lo stadio non usa la saturazione
  storica, o usa già quella della chiusura attiva.

Il condensatore è soltanto riportato. Un esito dimostrato motiva la
pianificazione di una modifica della legge di trasporto dello stadio sotto C4,
con protocollo, verifiche locali, suite ordinaria e riqualificazione propri;
non la autorizza. Nessun esito cambia scale, residui, box, inviluppo, C4 o
Exact-V9, né promuove R3 o M10.

## Comandi

`scripts\run-r3-stage-energy-law-reconstruction1.cmd` esegue revisione
statica, build Release con avvisi come errori, i controlli preliminari, la
valutazione e l'adjudication. Gli output sono in
`artifacts/r3-stage-energy-law-reconstruction1`. Dopo la revisione dell'esito
il pacchetto viene congelato e
`python eng/run-r3-stage-energy-law-reconstruction1.py review` ne esegue il replay.
