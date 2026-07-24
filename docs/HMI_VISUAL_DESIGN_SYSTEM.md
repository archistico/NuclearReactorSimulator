# HMI Visual Design System

**Stato:** specifica grafica normativa  
**Ambito:** shell desktop, schemi, pannelli operatore, strumenti, allarmi, trend e workstation  
**Tecnologia corrente:** Avalonia desktop  

## 1. Scopo

Questo documento è il contratto visivo della control room di Nuclear Reactor Simulator. Ogni nuova schermata e ogni modifica della UI deve rispettarlo, salvo deroga motivata e documentata.

L'interfaccia deve permettere all'operatore di rispondere rapidamente a quattro domande:

1. Dove mi trovo?
2. Qual è lo stato dell'impianto?
3. Quale percorso o dipendenza spiega lo stato?
4. Quale comando posso impartire e quale risposta devo osservare?

L'aspetto è futuristico-industriale, non decorativo. Gerarchia, contrasto e posizione comunicano importanza prima del colore.

## 2. Principi non negoziabili

- Sicurezza e allarmi restano visibili indipendentemente dal workspace selezionato.
- Lo schema è la superficie principale di comprensione del sottosistema.
- I comandi direttamente correlati allo schema sono il primo blocco immediatamente successivo.
- Dati misurati, dati di modello, setpoint e limiti di protezione restano semanticamente distinti.
- Il rosso è riservato a allarme, trip, SCRAM e protezione.
- Il colore di un fluido o di una forma di energia non comunica severità.
- Nessun testo operativo essenziale può richiedere un tooltip per essere letto.
- Nessun box può coprire un nodo, una freccia, una linea o un altro box.
- Una condizione non disponibile non viene sostituita da un valore plausibile o decorativo.
- Il testo esplicativo lungo appartiene al contesto, alla guida o alla diagnostica; non deve separare schema e comandi.

## 3. Architettura della schermata

La shell usa questa gerarchia verticale:

```text
┌──────────────────────────────────────────────────────────────┐
│ sessione / runtime / comandi globali                         │
├──────────────────────────────────────────────────────────────┤
│ situazione impianto e valori globali                         │
├──────────────────────────────────────────────────────────────┤
│ ZONA ALLARMI PERSISTENTE                                     │
├────────────┬─────────────────────────────┬───────────────────┤
│ sistemi    │ workspace                   │ ispettore         │
│            │ 1. schema                   │ contestuale       │
│            │ 2. comandi immediati        │                   │
│            │ 3. strumenti prioritari     │                   │
│            │ 4. dettagli/diagnostica     │                   │
└────────────┴─────────────────────────────┴───────────────────┘
```

Dimensioni guida alla finestra nominale 1440 × 900:

| Area | Dimensione |
|---|---:|
| Navigazione sistemi | 172 px |
| Ispettore contestuale | 260 px |
| Margine workspace | 18 px |
| Spazio tra sezioni principali | 18 px |
| Spazio interno pannello | 14–20 px |
| Altezza minima comando | 48 px |

La UI deve continuare a funzionare alla dimensione minima dichiarata. Se un gruppo di comandi non entra in una riga, si dispone su due righe; non si riduce il font sotto il minimo.

La finestra desktop viene avviata nello stato massimizzato standard del sistema operativo. L'utente può successivamente ripristinarla o ridimensionarla con i normali controlli della finestra.

Stato runtime, barra di avanzamento e logical-step progress non devono condividere lo stesso spazio di layout. Il blocco runtime nella barra superiore usa tre righe dedicate: stato, barra e step. Il contenitore ha larghezza fissa e clipping, quindi l'animazione della barra non può invadere il pulsante `Run`.

Lo step corrente viene mostrato una sola volta, nella barra superiore. Non va duplicato nella situation strip, nei riepiloghi di workspace o nella barra di stato dell'Operator Computer. Gli step associati a eventi, checkpoint e campioni storici restano visibili perché sono coordinate temporali dei rispettivi record, non copie del contatore corrente.

## 4. Colore

### 4.1 Superfici

| Token concettuale | Uso | Valore corrente |
|---|---|---|
| Canvas | sfondo applicazione | `#071119` |
| Shell | header e rail | `#0D171E` |
| Surface inset | strumenti e controlli | `#0D1820` |
| Surface raised | pannelli operativi | `#14232C` |
| Border | separatori ordinari | `#34505D` |
| Border strong | selezione e pannelli prioritari | `#4B7180` |

Le superfici differiscono principalmente per luminosità. Non usare gradienti decorativi o glow esteso.

### 4.2 Stati

| Stato | Colore | Significato |
|---|---|---|
| Information | ciano `#62D6E8` | dato, percorso, selezione |
| Normal | verde `#45D69A` | stato sano confermato |
| Warning | ambra `#F2C14E` | attenzione o limite in avvicinamento |
| Trip | rosso `#FF6268` | allarme, trip o protezione |
| Unavailable | grigio `#71808B` | qualità/autorità non disponibile |

