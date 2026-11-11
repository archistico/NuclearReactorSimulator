# Trial 5: stabilizzare il solo criterio secondario numerico

Trial 4 riproduce il rifiuto secondario H5 e verifica la soluzione primaria:
il piccolo raggio migliora, quelli maggiori attraversano la fase dell'hotwell
e peggiorano molto. La scelta di una variazione contenuta resta quindi utile.

La revisione offline dei 64 modelli H5 prova un cap secondario normalizzato
1e-6 al posto di 1e-9: 63 modelli passano gli stessi certificati, uno resta
rifiutato. Non si allenta il controllo del gap o dell'ammissibilità, non si
cambia il LP primario né la soglia di equilibrio. Il cap permette al criterio
L1 di scegliere entro 1e-6 * max(1,max|r|) dall'optimum lineare primario:
nel caso iniziale circa 3.56 unità scalate su 3.56 milioni. Questa concessione
riguarda soltanto l'ottimizzazione numerica secondaria, non l'accettazione fisica.

Si usa lo stesso modello iniziale H5 di Trial 4, riproducendo la base e tre
punti con raggi 1e-6, 1e-4 e 4e-4. Tutti ripetuti due volte: otto ricette,
un batch, massimo 26 passi fisici. I casi noti controllano optimum primario,
cap secondario e colonne nulle; il caso storico deve continuare a respingere
la vecchia soluzione secondaria e accettare quella nuova con gli stessi controlli.

Riproduzione di base e ripetizioni, classificazione di discesa/rapporto ≥0.1,
fasi, salute e inviluppo seguono Trial 4. Nessuna nuova ricerca o soglia fisica
è autorizzata da questa prova. Soltanto un esito favorevole consente un distinto
protocollo di ricerca; rifiuti numerici futuri restano registrati e non aggirati.
Il box ±2%, i 75 residui, i controllori e la produzione restano invariati.
