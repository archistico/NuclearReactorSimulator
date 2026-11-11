# Diagnosi dei responsabili nel punto grezzo C4 riparato

Il protocollo osserva lo stesso punto della misura `r3-stage-transport-ownership-repair1`,
dopo la riparazione del trasporto di fase. Non cerca una radice e non cambia la fisica.
Il contratto JSON viene scritto prima della prima acquisizione e lega implementazione,
alberi `src` e `tests`, riferimenti congelati e criteri seguenti.

Si eseguono due ricette identiche: ciascuna usa la factory produttiva C4 con due passi
preparatori di 10 ms e un solo passo Running di 10 ms, per sei passi runtime totali.
Nessuna perturbazione. Una console separata in `eng/diagnostics` legge tramite
riflessione la factory interna e lo snapshot canonico interno; non apre nuovi accessi
produttivi. L'identità completa dei due stati, prima e dopo Running, deve coincidere
con gli hash già misurati; i dodici inventari sono confrontati anche esplicitamente.
Le ripetizioni devono essere identiche. Un'acquisizione parziale non viene sovrascritta.

La diagnosi ricostruisce gli scambi termici da conduttanza e temperature impegnate,
le sorgenti nucleari dai gruppi di canali e gli eventuali riscaldatori. Per ogni corpo
termico verifica il bilancio con l'accumulo entro 0,01 W. Per `exhaust` confronta gli
accumuli effettivi di massa ed energia con gli ingressi dello stadio e l'uscita verso
il condensatore, entro 1e-6 kg/s e 0,01 W. Controlla che bypass e altre connessioni
non aggiungano flussi non contabilizzati; verifica anche la legge dei tre limiti
del condensatore e la composizione della capacità termica effettiva.

Per `outlet`, il trasporto energetico netto è **inferito** sottraendo il calore
ricostruito all'accumulo reale. Non è una misura indipendente delle singole portate
corrette: quelle mostrate dai componenti precedono la correzione idraulica. Questa
distinzione resta visibile nei risultati. Non si presenta la sottrazione come una
verifica indipendente del bilancio idraulico.

Il confronto sul raffreddamento è solo algebra a stato e ingresso dello stadio
fissi. Si calcolano separatamente la portata di condensazione che annullerebbe
l'accumulo di massa e quella che annullerebbe l'accumulo energetico. La differenza
non dimostra impossibilità in un box di ricerca e non autorizza una correzione fisica.
Il criterio di radice e le qualifiche R3/M10 restano invariati e non superati.

La compilazione della console e le fixture algebriche precedono l'acquisizione.
La ricevuta conserva i quattro assembly caricati e gli hash del contratto.
Il replay `python eng/review-r3-repaired-raw-owner-diagnostic1.py review` ricostruisce
l'analisi dai dati congelati senza eseguire il runtime. Gli alberi produttivi e di
test devono restare identici a quelli già verificati con 1.409 test ordinari superati.
