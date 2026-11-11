# Riparazione della proprietà del trasporto di fase nello stadio e nel condensatore

Piano aggiornato dopo la revisione del condensatore e delle conclusioni lineari.
La prosecuzione richiesta copre l'implementazione descritta qui e le sue verifiche.
L'attivazione produttiva C4 e l'installazione di seed restano decisioni distinte.

## Evidenza

La [ricostruzione della legge di stadio](r3-stage-energy-law-reconstruction2.md)
ha restituito `STAGE-ADMISSION-CLOSURE-INCOHERENCE-DEMONSTRATED` nel punto grezzo
del modo 2, al checkpoint 1:

- con il drenaggio della condensa, `TurbineExpansionSolver` assegna a vapore e
  condensa le energie di `IWaterSteamSaturationPropertyProvider.GetSaturationProperties`,
  che il modello semplificato calcola sempre con le correlazioni storiche:
  2.726,6 e 1.044,6 kJ/kg in entalpia, invece di 2.801,7 e 1.074,0 kJ/kg della
  chiusura C4 attiva;
- la miscela C4 di `turbine-inlet` coincide con le proprie proprietà di
  trasporto entro 1,7 J/kg; lo stadio ammette energia C4 e la rimuove con
  energie storiche;
- con le energie della chiusura attiva, a parità di stato, l'accumulo su
  `turbine-inlet` scende da 1,007 MW a 18,8 kW;
- `CondenserSystemSolver`, in `SaturatedLiquidAtSteamSpacePressure`, ricava
  l'energia della condensa dalla stessa saturazione storica: circa 6,3 kW.

È la stessa classe di difetto che la
[riparazione della proprietà del trasporto](m10/M10_FINAL_VR2_R3_ENERGY_TRANSPORT_OWNERSHIP_REPAIR_PLANNING1.md)
ha già chiuso per il separatore del drum: la capacità di trasporto di fase
appartiene alla chiusura attiva, la saturazione pubblica resta storica per
geometria e presentazione. Quella riparazione ha lasciato fuori stadio e
condensatore.

## Famiglie considerate

- **A — iniettare il provider di saturazione pubblico.** Insufficiente: è già
  quello in uso ed è identico fra le modalità. Non selezionata.
- **B — estendere la capacità di trasporto della chiusura attiva.** Aggiungere
  a `IWaterSteamPhaseTransportPropertyProvider` una lettura indicizzata solo
  dalla pressione e farla consumare da stadio e condensatore per le sole
  energie trasportate. **Selezionata.**
- **C — condizionale sul modo nei consumatori.** Codificherebbe l'identità della
  chiusura nei solutori e duplicherebbe la proprietà. Non selezionata.

### Perché una lettura indicizzata dalla pressione

L'attuale `GetSaturatedPhaseTransportProperties(pressure, temperature)` usa,
nelle modalità storiche, `GetSaturationProperties(temperature)`: era
l'identità richiesta per il drum, che leggeva la temperatura. Stadio e
condensatore leggono invece `GetSaturationProperties(pressure)`. Con la
temperatura del nodo, che non è bit per bit la temperatura di saturazione della
pressione, l'identità nelle modalità storiche non sarebbe garantita.

La nuova lettura `GetSaturatedPhaseTransportProperties(pressure)` restituisce:

- in `HistoricalCorrelationTopology` e `CorrelationConsistentInverseDomain`, i
  valori di `GetSaturationProperties(pressure)`, bit per bit;
- in `ReferenceConsistentTabulatedInverseDomain`, il lookup esistente
  `ReferenceConsistentTabulatedInverseResolver.ResolveSaturatedPhaseTransportProperties(pressure)`,
  già indicizzato dalla pressione, senza I/O e senza allocazioni per lettura.

## Superficie di produzione autorizzata

Esattamente questi quattro file esistenti; nessun file nuovo:

1. `src/NuclearReactorSimulator.Simulation/Physics/Fluids/IWaterSteamPhaseTransportPropertyProvider.cs`
   — aggiunge la lettura indicizzata dalla pressione.
2. `src/NuclearReactorSimulator.Simulation/Physics/Fluids/SimplifiedWaterSteamThermodynamicModel.cs`
   — la implementa nel provider della chiusura attiva.
3. `src/NuclearReactorSimulator.Simulation/Physics/TurbineIsland/Turbine/TurbineExpansionSolver.cs`
   — `ResolveSeparatedAdmissionAdvectedSpecificEnergies` usa la capacità di
   trasporto quando il modello la espone, altrimenti il comportamento attuale.
4. `src/NuclearReactorSimulator.Simulation/Physics/TurbineIsland/Condenser/CondenserSystemSolver.cs`
   — `ResolveSaturatedLiquidTransportState` fa lo stesso per la condensa.

I due solutori ricavano la capacità dallo stesso modello termodinamico che già
ricevono; non cambia nessun punto di composizione.

