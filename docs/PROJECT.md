# Progetto — stato corrente e punto di ripresa

## Lettura del checkpoint

Il prodotto esistente usa il desktop Exact-V9, con sala controllo, missioni,
salvataggi e replay. L'attività fisica aperta riguarda l'integrazione della nuova
chiusura termodinamica reference-consistent, disponibile come opzione esplicita.
Implementazione e verifica locale sono acquisite; l'equilibrio dell'impianto
completo con quella composizione non è ancora qualificato.

La [roadmap](ROADMAP.md) descrive le prossime consegne e il percorso fino a M15.
Questa pagina conserva invece gli esiti verificati e il punto dal quale riprendere.

## Checkpoint di esecuzione — 11 novembre 2026

La revisione offline è conclusa e sono state eseguite due nuove indagini fisiche
limitate ai test. La ricerca scalata migliora il residuo, ma non trova ancora
un equilibrio qualificato. **M10 rimane aperto e R3 resta RED.**

NRS-MARKER:R3-H-LOCAL-MODEL-DESIGN1-CURRENT

NRS-MARKER:R3-H-NUMERICAL-STOP-REVIEW1-CURRENT

### Che cosa è acquisito

La ricerca H sugli inventari conservati è stata eseguita con i limiti e i criteri
congelati: 24 incognite fluide, 75 residui, 527 tentativi contabilizzati e sette
aggiornamenti accettati. L'esito è `H-INCONCLUSIVE-NO-CANDIDATE-IN-BOUNDS`.
Non è stato trovato uno stato iniziale qualificato e non è stata dimostrata
l'impossibilità fisica di trovarlo.

La **Numerical Stop Review 1** è `PASS-REVIEW-EVIDENCE-COMPLETE`. La ricostruzione
dell'ultimo modello lineare localizza i 78 rifiuti terminali prima del valutatore
fisico, sul limite invariato dell'energia di aspirazione. Le sei direzioni complete
registrate sono identiche e gli smorzamenti perdono effetto nelle matrici ricostruite.
Tre sonde di derivata cambiano etichetta di fase: la regolarità fisica locale resta
da accertare.

La **Local-Model Design 1** è `PASS-OFFLINE-DESIGN-EVIDENCE`. La scalatura delle
colonne e una sola frazione globale rispetto al limite producono sei direzioni
ammissibili, con tutte le 24 diagonali sensibili agli smorzamenti. Sei casi numerici
indipendenti verificano la trasformazione e il trattamento dei limiti.

### Che cosa significa il confronto offline

Le sei previsioni riducono il massimo residuo locale da circa 7,226 milioni a valori
fra 6,908 e 7,172 milioni. La **Local Trial 1** le ha ora confrontate con il
modello fisico: sette punti ripetuti due volte, per 14 ricette e 44 passi totali
compresa la preparazione. Tutte le ripetizioni coincidono; tutte le direzioni
producono una discesa reale. La migliore, con damping 0.01, raggiunge
6.909.148,735, cioè una riduzione del 4,39%. Il rapporto fra riduzione reale e
prevista è compreso fra 0,9978 e 1,0002. Nessun punto rispetta tutto l'inviluppo.

Gli input e gli output compatti sono conservati sotto
`eng/frozen-evidence/r3-h-numerics1` e `eng/frozen-evidence/r3-h-local-model1`.
La traccia completa H resta nel proprio archivio originale. Le nuove evidenze sono
in `eng/frozen-evidence/r3-h-local-trial1` e
`eng/frozen-evidence/r3-h-scaled-search2`, con tracce complete compresse e manifest
dei contenuti originali. I casi negativi rifiutano ampliamenti di autorità,
limiti, input e budget.

### Risultato della ricerca scalata H2

H2 risolve il sottoproblema numerico con vincoli espliciti, così una coordinata
quasi al bordo non impedisce alle altre di muoversi. Sette casi numerici verificano
il metodo, compreso il rilascio di un vincolo attivo. Restano invariati le 24
incognite, i 75 residui, le scale, il box ±2%, le leggi fisiche e i controllori.

La ricerca parte dal miglior punto della prova precedente ed esegue 143 ricette
in cinque batch: 439 passi fisici, di cui dieci preparatori. Accetta un aggiornamento
e porta il massimo residuo da 6.909.148,735 a **4.907.140,615**, un calo del 28,98%
in questa ricerca e del 32,09% rispetto all'ultimo stato accettato della ricerca H
originale. Poi si arresta con `NO-STRICT-DESCENT`. La ricostruzione indipendente
ricalcola residui, sonde, sottoproblemi, selezioni e arresto dall'intera traccia.

Il residuo dominante è ora la variazione di massa nell'hotwell; seguono la sua
energia e l'energia all'ingresso turbina. L'energia di aspirazione è ancora vicina
al limite inferiore. Il punto accettato è sano, ma la portata primaria è circa
98,67 kg/s, sotto l'inviluppo 99,9–100,1 kg/s. Non è una radice e non è stato
installato come stato iniziale.

