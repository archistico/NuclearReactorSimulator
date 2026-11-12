# Osservazione delle capacità di portata nelle altre 22 direzioni

Il modello del minimo (revisione 9) e la sua verifica prospettica (prova 10)
coprono soltanto le otto direzioni attive della prova 6, le uniche con
capacità osservate. Questa prova osserva capacità e vincitore, a tutti e tre i
checkpoint, nelle altre **22 direzioni di lavoro** della base direzionale.
Non costruisce un modello, non esegue proposte e non avvia ricerche.
Sorgenti, test, ricetta, legge fisica, scale e criterio di radice restano
invariati.

## Ricette

Si ripetono esattamente richieste già eseguite e congelate, con la console
`R3CapacityObservation` senza modifiche:

- per le 18 direzioni centrali, le due sonde alla scala selezionata;
- per le quattro direzioni unilaterali (massa ed energia di hotwell e
  suction), la sonda positiva alla scala selezionata e a scala doppia;
- la baseline.

Ogni richiesta è ripetuta due volte: **90 ricette**, al massimo **272 passi
runtime**, inclusi i due dell'harness. Le sonde negative delle direzioni
unilaterali non si ripetono: nei record congelati cambiano la fase di hotwell
o suction e restano fuori dal perimetro. La scala doppia serve soltanto a
descrivere la curvatura sul lato ammesso.

## Controlli

Ogni record, tolto soltanto il blocco `StageFlow`, deve coincidere esattamente
con il record congelato di origine; le ripetizioni devono essere identiche. La
baseline deve coincidere, blocco `StageFlow` incluso, con quella congelata
dell'osservazione 8. Si ricostruiscono i bilanci precedenti con le stesse
tolleranze.

Le capacità si ricostruiscono con la funzione dell'osservazione 8, invariata,
con le stesse tolleranze: 1e-6 Pa sulle pressioni e 1e-9 kg/s sulla legge
delle valvole e sul minimo. Se un record esce dal perimetro di quella
ricostruzione (override di protezione, fail-safe, caratteristica non lineare,
legge non riprodotta), la prova non lo corregge: riporta il controllo fallito
e dichiara quella direzione non osservata. La baseline deve invece essere
ricostruibile.

## Cosa si riporta

Per ogni direzione: guardie di salute, inviluppo, fase e regime rispetto alla
baseline; capacità, vincitore e margine a ogni checkpoint; variazione delle
capacità per unità di coordinata; variazione della portata comandata; cambi
di vincitore. Per le direzioni centrali anche la somma delle variazioni di
portata dei due segni; per le unilaterali la differenza seconda delle
capacità fra baseline, scala selezionata e scala doppia. Si elencano le
direzioni che cambiano la portata dello stadio di più di 1e-9 kg/s e quelle
che non la cambiano.

Tutte queste differenze sono soltanto descrittive: una differenza che
attraversa vincitori diversi non è una derivata della portata. Nessun
coefficiente del modello viene stimato qui.

## Esito

Un PASS significa evidenza completa e riproducibile, non un modello esteso.
La ristima del modello sulle 30 direzioni e una nuova verifica prospettica
richiedono contratti successivi. I risultati precedenti non vengono
riscritti.

Fixture del budget e delle differenze, compilazione e preflight senza runtime
precedono l'acquisizione. Dopo il congelamento,
`python eng/run-r3-repaired-capacity-observation11.py review` ricostruisce
l'analisi senza nuove simulazioni. R3 e M10 restano aperti.
