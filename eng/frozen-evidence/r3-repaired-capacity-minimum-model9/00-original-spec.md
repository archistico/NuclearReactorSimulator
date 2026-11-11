# Modello offline del minimo delle capacità di portata

La prova 6 ha fallito le due previsioni affini (errori relativi 14,734 e
15,011 contro 0,10). L'attribuzione 7 e l'osservazione 8 hanno mostrato che la
portata dello stadio è il minimo di cinque capacità, che la base si trova sul
loro pareggio e che le proposte restano nel ramo admission a tutti i
checkpoint. Questa revisione dichiara un modello che conserva il minimo e lo
confronta, **senza nuove chiamate fisiche**, con le due proposte della prova 6
e con le sonde già congelate. Sorgenti, test, criteri e risultati precedenti
restano invariati.

## Dati

Si usano i 38 record congelati dell'osservazione 8, cioè le richieste della
prova 6 e le due sonde centrali di ciascuna delle otto direzioni attive del suo
passo, con le capacità osservate ai tre checkpoint. Le ripetizioni sono già
verificate identiche in quella prova; qui si usa la prima. Tutti i 18 punti
diversi dalla base devono restare disponibili, senza cambi di fase o di regime.
Le direzioni attive sono quelle con coefficiente oltre 1e-13; si registra il
massimo coefficiente trascurato. Nessuna delle quattro direzioni unilaterali è
attiva: il loro vincolo di segno resta quindi fuori dal confronto e intatto.

## Modello

Per ogni checkpoint k, ciascuna capacità è lineare nelle direzioni attive e
nelle portate comandate dello stadio ai checkpoint precedenti. La portata al
checkpoint k è il minimo delle capacità linearizzate, valutate in ordine dal
primo passo. Ognuna delle 81 righe è lineare nelle direzioni e nelle portate
fino al proprio checkpoint. La risposta di una riga dipende quindi dal
vincitore attraverso la derivata della capacità che vince: la secante centrale
non viene mai usata come derivata della portata quando attraversa vincitori
diversi.

I coefficienti di portata si stimano ai minimi quadrati, con QR di Householder,
dalle somme `più + meno − 2·base` delle sonde, che contengono il salto del
minimo. I coefficienti di direzione si ottengono dalla differenza
antisimmetrica delle sonde dopo aver tolto i termini di portata. Le proposte
non entrano nella stima. L'identificabilità richiede che il rapporto fra il
minimo e il massimo valore singolare delle somme di portata sia almeno 1e-3 per
ciascun sottoinsieme causale. Una fixture sintetica verifica che la
scomposizione ricostruisca esattamente un passo composto attraverso un minimo,
mentre la somma delle secanti centrali fallisce.

## Confronti

- **Proposte 0,25 e 0,50**: errore relativo sulle 81 righe con lo stesso
  denominatore e la stessa soglia 0,10 della prova 6, accordo del vincitore a
  tutti i checkpoint e guardie disponibili. Si riportano anche gli errori affini
  originali, senza riscriverli, e il rapporto di cancellazione fra la norma
  della variazione prevista e la somma delle norme dei contributi.
- **Sonde**: vincitori previsti e osservati, errore di portata ed errore relativo
  delle 81 righe per tutte le 16 sonde.
- **Esclusione di una direzione**: i coefficienti di portata si ristimano
  senza ciascuna direzione e si ripete la previsione delle due proposte.
- **Controlli strutturali**: il coefficiente fra coppia e portata dello stesso
  checkpoint deve coincidere entro 1e-3 relativo con
  `qualità × lavoro ideale × efficienza / ω` registrati alla base; la capacità di
  inventario deve rispondere alle portate precedenti con −0,5 entro 0,01,
  come impone la legge di metà inventario per passo.

## Limiti dichiarati

La forma del modello è stata provata in un'esplorazione preliminare non
congelata sugli stessi record prima di questo contratto: il confronto con le
proposte è **retrospettivo** e non costituisce una previsione prospettica.
Sono modellate soltanto le otto direzioni attive; le altre 22 non hanno
capacità osservate. Le somme di portata dei tre checkpoint sono quasi
collineari: i singoli coefficienti incrociati non sono affermazioni fisiche.
I termini lisci del secondo ordine sono trascurati. Una previsione conforme non
è una radice, una ricerca o una qualificazione di R3.

`python eng/review-r3-repaired-capacity-minimum-model9.py execute` scrive e
congela l'analisi; `review` la ricostruisce dagli stessi input congelati.
Zero passi runtime. R3 e M10 restano aperti.
