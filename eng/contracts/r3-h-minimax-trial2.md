# Prova fisica delle direzioni minimax: Trial 2

La revisione offline H3/minimax è riprodotta prima dell'aggiunta del nuovo
esecutore di batch. Questa prova usa i modelli congelati delle iterazioni H3
8 e 47. Per ciascuno riproduce il punto di partenza e prova la direzione con
minore massimo previsto per i raggi 1e-6 e 1e-4; lo spareggio segue l'ordine
dei candidati già registrato. Sono sei punti fissi, ciascuno ripetuto due volte
con ordine punto poi ripetizione. Budget esatto: 12 ricette, un batch, due passi
preparatori, al massimo 38 passi fisici. Nessuna nuova ricerca o nuova derivata.

Le due basi devono riprodurre esattamente tutti i 75 residui e le 48 fasi
registrati in H3. Le ripetizioni devono coincidere nell'intera osservazione,
compresi gli hash dello stato e i trasferimenti energetici del passo Running 1.
Un errore tecnico o una divergenza interrompe il giudizio; i rifiuti fisici noti
sono registrati senza tentativi aggiuntivi e consumano budget.

Restano fissi coordinate, box ±2%, residui, scale, soglia di radice 1, leggi
fisiche e controllori. Si confrontano riduzione reale e prevista, errore dei
75 residui, fasi, salute e inviluppo. È promettente una direzione sana con
discesa stretta e rapporto riduzione reale/prevista ≥0.1. I due raggi permettono
di confrontare la sensibilità delle derivate terminali molto piccole.

L'esecutore riutilizzabile accetta soltanto protocolli con nome vincolato sotto
`eng`, output corrispondente sotto `artifacts` e richieste legate tramite hash
a contratto e assembly Release revisionati. Non attribuisce autorità di ricerca:
selezione, budget e osservazioni sono definiti da ciascun contratto. Aggiunge
un solo file ai 409 test esistenti, conservandone tutti i byte.

Il passo Running 1 registra anche energie e portate di valvole, confine di
ammissione e stadio turbina, per leggere i residui insieme ai trasferimenti.
La registrazione non cambia il modello. Nessun esito installa un seed o promuove
R3/M10. Una prova favorevole permette di pianificare separatamente una ricerca
coerente con il massimo residuo; una sfavorevole riporta alla diagnosi dei bilanci.