Tutti i 24 tentativi dell'ultima iterazione attraversano il confine di fase
dell'hotwell e peggiorano l'obiettivo. Nel migliore di questi tentativi la pressione
grezza dell'hotwell passa da circa 10,81 a 22,82 kPa. Al contrario, una sonda già
registrata, di ampiezza frazionaria 1e-6 sulla massa del feedwater-inventory,
raggiunge 4.480.780,636 senza cambiare fase. Il protocollo non permette di accettare
automaticamente le sonde come aggiornamenti: questo dato localizza un possibile
progresso, non promuove un candidato. L'arresto delle quattro frazioni provate non
esaurisce i passi più piccoli e non dimostra impossibilità fisica.

La revisione corrente ricalcola anche le previsioni terminali: tutte e sei le
direzioni prevedono discesa, anche alle quattro frazioni effettivamente provate.
L'arresto non deriva quindi da una previsione già peggiorativa. Per damping 0.001,
ad esempio, il passo intero prevede circa 2,396 milioni, ma la risposta osservata
è molto peggiore. Le previsioni fino a 20 dimezzamenti restano calcoli offline:
non equivalgono a nuove prove fisiche di passi più piccoli.

### Prossima attività

Progettare un protocollo separato di ricerca locale che limiti l'ampiezza del
passo e provi una riduzione progressiva più profonda vicino al confine di fase.
La revisione deve confrontare le direzioni terminali con la sonda migliorativa
già disponibile, fissare prima dell'esecuzione selezione, budget e arresti e
verificare il metodo su casi noti con cambio di pendenza. Non basta aumentare
il numero di iterazioni della ricerca appena fermata.

L'obiettivo resta ottenere una radice e confermarla indipendentemente per 100
passi prima di pianificare R3 Requalification 3. Se un nuovo metodo locale non
funziona, si torna alla diagnosi dei bilanci e della superficie di stato; le famiglie
C/J richiedono una motivazione distinta. Non è stato dimostrato un difetto dei
controllori. R4 e la chiusura M10 mantengono le dipendenze descritte nella roadmap.

## Controlli correnti e riferimenti

```powershell
python .\eng\review-r3-h-scaled-search2.py
```

Questo è il controllo corrente: legge le evidenze congelate anche in un checkout
privo di `artifacts/`, verifica hash e manifest, riproduce la ricerca e la diagnosi
senza chiamare l'impianto. Controlla i 962 sorgenti e i 407 test preesistenti,
ammettendo solo l'aggiunta esplicita del test H2: 408 file di test complessivi.

I vecchi gate offline e Local Trial 1 mantengono le rispettive baseline storiche
406/407: non vanno rieseguiti come controlli del tree corrente. I runner delle due
nuove prove restano ricette di riproduzione, non il prossimo lavoro da avviare.
Il runner H2 conserva l'esecuzione e rifiuta di sovrascriverne la traccia.

Compilazione Release con avvisi trattati come errori: PASS. Suite ordinaria:
**1.387 superati, 162 espliciti esclusi, zero fallimenti**. Nessun sorgente di
produzione né test preesistente è stato modificato. I due nuovi test espliciti
sono stati eseguiti soltanto nelle rispettive indagini.

| Riferimento | Contenuto |
| --- | --- |
| [Ricerca H](../eng/contracts/m10/M10_FINAL_VR2_R3_H_CONSERVED_INVENTORY_FEASIBILITY1.md) | Protocollo ed evidenze della ricerca limitata già conclusa |
| [Revisione dello stop](../eng/contracts/m10/M10_FINAL_VR2_R3_H_NUMERICAL_STOP_REVIEW1.md) | Ricostruzione offline del limite numerico terminale |
| [Confronto locale scalato](../eng/contracts/m10/M10_FINAL_VR2_R3_H_LOCAL_MODEL_DESIGN1.md) | Sei smorzamenti, vincoli e casi numerici indipendenti |
| [Prova delle sei direzioni](../eng/contracts/r3-h-local-trial1.md) | Confronto fisico ripetuto fra previsione e risposta |
| [Ricerca scalata H2](../eng/contracts/r3-h-scaled-search2.md) | Metodo vincolato, budget, selezione e arresti congelati |
| [Sintesi della validazione](validation/README.md) | Risultati, campo delle conclusioni e problema aperto |

## Lavori di documentazione conclusi

La documentazione M10 è stata separata dagli input esatti dei validatori, raccolti
in `eng/contracts/m10`. Il README descrive il prodotto e il manuale italiano è
stato riscritto con spiegazioni dei componenti, delle interazioni e dei diagrammi.
I controlli testuali storici del manuale leggono la loro copia congelata.

Gli interventi di documentazione non cambiano fisica, test o identità produttive e non promuovono
R3 o M10. I checkpoint precedenti restano consultabili nel
[registro degli archivi](history/archives/README.md#snapshot-docs-project-md).