Non colorare intere aree di verde durante il funzionamento normale. Il verde conferma uno stato locale; il fondo ordinario rimane scuro.

### 4.3 Mezzi e forme di energia

| Percorso | Colore |
|---|---|
| Refrigerante primario | ciano |
| Vapore | bianco ghiaccio |
| Condensato | blu |
| Acqua alimento | verde-ciano |
| Potenza meccanica/albero | ambra |
| Potenza elettrica | violetto |

L'ambra dell'albero non è un warning. La severità viene rappresentata da bordo/stato del componente e dalla zona allarmi.

## 5. Tipografia

### 5.1 Famiglie

- **Inter:** navigazione, titoli, etichette, messaggi, pulsanti e testo descrittivo.
- **Cascadia Mono / Consolas fallback:** numeri, sequenze, setpoint, tempi logici e dati che richiedono allineamento tabellare.

Non applicare un font monospaziato all'intera finestra.

### 5.2 Scala minima

| Ruolo | Dimensione |
|---|---:|
| Titolo workspace | 24–26 px |
| Titolo pannello | 18–22 px |
| Valore primario | 24–28 px |
| Testo interfaccia | 12–14 px |
| Etichetta strumento | 10.5–12 px |
| Stato/metadata | 9.5–11 px |

Sotto 9.5 px sono ammessi solo elementi non essenziali in grafici ad alta densità. Input, output, stato, unità, limiti e testi dei comandi non scendono mai sotto 9.5 px.

Maiuscole e letter spacing si usano per label brevi, non per paragrafi.

## 6. Zona allarmi

La zona allarmi è persistente e si trova sopra il workspace.

Ogni canale è rappresentato da una tessera simile a un pulsante:

- fondo scuro e bordo neutro in stato normale;
- fondo ambra o rosso quando la condizione è attiva;
- bordo di severità quando resta annunciato dopo il rientro;
- indicazione esplicita `ACTIVE`, `ACKNOWLEDGED`, `RETURNED` e `FIRST OUT`;
- click della tessera apre la pagina allarmi senza riconoscere o resettare automaticamente.

Se almeno una `ConditionActive` è vera, l'intera zona pulsa con periodo indicativo di 1,1 secondi. Il lampeggio deve essere percepibile ma non stroboscopico. L'acknowledge modifica la memoria dell'annunciatore, non la condizione fisica e non la protezione.

Il lampeggio modifica colore e luminosità, mai le dimensioni. Padding, altezza e spessore del bordo della zona allarmi restano costanti in tutte le fasi, per evitare spostamenti verticali ciclici dell'intera interfaccia.

Priorità visiva:

1. trip attivo;
2. warning attivo;
3. annunciato rientrato e non riconosciuto;
4. riconosciuto/latched;
5. normale.

## 7. Schemi di sottosistema

### 7.1 Struttura

Ogni schema contiene due zone:

1. **diagramma:** nodi, percorsi e frecce;
2. **LINES & SIGNALS:** legenda viva con nome connessione e valori pubblicati.

Le etichette delle connessioni non vengono sovrapposte ai varchi tra nodi. I varchi sono riservati alle linee e alle punte delle frecce.

### 7.2 Nodo

Ogni nodo mostra nell'ordine:

- glifo di tipo;
- nome componente;
- stato;
- valore primario;
- valore secondario;
- `IN ‹ ...` su una riga dedicata;
- `... › OUT` sulla riga immediatamente successiva.

Il bordo comunica lo stato semantico; il fondo rimane scuro. Titoli e valori essenziali devono restare leggibili senza zoom.

Tutti i testi del nodo hanno dimensioni tipografiche massime definite dal componente. Il contenuto non può aumentare autonomamente il font o uscire dal box: testo eccedente viene troncato con ellissi e il contenuto completo può essere esposto come approfondimento, senza modificare la geometria dello schema.

### 7.3 Linee

- linee di processo/energia: spessore circa 2,8 px;
- segnali di misura, controllo e feedback: spessore circa 1,45 px;
- override di protezione: spessore circa 4 px;
- ogni percorso termina con freccia direzionale;
- i punti intermedi sui segnali distinguono il signal flow dalla piping grammar.

La griglia di fondo può aiutare l'allineamento, ma deve avere contrasto molto basso.

### 7.4 Regole anti-collisione

- Nessuna label flottante nel diagramma se il varco disponibile è minore della sua larghezza.
- Una stessa riga dello schema contiene al massimo quattro nodi.
- Quando i componenti sono più di quattro, il flusso prosegue su una nuova riga con un percorso direzionale esplicito.
- Altezza del nodo derivata dal contenuto e mai inferiore al minimo leggibile.
- La fascia connessioni riserva righe in base al numero di percorsi.
- Aggiungere spazio verticale prima di ridurre font o comprimere nodi.
- Le coordinate applicative definiscono la topologia, non autorizzano sovrapposizioni di presentazione.

## 8. Pannelli di comando

