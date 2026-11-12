# Proposte sul criterio originale entro l'inviluppo previsto

La prova 23 riduce il massimo originale del 5,35% con il passo al 25%, che
rispetta tutti i controlli. Il passo al 50% riduce di più, ma supera la
portata ammessa della pompa primaria e il margine termico dichiarato:
rimane escluso. Questa prova include i valori operativi nel modello della
proposta e aggiunge margine termico, senza cambiare le soglie di accettazione.

## Nuove righe ausiliarie da misure già congelate

Dai record delle prove 20–21 si ricavano derivate di potenza elettrica,
portata della pompa primaria, livello del drum e uscita del governatore,
ai tre checkpoint. Per ciascuna direzione si usano esattamente gli stencil
e le ampiezze selezionati in quelle prove. Il confronto fra ampiezze
normalizza ciascun valore per l'ampiezza del suo intervallo operativo;
la deviazione deve essere entro il 10%, con pavimento uno sulla norma
del vettore derivata normalizzato. Questa normalizzazione riguarda solo
il confronto delle nuove derivate e non modifica i residui originali.
Una direzione non conforme viene bloccata a coefficiente zero nel problema
lineare: non si inventa una colonna o si rilassa la soglia.

L'inviluppo resta quello dell'harness: 4,99–5,01 MWe, 99,9–100,1 kg/s
per la pompa primaria, livello 0,49–0,51 e governatore 29,27–29,30.
Le nuove disequazioni richiedono che la previsione conservi un margine
pari al 10% dell'ampiezza dell'intervallo da entrambi gli estremi, a ogni
checkpoint. Si tratta di un vincolo di previsione più interno alle soglie
esistenti, non di un nuovo inviluppo del runtime.

Ogni tasso termico previsto è limitato a `max(abs(tasso del centro)−10 W,1 W)`.
I 10 W di margine sono dichiarati prima della misura per evitare di lavorare
sul confine termico che la prova 23 ha oltrepassato per curvatura della
risposta. La verifica dei tassi misurati rimane quella della prova 23:
valore assoluto del centro, pavimento di 1 W e massimo eccesso di 1 W.

Il problema conserva l'obiettivo del massimo dei 75 residui originali,
scale, box delle ampiezze e due coefficienti di suction non negativi.
Si riusa senza modifiche il programma lineare vincolato della prova 23,
compreso il criterio secondario L1 e i certificati. Le capacità concorrenti
di expansion devono conservare almeno metà del margine del centro, sia
nella previsione sia nella misura. Il centro è sempre il punto della prova 19.

## Due misure nuove

Si eseguono metà e intero passo vincolato, oltre a centro e baseline storica,
tutti ripetuti: **otto ricette**, un batch, al massimo **26 passi runtime**.
La console e il dominio diagnostico della prova 20 rimangono invariati.
Modello, richieste, fixture e identità dei binari sono congelati prima delle
chiamate fisiche. Nessun esito della nuova prova modifica il passo generato.

Per dichiarare conforme la previsione, le variazioni dei 75 residui,
delle 81 righe diagnostiche e delle 15 capacità devono concordare entro
il 10% sulle rispettive norme. L'errore assoluto di ciascun valore operativo
previsto, diviso per l'ampiezza del suo intervallo originario, deve essere
entro 0,01. Restano necessari salute, inviluppo effettivo, fasi e regimi,
expansion limitante e margini delle capacità. Per essere ammissibile a una
revisione successiva, il punto deve anche ridurre il massimo originale e
rispettare i limiti termici misurati. Ogni rifiuto viene conservato.

Ripetizioni, identità integrale di centro e baseline e bilanci indipendenti
mantengono i controlli precedenti. Il replay
`python eng/run-r3-repaired-envelope-objective24.py review` ricostruisce
anche le nuove derivate operative senza passi runtime. Questo protocollo
non accetta un aggiornamento di ricerca, non installa seed e non esegue
conferme di 100 passi o qualificazione R3. Non cambia produzione o test.
