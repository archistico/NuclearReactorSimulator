# Roadmap del progetto

Questa pagina spiega la direzione del lavoro e le condizioni per avanzare. Lo stato
verificato, le evidenze e i comandi correnti sono mantenuti in [PROJECT.md](PROJECT.md).
I piani di dettaglio restano riferimenti per i rispettivi ambiti; le loro vecchie
diciture «current» o «next» non sostituiscono il checkpoint attuale.

## 1. Quale obiettivo stiamo perseguendo

Il simulatore dispone già di una sala controllo, regolazione e protezioni, missioni,
salvataggi e replay. Il desktop ordinario usa la propria configurazione Exact-V9.
Il lavoro fisico immediato riguarda invece l'integrazione di una chiusura
termodinamica più coerente con il riferimento indipendente acqua-vapore.

La «chiusura termodinamica» è il calcolo che ricava pressione, temperatura e fase
del fluido da massa, energia e volume. Se quel calcolo cambia, il vecchio insieme
di inventari può non produrre più pressioni e portate compatibili fra loro.

La nuova chiusura è implementata come opzione esplicita e ha superato le verifiche
locali. Quando la si collega all'impianto completo, però, non basta sostituire una
funzione: occorre che massa, energia, pressioni, portate e controlli descrivano un
punto iniziale compatibile. È questo il problema ancora aperto in R3, la verifica
integrata breve. Giri e potenza elettrica quasi costanti non dimostrano che tutti
gli inventari siano in equilibrio.

La pulizia della documentazione e la riscrittura del
[manuale italiano](usermanual/MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md) sono
concluse. Migliorano comprensione e manutenzione; non costituiscono una nuova
qualificazione della fisica. Il [riepilogo della validazione](validation/README.md)
separa i risultati già acquisiti dal problema di integrazione ancora aperto.

## 2. La prossima finestra di lavoro

NRS-MARKER:R3-H-BOUNDED-SCALED-LOCAL-MODEL-DESIGN1-NEXT

La ricerca H8 riduce il miglior massimo accettato a 3.337.270,068, contro il
criterio 1, e accetta 16 passi accoppiati. Il vincolo sulla traiettoria di fase
ha quindi prodotto un miglioramento reale. La ricerca arriva però a qualità
riportata zero nell'hotwell e si arresta al raggio minimo: nessun equilibrio
o inviluppo operativo è ancora qualificato.

La diagnosi successiva distingue volume ed energia al confine C4. Il margine
energetico ricostruisce le fasi dei campioni esaminati; Trial 8 mostra però
che, anche restando miscela, l'errore della previsione può superare il piccolo
guadagno osservato. Il passo immediato è verificare l'affidabilità delle
derivate prima di progettare un'altra ricerca.

| Passo | Stato e consegna | Condizione per avanzare |
| --- | --- | --- |
| Ricerche H–H8 e prove ripetute | Concluse senza radice; replay delle evidenze correnti PASS. | La riduzione del residuo non promuove un seed. |
| Diagnosi della traiettoria e del confine C4 | Conclusa sui campioni dei Trial 6–8: qualità troncata e margine di volume da soli non bastano. | Il margine energetico è una spiegazione locale, non un vincolo di ricerca già qualificato. |
| Derivate su più ampiezze — prossima | Confrontare residui, margine energetico e fasi da H8, con differenze centrali/unilaterali e direzione fissa di Trial 8. | Individuare passi per cui l'errore del modello consenta di giudicare la discesa; altrimenti rivedere la rappresentazione. |
| Nuova prova e ricerca | Solo dopo la revisione delle derivate, congelare proposte e confrontarle due volte con la risposta reale. | Accordo misurato, ramo e discesa coerenti prima di impegnare una nuova ricerca. |
| Conferma e integrazione | Dopo una radice osservata e revisione, conferme indipendenti e composizione di prova. | Residui conformi e inviluppo invariato di 100 passi. |
| R3 Requalification 3 | Verifica integrata di 120 secondi, 12.000 passi da 10 ms. | Solo un R3 PASS restituito e revisionato permette la pianificazione di R4. |

