# Verifica prospettica del modello del minimo delle capacità

La revisione 9 ha mostrato, in modo solo retrospettivo, che un modello con il
minimo delle capacità linearizzate riproduce le due proposte della prova 6 e
le 16 sonde congelate. Questa prova misura **proposte nuove**, le cui
previsioni sono congelate prima della prima chiamata fisica. Non qualifica una
famiglia e non avvia una ricerca. Sorgenti, test, ricetta, legge fisica,
scale e criterio di radice restano invariati.

## Modello

Il modello è quello della revisione 9, ricostruito dai 38 record congelati
dell'osservazione 8. I suoi coefficienti devono coincidere esattamente con
quelli dell'analisi 9 congelata. Si usano soltanto le otto direzioni attive,
le uniche con capacità osservate; nessuna è unilaterale.

## Regola delle proposte

All'interno di una sequenza di vincitori fissata ai tre checkpoint, capacità e
righe sono affini nel passo. Si provano tutte le 125 sequenze. Una sequenza è
ammessa se il suo programma lineare è realizzabile con:

- ogni coefficiente entro l'ampiezza campionata della propria direzione;
- il vincitore dichiarato in vantaggio di almeno 1e-3 kg/s sulle altre
  capacità, a ogni checkpoint, al passo intero.

L'obiettivo primario minimizza il massimo valore assoluto delle 81 righe
pesate previste. Un secondo programma minimizza la somma assoluta dei
coefficienti normalizzati, con un peggioramento primario ammesso di 1e-8 nella
scala interna. Entrambi devono restituire un certificato numerico. La regola è
stata esplorata offline sui record congelati prima di questo contratto: le
sequenze ammesse sono tre, con vincitore costante stop, admission ed
expansion. Con 1e-9 il secondo programma del ramo admission non veniva
certificato; per questo l'ammissione è 1e-8. Il contratto fissa il numero di
rami a tre: un numero diverso ferma la prova prima di ogni chiamata fisica.

Per il ramo control non esiste un passo con il margine richiesto: la baseline
sta sul pareggio fra stop e control, e control non resta in vantaggio in modo
robusto. Questo ramo e le sequenze miste restano fuori dalla verifica.

## Esecuzione

Il passo intero non viene eseguito. Per ogni ramo si misurano le frazioni 0,25
e 0,50, ciascuna ripetuta due volte, più due baseline: **14 ricette**, al
massimo **44 passi runtime**, inclusi i due dell'harness. Le previsioni delle
frazioni usano il modello completo, non una scalatura lineare. A ogni frazione
il modello deve prevedere la sequenza dichiarata con margine di almeno
1e-4 kg/s, e le coordinate aggregate devono rispettare il limite ereditato del
2%. Prima dell'esecuzione si congelano, per ogni proposta:

- la variazione prevista delle 81 righe;
- capacità, vincitori e margini previsti a ogni checkpoint;
- la variazione prevista della portata e il rapporto di cancellazione;
- richieste, certificati e hash degli assembly.

Si usa la console `R3CapacityObservation`, senza modifiche. Non si
sovrascrivono acquisizioni esistenti.

## Criteri

Una previsione è conforme se valgono insieme:

- record disponibile, con salute, inviluppo e regimi idraulici, termici,
  meccanici e del governor invariati, e nessun cambio di fase;
- vincitori osservati uguali ai previsti a tutti i checkpoint;
- norma dell'errore sulle 81 righe divisa per la norma della variazione
  prevista non superiore a 0,10.

Si riportano anche errore di portata e di capacità, margini osservati, massimo
delle 81 righe pesate previsto e osservato e massimo dei 75 residui originali.
Le ripetizioni devono essere identiche. La baseline deve coincidere con il
record congelato dell'osservazione 8. Si ricostruiscono i bilanci precedenti
entro 0,01 W, 1e-6 kg/s e 1e-6 N·m. Il minimo delle capacità deve riprodurre
la portata comandata come nell'osservazione 8.

Un PASS del pacchetto significa evidenza completa e riproducibile; l'esito
delle sei previsioni resta separato. Una previsione non conforme non autorizza
correzioni, nuove scale o altre ricette. I risultati precedenti non vengono
riscritti.

## Limiti dichiarati

I rapporti di cancellazione previsti sono bassi, fra circa 0,08 e 0,13: la
variazione prevista è una piccola parte della somma dei contributi delle
singole direzioni. Il criterio relativo è quindi severo. Si verificano solo
rami con vincitore robusto, solo otto direzioni e solo frazioni del passo. Una
previsione conforme non è una radice, una ricerca o una qualificazione di R3.

Fixture del programma di ramo e del budget, compilazione e preflight senza
runtime precedono l'acquisizione. Dopo il congelamento,
`python eng/run-r3-repaired-minimum-proposals10.py review` ricostruisce
modello, richieste e risultati senza nuove simulazioni. R3 e M10 restano
aperti.
