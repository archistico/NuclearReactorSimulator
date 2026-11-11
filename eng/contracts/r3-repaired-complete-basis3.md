# Misura delle trenta colonne sul punto C4 riparato

La prova estende le due diagnostiche precedenti a tutte le 24 coordinate degli
inventari fluidi e alle sei grandezze non fluide dell'harness esistente. Non cambia
la ricetta, la legge fisica, le scale, l'inviluppo o il criterio originale di radice.
Il contratto autentica prima della misura direzioni, richieste, implementazione e
input congelati. Gli alberi produttivi e di test restano quelli già verificati.

Ogni direzione usa tre ampiezze relative: 1e-6, 1e-5 e 1e-4. Le due coordinate
di `pressure` usano invece 1e-7, 1e-6 e 2e-6; l'energia di `exhaust` usa 1e-6,
3e-5 e 1e-4. Queste eccezioni derivano dal raffinamento 2, conservato intatto.
Ogni segno e la baseline sono ripetuti due volte: **362 ricette**, al massimo
**1.088 passi runtime**, inclusi i due passi di inizializzazione dell'harness.
Il runner separato riutilizzabile è quello già compilato per il raffinamento.
Le frazioni delle temperature mantengono la convenzione Celsius dell'harness.

Controlli di salute, inviluppo, fasi e rami idraulici, termici, meccanici e del
governor sono gli stessi del raffinamento 2 e valgono lungo i tre checkpoint.
Si ricostruiscono inventari, residui, bilanci termici, di exhaust e meccanici
con le stesse tolleranze: 0,01 W, 1e-6 kg/s e 1e-6 N·m. Ogni punto sovrapposto
alle due prove precedenti deve avere identici inventari, residui, fasi e hash
completi. Le ripetizioni devono coincidere; rifiuti e file parziali si conservano.

Si confrontano tutte le coppie di ampiezze adiacenti. Una colonna è selezionata
solo se i due stencil centrali sono ammessi e differiscono al più del 10% nella
medesima norma fisica pesata della prova 1, comprese le sei righe termiche.
Si sceglie la maggiore ampiezza conforme e si conserva ogni confronto. Non si
cambia riferimento dopo aver visto un risultato e non si forza una colonna mancante.

Selezione e disponibilità non provano indipendenza o sufficienza della famiglia.
Si riporta anche un rango numerico diagnostico: colonne normalizzate, Gram-Schmidt
con pivot e due riortogonalizzazioni, soglia 1e-8 sulla norma residua. I pivot e
i margini restano osservabili. Non è un certificato di rango o fattibilità fisica.
Le 81 righe e le colonne selezionate sono conservate per la successiva revisione.
I sei tassi termici sono diagnostici e non sostituiscono i 75 residui originali.

Non si ottimizzano passi, non si eseguono proposte o ricerche e non si qualifica
un seed. Una base disponibile richiederà ancora previsioni confrontate con
l'impianto prima della ricerca. L'accordo fra due scale non autorizza una regione
globale o l'estrapolazione verso un equilibrio.

Compilazione, fixture e preflight senza runtime precedono l'acquisizione. L'analisi
è separata dalle chiamate fisiche. Dopo il congelamento il replay
`python eng/run-r3-repaired-complete-basis3.py review` ricostruisce l'esito senza
nuove simulazioni. R3 e M10 restano aperti.
