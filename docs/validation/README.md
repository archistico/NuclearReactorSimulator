# Sintesi della validazione

M10 riunisce integrazione dell'impianto, operatività, replay deterministico e
confronto con riferimenti indipendenti. Il [checkpoint](../PROJECT.md#stato-corrente)
mantiene risultati e stato verificato; l'[handoff](../PROJECT.md#handoff) contiene
i comandi di ripresa e la [roadmap](../ROADMAP.md) definisce il percorso futuro.

## Che cosa dimostrano le verifiche

| Ambito | Evidenza acquisita | Limite della conclusione |
| --- | --- | --- |
| Operatività e replay | Regressioni dedicate a flussi operatore, missioni, salvataggi, checkpoint e replay deterministico. | I test non sostituiscono l'accettazione manuale HMI applicabile. |
| Identità esatte | Baseline produttive e archivi conservano la propria versione. | Un esperimento non modifica automaticamente il desktop o il significato di un replay. |
| Termodinamica C4 | Implementazione R1 e verifiche locali R2 dell'opzione esplicita. | Non qualificano l'equilibrio integrato R3. |
| Trasporto di fase | Identità storiche, test locali e due misure ripetute con la legge riparata; adjudication 2 del bilancio con accumulo. | Tre checkpoint sani non dimostrano equilibrio o qualificazione R3. Il primo esito non conforme resta conservato. |
| Ricerche di equilibrio | Proposte fisiche, confronti delle derivate e ricerche con criteri e budget congelati. | Un residuo ridotto non equivale a una radice o a un seed qualificato. |
| Confine di fase | Osservazioni lungo tutta la ricetta e ricostruzione locale del margine energetico. | L'accordo sui campioni non prova equivalenza con l'intero resolver. |
| Bilanci | Accumuli ricostruiti da inventari e trasferimenti registrati. | Conservazione e stazionarietà sono condizioni distinte. |
| Diagnosi del punto riparato | Due ripetizioni con stato completo identico alla misura precedente; scambi termici e bilanci di exhaust ricostruiti. | Il trasporto netto di outlet è inferito nel pacchetto; il confronto sul raffreddamento mantiene fissi stato e ingresso. Nessuna sensibilità o ricerca eseguita. |
| Sensibilità accoppiate riparate | Prima sonda combinata verificata entro lo 0,037%; raffinamento di 70 ricette e misura estesa di 362, tutti ripetibili. Selezionate 19 colonne su 30. | Undici colonne restano non selezionate per fase, scale non ammesse o disaccordo fra ampiezze. Il rango numerico 19 è diagnostico; nessuna base completa o proposta di equilibrio. |
| Base direzionale | 394 ricette recuperano nove direzioni; altre 114 aggiungono due direzioni accoppiate. Rango numerico diagnostico 30. | Quattro coefficienti unilaterali; due derivate cartesiane non sono validate. La somma delle direzioni richiede verifica. |
| Prime proposte riparate | Sei ricette ripetibili, sane e nell'inviluppo; due passi combinati confrontati con il modello. | Entrambe le previsioni falliscono: errori 14,734 e 15,011 contro 0,10. Il PASS dell'evidenza conserva questo fallimento. |
| Limite di portata dello stadio | Minimo delle capacità e coppia ricostruiti offline entro 1e-9 kg/s e 1e-6 N·m; cambia il vincitore fra baseline e proposte. | Attribuzione del solo primo passo. Confine non incluso nei regimi precedenti; nessun nuovo difetto fisico o modello a tratti qualificato. |
| Capacità a tutti i checkpoint | 38 ricette ripetute, identiche alle origini; minimo delle capacità esatto in binary64 in 57 ricostruzioni. Proposte nel ramo admission ovunque. | Osservazione descrittiva: secanti delle capacità non validate, nessun modello o proposta nuova. |
| Modello del minimo | Errori delle proposte 1,78e-4 e 1,50e-4 contro 14,7 e 15,0; vincitori corretti nelle 16 sonde; controlli strutturali di coppia e inventario. | Retrospettivo e limitato alle otto direzioni attive; nessuna proposta nuova o ricerca. |
| Proposte prospettiche | Sei previsioni su tre rami congelate prima della misura: tutte conformi, errore massimo 4,7e-4 contro 0,10, vincitori corretti. Massimo pesato da 1,157 a 1,056. | Otto direzioni su 30; ramo control e sequenze miste non verificati; solo frazioni del passo. Nessuna radice o ricerca. |
| Capacità nelle altre 22 direzioni | 90 ricette ripetute, identiche alle origini; minimo esatto in 135 ricostruzioni. 17 direzioni non cambiano la portata dello stadio, cinque cambiano portata e vincitore. | Osservazione descrittiva: nessun coefficiente stimato; unilaterali osservate solo sul lato positivo; modello ancora su otto direzioni. |
| Modello sulle 30 direzioni | Ristima offline; proposte congelate delle prove 6 e 10 e sonde doppie unilaterali previste entro 2,4e-3. Otto previsioni nuove su quattro sequenze, 26 direzioni mosse insieme: tutte conformi, errore massimo 1,8e-3, vincitori corretti. | Un passo per sequenza, solo frazioni, entro le ampiezze campionate; unilaterali non verificate in combinazione. Ottimo previsto circa 0,955: nessuna radice o ricerca. |
| Limite del passo | Nelle ampiezze campionate il massimo previsto non scende sotto 0,9552: lo fissano energia di exhaust e coppia, mosse insieme dalla portata. Stesso limite in ogni ramo. | Oltre le ampiezze campionate (0,39 MW al 2%, 3,4% per annullare) è estrapolazione lineare, non previsione; leva misurata solo fino a 1e-4; lato condensatore non esaminato. |
| Leva di exhaust al percento | Prova 15: 22 ricette sane, nell'inviluppo e ripetibili fino a ±2%; secanti entro il 2,22% del riferimento. | A +2% la combinazione vale ancora +0,4116 MW. È una misura della sola coordinata, non impossibilità dell'intera famiglia. |
| Sonda di dominio di exhaust | Prova 16: 12 ricette sane, nell'inviluppo e ripetibili, solo energia positiva fino al 4%; cambio di segno fra +3,5% e +4%. | Quasi stazionarietà energetica del nodo al +3,5%, massa ancora in calo e squilibrio degli altri inventari. Il 4% è diagnostico, non un nuovo box o un equilibrio. |
| Replay delle evidenze | Richieste, risposte, selezioni, rifiuti e arresti ricostruiti dai pacchetti congelati. | PASS attesta la coerenza dell'esperimento, non la qualificazione fisica. |

## Come leggere l'indagine di equilibrio

La superficie H storica mantiene 24 inventari fluidi, box ±2%, 75 residui e criterio
massimo assoluto ≤1. I confronti separano la correttezza del programma lineare,
la qualità della previsione e la risposta reale dell'impianto. Una derivata
non disponibile nel ramo viene dichiarata tale; un passo che cambia fase resta
un'osservazione fisica, con la sua classificazione conservata.

Le [evidenze correnti](../PROJECT.md#evidenze-e-verifiche) collegano specifiche,
risultati e ricevute. Il [blocco da risolvere](../PROJECT.md#il-problema-da-risolvere)
spiega gli accumuli ancora presenti e il limite della previsione affine.
La diagnosi attribuisce il limite del condensatore alla superficie UA e distingue
gli accumuli di massa ed energia. Le vecchie derivate non si riusano dopo la
riparazione del trasporto. Queste conclusioni non dimostrano
l'impossibilità di una radice né un difetto indipendente dei controllori.

Le nuove sonde mostrano perché osservare soltanto i fluidi può rendere quasi
collineari sorgente nucleare e temperature. Includere i tassi di energia dei
corpi termici distingue l'energia aggiunta da quella trasferita. Queste righe
diagnostiche non modificano i 75 residui o il criterio di radice originali.
Una ricetta sana ma fuori inviluppo non rende disponibile la colonna prevista;
un PASS dell'evidenza non promuove una colonna che manca il limite di linearità.

La disponibilità deve includere tutti i nodi: una sonda di drum può cambiare
la fase di suction soltanto al primo Running. Anche una coppia di scale
conforme resta locale: per l'energia di exhaust, il raffinamento trova un
accordo del 6,15% fra 3e-5 e 1e-4, mentre altre coppie falliscono. Tutti i
confronti e la regola di selezione restano conservati, senza reinterpretare
come conforme il fallimento della prima prova.

La legge della portata prende il minimo fra più capacità. Vicino a capacità
quasi coincidenti, due secanti centrali possono attraversare vincitori diversi:
anche se concordano fra scale, la loro somma può fallire. La ricostruzione del
primo passo delle proposte mostra questo caso. I vecchi controlli registrati
rimangono conformi nel proprio ambito; non si aggiunge retroattivamente un
nuovo rifiuto del ramo. Il modello che conserva il minimo riproduce ora quel
passo composto, ma soltanto in modo retrospettivo: la verifica prospettica
richiede proposte nuove.

La revisione offline conserva anche il fallimento del primo controllo sulle
frazioni esattamente zero: quelle residue dell'ottimizzatore producono ingressi
identici in binary64. La correzione dichiara il nuovo controllo di identità
effettiva e conserva codice, contratto e log originali, senza nuove simulazioni.

Una radice richiede ancora conferme indipendenti e una verifica integrata R3.
R4, R5, R6 e le prove finali mantengono i propri criteri di ingresso: la chiusura
dei singoli fogli M10.1–M10.9 non equivale alla chiusura finale M10.

## Riferimenti

Le [decisioni ingegneristiche](MODEL_DECISIONS.md) raccolgono i vincoli durevoli.
Gli [input storici esatti](../../eng/contracts/m10/README.md) sono separati dalle
guide leggibili. Le [evidenze congelate](../../eng/frozen-evidence/README.md)
conservano manifest e tracce; il [registro storico](../history/README.md)
documenta le migrazioni. I [limiti del modello](../KNOWN_MODEL_LIMITATIONS.md)
definiscono il campo educativo: queste verifiche non costituiscono una
qualificazione industriale o autorizzativa.
