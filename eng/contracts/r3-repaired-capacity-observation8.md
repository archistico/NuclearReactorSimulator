# Osservazione delle capacità di portata dello stadio a tutti i checkpoint

L'attribuzione 7 ha ricostruito il minimo delle capacità solo al primo passo
preparatorio, usando l'uscita iniziale del governor come apertura della valvola
di control. Questa prova osserva le capacità e il vincitore a tutti e tre i
checkpoint, leggendo le posizioni effettive dagli snapshot dei solutori.
Non costruisce un modello, non esegue proposte nuove e non avvia ricerche.
Sorgenti, test, ricetta e legge fisica restano invariati.

Il resolver della portata dello stadio e la rete del vapore principale valutano
lo stesso stato comandato del passo: nodi del passo precedente e valvole dopo
gli attuatori a corsa limitata, le protezioni e i guasti idraulici. Una nuova
console diagnostica, copia di `R3ProbeRunner` con un solo blocco aggiuntivo
`StageFlow` per checkpoint, registra per stop, control e admission posizione
effettiva, coefficiente, differenza di pressione, portata, resistenza e
posizioni richieste; per lo stadio portata comandata, portata di vapore
ammessa, pressioni di ingresso e scarico, qualità e resistenza di espansione.
Il runner precedente non viene modificato: i suoi hash restano autenticati.

Si ripetono esattamente le 38 richieste già eseguite: le sei ricette della prova
6 e, per ciascuna delle otto direzioni attive del suo passo lineare, le due
sonde centrali alla scala selezionata, ciascuna con le sue due ripetizioni.
Al massimo **116 passi runtime**, inclusi i due dell'harness. Ogni record,
tolto soltanto il blocco `StageFlow`, deve coincidere esattamente con il record
congelato di origine; le ripetizioni devono essere identiche.

A ogni checkpoint le capacità delle valvole sono le portate positive dello
snapshot. Si verificano posizione effettiva uguale allo stato registrato,
assenza di fail-safe e di override di protezione, caratteristica lineare,
differenze di pressione uguali ai nodi del passo precedente entro 1e-6 Pa e
legge quadratica entro 1e-9 kg/s. Espansione e metà inventario per passo si
ricostruiscono dagli stessi nodi. Il minimo delle cinque capacità deve
riprodurre la portata comandata dello stadio entro 1e-9 kg/s; si conta quante
ricostruzioni sono esatte in binary64. Al primo checkpoint capacità e vincitore
devono riprodurre l'attribuzione 7 entro 1e-9 kg/s.

Si riportano vincitore, margine verso la capacità successiva, capacità entro
1e-6 kg/s dal minimo, scarto fra posizione richiesta ed effettiva della
control e cambi di vincitore fra checkpoint e fra i due segni delle sonde.
Le secanti centrali delle singole capacità sono soltanto descrittive: una
secante che attraversa vincitori diversi non è una derivata della portata.
I controlli e gli esiti precedenti non vengono riscritti.

Fixture del budget e del minimo, compilazione e preflight senza runtime
precedono l'acquisizione. Dopo il congelamento,
`python eng/run-r3-repaired-capacity-observation8.py review` ricostruisce
l'analisi senza nuove simulazioni. R3 e M10 restano aperti.
