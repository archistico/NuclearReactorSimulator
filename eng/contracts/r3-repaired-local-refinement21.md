# Raffinamento delle otto direzioni mancanti nel nuovo punto

La prova 20 recupera 22 direzioni su 30. Le altre otto hanno risposte
incompatibili fra le due ampiezze, pur rispettando i controlli originari.
Nel centro il limite attivo è expansion ai tre checkpoint, mentre diverse
sonde passano ai limiti delle valvole. Una secante attraverso questi cambi
non è automaticamente una derivata locale utilizzabile.

Si ripetono soltanto le otto direzioni non selezionate, senza modificare
centro, vettori, soglie, fisica o riserva iniziale. Le ampiezze sono i fattori
0,001, 0,003, 0,01, 0,03, 0,1 e 0,3 dell'ampiezza storica, ai due segni.
Ogni punto è ripetuto due volte; si aggiungono centro e baseline storica,
anch'essi ripetuti. Budget fisso: **196 ricette**, un batch, al massimo
**590 passi runtime**, inclusa la preparazione. Non si generano richieste
adattive e non si estende il dominio diagnostico della prova 20.

Si riusa la console della prova 20 senza cambiarne codice o assembly.
Prima della misura si congelano contratto, richieste, fixture e identità
dei binari. Centro e baseline devono riprodurre integralmente i record
precedenti, tolto l'identificativo. Tutte le osservazioni e i bilanci
mantengono le tolleranze della prova 20.

Una coppia di ampiezze adiacenti è ammessa soltanto se entrambe le sonde
ai due segni conservano salute, inviluppo, fasi e regimi precedenti e, in
aggiunta, il vincitore del minimo di portata del centro ai tre checkpoint.
Le derivate centrali devono concordare entro il 10% sulle 81 righe pesate
e sulle 15 capacità, usando il medesimo pavimento dichiarato per capacità
nulle. Si sceglie la coppia conforme con ampiezza maggiore; altrimenti
la direzione rimane non disponibile. Questo requisito di ramo è specifico
della nuova misura e non riscrive l'esito della prova 20.

Le 22 colonne precedenti mantengono ambito e orientamento originali. Le
nuove possono completare la base soltanto alle scale effettivamente
misurate: il rango numerico non dimostra sufficienza della famiglia o
fattibilità del cono unilaterale. Una proposta composta richiederà una
previsione nuova, congelata prima di essere misurata. I 75 residui e il
criterio di radice rimangono invariati; nessun seed o qualificazione R3.

Il replay `python eng/run-r3-repaired-local-refinement21.py review` ricalcola
selezioni e rifiuti senza eseguire l'impianto.