Restano invariati: la legge del lavoro (lavoro specifico, coppia, potenza
all'albero), la ripartizione di massa fra vapore e condensa, le politiche di
ammissione, la semantica pubblica di `GetSaturationProperties`, la legge e i limiti
dello scambio termico del condensatore, il payload C4 e i suoi
hash, la chiusura predefinita, il factory canonico Exact-V9, soglie e inviluppi.

## Conseguenza attesa, da verificare

A parità di stato la correzione sposta energia a valle invece di eliminarla:
circa +979 kW verso `exhaust` con il vapore e +9 kW verso l'hotwell con la
condensa. Nel condensatore la portata si ricalcola con la stessa legge
`min(massimo, inventario/dt, potenza disponibile/(energia vapore-energia condensa))`:
non è un invariante numerico. Sullo stato congelato, mantenendo la potenza
termica di 27,593 MW, il controfattuale passa da 13,173831 a 13,170825 kg/s
e il trasferimento all'hotwell diminuisce di circa 6,834 kW. Sono previsioni
a stato fisso, da distinguere dalla nuova evoluzione della ricetta.
L'ipotesi è che questi accumuli, diversamente dallo scarto strutturale su
`turbine-inlet`, dipendano dallo stato e possano essere bilanciati dagli
inventari. Il punto grezzo del modo 2 non diventa quindi un equilibrio e non si
attende che lo diventi: va rimisurato.

## Verifiche dell'implementazione

### Statiche

- cambiano soltanto i quattro file autorizzati;
- nessun byte del payload C4 cambia; nessuna chiusura predefinita cambia;
- nessuna soglia, tolleranza o inviluppo cambia; nessuna dipendenza nuova.

### Identità nelle modalità storiche

- per entrambe le modalità storiche, su una griglia di pressioni che copre
  linea vapore e condensatore, la nuova lettura coincide bit per bit con
  `GetSaturationProperties(pressure)`;
- la suite ordinaria completa, le baseline delle evidenze correnti e i test del
  contratto di determinismo Exact-V9 passano **senza modificare alcun valore
  atteso**. Un test ordinario che fallisce blocca l'implementazione.

Il controllo ordinario H1 che confrontava la legge C4 corrente con il CSV
pre-riparazione richiede una migrazione di ambito: traccia e CSV storici
continuano a essere autenticati e confrontati esattamente, mentre una nuova
regressione controlla il trasporto attivo e la riduzione dell'accumulo.
Nessun test viene escluso e nessun valore atteso storico viene aggiornato.

### Trasporto sotto C4

- nella modalità C4 la nuova lettura coincide con il lookup esistente e non
  alloca memoria gestita per lettura dopo il riscaldamento;
- fuori dall'inviluppo di pressione del payload solleva l'eccezione esistente:
  il comportamento resta fail-closed, senza ripiego sulle correlazioni;
- i test del condensatore coprono limite termico, portata massima e inventario:
  la nuova energia può cambiare portata e potenza utilizzata, secondo le leggi
  esistenti; i bilanci chiudono senza compensazioni;
- valutando il punto grezzo del modo 2 con l'harness esistente: le energie di
  vapore e condensa dello stadio coincidono con la capacità di trasporto C4
  entro 1e-9 relativo, la condensa del condensatore pure; il tasso di energia
  di `turbine-inlet` a ciascun checkpoint è al più il 10% di 1.006.560 W.

Queste ultime misure si registrano con un esecutore solo di test e un
protocollo distinto, con soglie fissate prima dell'esecuzione, come le
ricostruzioni precedenti; riportano anche i nuovi tassi su `exhaust` e hotwell.

## Effetti sulle evidenze esistenti

Il record iniziale di H feasibility 1 e tutte le evidenze del modo 2 (H1–H9,
Trial, diagnostiche non fluide, revisione vincolata, ricostruzioni) descrivono
la legge precedente: restano storiche e si riproducono ai commit che le
contengono. I test espliciti storici del modo 2 non si rieseguono sulla nuova
legge. Jacobiani e colonne congelati non si riusano come derivate.

## Sequenza successiva

1. Recepimento delle correzioni della revisione nel presente piano.
2. Implementazione nei quattro file, con test unitari di identità e trasporto,
   suite ordinaria e baseline delle evidenze correnti.
3. Protocollo di misura del punto grezzo del modo 2 con la legge corretta:
   residui fisici e scalati, fasi, inviluppo, e verifica dello spostamento
   atteso verso `exhaust`.
4. Solo dopo, una nuova diagnosi della famiglia e una nuova ricerca.

## Non autorità

Questo piano non attiva C4 nel desktop, non installa seed, non cambia Exact-V9
né i suoi archivi, non rilassa criteri di radice o inviluppi e non promuove R3
o M10. R3 resta RED.
