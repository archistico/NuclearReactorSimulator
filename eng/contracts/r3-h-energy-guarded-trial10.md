# Trial 10 — proposte con margine energetico dopo la revisione delle derivate

Trial 9 non risolve i due piccolissimi cambiamenti di Trial 8 con il criterio
sufficiente dichiarato. La prova distingue quindi l'accuratezza su quella scala
dalla possibilità di verificare proposte più grandi. Non autorizza una ricerca
sulla sola scelta di un nuovo Jacobiano.

Si sceglie la maggiore ampiezza con tutte le 24 colonne compatibili nel risultato
congelato Trial 9: 1e-7. I sei valori provati restano conservati; le ampiezze da
1e-6 in su hanno colonne indisponibili, che non vengono sostituite da zero.
Il massimo errore direzionale misurato sui punti a fase compatibile viene usato
come filtro: la riduzione prevista di ogni nuova proposta deve essere almeno
dieci volte tale errore. Non si presume che questo sia un limite dimostrato
per altre direzioni; occorre misurare l'errore della nuova proposta.

Il sottoproblema minimax già verificato riceve quattro disequazioni sul margine
energetico locale: `e_k + Je_k*delta >= 0.1*e_k`. Le derivate provengono dagli
stessi stencil dei residui in Trial 9. La conversione J/kg→MJ/kg e la
normalizzazione delle disequazioni non modificano i 75 residui, le scale, il
box ±2% o la soglia fisica di radice ≤1. La tolleranza C4 è quella produttiva.

Si richiedono tre raggi prefissati, 1e-6, 1e-5 e 1e-4. Un certificato numerico
fallito viene conservato come rifiuto senza chiamate fisiche, senza rilassamenti
o tentativi correttivi. Le proposte certificate che superano il filtro e la
baseline H8 vengono eseguite due volte. Budget massimo: 8 ricette, un batch,
26 passi conservativi. Ogni ripetizione deve coincidere anche negli hash di stato.

Una proposta è favorevole soltanto se sana, nelle stesse 48 fasi della baseline,
con quattro margini energetici osservati positivi e massimo residuo inferiore.
Il rapporto fra riduzione reale e prevista deve essere almeno 0,1 e l'errore
massimo sui 75 residui non deve superare il 25% della riduzione prevista.
Questo criterio qualifica la prova locale, non l'equilibrio o l'inviluppo.
Un esito negativo richiede revisione delle evidenze prima di un altro metodo.

Nessuna modifica a C#, controllori, condizioni non fluide o seed produttivo.
Un punto favorevole non promuove R3 e non chiude M10. Il replay indipendente
ricostruisce selezione del modello, certificati/rifiuti, filtro, richieste,
risposte, margini, errori e adjudication dalle evidenze congelate.
