# H9 — ricerca limitata dopo la prova energetica ripetuta

Trial 10 qualifica localmente la proposta al raggio 1e-5: entrambe le ripetizioni
coincidono, le fasi restano compatibili e l'errore dei residui rispetta il criterio
congelato. Il punto al raggio 1e-4 attraversa la fase e non viene promosso.
Il rifiuto numerico al raggio 1e-6 resta conservato, senza rilassare certificati.
H9 parte dal miglior punto favorevole di Trial 10 e lo riproduce esattamente.

Il sottoproblema resta il minimax accoppiato con quattro disequazioni sul margine
energetico C4, mantenendo il 10% del margine corrente. I margini sono convertiti
in MJ/kg prima della normalizzazione numerica. La tolleranza del resolver,
i 75 residui, le scale, le 24 incognite e il box ±2% restano invariati.

Le sonde usano prima h=1e-7. Soltanto le colonne indisponibili vengono riprovate
a h=1e-8; non si torna a 1e-9. Lo stencil centrale/unilaterale richiede risposta
sana e tutte le 48 fasi della baseline. Residui e margini energetici usano gli
stessi campioni e la stessa ampiezza per ciascuna colonna. Colonne mancanti o
certificati falliti non producono direzioni fittizie.

Raggio iniziale/massimo 1e-5, minimo 1e-7. Si propongono il passo completo e
quattro dimezzamenti, eseguendo soltanto quelli con riduzione prevista positiva
e almeno dieci volte l'errore direzionale osservato nel modello di Trial 10
(67,12488194 nelle scale congelate). Questo filtro limita le decisioni numeriche:
non è una tolleranza di radice né un limite dimostrato per tutte le direzioni.

Un tentativo è ammissibile se sano, nelle fasi della baseline, con tutti i margini
energetici positivi, errore massimo dei residui ≤25% della riduzione prevista
e rapporto riduzione reale/prevista ≥0,1. Sonde ammissibili possono migliorare
il punto anche senza una proposta; per esse vale la discesa reale stretta.
La selezione confronta il massimo residuo effettivo, con spareggio sull'ordine
registrato. Il raggio resta fisso quando si migliora e dimezza altrimenti.

Budget: 32 iterazioni, al massimo 3233 ricette e 97 batch, 9893 passi conservativi.
Si arresta a radice osservata, budget, limite di iterazioni o raggio minimo senza
discesa ammissibile. Il replay ricostruisce anche stencil di riserva, errori,
filtri, scarti, richieste, selezioni e arresto. Fixture coprono una radice
accoppiata nota e l'arresto prima di sonde non finanziate, oltre ai casi numerici.

Nessuna modifica produttiva o installazione di seed. Una radice osservata
richiede comunque revisione, conferme indipendenti di 100 passi e tutte le
condizioni d'ingresso di R3; R3 e M10 restano distinti dalla completezza del replay.
