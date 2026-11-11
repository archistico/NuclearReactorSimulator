# Raffinamento delle direzioni mancanti sul punto C4 riparato

Il protocollo completa la diagnosi delle tre colonne non validate nella prova
accoppiata 1. Usa la medesima ricetta, gli stessi inventari grezzi e le stesse
proprietà riparate, senza cambiare sorgenti produttive o test. Contratto,
implementazione, richieste e input congelati vengono autenticati prima della
prima misura. Le vecchie osservazioni e classificazioni restano immutate.

Le sonde centrali su massa ed energia di `pressure` usano ampiezze relative
1e-7, 3e-7, 1e-6 e 2e-6. L'energia di `exhaust` usa 1e-6, 3e-6, 1e-5, 3e-5,
1e-4, 3e-4 e 1e-3. Una quarta direzione varia insieme massa ed energia di
`pressure`: dM/M = ε e dU/U = kε. Il coefficiente k è il rapporto negativo
delle variazioni centrali della pressione grezza nelle sonde precedenti a 1e-5,
circa −0,66016449. È una compensazione locale proposta dai dati della legge
riparata, non una correzione automatica né una derivata dei residui ammessa
malgrado l'uscita dall'inviluppo. Questa direzione è misurata a 1e-5 e 1e-4.

Baseline e tutte le sonde sono ripetute due volte: **70 ricette**, al massimo
**212 passi runtime** inclusi i due passi di inizializzazione dell'harness.
Ogni ricetta esegue un passo preparatorio, un secondo passo preparatorio e
un Running da 10 ms. Il nuovo runner separato è riutilizzabile per protocolli
autenticati, limita le acquisizioni a 512 ricette e ±2% e verifica richieste,
budget e identità prima dell'istanziazione fisica. Il protocollo corrente ha
il proprio elenco esatto di 70 richieste. Nessun file parziale è sovrascritto.

Si conservano le osservazioni precedenti, compresi i corpi termici ai tre
checkpoint. Si aggiungono coppia di turbina, carico del generatore, perdite,
coppia netta, velocità, inerzia, lavoro ideale dello stadio e diagnostica del
governor. La verifica ricostruisce i bilanci di massa ed energia precedenti
entro 1e-6 kg/s e 0,01 W. Ricostruisce inoltre coppia netta, carico del
generatore, variazione del momento angolare e legge lavoro-portata-coppia
dello stadio entro 1e-6 N·m. Il confronto della coppia resta un confronto di
accumulo dinamico: non si impone coppia zero a un punto non stazionario.

Per ogni punto già presente nella prova 1, hash completi, inventari, fasi e
75 residui devono essere identici. Le ripetizioni devono coincidere. Una
colonna è ammessa solo con entrambi i segni sani, nell'inviluppo e con fase,
regime idraulico e limiti del condensatore uguali alla baseline lungo tutta
la ricetta. In aggiunta devono coincidere i rami di limitazione del lavoro,
scelta della velocità di riferimento della coppia, limitazione dei carichi
al fermo, saturazione e anti-windup del governor. Un ramo diverso resta
un'osservazione e non riceve una colonna nel ramo della baseline.

Pesi, 81 righe diagnostiche e tolleranza relativa del 10% sono quelli della
prova 1; i 75 residui originali e il criterio di radice non cambiano. Per ogni
direzione si confrontano tutte le coppie di ampiezze adiacenti dichiarate.
Si seleziona la maggiore ampiezza della coppia ammessa che rispetta il 10%,
conservando anche gli altri confronti. La selezione locale non certifica
l'intera regione né una derivata al limite per ampiezza tendente a zero.

La decomposizione della coppia di `exhaust` viene riportata per ciascuna ampiezza
e checkpoint, per distinguere contributi di turbina, generatore e perdite.
Non si cambiano i pesi per promuovere una colonna. Nessuna proposta, ricerca,
qualificazione di famiglia o modifica fisica è prevista da questo protocollo.
Un PASS dell'evidenza non implica che tutte le direzioni risultino validate.

Compilazione, fixture e preflight senza chiamate fisiche precedono l'acquisizione.
Analisi e congelamento sono separati: un errore di analisi non autorizza una
nuova acquisizione. Dopo il congelamento il comando
`python eng/run-r3-repaired-missing-directions2.py review` ricostruisce l'esito
senza nuovi passi runtime. R3 e M10 restano aperti.
