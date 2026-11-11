# Ricostruzione della legge di energia dello stadio, revisione 2

Questa revisione sostituisce la [ricostruzione 1](r3-stage-energy-law-reconstruction1.md),
di cui mantiene ipotesi, verifiche, soglie ed esiti senza modifiche.

## Perché la prima esecuzione non è valida

Lo stadio e il condensatore ricavano le energie di trasporto dallo stato
committato all'inizio del passo. L'esecutore della ricostruzione 1 ha invece
interrogato i provider di saturazione sullo stato candidato a fine passo:
3.816.290 Pa invece dei 3.816.278 Pa usati dallo stadio. Le energie registrate
dello stadio coincidono con la saturazione storica soltanto a 5e-8 relativo,
oltre la tolleranza di identità di 1e-9, e l'esito formale è stato
`NO-STAGE-ADMISSION-CLOSURE-INCOHERENCE`. Il pacchetto resta congelato come
evidenza del difetto; il suo esito non descrive la legge dello stadio.

## Correzione

L'esecutore `M10R3StageEnergyLawReconstructionTests` interroga i provider sulla
pressione e temperatura di ingresso registrate nello snapshot dello stadio e,
per il condensatore, sullo stato iniziale dello spazio vapore registrato nel
suo snapshot. Riceve il protocollo dalla variabile `NRS_R3_STAGE_PROTOCOL`.
Il file è modificato rispetto all'albero a 412 file, che resta di 412 file; il
replay della ricostruzione 1 si esegue al commit che contiene il suo esecutore.

Policy, soglie (identità 1e-9, 10% e 50% dell'accumulo osservato), esiti e
limiti della conclusione sono quelli della ricostruzione 1.

## Comandi

`scripts\run-r3-stage-energy-law-reconstruction2.cmd`, poi, dopo la revisione
e il congelamento, `python eng/run-r3-stage-energy-law-reconstruction2.py review`.
