# Nuova base locale attorno al punto diagnostico 19

La prova 19 ha verificato la risposta della potenza netta globale al primo
checkpoint, ma non ha annullato gli accumuli dei singoli nodi. Hotwell e
feedwater conservano residui importanti. Il modello locale della baseline
precedente non si trasferisce automaticamente a questo nuovo punto.

Questa osservazione riprende i soli vettori delle trenta direzioni della
prova 5, senza riusarne le derivate. Il centro è esattamente il record
`source/1/repeat/1` della prova 19, inclusa la riserva iniziale di hotwell.
La baseline storica è ripetuta come controllo di identità indipendente.
Entrambi devono coincidere integralmente con i record precedenti, a meno
dell'identificativo della richiesta.

## Dominio e budget dichiarati prima delle misure

Le 26 direzioni storicamente centrali sono misurate ai due segni, a metà
e all'intera ampiezza selezionata in precedenza. Le quattro storicamente
unilaterali sono misurate anche al doppio, sempre ai due segni: la riserva
iniziale potrebbe rendere disponibile una variazione prima non ammessa,
ma questo deve risultare dalle nuove misure. Non si cambia o corregge uno
stato che attraversa il confine di fase: lo si registra e lo si esclude
dallo stencil che richiede la fase invariata lungo tutti i checkpoint.

Sono **260 ricette**, tutte ripetute due volte, in un solo batch;
al massimo **782 passi runtime**, inclusi i due passi di preparazione.
Nessuna richiesta è generata in modo adattivo. Ogni coordinata rimane
entro ±0,0002 rispetto al centro, con sola tolleranza binary64 di 1e-15
per la verifica del dominio. Questa è un'eccezione diagnostica circoscritta:
il centro supera i vecchi limiti di energia di exhaust e popolazione
neutronica, che non vengono riscritti. Una console separata rifiuta gli
input fuori dominio prima di qualsiasi passo fisico.

Le temperature mantengono la convenzione delle frazioni dei valori Celsius
dell'harness storico. I controlli operativi sono coordinate iniziali;
guadagni, leggi fisiche, produzione e test rimangono invariati. La riserva
di hotwell appartiene allo stato iniziale, senza reintegri nel runtime.

## Confronto delle risposte e delle capacità

Si osservano i tre checkpoint, tutti gli inventari, i due corpi termici,
rotore, potenze e cinque capacità di portata. Si riusano la ricostruzione
del minimo della prova 8 e i bilanci indipendenti della prova 17, con
tolleranze 1e-9 kg/s per le portate ricostruite, 1e-6 kg/s per il bilancio
globale di massa, 0,01 W per l'energia e 1e-6 N·m per la coppia.

Una direzione centrale è selezionata se entrambe le ampiezze rispettano
salute, inviluppo, fase e regimi già dichiarati, e se le derivate confrontate
concordano entro il 10% sia sulle 81 righe pesate sia sulle 15 capacità
(cinque per checkpoint). Le 81 righe comprendono i 75 residui originali e
sei righe termiche diagnostiche, con gli stessi pesi fisici precedenti.
Per un vettore di capacità nullo il denominatore del confronto ha il
pavimento dichiarato di 1e-12 kg/s per unità di coordinata.

Solo per le quattro direzioni storicamente unilaterali, se non è selezionata
la centrale, si confrontano gli stencil unilaterali di secondo ordine a
metà e intera ampiezza. Si preferisce il lato positivo in caso di doppia
conformità. Il vettore selezionato è orientato nel verso misurato e il
suo coefficiente rimane non negativo. Nessuna disponibilità viene presunta.

Il risultato riporta anche capacità limitanti, cambi di vincitore, rifiuti,
massimi residui in unità fisiche e rango numerico diagnostico. Il confronto
fra due ampiezze non valida la somma delle direzioni né un modello composto
del minimo: eventuali proposte richiedono previsioni e misure successive.
Il criterio originale sui 75 residui resta massimo assoluto ≤1.

Contratto, richieste, fixture e hash degli assembly precedono l'acquisizione.
Il replay `python eng/run-r3-repaired-local-basis20.py review` ricostruisce
selezioni e rifiuti dai dati congelati, senza nuove simulazioni. Nessun seed,
conferma di 100 passi o qualificazione R3 è oggetto di questo protocollo.
