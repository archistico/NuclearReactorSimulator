# Revisione 2 — obiettivo coerente per il massimo energetico

La revisione vincolata precedente ottimizza una somma di quadrati con pesi
su massa, energia e coppia, e classifica il massimo energetico della soluzione.
I 335–357 kW ottenuti descrivono quelle soluzioni L2; non sono limiti inferiori
del massimo energetico, né provano che nessuna combinazione possa ridurlo.
L'esito precedente resta congelato, con la propria policy originale.

Questa revisione offline usa gli stessi residui, le stesse 24 colonne fluide
e le sei colonne non fluide, autenticate dalle rispettive ricevute. Impone
gli stessi box: inventari ±2%, grandezze non fluide ±1%. Confronta ora due LP
che minimizzano direttamente il massimo assoluto dei 36 tassi energetici:
soli inventari e inventari con tutte le grandezze non fluide separate.
Il solutore già verificato deve restituire certificati primale/duale e gap;
il massimo viene ricostruito nelle unità originali e i limiti ricontrollati.

Il rapporto usa il minimo energetico dei soli inventari come denominatore,
non il massimo della vecchia soluzione L2. La soglia diagnostica resta 50%.
Si riportano anche residui di massa, coppia e massimo scalato. La minimizzazione
energetica non impone accettazione di massa o coppia e non produce un seed:
i punti sono previsioni nel modello lineare storico, non ricette dell'impianto.
Non si alterano criteri fisici, classificazioni congelate o famiglie attivate.

Non ci sono chiamate fisiche né dipendenze dal tree produttivo corrente.
Sono autenticati file del calcolo, contratti e payload storici necessari;
il replay resta valido dopo una modifica della legge produttiva, continuando
a descrivere esclusivamente la vecchia linearizzazione.

Comandi: `python eng/review-r3-op-energy-objective2.py execute`, congelamento
di `01-analysis.json`, poi `python eng/review-r3-op-energy-objective2.py review`.
