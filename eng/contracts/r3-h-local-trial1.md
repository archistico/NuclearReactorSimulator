# H: verifica non lineare delle sei direzioni locali

Questa prova chiude i passi A e B della roadmap e misura il passo C senza
confondere una previsione con un equilibrio. La revisione offline dell'11 novembre
2026 ha riprodotto la ricerca originale e i sei modelli con esito PASS. La
scalatura rende effettivo lo smorzamento su tutte le 24 colonne; tuttavia tre
sonde del Jacobiano cambiano fase nell'hotwell. Nessuna direzione è quindi
selezionata come candidato fisico sulla sola previsione.

Si riproduce l'evento accettato 401, quindi si valutano, nell'ordine originario,
le sei direzioni già congelate con damping 0.001, 0.01, 0.1, 1, 10 e 100.
Ogni ricetta è ripetuta due volte, a partire dagli stessi inventari grezzi.
Il budget è esattamente 14 tentativi di ricetta, compresi eventuali rifiuti.
Una sola costruzione dell'harness richiede inoltre due passi di preparazione:
al massimo 44 passi fisici, di cui 42 nelle ricette (tre per ricetta).
Non si effettuano iterazioni aggiuntive, derivate, line search o conferme lunghe.

Restano invariati i 24 inventari fluidi, il box frazionario ±0.02, i 75 residui
e le loro scale, la soglia di radice 1, i controlli, i guadagni e la modalità
termodinamica. Si usa la frazione globale già calcolata; nessun clipping.
La riproduzione del residuo e delle 48 fasi dell'evento 401 è un prerequisito:
una differenza interrompe la prova con errore tecnico. Lo stesso vale per una
divergenza tra ripetizioni o per eccezioni inattese. I rifiuti di dominio noti
sono registrati e consumano budget; non provocano nuovi tentativi.

Per ogni ricetta valida si conservano inventari, tre osservazioni, fasi,
sentinelle di salute e inviluppo e hash dello stato esplicito. L'adjudicator
ricalcola i residui da differenze di massa ed energia e coppia del rotore,
confronta previsione e risposta e identifica ogni cambiamento di fase.
Una direzione è promettente solo se entrambe le ripetizioni sono sane e il
massimo residuo reale diminuisce strettamente. L'inviluppo viene riportato
separatamente e resta obbligatorio per un'eventuale qualificazione successiva.

Un rapporto di riduzione reale/prevista inferiore a 0.1 segnala un modello
locale inaffidabile per quella direzione; nessuna discesa implica che questo
modello non giustifica un'altra ricerca. Anche una buona discesa autorizza
soltanto a progettare una ricerca separata con budget e arresti espliciti.
Questa prova non installa seed e non dichiara H, R3 o M10 qualificati.

Il contratto JSON autentica i sorgenti originali e i 406 file di test in ingresso,
ammettendo esclusivamente il nuovo file di test elencato. I vecchi contratti
conservano la loro baseline: dopo questa aggiunta il comando corrente è
`scripts/run-r3-h-local-trial1.cmd`. Prima delle chiamate fisiche, la revisione
lega contratto, input derivati e assembly Release ai rispettivi SHA-256 e
verifica il risultato dei test ordinari di questo protocollo.
