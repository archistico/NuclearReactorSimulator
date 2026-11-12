# Misura della leva energetica di exhaust fino al 2%

La revisione 14 trova, nel modello verificato alle piccole ampiezze, una
combinazione delle righe di energia di exhaust e coppia che non dipende dalla
portata. La leva diretta dominante è `exhaust:energy`; la risposta alla scala
del percento non è stata misurata. Questa prova esegue la misura prevista
dall'handoff, senza cambiare fisica, famiglia, limite del 2% o criterio di radice.
Implementazione, input, richieste e budget sono autenticati prima dell'acquisizione.

Si varia solo la frazione dell'energia interna grezza di exhaust, indice 5,
alle ampiezze ±1e-4, ±1e-3, ±5e-3, ±1e-2 e ±2e-2. La massa e tutte le altre
coordinate, comprese quelle non fluide, restano alla baseline. Ogni punto e
la baseline sono ripetuti due volte: **22 ricette**, al massimo **68 passi
runtime**, inclusi due passi iniziali dell'harness. Si riutilizza la console
`R3CapacityObservation`, con controllo degli assembly e preflight senza runtime.
Non si sovrascrivono acquisizioni complete o parziali.

Si conservano fase e inventari grezzi, tre checkpoint, residui e hash completi,
salute e inviluppo, rami idraulici e meccanici, capacità e vincitore della
portata, limiti di condensazione e di scambio termico. Si riportano i tassi
di massa ed energia di exhaust, coppia, energie specifiche e potenza del
condensatore. Si ricostruiscono i bilanci con le tolleranze già usate:
0,01 W, 1e-6 kg/s e 1e-6 N·m, e il minimo delle capacità entro 1e-9 kg/s.
Eventuali uscite dal campo di ricostruzione vengono dichiarate, non corrette.
I punti sovrapposti all'osservazione 8 devono coincidere integralmente.

La combinazione al primo checkpoint è energia di exhaust in MW meno
`ratio_per_nm` congelato nella revisione 14 per la coppia in N·m. Si misurano
le secanti rispetto alla baseline e si confrontano con la leva diretta
congelata, circa −28 MW per unità di frazione, con tolleranza relativa del 10%.
Questa classificazione diagnostica non qualifica il modello completo a grandi
ampiezze. Il rapporto congelato riguarda solo il primo checkpoint; agli altri
si riportano separatamente i bilanci, senza presumere la stessa cancellazione.

La risposta è distinta fra conforme alla secante, piegata, con cambio di fase,
fuori inviluppo, con cambio di regime o non sana. I dati fuori dai controlli
restano osservazioni. Si riporta se il punto positivo al 2% attraversa lo zero
della combinazione e quale coordinata ne deriverebbe per estrapolazione della
secante. Un valore oltre il 2% non è una radice misurata, un nuovo limite di
ricerca o una prova generale d'impossibilità della famiglia.

Non si calcolano proposte o ricerche. Dopo il congelamento il replay
`python eng/run-r3-repaired-exhaust-lever15.py review` ricostruisce l'esito
senza nuove simulazioni. I risultati precedenti, R3 e M10 restano invariati.
