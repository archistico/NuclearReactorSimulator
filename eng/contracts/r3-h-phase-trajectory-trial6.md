# Trial 6: diagnosi della traiettoria e vincolo sul ramo locale

La ricostruzione del primo modello H6 estende le medesime sonde alla qualità e
alla pressione dell'hotwell nei quattro checkpoint: grezzo, seed-step1, post-seed,
Running 1. Si usa esattamente la stessa scelta centrale/unilaterale compatibile
dei residui. Per il passo H6, q+Jq*delta prevede già l'uscita dalla miscela.
La frazione limite più stretta è circa 0.00305482, inferiore a 1/256, ultimo
tentativo H6. A 1/256 il modello prevede qualità positive nei primi due stati
e negative negli ultimi due, come confermano le fasi osservate.

Si confrontano inoltre le derivate di pressione del punto iniziale liquido H7,
senza attribuire una qualità fittizia al liquido. La differenza fra rami spiega
perché una previsione di pressione nel ramo miscela non vale dopo il passaggio.
Questo primo lavoro è offline: nessuna nuova chiamata fisica per la diagnosi.

La proposta numerica aggiunge al solo sottoproblema locale quattro disequazioni:
q_k + Jq_k*delta ≥ 0.1*q_k, una per checkpoint. Non sono nuovi residui fisici
e non modificano il criterio di equilibrio; limitano l'uso del Jacobiano al ramo
in cui è stato costruito. Ogni disequazione è normalizzata per la propria scala
di variazione per non perdere qualità dell'ordine di 1e-11 nel solver. Restano
LP primario minimax, cap secondario 1e-6, certificati, box ±2% e frazione unica 0.99.
Una verifica aggiuntiva controlla il vincolo nelle unità originali della qualità.

La prova fisica usa cinque punti fissi, due ripetizioni consecutive ciascuno:
base H6, il dimezzamento 9 del vecchio passo (non eseguito in H6), e tre proposte
con vincoli di ramo per raggi 1e-6, 1e-5 e 1e-4. Dieci ricette in un batch,
massimo 32 passi fisici compresi due preparatori. Non è una nuova ricerca.

La base deve riprodurre i 75 residui e le 48 fasi; tutte le ripetizioni devono
coincidere. Si confrontano previsioni e osservazioni di residui, pressione e
qualità, mantenendo separati salute, inviluppo e fase. Un punto è promettente
se sano, strettamente migliorativo, con rapporto reale/previsto ≥0.1 e tutti
i quattro stati hotwell ancora miscela. Un passaggio reale al liquido non viene
reinterpretato come qualità negativa: è un limite del modello locale registrato.

I casi numerici verificano margini ordinari e piccolissimi, movimenti simultanei
e rifiuto di una fittizia qualità interna nulla. Un esito favorevole permette
un distinto protocollo di ricerca nel ramo dichiarato. Non prova l'esistenza
di una radice, non esclude il ramo liquido e non installa uno stato produttivo.
Le 24 coordinate, i 75 residui, le scale e le soglie fisiche restano invariati.
