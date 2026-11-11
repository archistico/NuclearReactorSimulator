# Sensibilità alla valvola di regolazione nel punto grezzo del modo 2

La [revisione di sufficienza](r3-h-family-sufficiency-review1.md) indica che i
soli inventari fluidi non annullano, nel modello lineare, 300–460 kW sulla linea
vapore. Questa diagnostica misura se l'apertura iniziale della valvola di
regolazione sposta quei residui. È una misura: non seleziona C o J, non cerca
una radice e non installa seed.

## Superficie di test

Il seam del seed riceve già l'apertura iniziale della valvola. Lo stesso valore
inizializza la posizione dell'attuatore e l'uscita del regolatore di velocità:
la sonda li muove insieme, senza toccare guadagni, riferimenti o modalità.

Servono due modifiche solo di test. `R3HFeasibilityHarness` espone un punto
operativo con sei grandezze (valvola di regolazione, pompe di condensato e
alimento, popolazione neutronica, temperature di combustibile e struttura),
con valori predefiniti identici ai letterali precedenti. Il nuovo esecutore
`M10R3OperatingPointProtocolBatchTests` accetta, oltre ai 24 inventari, sei
frazioni relative a quei valori, ciascuna entro ±0,02. Questo protocollo usa
soltanto la prima; le altre cinque restano nulle e non sono autorizzate qui.

L'albero `tests/` passa da 410 a 411 file. I 409 file non toccati mantengono
l'hash registrato nel contratto macchina. I protocolli H4–H9, i Trial e le
revisioni restano legati all'albero precedente e si riproducono al commit
che lo contiene; i loro pacchetti congelati non cambiano.

## Ricette

Inventari a coordinate nulle. Undici punti, ciascuno eseguito due volte: la
baseline e la frazione di valvola ±1e-6, ±1e-5, ±1e-4, ±1e-3, ±1e-2. Budget:
22 ricette, un batch, 66 passi. Ogni ricetta esegue i due passi di seed e un
passo Running invariati e registra gli stessi 75 residui, fasi, osservazioni,
bilanci dello stadio e hash di stato dei protocolli H.

## Verifiche

La baseline deve riprodurre esattamente residui e fasi del record iniziale di
H feasibility 1: una differenza è un errore tecnico dell'harness modificato.
Le ripetizioni devono coincidere, hash di stato inclusi. I residui sono
ricalcolati dagli inventari registrati.

Per ogni ampiezza si calcola la colonna di sensibilità con la regola di
compatibilità di fase delle ricerche H: centrale se entrambe le sonde
mantengono le 48 fasi, unilaterale se una sola, indisponibile altrimenti.
Per ogni sonda si riportano metrica fisica, inviluppo e uscita del regolatore.

La colonna primaria è quella a 1e-4. È utilizzabile se disponibile e se la sua
differenza massima pesata dalla colonna a 1e-5 non supera il 10% del proprio
massimo. I pesi fisici sono quelli della revisione di sufficienza.

Con la colonna primaria si risolvono ai minimi quadrati, senza vincoli, tre
modelli sui 75 residui: soli inventari, sola valvola, inventari più valvola,
nelle scale del contratto e con i pesi fisici. Lo Jacobiano degli inventari
è quello congelato e compatibile di fase.

## Esiti

- `CONTROL-VALVE-MATERIAL-IN-LINEAR-MODEL`: con i pesi fisici, il massimo tasso
  di energia previsto dal modello congiunto è al più il 50% di quello dei soli
  inventari.
- `CONTROL-VALVE-NOT-MATERIAL-IN-LINEAR-MODEL`: la riduzione è inferiore.
- `INCONCLUSIVE-NONLINEAR-OR-PHASE-INCOMPATIBLE-PRIMARY-COLUMN`: la colonna
  primaria non è utilizzabile.

Le soglie 10% e 50% classificano questa diagnostica; non sono tolleranze
fisiche né criteri di radice. Le previsioni lineari non sono osservazioni.
Un esito materiale motiva soltanto la pianificazione di una famiglia che
includa la valvola, con protocollo distinto. Un esito non materiale motiva
la stessa diagnostica sulle altre grandezze o la revisione della legge di
energia dello stadio. Nessun esito cambia scale, residui, box, inviluppo,
guadagni, C4 o Exact-V9, né promuove R3 o M10.

## Comandi

`scripts\run-r3-op-control-valve-sensitivity1.cmd` esegue revisione statica,
build Release con avvisi come errori, i tre controlli preliminari, le 22
ricette e l'adjudication. Gli output sono in
`artifacts/r3-op-control-valve-sensitivity1`. Dopo la revisione dell'esito,
il pacchetto viene congelato e
`python eng/run-r3-op-control-valve-sensitivity1.py review` ne esegue il replay.
