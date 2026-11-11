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

La revisione offline e la prova fisica delle sei direzioni sono concluse. Tutte
le direzioni migliorano realmente il residuo. La successiva ricerca scalata H2
lo riduce ancora, fino a 4,907 milioni, poi si arresta senza una radice dopo
143 ricette. I 75 residui devono invece essere tutti entro la soglia 1: il
miglioramento numerico è reale, ma l'equilibrio non è ancora qualificato.

Il problema immediato è ora localizzato: tutti i tentativi terminali attraversano
il confine di fase dell'hotwell e peggiorano il risultato, mentre una piccola
sonda già registrata migliora senza cambiare fase. H continua a indicare una
correzione limitata ai 24 inventari fluidi; questo esito non richiede di per sé
nuove leggi fisiche o una taratura dei controllori.

| Passo | Lavoro previsto | Consegna verificabile | Condizione per avanzare |
| --- | --- | --- | --- |
| A. Revisione offline — conclusa | Riproduzione dei sei modelli e verifica della scalatura. | PASS, con caveat sui cambi di fase. | Ha consentito il protocollo Local Trial 1. |
| B. Verifica fisica — conclusa | Sette punti ripetuti due volte, 14 ricette. | Tutte le direzioni migliorano; migliore riduzione 4,39%, ripetizioni identiche. | Ha consentito la ricerca H2, senza qualificare un seed. |
| C. Ricerca H2 — conclusa, inconclusiva | Sottoproblemi scalati con vincoli espliciti e 143 ricette fisiche. | Un aggiornamento, residuo 4,907 milioni, replay completo PASS. | Arresto `NO-STRICT-DESCENT`; nessuna radice o conferma. |
| C2. Ricerca locale vicino al confine di fase — prossima | Confrontare le direzioni terminali con la sonda migliorativa; progettare un limite al passo e una riduzione progressiva più profonda. | Metodo verificato su casi noti e protocollo separato con selezione, budget e arresti congelati; poi indagine limitata ai test. | Stesse incognite, scale e limiti; una radice osservata richiede conferme indipendenti, un nuovo arresto richiede revisione dei bilanci. |
| D. Conferma e integrazione del candidato | Dopo una radice e revisione indipendente, pianificare conferme ripetute e integrare il solo stato iniziale qualificato nella composizione di prova. | Riproduzione dello stato, residui conformi e inviluppo invariato di 100 passi. | Nessuna modifica implicita di controllori, guadagni, proprietà o identità storiche. |
| E. R3 Requalification 3 | Eseguire la verifica integrata di 120 secondi, cioè 12.000 passi da 10 ms. | Evidenze complete e giudizio indipendente sull'esito R3. | Solo un R3 PASS restituito e revisionato permette la pianificazione di R4. |

La figura rappresenta questi passaggi e il punto in cui ci si ferma se l'indagine
non produce un candidato. Le frecce sono dipendenze del lavoro, non automatismi
che autorizzano un'altra ricerca quando quella precedente è inconclusiva.

```mermaid
flowchart TD
    A["Revisione e prova delle sei direzioni: concluse"] --> B["H2: discesa reale, nessuna radice"]
    B --> C["Prossimo: protocollo locale vicino al cambio di fase"]
    C --> D{"Radice osservata?"}
    D -->|"no"| S["Conservare evidenze e rivedere il piano"]
    D -->|"sì"| E["Revisione e conferme indipendenti di 100 passi"]
    E --> F["R3: verifica integrata di 120 secondi"]
    F --> G["Revisione dell'esito prima di R4"]
```

Il miglioramento misurato consente di superare il vecchio dubbio sullo smorzamento,
ma non qualifica l'impianto. Per il passo C2 occorre ora controllare l'ampiezza
del passo vicino alla saturazione: il migliore dei tentativi terminali H2 porta
la pressione grezza dell'hotwell da circa 10,81 a 22,82 kPa. Una sonda più piccola
raggiunge invece un residuo di 4,481 milioni senza cambiare fase. Questo confronto
motiva il nuovo disegno numerico, senza dimostrare che una radice esista nel box.

Restano fissi residui, scale, incognite e limiti del percorso H finché un distinto
piano verificato non motivi diversamente. L'esaurimento del budget non autorizza
un ampliamento dei limiti, una taratura dei controllori o il passaggio automatico
alle altre famiglie di intervento. L'esito inconclusivo è un punto di decisione.

**A, B e C sono consegne completate; C2 è il prossimo lavoro.** D ed E dipendono
da ciò che emerge: non sono date per acquisite e non hanno ancora una data di
completamento attendibile. Le due nuove indagini hanno evidenze congelate e non
sono esecuzioni ancora in corso.

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
