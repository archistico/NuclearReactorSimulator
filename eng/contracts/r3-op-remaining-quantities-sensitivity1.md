# Sensibilità alle altre grandezze non fluide nel punto grezzo del modo 2

La [diagnostica della valvola](r3-op-control-valve-sensitivity1.md) ha concluso
che la valvola di regolazione non è materiale nel modello lineare: insieme agli
inventari porta il massimo tasso di energia previsto da 302 a 209 kW, il 69%.
La sua specifica indica, per un esito non materiale, la stessa diagnostica
sulle altre grandezze. Questa diagnostica misura le cinque restanti esposte
dal seam del seed. È una misura: non seleziona C o J, non cerca una radice e
non installa seed.

## Superficie di test

Nessuna modifica al codice di test. `M10R3OperatingPointProtocolBatchTests` e
`R3HFeasibilityHarness` accettano già sei frazioni relative ai valori del
punto grezzo, ciascuna entro ±0,02. Questo protocollo usa le ultime cinque:

| Grandezza | Valore di baseline |
| --- | --- |
| Pompa di condensato | 42,97% |
| Pompa di alimento | 96,93% |
| Popolazione neutronica relativa | 0,3297 |
| Temperatura iniziale del combustibile | 305,63 °C |
| Temperatura iniziale della struttura | 289,14 °C |

La frazione della valvola resta nulla. Le temperature sono variate in frazione
del valore in gradi Celsius: 1e-2 corrisponde a circa 3 K. L'albero `tests/`
resta quello a 411 file della diagnostica della valvola.

## Ricette

Inventari a coordinate nulle. Ogni grandezza riceve, da sola, le frazioni
±1e-6, ±1e-5, ±1e-4, ±1e-3, ±1e-2; con la baseline sono 51 punti, ciascuno
eseguito due volte. Budget: 102 ricette, tre batch (48, 48, 6), 306 passi. Le
due ripetizioni di un punto cadono nello stesso batch. Ogni ricetta esegue i
due passi di seed e un passo Running invariati e registra gli stessi 75
residui, fasi, osservazioni, bilanci dello stadio e hash di stato.

## Verifiche

La baseline deve riprodurre esattamente residui e fasi del record iniziale di
H feasibility 1 e coincidere, record per record, con la baseline congelata
della diagnostica della valvola. Le ripetizioni devono coincidere, hash di
stato inclusi. Per ogni grandezza e ampiezza si calcola la colonna di
sensibilità con la regola di compatibilità di fase delle ricerche H e si
riportano metrica fisica, inviluppo e uscita del regolatore di ogni sonda.

La colonna primaria di ogni grandezza è quella a 1e-4. Il suo stato è:

- `usable` se disponibile e se la differenza massima pesata dalla colonna a
  1e-5 non supera il 10% del proprio massimo;
- `null` se disponibile e identicamente nulla: la grandezza non sposta alcun
  residuo ed è determinata come non materiale;
- `nonlinear` se la differenza supera il 10%;
- `unavailable` se la regola di fase non fornisce la colonna a 1e-4 o a 1e-5.

## Modelli lineari

Con i pesi fisici della revisione di sufficienza si risolvono ai minimi
quadrati, senza vincoli, sui 75 residui: soli inventari; per ogni colonna
utilizzabile, la sola grandezza e gli inventari più la grandezza; infine gli
inventari più tutte le colonne non fluide utilizzabili. Quest'ultimo modello
include la colonna primaria della valvola, ricostruita dal pacchetto congelato
e autenticato dalla sua ricevuta. Lo Jacobiano degli inventari è quello
congelato e compatibile di fase.

## Esiti

Il rapporto di una grandezza è il massimo tasso di energia previsto dal modello
inventari più grandezza diviso quello dei soli inventari.

- `NONFLUID-QUANTITY-MATERIAL-IN-LINEAR-MODEL`: almeno una grandezza ha
  rapporto al più del 50%; l'esito le elenca.
- `NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL`: nessuna da sola, ma il
  modello con tutte le colonne non fluide raggiunge il 50%.
- `INCONCLUSIVE-UNUSABLE-PRIMARY-COLUMN`: nessuno dei casi precedenti e almeno
  una colonna `nonlinear` o `unavailable`.
- `NO-NONFLUID-QUANTITY-MATERIAL-IN-LINEAR-MODEL`: tutte le colonne sono
  `usable` o `null` e nessun modello raggiunge il 50%.

Le soglie 10% e 50% classificano la diagnostica; non sono tolleranze fisiche
né criteri di radice. Le previsioni lineari non sono osservazioni e passi oltre
±2% sono fuori dalla regione descritta dalle sonde. Un esito materiale motiva
soltanto la pianificazione di una famiglia che includa quelle grandezze, con
protocollo distinto. L'esito `NO-…` soddisfa la condizione per conservare il
risultato e rivedere la legge di energia dello stadio prima di altre ricerche.
Nessun esito cambia scale, residui, box, inviluppo, guadagni, C4 o Exact-V9,
né promuove R3 o M10.

## Comandi

`scripts\run-r3-op-remaining-quantities-sensitivity1.cmd` esegue revisione
statica, build Release con avvisi come errori, i tre controlli preliminari, le
102 ricette e l'adjudication. Gli output sono in
`artifacts/r3-op-remaining-quantities-sensitivity1`. Dopo la revisione
dell'esito il pacchetto viene congelato e
`python eng/run-r3-op-remaining-quantities-sensitivity1.py review` ne esegue il replay.
