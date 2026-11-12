# Verifica prospettica della riconciliazione energetica del ciclo

La prova 18 annulla massa ed energia di exhaust al primo checkpoint, con una
riserva iniziale fissa dell'hotwell. Il bilancio globale chiude ma accumula
circa −0,942 MW: la sorgente iniziale è 32,971 MW e il condensatore rigetta
circa 28,535 MW. Questa prova verifica una risposta accoppiata della sorgente
e dei corpi termici. Non modifica leggi fisiche, guadagni, soglie o vecchi
domini e non qualifica una ricerca integrata.

## Proposta dichiarata prima della misura

Dal primo checkpoint selezionato della prova 18 si prende l'opposto della
potenza netta esterna, circa +0,942 MW. La frazione di popolazione neutronica
è questo valore diviso per la sorgente osservata, circa +2,856%. È un'eccezione
diagnostica esplicita oltre il vecchio limite del 2%, con tetto +3%, non un
nuovo box di ricerca o una modifica al costruttore storico FromFractions.

Per evitare di confondere energia aggiunta e accumulo nei corpi termici,
l'incremento di temperatura di ciascun corpo è la sua frazione della nuova
potenza nucleare divisa per la conduttanza verso outlet. Potenze e conduttanze
sono quelle registrate nella prova 18. Le temperature sono convertite nelle
frazioni Celsius dell'harness esistente e devono rimanere entro +0,3%.
Il calcolo è congelato e non usa risultati della nuova prova.

La previsione riguarda il solo primo checkpoint: mantenendo le altre potenze,
la variazione netta deve coincidere con quella della sorgente. Si confronta
la variazione osservata con la prevista entro l'1%, senza dividere per un
residuo finale previsto quasi nullo. L'identità di fase, inviluppo e regimi
rimane una condizione separata per dichiarare la previsione conforme.

## Misure e dominio

Si misurano baseline storica, punto locale della prova 18, metà proposta e
proposta intera, tutti ripetuti: **otto ricette**, **26 passi runtime** in
un batch. Per i tre punti non storici gli inventari rimangono esattamente
quelli del punto locale, compresa la riserva dell'hotwell. Valvole e pompe
non vengono variate. Le sole altre coordinate mosse sono popolazione
neutronica e le due temperature, nella relazione prescritta.

Una console diagnostica separata controlla il dominio prima della fisica.
Costruisce gli stessi valori `baseline * (1 + frazione)` dell'OperatingPoint
storico, con eccezione di dominio esplicita per la popolazione. Baseline
e punto locale devono coincidere integralmente con i record della prova 18,
tolto l'identificativo. Codice produttivo, test e FromFractions restano intatti.

Si osservano tutti i tre checkpoint, inventari, corpi termici, potenze e
capacità. Si ricostruiscono i bilanci della prova 17 entro 0,01 W,
1e-6 kg/s e 1e-6 N·m. Un bilancio globale chiuso con potenza netta nulla
non significa che tutti gli inventari siano stazionari: residui e accumuli
dei singoli nodi rimangono visibili. Nessun punto è installato.

Contratto, input, piano numerico, richieste e assembly precedono le chiamate
fisiche; ogni acquisizione è conservata. Il replay
`python eng/run-r3-repaired-cycle-power19.py review` ricostruisce previsioni
e risultati senza simulazioni. R3 e M10 restano aperti.
