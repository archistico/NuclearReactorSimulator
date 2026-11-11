# Verifica della riparazione del trasporto di fase — stadio e condensatore

Il piano revisionato `r3-stage-transport-ownership-repair-planning1.md` limita
la modifica a quattro file produttivi. La lettura di trasporto indicizzata dalla
pressione mantiene le proprietà storiche bit per bit nei modi 0/1; sotto C4
usa il lookup esistente, senza modificare payload, geometria o attivazione.
Stadio e condensatore usano la capacità della stessa chiusura termodinamica;
modelli senza tale capacità conservano il percorso di saturazione esistente.

Il contratto macchina registra baseline entrante, hash dei file produttivi prima
e dopo, file di test aggiunti/modificati, implementazione e prerequisiti congelati.
Gli altri sorgenti e test devono rimanere identici. Prima della misura passano
compilazione Release, suite ordinaria, baseline delle evidenze correnti e il
controllo preliminare del nuovo esecutore.

Il primo giro della suite ha individuato un confronto ordinario fra inventari
C4 correnti e il CSV della legge precedente. Quel controllo viene migrato
senza aggiornare valori attesi o escludere test: autentica traccia e CSV storici,
ne confronta i valori esatti e verifica che il vettore raw corrente e le
ripetizioni mantengano identità. Una regressione ordinaria distinta verifica
ora la legge C4 attiva e la riduzione dell'accumulo con la soglia già fissata.
Il fallimento iniziale viene conservato. Solo la suite completa verde permette
la misura esplicita.

La misura esplicita esegue due ricette identiche a coordinate fluide nulle,
senza modificare il vettore iniziale, gli stati non fluidi o i controllori.
Il confronto delle due ripetizioni include tutti i dati e gli hash di stato.
Budget: due ricette, sei passi di valutazione e due preparatori dell'harness,
otto passi conservativi. Si osservano raw, seed-step1, post-seed e Running 1.

I 75 residui sono ricostruiti dagli inventari, con le scale originali.
L'accumulo assoluto su turbine-inlet in ciascuno dei tre checkpoint deve
essere ≤10% di 1.006.560,2752685547 W, congelato nella ricostruzione 2.
Al Running 1 le energie di vapore e condensa dello stadio e della condensa
del condensatore devono coincidere con la capacità C4 entro 1e-9 relativo.
La massa di ammissione deve coincidere con vapore più drenaggio entro 1e-6 kg/s;
il bilancio energetico dello stadio deve chiudere entro 0,01 W.

Nel condensatore si ricalcolano limite termico e portata effettiva dalle nuove
energie, dalla potenza disponibile e dai limiti di inventario e portata massima.
Il trasferimento energetico rimosso dal vapore meno quello ceduto all'hotwell
deve coincidere con la potenza termica rifiutata entro 0,01 W. Le tre famiglie
di limite sono coperte anche da test locali indipendenti. La portata C4 non è
un invariante numerico rispetto alla legge precedente.

Si riportano residui scalati e fisici, tassi su exhaust/hotwell, inviluppo e
scostamenti dalle precedenti registrazioni. Non si pretende equilibrio del
punto grezzo: il cambiamento sposta energia a valle e richiede nuova diagnosi.
Esito `PASS-STAGE-TRANSPORT-OWNERSHIP-REPAIR` solo se tutti i criteri passano;
un criterio fallito resta non conforme, senza modificare le soglie.

I pacchetti precedenti restano storici e si riproducono ai rispettivi commit.
Nessuna riga attesa dei vecchi test viene aggiornata. Non si riusano i vecchi
Jacobiani sulla nuova legge. La misura non qualifica R3, non installa un seed
e non attiva C4 nel desktop. Il replay deve funzionare dai soli file congelati.