```mermaid
flowchart TD
    A["H8 e Trial 6-8: miglioramento, nessuna radice"] --> B["Prossimo: affidabilità delle derivate"]
    B --> C{"Previsioni verificate su più passi?"}
    C -->|"no"| R["Rivedere la rappresentazione locale"]
    R --> B
    C -->|"sì"| D["Nuova prova ripetuta prima della ricerca"]
    D --> E{"Radice osservata?"}
    E -->|"no"| R
    E -->|"sì"| F["Conferme indipendenti di 100 passi"]
    F --> G["R3: verifica integrata di 120 secondi"]
    G --> H["Revisione prima di R4"]
```

Le frecce indicano condizioni da dimostrare. La diagnosi del confine è progredita,
ma un certificato del programma lineare non garantisce l'accuratezza della sua
previsione fisica. Ridurre ancora il passo o aggiungere soltanto il margine
energetico non basta a superare il limite osservato in Trial 8.

Restano fissi residui, scale, incognite e box H finché una decisione distinta
non motivi diversamente. Le altre famiglie C/J, gli allargamenti dei limiti e
le modifiche fisiche non sono conseguenze automatiche di un esito inconclusivo.
Non è stata dimostrata l'impossibilità di una radice né un difetto dei controllori.
Nessuna ricerca è in corso e non c'è ancora una data attendibile per R3 o M10.

## 3. Che cosa manca dopo R3 per chiudere M10

R3 non è la chiusura finale del progetto né della campagna M10. La catena seguente
mantiene distinti i controlli con scopi diversi, così «abbiamo passato un test»
non diventa «abbiamo qualificato tutto».

| Fase | Domanda alla quale deve rispondere | Uscita necessaria |
| --- | --- | --- |
| R4 | La nuova composizione regge il ricontrollo lungo di materialità equivalente a P1B? | Confronto lungo completo e revisionato. Non è ancora la campagna finale di chiusura. |
| R5 | Il candidato soddisfa il rientro nella valutazione fisica acqua-vapore VR2? | Risultato non bloccante nel dominio e nei criteri dichiarati. |
| R6 | È giustificato attivare una nuova versione produttiva? | Decisione esplicita; Exact-V9 e vecchi archivi mantengono il proprio significato. |
| Valutazioni rimanenti | Quali conclusioni sono sostenute dai riferimenti di xeno, calore di decadimento e dalla valutazione consolidata? | Benchmark e applicabilità espliciti; non introduzione automatica di nuovi modelli nel desktop. |
| Decisione integrata e P4 | Restano interventi necessari e il ciclo sostiene la manovra breve 5→10→5? | Decisione motivata e verifica breve qualificata, prima di una nuova campagna lunga. |
| P5 | Il candidato supera la campagna lunga finale sul carico congelato? | Baseline della prova, artefatti completi e risultati conformi. |
| P6 | Tutte le evidenze richieste consentono di chiudere M10? | Chiusura esplicita, matrice di verifica aggiornata e accettazione HMI applicabile. |

Questa sequenza deriva dal
[percorso di riparazione e qualifica](../eng/contracts/m10/M10_FINAL_VR2_ENGINEERING_REPAIR_PLANNING1.md)
e dal [piano di chiusura](../eng/contracts/m10/M10_FINAL_CLOSURE_AND_M11_BOOTSTRAP_PLAN.md).
Ogni fase richiede il proprio piano aggiornato al risultato precedente; i documenti
storici conservano la sequenza e i vincoli, non una nuova autorizzazione di esecuzione.

## 4. La direzione successiva: M11–M15

L'ordine di fondo resta quello già pianificato. Cambia la natura della consegna:
prima un prodotto distribuibile, poi basi fisiche più estese, quindi nuove superfici
operative e modelli spaziali e di conseguenza.

```mermaid
flowchart TD
    M10["M10: qualificare e chiudere il ciclo attuale"] --> M11["M11: rendere il prodotto distribuibile"]
    M11 --> M12["M12: consolidare fisica ed estremi operativi"]
    M12 --> M13["M13: migliorare l'esperienza di sala controllo"]
    M13 --> M14["M14: rendere il nocciolo multizona"]
    M14 --> M15["M15: modellare conseguenze persistenti"]
```

