# Verifica prospettica del modello del minimo sulle 30 direzioni

La revisione 12 ha ristimato il modello del minimo su tutte le 30 direzioni,
ma lo ha confrontato solo con record congelati che muovono le otto direzioni
della prova 6. Questa prova misura **proposte nuove che muovono insieme molte
delle direzioni aggiunte**, con previsioni congelate prima della prima chiamata
fisica. Non qualifica una famiglia e non avvia una ricerca. Sorgenti, test,
ricetta, legge fisica, scale e criterio di radice restano invariati.

## Modello

Il modello è quello della revisione 12, ricostruito dai record congelati
delle osservazioni 8 e 11. I suoi coefficienti devono coincidere esattamente
con quelli dell'analisi 12 congelata.

## Regola delle proposte

Come nella prova 10, dentro una sequenza di vincitori fissata il modello è
affine. Si provano tutte le 125 sequenze. Una sequenza è ammessa se il suo
programma lineare è realizzabile con:

- ogni coefficiente entro l'ampiezza campionata della propria direzione;
- i quattro coefficienti unilaterali non negativi;
- il vincitore dichiarato in vantaggio di almeno 1e-3 kg/s, a ogni
  checkpoint, al passo intero.

L'obiettivo minimizza il massimo valore assoluto delle 81 righe pesate
previste, con certificato numerico. **Diversamente dalla prova 10 non si
applica il secondo programma che riduce il numero di direzioni**: si usa il
vertice certificato del programma primario. La scelta è voluta. Il secondo
programma riporterebbe il passo quasi soltanto sulle otto direzioni già
verificate; il vertice primario muove invece 26 direzioni, 18 delle quali fra
le 22 aggiunte, quasi tutte al limite dell'ampiezza. È una prova più severa
del modello esteso, non un passo migliore. Ogni proposta deve muovere almeno
dieci direzioni aggiunte.

La regola è stata esplorata offline sui record congelati prima di questo
contratto. Le sequenze ammesse sono quattro: vincitore costante stop,
admission, expansion e la sequenza mista control–expansion–control. Il
contratto fissa il numero a quattro: un numero diverso ferma la prova prima di
ogni chiamata fisica.

Due fatti noti prima della misura:

- l'ottimo previsto dei tre rami costanti è circa 0,955, lo stesso della
  prova 10: le 22 direzioni aggiunte non migliorano il massimo previsto entro
  le ampiezze campionate;
- la sequenza mista **peggiora** il massimo previsto (circa 2,98 al passo
  intero). Si misura lo stesso, perché è il primo passo ammesso che contiene
  il ramo control e una sequenza mista: qui conta l'accuratezza della
  previsione, non il miglioramento.

I quattro coefficienti unilaterali restano a zero in tutti i vertici: le
direzioni unilaterali non sono verificate in combinazione.

## Esecuzione

Il passo intero non viene eseguito. Per ogni sequenza si misurano le frazioni
0,25 e 0,50, ciascuna ripetuta due volte, più due baseline: **18 ricette**, al
massimo **56 passi runtime**, inclusi i due dell'harness. Le previsioni delle
frazioni usano il modello completo. A ogni frazione il modello deve prevedere
la sequenza dichiarata con margine di almeno 1e-4 kg/s, e le coordinate
aggregate devono rispettare il limite ereditato del 2%. Prima dell'esecuzione
si congelano variazione prevista delle 81 righe, capacità, vincitori, margini,
variazione di portata, rapporto di cancellazione, richieste, certificati e
hash degli assembly. Si usa la console `R3CapacityObservation`, senza
modifiche. Non si sovrascrivono acquisizioni esistenti.

## Criteri

Una previsione è conforme se valgono insieme:

- record disponibile, con salute, inviluppo e regimi invariati, e nessun
  cambio di fase;
- vincitori osservati uguali ai previsti a tutti i checkpoint;
- norma dell'errore sulle 81 righe divisa per la norma della variazione
  prevista non superiore a 0,10.

Un record non valido, o fuori dal perimetro della ricostruzione delle
capacità, è una previsione non conforme e viene riportato con il motivo; non
ferma l'analisi. Si riportano anche errore di portata e di capacità, margini
osservati, massimo delle 81 righe pesate previsto e osservato e massimo dei 75
residui originali. Le ripetizioni devono essere identiche. La baseline deve
coincidere con il record congelato dell'osservazione 8. Si ricostruiscono i
bilanci precedenti con le stesse tolleranze.

Un PASS del pacchetto significa evidenza completa e riproducibile; l'esito
delle otto previsioni resta separato. Una previsione non conforme non
autorizza correzioni, nuove scale o altre ricette. I risultati precedenti non
vengono riscritti.

Fixture del programma di ramo e del budget, compilazione e preflight senza
runtime precedono l'acquisizione. Dopo il congelamento,
`python eng/run-r3-repaired-minimum-proposals13.py review` ricostruisce
modello, richieste e risultati senza nuove simulazioni. R3 e M10 restano
aperti.
