# Trial 8: verificare la derivata lungo una direzione fissa al bordo

Trial 7 non trova discesa con i tre raggi, anche quando la fase resta miscela.
Prima di cambiare solver si distingue un errore di derivata da un passo troppo
lungo. Si usa una sola direzione già calcolata: la proposta Trial 7 con raggio
1e-6. Restano fissi base H8, direzione e Jacobiano H8 terminale (h=1e-9).

Si provano la base e le frazioni 1, 0.1, 0.01, 0.001, 0.0001 della direzione,
in tale ordine, due ripetizioni per punto: 12 ricette in un batch, massimo
38 passi fisici. Tutti i punti restano sul segmento già interno al box;
non si usano passi negativi che uscirebbero dai limiti attivi.

Si confrontano i 75 incrementi reali con quelli previsti, l'errore diviso per
la frazione, il massimo residuo, fasi, pressione e margine C4 nei quattro stati.
Un errore di primo ordine che non diminuisce al restringersi del passo richiede
revisione dello stencil o del ramo; un errore che diminuisce più rapidamente
della frazione può sostenere una diversa scala di passo, senza dimostrarla
globalmente. Le soglie e il rumore numerico non vengono ignorati.

Base e ripetizioni devono coincidere, i rifiuti sono registrati e non generano
nuove ricette. Nessuna ricerca, sostituzione del Jacobiano, modifica fisica o
nuova accettazione R3/M10 è autorizzata da questo confronto. Restano invariati
24 coordinate, box ±2%, 75 residui e criterio di radice ≤1.