Il pannello `IMMEDIATE OPERATOR CONTROLS` segue direttamente lo schema.

Un gruppo operativo contiene:

- selettore del target;
- stato attuale e permissive/blocco essenziale;
- pulsanti del comando normale;
- comando di protezione separato e visivamente più raro;
- feedback dell'ultimo comando.

Regole:

- altezza minima 48 px;
- area cliccabile estesa a tutta la faccia;
- bordo inferiore più forte per suggerire fisicità;
- riempimento solo per stato persistente realmente confermato o breve feedback di pressione;
- un comando momentaneo non resta illuminato;
- comandi mutuamente esclusivi mostrano chiaramente quello attivo;
- comandi non disponibili restano visibili ma disabilitati;
- SCRAM/TRIP non va accostato a comandi ordinari senza separazione o label esplicita.

## 9. Strumenti e grafici

### 9.1 Scelta del componente

| Informazione | Componente |
|---|---|
| valore puntuale | numeric indicator |
| valore rispetto a banda/limiti | linear gauge |
| posizione angolare utile alla lettura | circular gauge |
| andamento recente | trend line/sparkline |
| confronto tra più grandezze | multi-series trend |
| distribuzione spaziale | map/heatmap |
| bilancio entrate/uscite | flow or Sankey-style diagnostic, solo se quantitativamente corretto |

Un grafico viene aggiunto solo se riduce il tempo necessario a riconoscere una relazione. Non creare gauge circolari per ogni numero.

### 9.2 Semantica

Range strumento, banda normale, target, warning, alarm e protection limit sono livelli distinti. Il fatto che il valore sia dentro scala non implica normalità.

Trend e animazioni dipendono dal logical step pubblicato, non dal frame rate.

## 10. Movimento

Movimento ammesso:

- lampeggio zona allarmi;
- breve feedback di pressione comando;
- direzione di flusso, se basata su stato pubblicato;
- trend e cambi di selezione.

Movimento vietato:

- glow o scanning line decorativi;
- pulsazioni permanenti di pannelli normali;
- animazioni che suggeriscono una portata non disponibile;
- transizioni che ritardano la lettura di un trip.

## 11. Accessibilità e leggibilità

- Il colore non è mai l'unico portatore di stato: aggiungere testo, forma o spessore.
- Usare contrasto elevato per valori e comandi.
- Tooltip come approfondimento, non sostituto di label essenziali.
- Target cliccabili almeno 44 × 44 px.
- Evitare paragrafi tutti maiuscoli.
- Verificare la UI alla dimensione minima, nominale e su scaling Windows 125%/150%.
- Il lampeggio non deve superare 2 Hz.

## 12. Governance dei componenti

Prima di introdurre un nuovo stile locale, verificare se può diventare:

- token in `ControlRoomPalette`;
- regola tipografica in `ControlRoomTypography`;
- controllo riutilizzabile in `Controls`;
- classe di stile della shell.

I colori semantici non vanno duplicati come literal se esiste già un token. Un nuovo componente deve consumare snapshot di presentazione e non introdurre logica fisica, di protezione o di controllo.

## 13. Checklist di accettazione

Una schermata è pronta solo se:

- [ ] allarmi e trip restano visibili;
- [ ] schema e comandi principali sono visibili nello stesso percorso verticale;
- [ ] nessuna label copre nodi, frecce o valori;
- [ ] input e output sono espliciti e disposti su righe separate;
- [ ] nessuna riga dello schema contiene più di quattro nodi;
- [ ] nessun testo esce dal box del componente;
- [ ] il testo operativo essenziale è almeno 9.5 px;
- [ ] measured/model/target/protection sono distinguibili;
- [ ] colori di processo e severità non sono confusi;
- [ ] stato attivo, disabilitato e momentaneo dei pulsanti è chiaro;
- [ ] non esistono duplicati operativi nello stesso workspace;
- [ ] la schermata funziona alla dimensione minima;
- [ ] build e test UI contract sono verdi;
- [ ] una validazione manuale copre normal, warning, trip, unavailable e first-out.

## 14. Evoluzioni proposte

Proposte da valutare in iterazioni successive:

1. **Modalità focus schema:** collasso temporaneo dell'ispettore per guadagnare larghezza, senza nascondere allarmi.
2. **Pannello conseguenza comando:** direct effect, permissive, cosa monitorare e risposta osservata.
3. **Mini-trend vicino ai gauge critici:** pressione drum, livello drum, potenza reattore, vuoto condensatore e output elettrico.
4. **Bilancio energia/massa di workspace:** entrate, accumulo e uscite con tolleranza, solo da dati canonici.
5. **Ricerca e filtro allarmi:** severity, active, unacknowledged, returned e first-out.
6. **Densità operatore/analisi:** stessa gerarchia e semantica, diversa quantità di diagnostica.
7. **Test visuali automatizzati:** screenshot alle dimensioni nominale/minima per prevenire regressioni di sovrapposizione.
