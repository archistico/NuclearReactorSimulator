# Trial 9 — derivate su più ampiezze al punto finale H8

La prova diagnostica parte esattamente dal punto finale H8 e usa l'esecutore
di batch già verificato. Non ottimizza, non installa seed e non modifica C#.
Restano 24 incognite, box frazionario ±2%, 75 residui, scale e criterio di
radice ≤1. La baseline deve riprodurre anche gli hash di stato H8.

Si provano sei ampiezze prefissate: 1e-9, 1e-8, 1e-7, 1e-6, 1e-5, 1e-4.
Per ogni coordinata si eseguono entrambi i segni ammessi dal box; nessun
punto è tagliato sul limite. Ogni ricetta, compresa la baseline, viene
ripetuta due volte e deve restituire registrazioni identiche. Il massimo è
578 ricette in 13 batch, 1760 passi conservativi inclusa la preparazione.

Le colonne ammissibili richiedono una risposta sana e le stesse 48 etichette
di fase della baseline, nell'intera ricetta. Quando entrambi i lati sono
ammissibili si usa la differenza centrale; altrimenti quella unilaterale
disponibile. Nessuna colonna mancante diventa zero. Si conservano anche le
derivate unilaterali separate e si confrontano colonne e stencil tra ampiezze
adiacenti. Residui e margine energetico locale C4 usano gli stessi campioni;
il margine è espresso in J/kg e mantiene la tolleranza originale del resolver.

Ogni Jacobiano completo viene confrontato con tutti i cinque punti direzionali
di Trial 8, già congelati, senza rieseguirli. Si registrano errori sui 75 residui,
previsioni del massimo e margini energetici previsti/osservati. I punti che
cambiano fase restano visibili ma non dimostrano validità nel ramo locale.

Un criterio sufficiente, dichiarato prima delle nuove misure, richiede che
due ampiezze adiacenti risolvano entrambe le piccole variazioni direzionali
a frazione 0,001 e 0,0001: errore massimo dei residui non superiore a 0,25
volte il modulo della variazione osservata del massimo, con fasi compatibili.
È un criterio diagnostico di affidabilità della previsione, non una nuova
tolleranza fisica. Un esito negativo non esclude modelli validi su altre
direzioni o scale, né prova inesistenza di un equilibrio.

Si ricostruisce inoltre il difetto di convessità dei massimi direzionali.
La norma massima di un residuo affine è convessa: i due piccoli passi di
Trial 8 possono quindi imporre un limite inferiore all'errore del modello.
Il limite è calcolato per ciascun Jacobiano, includendo il termine dovuto
alla mancata collinearità esatta delle coordinate arrotondate. Non si assume
che frazioni nominali producano coordinate perfettamente collineari.

Fixture analitiche coprono derivata centrale quadratica, orientamento del
lato negativo, indisponibilità, duplicati e limite affine con arrotondamento.
Il replay verifica identità, richieste, budget, ripetizioni, bilanci, colonne,
previsioni, selezioni diagnostiche ed esito senza altre chiamate fisiche.
Il miglior punto osservato non è promosso a seed o punto accettato di ricerca.
Qualsiasi nuovo ottimizzatore richiede prima una prova ripetuta delle proposte.