Le frecce indicano prerequisiti di implementazione. Possiamo chiarire prima una
scelta o migliorare una guida, ma questo non promuove una milestone successiva.
La parte di danno richiede in particolare sia basi fisiche adeguate sia uno stato
spaziale che sostenga le conseguenze locali rappresentate.

| Milestone | Risultato concreto da ottenere | Prime attività e criterio di uscita |
| --- | --- | --- |
| [M11 — Preparazione della release](milestones/M11.md) | Un pacchetto riproducibile, supportato e utilizzabile su una macchina pulita. | Decidere piattaforme e formato; verificare vecchie sessioni, prestazioni, memoria, avvio e salvataggi. Chiusura con prove sul pacchetto effettivamente distribuito. M11 resta senza nuove funzioni di prodotto. |
| [M12 — Fondamenti fisici](milestones/M12.md) | Equilibri e limiti operativi classificati, flussi e bilanci più completi, basi esplicite del raffreddamento dopo uno scatto. | Partire dall'ispezione dell'equilibrio; poi direzionalità, condizioni estreme, energia delle pompe, calore residuo e integrità. Nessun correttore nascosto dello stato. |
| [M13 — Sala controllo](milestones/M13.md) | Comandi e procedure più chiari, accesso agli esercizi e modalità istruttore distinguibile. | Bersagli stabili, selettori, mimic navigabile, procedure, scelta di scenari/missioni e guasti supportati. Verificare anche qualità dei segnali e trasparenza dell'automazione. |
| [M14 — Reattore spaziale](milestones/M14.md) | Un nocciolo educativo con più zone realmente appartenenti al modello. | Definire zone, gruppi di canali e barre; poi mappa 2D e approfondimenti. Chiusura su coerenza delle somme, determinismo, replay e costo di calcolo. |
| [M15 — Evoluzione degli incidenti](milestones/M15.md) | Degrado, perdite e danni con cause fisiche e memoria persistente. | Introdurre un meccanismo alla volta con bilanci e transizioni verificabili. Un allarme o il nome di uno scenario non creano da soli un danno. |

M12.0, l'ispezione generale dell'equilibrio, resta lavoro successivo a M11: la ricerca
H attuale non diventa per estensione un correttore automatico utilizzabile durante
qualsiasi simulazione. Riutilizzeremo soltanto strumenti e risultati che abbiano
superato una verifica adatta al nuovo scopo.

## 5. Ulteriori passi che rendono il percorso più utile

Tre esigenze emerse dalla revisione del manuale vengono rese esplicite nei piani,
senza anticipare l'implementazione delle milestone successive.

**Accesso agli esercizi.** In M13.8 pianifichiamo un ingresso grafico agli scenari e
alle missioni supportati: descrizione dell'obiettivo, condizioni iniziali, scelta
esplicita e passaggio controllato alla nuova sessione. La normale scelta di un
esercizio rimane distinta dall'autorità dell'istruttore che inietta guasti.

**Documentazione verificabile.** In M11.5 colleghiamo manuale, etichette, incrementi
dei comandi e identità produttive alla release effettiva. I vecchi controlli
continuano a leggere i propri input storici; la guida attuale viene verificata
per il comportamento attuale e per i collegamenti ai contenuti supportati.

**Dimostrazioni ripetibili.** Prepariamo pochi percorsi rappresentativi per
accettazione e didattica: lettura del punto iniziale, manovra di carico nel dominio
qualificato, arresto normale, lettura di un intervento e ripristino di una sessione.
Saranno verificati sulla versione e sul pacchetto scelti; non daremo per sostenibile
una manovra soltanto perché è descritta dal manuale o richiesta da una missione.

## 6. Come tenere il piano leggibile

Per ciascun intervento registriamo domanda, ingresso, consegna, prova di uscita ed
esito se la prova non riesce. Aggiorniamo il checkpoint e questa roadmap, mantenendo
i dettagli eseguibili con i contratti di ingegneria. Non serve una nuova catena di
documenti di stato per ogni tentativo.

Non fissiamo ora date per i rami ancora dipendenti dalla ricerca di equilibrio.
Le priorità e le condizioni di avanzamento sono definite; il calendario potrà
essere stimato quando il percorso fisico sarà sufficientemente prevedibile.
