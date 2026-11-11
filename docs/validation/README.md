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
| Ricerche di equilibrio | Proposte fisiche, confronti delle derivate e ricerche con criteri e budget congelati. | Un residuo ridotto non equivale a una radice o a un seed qualificato. |
| Confine di fase | Osservazioni lungo tutta la ricetta e ricostruzione locale del margine energetico. | L'accordo sui campioni non prova equivalenza con l'intero resolver. |
| Bilanci | Accumuli ricostruiti da inventari e trasferimenti registrati. | Conservazione e stazionarietà sono condizioni distinte. |
| Replay delle evidenze | Richieste, risposte, selezioni, rifiuti e arresti ricostruiti dai pacchetti congelati. | PASS attesta la coerenza dell'esperimento, non la qualificazione fisica. |

## Come leggere l'indagine di equilibrio

La superficie H mantiene 24 inventari fluidi, box ±2%, 75 residui e criterio
massimo assoluto ≤1. I confronti separano la correttezza del programma lineare,
la qualità della previsione e la risposta reale dell'impianto. Una derivata
non disponibile nel ramo viene dichiarata tale; un passo che cambia fase resta
un'osservazione fisica, con la sua classificazione conservata.

Le [evidenze correnti](../PROJECT.md#evidenze-e-verifiche) collegano specifiche,
risultati e ricevute. Il [blocco da risolvere](../PROJECT.md#il-problema-da-risolvere)
spiega perché l'ultima ricerca si è fermata. Queste conclusioni non dimostrano
l'impossibilità di una radice né un difetto indipendente dei controllori.

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
