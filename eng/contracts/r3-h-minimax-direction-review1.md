# Revisione H3 e confronto minimax lungo direzioni locali

H3 ha eseguito 8192 ricette e accettato 48 aggiornamenti. Il suo residuo massimo
finale è 3.799.306,525; non è una radice e il punto non soddisfa l'inviluppo.
All'iterazione terminale 47 tutte le sei direzioni ai minimi quadrati riducono
la somma dei quadrati ma prevedono un aumento del massimo assoluto. Dimezzarle
non risolve questa incompatibilità tra obiettivo numerico e criterio di accettazione.

Questa revisione è soltanto offline. Riproduce H3 e confronta i modelli locali
delle iterazioni 8 (prima scelta di una sonda dopo l'ultima discesa di modello)
e 47. Per ciascuna usa due raggi: 1e-6 e 1e-4. I Jacobiani sono quelli ricostruiti
dalle osservazioni congelate, con le stesse regole di compatibilità di fase.
Una colonna indisponibile impedisce il confronto di quel modello.

Le direzioni sono fissate: prima le 24 coordinate in ordine, verso positivo
e negativo dove il box lo consente, poi le sei direzioni vincolate H3. Ogni
variazione resta nel raggio e nel box ±0.02 grazie a una frazione globale 0.99.
Lungo ciascuna direzione si minimizza direttamente
`max(abs(r + alpha * J * delta))` per alpha in [0,1]. È una funzione convessa
lineare a tratti: si cercano le pendenze attive per al massimo 80 bisezioni,
controllando anche gli estremi e spareggiando verso l'alpha minore.

Il risultato è un confronto fra direzioni di un modello lineare, non la soluzione
globale del problema minimax a 24 variabili. Non dimostra regolarità fisica e
non riutilizza le previsioni come osservazioni dell'impianto. I raggi maggiori
possono oltrepassare la regione descritta dalle piccole sonde: occorre una prova
fisica separata, congelata prima dell'esecuzione, per verificarli.

I casi numerici verificano optimum interno noto, radice, optimum all'estremo,
direzione nulla e rifiuto di valori non finiti. In particolare il caso
`r=[10,9,9], J=[1,-1,-1]` mostra che i minimi quadrati scelgono un incremento
positivo, mentre il minimo del massimo lungo il verso negativo è alpha=0.5,
con massimo 9.5. Le scale originali dei residui non cambiano.

Non si eseguono chiamate fisiche, non si aggiungono test o sorgenti produttivi,
non si installano inventari e non si promuovono R3 o M10. La revisione produce
punti proposti e previsioni esplicitamente etichettate, con provenienza e hash.
