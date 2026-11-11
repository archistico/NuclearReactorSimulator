# Sintesi della validazione

M10 riunisce integrazione dell'impianto, operatività, replay deterministico e
confronto con riferimenti indipendenti. Questa pagina riassume le conclusioni;
[PROJECT.md](../PROJECT.md) mantiene il checkpoint e i comandi correnti.

## Funzionalità operative acquisite

La sala controllo integra informazioni, guida, diagnostica, allarmi, registro
incidenti e comandi contestuali. Assistenza e autorità di controllo sono distinte,
comprese proprietà richiesta ed effettiva durante la supervisione automatica.
Le sessioni supportano checkpoint, replay, salvataggio e ispezione dei controlli
con tempo logico deterministico. Strumenti, mimic e schemi collegano lo stato
ingegneristico a missioni, conseguenze e prestazioni dell'operatore.

La chiusura dei singoli fogli M10.1–M10.9 non equivale alla chiusura finale M10.
I test automatici non sostituiscono l'accettazione manuale HMI richiesta.

## Che cosa dimostrano le evidenze

| Ambito | Risultato acquisito | Limite della conclusione |
| --- | --- | --- |
| Operatività e replay | Flussi operatore, missioni, checkpoint e replay hanno regressioni dedicate. | Non è una nuova accettazione manuale. |
| Identità esatte | Exact-V9 resta la baseline produttiva; le identità storiche sono preservate. | Le opzioni fisiche successive non cambiano il significato degli archivi. |
| Termodinamica C4 | Implementazione R1 e verifica focalizzata R2 PASS per il mode-2 esplicito. | Non qualificano l'equilibrio integrato R3. |
| Ricerche di equilibrio | H8 porta il massimo accettato a 3.337.270,068, con 21 aggiornamenti e 16 passi accoppiati. | Nessuna radice; arresto al raggio minimo e punto fuori inviluppo. |
| Confine di fase | Trial 6 verifica le proposte vincolate; il margine energetico concorda con 120 osservazioni nei Trial 6–8. | Verifica locale, non equivalenza globale del resolver o nuovo vincolo già qualificato. |
| Previsione direzionale | Trial 8 misura la risposta reale a cinque frazioni di una direzione, ripetute identicamente. | Anche con fase compatibile l'errore del modello può superare il guadagno; serve una revisione delle derivate. |
| Bilanci dei nodi | Accumuli ricostruiti dai trasferimenti con errore energetico inferiore a 0,01 W. | Conservazione e stazionarietà sono condizioni distinte. |

## Il problema ancora aperto

La superficie H mantiene 24 inventari fluidi, box ±2%, 75 residui e criterio
massimo assoluto ≤1. Il miglior punto accettato H8 è ancora a circa 3,337 milioni
ed è fuori dall'inviluppo. Al primo Running accumula circa 428,1 kW all'ingresso
turbina e 1,169 MW in control-out. Questi accumuli coincidono con la differenza
fra energia entrante e uscente: non sono una perdita inspiegata di inventario.

H8 dimostra l'utilità di verificare la fase lungo l'intera ricetta. Arriva poi
al confine nel quale la qualità C4 è troncata a zero. Trial 7 non trova proposte
utili con il solo margine di volume; la successiva ricostruzione energetica
spiega il cambio di ramo nei campioni osservati. Il criterio energetico usa
la tolleranza già presente in C4, senza allentarla.

Trial 8 restringe il problema residuo: alla frazione 0,001 il massimo migliora
di 0,135, ma l'errore massimo della previsione sui 75 residui è 6,059. Alla
frazione 0,0001 il massimo peggiora, pur restando miscela. Prima di un'altra
ricerca occorre confrontare le derivate su più ampiezze, verificando insieme
residui, margine energetico e traiettoria. Questi esiti non provano impossibilità
fisica né un difetto indipendente dei controllori.

R3 Requalification 3, R4 e la chiusura M10 restano subordinate all'equilibrio e
alle rispettive condizioni d'ingresso. Nessuna nuova prova lunga è in corso.

## Controlli e riferimenti

L'ultima compilazione Release con avvisi come errori e la suite ordinaria hanno
dato PASS: **1.392 superati, 164 espliciti esclusi, zero fallimenti**. Precedono
i nuovi protocolli, che mantengono invariati i 962 sorgenti produttivi e i 410
file di test. Trial 6, H8, Trial 7 e Trial 8 hanno ciascuno superato tre test
preliminari, con uno esplicito escluso, prima dell'esecuzione fisica esplicita.

Il comando corrente è `python eng/review-r3-h-current-equilibrium.py`: riproduce
H8, i Trial 6–8 e la revisione energetica dalle evidenze congelate, senza chiamate
fisiche. La revisione energetica da sola si esegue con
`python eng/review-r3-h-energy-boundary1.py review`. Entrambi i controlli passano;
le baseline dei gate precedenti conservano il proprio significato storico.

Le [decisioni ingegneristiche](MODEL_DECISIONS.md) raccolgono i vincoli durevoli.
Gli [input esatti storici](../../eng/contracts/m10/README.md) e i protocolli in
`eng/contracts/` appartengono agli strumenti di verifica. Le evidenze complete
sono conservate sotto `eng/frozen-evidence/`, con tracce dei batch compresse;
il [registro storico](../history/README.md) conserva le migrazioni documentali.

I [limiti del modello](../KNOWN_MODEL_LIMITATIONS.md) definiscono il campo del
simulatore educativo. I controlli numerici non costituiscono una qualificazione
industriale o autorizzativa.
