# H8: ricerca nel ramo locale validato lungo tutta la ricetta

Trial 6 verifica che il modello della qualità H6 prevede già l'uscita dalla
miscela dopo la preparazione. Il nono dimezzamento e tutte le tre proposte
vincolate migliorano fisicamente conservando la miscela nei quattro checkpoint,
con ripetizioni identiche. H8 sceglie il miglior punto sano, pari a 3.514.643,573,
e ne riproduce residui e fasi prima delle nuove sonde.

La ricerca usa il modello minimax con le quattro disequazioni di Trial 6:
q_k+Jq_k*delta ≥0.1*q_k. Le derivate della qualità usano le stesse sonde e lo
stesso stencil centrale/unilaterale compatibile dei residui. I vincoli sono
normalizzati separatamente per essere visibili al solver, non ripesano i 75
residui. LP primario, cap secondario 1e-6, certificati e frazione globale 0.99
sono invariati rispetto alla prova. Il vincolo è ricontrollato nelle unità q.

Il dominio numerico dichiarato è il ramo miscela hotwell nello stato grezzo
e nei tre checkpoint. Tutte le ricette previste sono registrate; punti sani
che escono dal ramo restano osservazioni fisiche valide, ma non candidati
accettabili per questa ricerca locale. Non vengono riscritti i loro residui
o classificati come errori fisici. Un esito inconclusivo non esclude una radice
nel liquido o su un'altra porzione del box.

Raggio iniziale/massimo 1e-4, minimo 1e-8; h=min(1e-6,raggio/10). Si propone
una direzione vincolata e otto dimezzamenti. Si confrontano sul massimo reale
tutte le sonde e i tentativi sani rimasti nel ramo, con discesa stretta e
spareggio sull'ordine. Restano le regole H6 per adattare il raggio e contare
stagnazione. Colonne mancanti o certificati falliti non generano direzioni fittizie.

Budget massimo 4096 ricette, 64 iterazioni, 129 batch e 12546 passi conservativi.
Si arresta anche a radice osservata, minimo raggio senza discesa o tre progressi
relativi consecutivi ≤1e-9. Il replay deve ricostruire anche i rifiuti di ramo,
qualità previste/osservate, certificati, richieste, selezioni e arresto.

I casi noti verificano margini ordinari/piccoli, movimenti simultanei, rifiuto
di una qualità interna fittizia, conservazione del raggio per sonde accettate,
convergenza accoppiata e budget. Restano 24 inventari, box ±2%, scale e soglia
fisica di radice ≤1. Non si modifica produzione o controllori. Una radice
richiede ancora conferme indipendenti di 100 passi prima di R3/M10.
