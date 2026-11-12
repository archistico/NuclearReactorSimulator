# Proposte sul criterio originale con vincoli termici separati

La prova 22 verifica correttamente due previsioni nel nuovo punto, ma il suo
obiettivo diagnostico favorisce la coppia e aumenta il massimo dei residui
originali. Questa prova cambia l'obiettivo della proposta, senza modificare
il criterio di radice: si minimizza direttamente il massimo assoluto dei
**75 residui scalati originali**, con scale e soglia ≤1 invariate.

## Modello e vincoli dichiarati prima delle misure

Si usano le trenta direzioni rimisurate nelle prove 20–21, con le ampiezze
selezionate e i due coefficienti di suction non negativi. Il centro rimane
il punto della prova 19. Le 75 colonne sono ricavate dalle colonne pesate
invertendo esattamente la conversione di unità e i pesi diagnostici; le
fixture confrontano la conversione anche sul vettore del centro. Non si
riusano le derivate del vecchio punto.

I sei tassi dei corpi termici sono vincoli ausiliari distinti dall'obiettivo.
Ogni valore previsto deve rimanere entro il valore assoluto del corrispondente
tasso del centro, con pavimento di 1 W. Si evita così che il passo apparentemente
favorevole ai fluidi trasferisca gli squilibri ai corpi solidi. Sulla misura
si ammette un eccesso massimo di 1 W rispetto a questi limiti, per confrontare
la previsione locale con il runtime. Non è una tolleranza di qualificazione
dell'equilibrio termico.

Anche le capacità sono vincoli separati: la differenza prevista fra ogni
capacità concorrente e expansion deve conservare almeno metà del margine
del centro, in ciascuno dei tre checkpoint. Questa metà del margine è
richiesta anche alla risposta misurata; il solo nome del vincitore non basta.

Il programma lineare usa il solutore certificato già disponibile, con
traslazione del box firmato in variabili non negative. La normalizzazione
numerica delle disequazioni ausiliarie non ripesa i residui. Una seconda
ottimizzazione minimizza la norma L1 del passo normalizzato, mantenendo il
massimo primario entro l'identica tolleranza numerica 1e-9 sul problema
normalizzato. Sono conservati certificati primario e secondario e gli
eventuali aggiustamenti dei soli sconfinamenti numerici entro 1e-12.
Fixture analitiche verificano vincolo attivo, unilateralità e criterio L1.

## Due proposte prospettiche

Si misurano il 25% e il 50% del passo vincolato completo, oltre a centro e
baseline storica, tutti ripetuti: **otto ricette**, un batch, al massimo
**26 passi runtime**. La console della prova 20 è riusata senza modificarla;
ogni variazione aggregata deve restare entro il suo dominio diagnostico.
Nessun nuovo box di ricerca o parametro produttivo è introdotto.

Le previsioni sulle variazioni dei 75 residui originali, delle 81 righe
diagnostiche e delle 15 capacità devono concordare con le misure entro il
10%, ciascuna sulla propria norma vettoriale. Per le capacità rimane il
pavimento di 1e-9 kg/s. Si richiedono inoltre salute, inviluppo, fasi e
regimi precedenti, expansion limitante e margini conservati almeno a metà.
Per essere ammissibile a una revisione successiva, il punto deve anche
ridurre realmente il massimo originale e rispettare i limiti termici.
Un esito negativo rimane registrato; nessuna proposta viene proiettata,
corretta o ripetuta in modo adattivo.

Si riportano i residui di tutti i nodi in unità fisiche, i corpi termici e
le potenze globali. Ripetizioni, identità integrale di centro e baseline
e bilanci indipendenti mantengono i controlli precedenti. Contratto,
previsioni, richieste e hash dei binari precedono la misura. Il replay
`python eng/run-r3-repaired-root-objective23.py review` non esegue l'impianto.

Nessun punto è installato o accettato come aggiornamento di una ricerca
integrata. Anche una riduzione conforme richiederà rimisura, algoritmo
e dominio motivati, conferme di 100 passi e qualificazione R3.
