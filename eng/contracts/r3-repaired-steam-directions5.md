# Direzioni accoppiate per i due assi mancanti della linea vapore

La prova 4 ha selezionato 28 direzioni, con quattro coefficienti vincolati
al verso positivo. Restano non validate le derivate cartesiane dell'energia
di control-out e della massa di turbine-inlet. Questa prova misura direzioni
alternative; non cambia il giudizio sulle due derivate o i dati precedenti.
Sorgenti produttive, test, fisica e ricetta rimangono invariati.

Per ognuno dei due nodi si misurano due direzioni: frazioni di massa ed energia
uguali, che conservano l'energia interna specifica al punto grezzo, e una
compensazione della pressione grezza al primo ordine. Per quest'ultima,
dM/M = ε e dU/U = kε; k è il rapporto negativo delle variazioni centrali della
pressione grezza nelle sonde della prova 4 a 1e-6. I coefficienti sono circa
0,40613291 per control-out e 0,40207222 per turbine-inlet. La compensazione
non impone pressione costante durante la ricetta e non corregge lo stato.

Ogni direzione ha sonde centrali a 1e-7, 3e-7, 1e-6, 3e-6, 1e-5, 3e-5 e
1e-4. Entrambi i segni e la baseline si ripetono due volte: **114 ricette**,
al massimo **344 passi runtime**, inclusi due passi di inizializzazione.
Contratto, implementazione, richieste e budget sono autenticati prima
dell'acquisizione; non si sovrascrivono misure parziali o complete.

Si applicano i medesimi controlli lungo tutta la ricetta: salute, inviluppo,
fasi di tutti i nodi, rami idraulici, del condensatore, meccanici e del governor.
Si ricostruiscono residui e bilanci con le stesse tolleranze: 0,01 W,
1e-6 kg/s e 1e-6 N·m. Ripetizioni identiche; inventari, residui, fasi e hash
completi della baseline devono coincidere con la prova precedente.

Si confrontano tutte le coppie di ampiezze adiacenti con gli stessi pesi,
81 righe diagnostiche e tolleranza relativa del 10%. Per ciascuna direzione
si seleziona la maggiore coppia validata. Per ogni nodo si preferisce la
direzione compensata in pressione, se validata; altrimenti si sceglie quella
a energia interna specifica costante, se validata. Non si seleziona in base
al rango finale e non si forza una direzione mancante. Tutti i confronti
sono conservati. I 75 residui e il criterio originale di radice non cambiano.

Le direzioni scelte si aggiungono alle 28 precedenti. Si conserva la mappa
esplicita dai coefficienti della base alle 24 frazioni fluide e alle sei
grandezze non fluide. I quattro coefficienti unilaterali restano non negativi.
Una direzione accoppiata non valida automaticamente le due derivate isolate:
il risultato è una base direzionale con vincoli, non trenta coordinate libere.
Il rango normalizzato, con soglia 1e-8, resta una diagnosi numerica e non
dimostra fattibilità del cono, sufficienza della famiglia o compatibilità
di una proposta composta da più direzioni.

Compilazione, fixture e preflight senza runtime precedono la misura. Questa
prova non calcola né esegue proposte o ricerche. Dopo il congelamento,
`python eng/run-r3-repaired-steam-directions5.py review` ricostruisce l'esito
senza nuove simulazioni. R3 e M10 restano aperti.
