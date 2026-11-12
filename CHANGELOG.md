# Changelog

La cronologia registra le modifiche e gli esiti al momento della loro verifica.
Lo [stato corrente](docs/PROJECT.md), l'[handoff](docs/PROJECT.md#handoff) e la
[roadmap](docs/ROADMAP.md) mantengono rispettivamente checkpoint, ripresa e passi futuri.

## 2026-11-12

### Misura della leva energetica e sonda del dominio di exhaust

- Eseguita la prova 15 prevista dall'handoff: 22 ricette sane, nell'inviluppo
  e ripetibili entro ±2%, con capacità e limiti del condensatore registrati.
  Secanti entro il 2,22% del riferimento; al +2% la combinazione energia-coppia
  resta positiva, circa 0,412 MW.
- Dichiarata prima dell'acquisizione la sonda 16, limitata alla sola energia
  positiva di exhaust fino al 4%, con tutte le altre variabili fisse. Console
  separata con fixture del dominio; nessuna modifica a sorgenti produttive o test.
- Dodici ricette sane, nell'inviluppo e ripetibili: cambio di segno della
  combinazione fra +3,5% e +4%. Al +3,5% l'accumulo energetico del nodo è circa
  4,14 kW, ma la massa scende ancora di 0,1005 kg/s al primo checkpoint.
- La perdita di outlet resta −1,157 MW; la somma degli accumuli energetici
  fluidi diventa circa −0,920 MW. Il risultato non è un equilibrio né un nuovo
  box di ricerca. Handoff verso bilanci accoppiati e verifica dell'intero ciclo.
- Compilazione senza avvisi o errori; fixture, preflight, bilanci e replay
  PASS. Evidenze congelate, baseline e punto +2% riprodotti esattamente.
  R3 resta RED e M10 aperto.

### Bilanci accoppiati di exhaust e previsione della potenza globale

- Eseguita la prova 17 su massa ed energia di exhaust: quattro ricette,
  arresto iniziale per cambio di fase di hotwell. Il rifiuto è conservato.
- Dichiarata prima della prova 18 una riserva iniziale fissa di circa 10 g
  in hotwell. Su 24 ricette ripetibili, due aggiornamenti bilanciano exhaust
  al primo checkpoint: −8,37e-10 kg/s e −0,00212 W. Al primo Running restano
  +2,56e-6 kg/s e +6,33 W; nessuna conferma di 100 passi.
- Prova 19: dichiarato un dominio diagnostico per popolazione neutronica e
  temperature accoppiate. Otto ricette verificano due previsioni nuove entro
  l'1% della variazione richiesta. Il punto intero, +2,855786% di neutroni,
  annulla la potenza netta globale iniziale; al primo Running restano +284 W.
- Ricostruiti indipendentemente i bilanci energetici globali entro 0,01 W.
  Il massimo dei 75 residui originali resta 8.365.513,134, dominato da hotwell:
  il risultato locale e globale iniziale non è un equilibrio integrato.
- Console diagnostiche compilate senza avvisi o errori, fixture e replay
  PASS; 36 ricette e 122 passi runtime complessivi. Alberi produttivi e test
  invariati, suite ordinaria non rieseguita. Evidenze e rifiuti congelati.
- Aggiornati checkpoint, roadmap, handoff e decisioni: prossima attività,
  rimisurare il modello e le capacità attorno al nuovo punto diagnostico,
  prima di dichiarare la ricerca integrata. R3 RED, M10 aperto.

## 2026-11-11

### Indagine sull'equilibrio integrato R3

- Consolidati i confronti fisici ripetuti e gli esiti delle ricerche H2–H9, con contratti,
  budget, identità degli input e replay delle evidenze congelate. L'esecutore
  di batch è riutilizzato dai nuovi protocolli; la produzione resta Exact-V9.
- Confrontati minimo dei quadrati e minimo del massimo residuo; separati i
  certificati numerici dalla validità delle previsioni fisiche. Le scelte
  numeriche mantengono invariati box, scale e criteri di equilibrio.
- Ricostruiti i cambi di fase dell'hotwell lungo i quattro checkpoint. H8 ha
  portato il massimo residuo accettato a 3.337.270,068; la successiva diagnosi
  distingue qualità troncata, margine di volume e criterio energetico C4.
- Verificato il margine energetico locale sui 120 campioni dei Trial 6–8.
  Trial 9 ha confrontato sei ampiezze con 554 ricette ripetute. Trial 10 ha
  verificato una proposta favorevole al raggio 1e-5, poi usata come ingresso H9.
- H9 ha eseguito 476 ricette e accettato due aggiornamenti, arrivando a
  3.334.923,856. L'arresto conserva la colonna indisponibile della massa di
  control-out: le sonde ammesse cambiano fase all'ultimo checkpoint.
- Ricostruiti gli accumuli di energia dai trasferimenti registrati. I replay
  passano, ma nessuna radice o nuovo seed è qualificato: R3 resta RED e M10 aperto.

Specifiche, esiti e comandi sono raggiungibili dalle
[evidenze del checkpoint](docs/PROJECT.md#evidenze-e-verifiche).

### Revisione di sufficienza della famiglia H

- Riletti in unità fisiche i residui del punto iniziale e dei finali H4–H9: il
  massimo scalato scende da 7,85 a 3,33 milioni mentre la somma assoluta dei
  tassi di energia sale da 2,69 a 12,87 MW e la coppia netta da −0,9 a −166 N·m.
- Attribuiti i due squilibri iniziali allo spostamento di energia specifica di
  C4 a parità di stato bersaglio, entro lo 0,07% dei valori osservati.
- Risolto il modello lineare 75×24 senza vincoli di box: passi fino al 4,6%,
  sei coordinate oltre ±2% e 300–460 kW residui sulla linea vapore.
- Esito offline PASS, replay identico su Windows e Linux. Nessuna radice,
  famiglia o allargamento del box è qualificato: R3 resta RED e M10 aperto.
  La prossima attività è la diagnostica di sensibilità ai punti operativi non fluidi.

### Diagnostica della valvola di regolazione (preparata)

- Aggiunto l'esecutore di test per i punti operativi non fluidi e reso
  parametrico l'harness su sei grandezze, con valori predefiniti invariati.
  `tests/` passa da 410 a 411 file; i replay H4–H9 restano legati al commit precedente.
- Congelati specifica, contratto e runner della diagnostica: 22 ricette, colonna
  primaria a 1e-4, tre modelli lineari e soglie di classificazione dichiarate.
  Revisione statica PASS; compilazione, suite ordinaria ed esecuzione sono da fare.

### Diagnostica della valvola di regolazione (eseguita)

- CI ordinaria PASS con i 411 file di test: 1.560 test, 1.395 superati,
  165 espliciti esclusi, zero fallimenti; baseline delle evidenze correnti PASS.
- Eseguite le 22 ricette in un batch: tutte valide, ripetizioni identiche con
  gli hash di stato, baseline del modo 2 riprodotta, fasi invariate a ogni ampiezza.
  Da ±1e-3 l'uscita del regolatore lascia l'inviluppo 29,27–29,30%.
- La valvola sposta energia fra `stop-out` e `control-out`, circa 3,7 kW per
  1e-4, e la coppia del rotore; non agisce sui 1,157 MW di `outlet`. Colonna
  primaria utilizzabile, con deviazione dell'8,6% da quella a 1e-5.
- Con i pesi fisici il modello congiunto porta il massimo tasso di energia da
  302 a 209 kW, il 69%: esito `CONTROL-VALVE-NOT-MATERIAL-IN-LINEAR-MODEL`. La
  previsione richiede +2,9% di valvola e sette coordinate oltre ±2%, e sposta il
  massimo sull'hotwell; con le scale del contratto peggiora da 464 a 526 kW.
- Congelato il pacchetto con ricevuta; replay PASS. Nessuna famiglia, radice o
  seed è qualificato: R3 resta RED e M10 aperto.

### Sensibilità alle altre grandezze non fluide

- Preparati e committati prima dell'esecuzione specifica, contratto e runner
  per pompe di condensato e alimento, popolazione neutronica e temperature
  iniziali, senza modifiche a `src/` o `tests/`. Revisione statica PASS.
- Eseguite 102 ricette in tre batch: tutte valide e nell'inviluppo, ripetizioni
  identiche, baseline uguale a quella congelata della valvola. Un solo cambio di
  fase: pompa di alimento a +1e-2, su `feedwater-inventory` e hotwell.
- Le pompe spostano massa fra hotwell, `feedwater-inventory` e drum; popolazione
  e temperature agiscono solo sull'energia di `outlet`, con colonne pesate
  collineari (coseno 1,0000). Con gli inventari nessuna grandezza scende sotto il
  98% del massimo dei soli inventari; il residuo resta su `stop-out`, 295–314 kW.
- Esito congelato `NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL`: il modello
  con tutte le colonne prevede 18 kW, il 6%, ma con passi fino a −6.077% sulla
  pompa di condensato e +2.136% sulla popolazione. È un artefatto della
  collinearità, non una motivazione fisica; replay PASS. R3 resta RED.
- Registrata la decisione di classificare i modelli lineari entro i limiti
  delle sonde, raggruppando le direzioni collineari.

### Revisione vincolata della combinazione non fluida

- Aggiunto un solutore ai minimi quadrati con limiti, a insieme attivo, con
  certificato KKT e sei casi analitici; confrontato fuori dal repository con
  il gradiente proiettato su 60 problemi casuali (eccesso massimo 1,4e-14).
- Committati specifica e contratto prima del calcolo: inventari entro ±2%,
  grandezze non fluide entro ±1e-2, nocciolo raggruppato e controllo separato.
- Con i soli inventari vincolati il massimo è 335 kW su `turbine-inlet`; con le
  grandezze non fluide sale a 357 kW, il 107%, in tutti i modelli. Certificati
  soddisfatti, raggruppamento ininfluente, nessuna chiamata all'impianto.
- Lungo la linea vapore restano 190–210 kW su `stop-out` e `control-out` e
  circa 350 kW su `turbine-inlet` qualunque sia la combinazione. Esito
  `NO-BOUNDED-NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL`, congelato con
  replay PASS: la prossima attività è la revisione della legge di energia dello stadio.

### Ricostruzione della legge di energia dello stadio

- Individuata nel codice l'ipotesi: con il drenaggio della condensa lo stadio
  assegna a vapore e condensa le energie di `GetSaturationProperties`, sempre
  storiche, mentre il separatore del drum usa già il trasporto di fase della
  chiusura attiva. Lo stesso vale per la condensa del condensatore.
- Aggiunto un esecutore solo di test (`tests/` a 412 file) e committati
  protocollo e soglie prima dell'esecuzione; nessuna modifica a `src/`.
- La prima esecuzione è tecnicamente non valida: i provider sono stati valutati
  sullo stato di fine passo invece che su quello letto dallo stadio, e
  l'identità è fallita a 5e-8 contro 1e-9. Congelata come evidenza del difetto,
  con la relativa decisione ingegneristica; nessuna soglia è stata cambiata.
- La revisione 2 valuta i provider sugli ingressi registrati negli snapshot:
  esito `STAGE-ADMISSION-CLOSURE-INCOHERENCE-DEMONSTRATED`. Vapore e condensa
  coincidono esattamente con la saturazione storica (2.726,6 e 1.044,6 kJ/kg)
  invece che con la chiusura attiva (2.801,7 e 1.074,0 kJ/kg).
- Con le energie della chiusura attiva, a parità di stato, l'accumulo su
  `turbine-inlet` scende da 1,007 MW a 18,8 kW, l'1,9%. La miscela C4 del nodo
  coincide con le proprie proprietà di trasporto entro 1,7 J/kg; il condensatore,
  solo riportato, vale circa 6,3 kW. Replay PASS; R3 resta RED.
- CI ordinaria PASS con 412 file di test: 1.563 test, 1.397 superati, 166
  espliciti esclusi, zero fallimenti; baseline delle evidenze correnti PASS.

### Riparazione del trasporto di fase e verifica revisionata

- Corretto il piano del condensatore: invarianti legge e limiti termici, con
  portata ricalcolata dalle energie attive. Aggiunta la lettura di trasporto
  indicizzata dalla pressione e applicata a stadio e condensatore, nei quattro
  file produttivi previsti. Modalità storiche bit per bit e payload C4 preservati.
- Verificati fallback, ripartizione di massa, lavoro dello stadio, tre limiti
  di condensazione, conservazione, lookup C4 e assenza di allocazioni marginali.
- Migrato il controllo ordinario del CSV C4 pre-riparazione: dati storici
  autenticati e confrontati, raw e ripetibilità correnti preservati, nuova
  regressione della legge attiva. Nessun valore atteso storico aggiornato.
- Compilazione Release senza avvisi; suite completa PASS, 1.409 superati e
  167 espliciti esclusi. PASS anche contratto CI e baseline correnti Exact-V9.
  I log dei primi fallimenti sono conservati nel pacchetto della misura.
- Due ricette C4 identiche: accumulo turbine-inlet 18,963–19,465 kW, circa il
  2% del precedente MW. L'energia passa a valle; il punto grezzo non è una radice.
- Il primo protocollo richiedeva erroneamente stazionarietà della massa: il
  suo esito non conforme resta congelato. Adjudication 2 PASS sui medesimi dati,
  senza nuove chiamate, verificando accumulo contro ingressi meno uscite.
- Congelata la revisione coerente dell'obiettivo energetico: 125,0 e 74,8 kW
  nei due modelli lineari, rapporto 59,8%, con residui di massa non conformi.
  I precedenti massimi dei fit L2 non erano limiti inferiori del massimo.
- Riallineati handoff e diagramma alla nuova misura e alla prossima diagnosi.
  R3 resta RED, M10 aperto; nessuna attivazione produttiva C4 o installazione di seed.

### Diagnosi dei bilanci dopo la riparazione

- Aggiunta una console separata sotto `eng/diagnostics`, senza modificare
  `src` o `tests`: due ricette identiche, sei passi runtime totali. Gli hash
  degli stati completi coincidono con la precedente misura riparata.
- Ricostruiti 32,974 MW di calore verso outlet e il trasporto netto di
  34,011 MW, coerenti con −1,037 MW di accumulo. Verificati indipendentemente
  i bilanci di combustibile e struttura; il pacchetto distingue il trasporto
  netto inferito dalle singole portate corrette non osservate.
- Chiusi i bilanci di exhaust entro 1e-6 kg/s e 0,01 W: +0,956 MW e
  −0,1427 kg/s di accumulo. Limite del condensatore attribuito a UA,
  27,597 MW effettivi rispetto a 40 MW installati.
- Il confronto algebrico a stato fisso mostra che variare solo il raffreddamento
  non annulla insieme massa ed energia; non è una prova di impossibilità nel
  box. Compilazione, fixture e replay PASS. Prossimo passo: sonde accoppiate
  con la legge riparata; R3 resta RED e M10 aperto.

### Prime sensibilità accoppiate sul punto riparato

- Eseguite 82 ricette, entro 248 passi runtime, mediante console separata e
  senza modificare gli alberi produttivi o di test. Ripetizioni identiche;
  baseline e hash completi coincidono con la misura della riparazione.
- Tutte le ricette sono valide, 66 nell'inviluppo. Validate sei direzioni
  indipendenti; le sonde di pressure escono dall'inviluppo, l'energia di
  exhaust manca il limite di linearità, con scarto pesato del 25,1%.
- Inclusi i bilanci dei corpi termici: sorgente nucleare e temperature,
  quasi collineari osservando i fluidi, diventano distinguibili. La sonda
  nucleare-termica combinata coincide con la somma delle colonne entro lo
  0,037%, alla scala misurata. Nessuna proposta o ricerca eseguita.
- Compilazione, preflight, fixture e replay PASS; evidenza congelata. La
  prossima prova deve completare le direzioni mancanti mantenendo invariati
  inviluppo, scale e criterio di radice. R3 resta RED, M10 aperto.

### Raffinamento e misura estesa della base riparata

- Aggiunto un esecutore separato riutilizzabile per le sonde, con richieste,
  assembly e budget autenticati prima dell'istanziazione fisica. Alberi
  produttivi e di test invariati rispetto alla suite già verificata.
- Eseguite 70 ricette ripetibili, tutte sane e nell'inviluppo: recuperate le
  colonne di pressure con ampiezze ridotte e quella energetica di exhaust
  con accordo locale del 6,15% fra 3e-5 e 1e-4. Conservati tutti i confronti.
- Verificati i bilanci meccanici entro 1e-6 N·m; lo scarto delle sonde di
  exhaust proviene soprattutto dalla portata dello stadio, con lavoro ideale
  invariato. Misurata anche una direzione compensata di massa ed energia.
- Eseguite altre 362 ricette sulle trenta coordinate: tutte sane e ripetibili,
  334 nell'inviluppo, 43 punti sovrapposti con identici hash e residui.
  Selezionate 19 colonne, comprese tutte le sei grandezze non fluide.
- Restano sette colonne senza due scale nel ramo ammesso e quattro con
  disaccordo fra ampiezze. Il rango numerico 19 sulle colonne selezionate è
  diagnostico; nessuna famiglia, proposta o radice qualificata. Compilazione,
  fixture, preflight e replay PASS; entrambi i pacchetti congelati.
- Riallineati checkpoint, roadmap e handoff alle direzioni da costruire nei
  rami ammessi. R3 resta RED e M10 aperto.

### Base direzionale, prime proposte e limite di portata

- Eseguite 394 ricette ripetibili, tutte sane e 384 nell'inviluppo: recuperate
  nove direzioni, quattro mediante stencil unilaterali di secondo ordine.
  Coefficienti e vincoli del verso conservati, senza promuoverli a coordinate libere.
- Eseguite altre 114 ricette ripetibili, tutte sane e 110 nell'inviluppo:
  due direzioni accoppiate portano il rango numerico diagnostico a 30.
  Le due derivate cartesiane mancanti mantengono il proprio esito precedente.
- Eseguita la prima prova combinata, sei ricette: passi 0,25 e 0,50 di una
  proposta lineare vincolata, con ripetizioni identiche e controlli registrati
  rispettati. Entrambe le previsioni falliscono: errore relativo 14,734 e
  15,011 contro 0,10. Nessuna ricerca avviata o seed installato.
- Ricostruiti offline portata e coppia al primo passo: la legge sceglie il
  minimo fra capacità delle valvole, espansione e inventario. La baseline
  è quasi sul pareggio e le proposte cambiano vincitore. Le secanti miste
  non predicono la direzione composta; nessun nuovo difetto fisico dimostrato.
- Conservato il primo rifiuto della revisione, che richiedeva frazioni
  esattamente zero; il controllo corretto verifica ingressi effettivi identici
  in binary64. Codice, specifica, contratto e log originali congelati; nessuna
  nuova chiamata fisica per la revisione.
- Compilazione, fixture e replay PASS nei rispettivi ambiti. Handoff aggiornato
  verso l'osservazione di tutte le capacità e un modello del minimo lungo
  la ricetta. Alberi produttivi e di test invariati; R3 RED e M10 aperto.

### Capacità di portata a tutti i checkpoint

- Aggiunta la console diagnostica `R3CapacityObservation`, copia del runner
  riutilizzabile con il solo blocco `StageFlow`: posizioni effettive, portate
  delle valvole, portata comandata dello stadio e pressioni dello stesso passo.
  Il runner precedente e i suoi hash restano invariati.
- Ripetute le 38 richieste di origine, al massimo 116 passi runtime: le sei
  della prova 6 e le sonde centrali delle otto direzioni attive. Tutti i record,
  tolto il blocco aggiunto, coincidono con quelli congelati; ripetizioni identiche.
- Il minimo delle cinque capacità riproduce **esattamente in binary64** la
  portata comandata in tutte le 57 ricostruzioni; pressioni e leggi delle
  valvole senza scarti. Al primo passo è riprodotta l'attribuzione 7.
- La posizione effettiva della control coincide con l'uscita del governor
  ai tre checkpoint. Nella baseline vince stop al primo passo, poi control
  con margini di circa 5,9e-6 e 4,5e-6 kg/s; le due proposte restano nel ramo
  admission a tutti i checkpoint, con margini da 2,6e-3 a 5,6e-3 kg/s.
- Cinque delle otto direzioni attive cambiano vincitore fra i due segni a tutti
  i checkpoint, due solo al primo, una mai. Le secanti delle capacità sono
  descrittive; nessun modello, proposta o ricerca. R3 RED e M10 aperto.

### Modello offline del minimo delle capacità

- Aggiunta la revisione `r3-repaired-capacity-minimum-model9`, senza passi
  runtime. Ogni capacità è lineare nelle direzioni attive e nelle portate dei
  checkpoint precedenti; la portata è il loro minimo; le 81 righe rispondono
  alle direzioni e alle portate fino al proprio checkpoint.
- Stima dai soli 38 record congelati dell'osservazione 8, senza le proposte:
  somme delle sonde opposte per i termini di portata, differenze per quelli di
  direzione. Rapporto minimo fra valori singolari 0,0065.
- Proposte della prova 6 previste con errori relativi 1,78e-4 e 1,50e-4, contro
  14,734 e 15,011 del modello affine conservato; ramo admission previsto a tutti
  i checkpoint. Vincitori corretti nelle 16 sonde, errore massimo 1,65e-3;
  massimo 2,27e-4 escludendo una direzione alla volta.
- Coppia per unità di portata entro 3,3e-4 dalla relazione meccanica; risposta
  dell'inventario −0,5 entro 2,1e-3. Confronto dichiarato retrospettivo.
- Il primo replay è fallito per chiavi intere diventate stringhe nel JSON; il
  tentativo resta congelato come `00-original-*` e il contratto dichiara la
  correzione. Handoff verso proposte prospettiche. R3 RED e M10 aperto.

### Verifica prospettica del modello del minimo

- Aggiunta la prova `r3-repaired-minimum-proposals10`: 14 ricette ripetute, al
  massimo 44 passi runtime, con la console `R3CapacityObservation` invariata.
- Per ogni sequenza di vincitori un programma lineare sceglie il passo che
  minimizza il massimo delle 81 righe pesate, entro le ampiezze campionate e
  con margine di 1e-3 kg/s. Tre rami realizzabili: stop, admission, expansion.
  Previsioni, vincitori, margini e rapporti di cancellazione congelati prima
  della prima chiamata fisica.
- Sei previsioni su sei conformi alle frazioni 0,25 e 0,50: errore relativo
  massimo 4,7e-4 contro 0,10, vincitori corretti, errore di portata entro
  1e-7 kg/s, nessun cambio di fase o regime. Rapporti di cancellazione 0,08–0,13.
- Massimo delle righe pesate osservato da 1,157 a 1,056 nei rami stop e
  admission, come previsto. Massimo dei 75 residui originali circa 8,56
  milioni: nessuna radice, nessun seed, nessuna ricerca.
- Limiti: otto direzioni su 30; ramo control e sequenze miste non verificati.
  Handoff verso l'osservazione delle altre 22 direzioni. R3 RED e M10 aperto.

### Capacità di portata nelle altre 22 direzioni

- Aggiunta l'osservazione `r3-repaired-capacity-observation11`: 90 ricette
  ripetute, al massimo 272 passi runtime, console `R3CapacityObservation`
  invariata. Ripete le sonde congelate delle 18 direzioni centrali mancanti e
  il lato positivo, a scala selezionata e doppia, delle quattro unilaterali.
- Record identici alle origini tolto il blocco aggiunto; baseline uguale alla
  prova 8; minimo delle capacità esatto in 135 ricostruzioni su 135; guardie
  disponibili per tutte le sonde.
- 17 direzioni, incluse le quattro unilaterali, non cambiano la portata dello
  stadio né il vincitore. Cinque cambiano entrambi: control-valve, header:mass
  e stop-out:mass a tutti i checkpoint (fino a 1,8e-3 kg/s),
  control-out:raw-isobaric e steam:energy a un solo checkpoint.
- Nessun coefficiente stimato, nessuna proposta o ricerca. Handoff verso la
  ristima del modello sulle 30 direzioni. R3 RED e M10 aperto.

### Modello del minimo sulle 30 direzioni

- Aggiunta la revisione offline `r3-repaired-capacity-minimum-model12`, zero
  passi runtime: termini di portata stimati sulle 13 direzioni che la cambiano,
  risposta diretta per le altre 17, differenza in avanti per le unilaterali.
- Confronti con record non usati nella stima: proposte delle prove 6 e 10 e
  sonde doppie unilaterali, 12 su 12 conformi con errore massimo 2,4e-3;
  vincitori corretti in 26 sonde su 26.
- Aggiunta la prova `r3-repaired-minimum-proposals13`: 18 ricette ripetute, al
  massimo 56 passi runtime. Quattro sequenze ammesse (stop, admission, expansion
  e control–expansion–control); ogni passo muove 26 direzioni, 18 fra le 22
  aggiunte. Previsioni congelate prima della misura.
- Otto previsioni su otto conformi: errore relativo massimo 1,8e-3 contro
  0,10, vincitori corretti, nessun cambio di fase o regime. Primo ramo control
  e prima sequenza mista verificati.
- L'ottimo previsto entro le ampiezze campionate resta circa 0,955 anche con
  30 direzioni; residuo originale circa 8,56 milioni. Nessuna radice o ricerca.
  Handoff verso la dichiarazione di una ricerca a più passi. R3 RED e M10 aperto.

### Diagnosi del limite del passo

- Aggiunta la revisione offline `r3-repaired-step-limit14`, zero passi
  runtime: 30 programmi lineari certificati sul modello 12, in tre rami, con e
  senza vincoli di ramo, nelle ampiezze campionate e con limiti uniformi fino
  al 2%.
- Il limite a 0,955 viene da due righe del primo checkpoint: energia di
  exhaust (0,96 MW di accumulo) e coppia del rotore. La portata dello stadio le
  muove in proporzione fissa; la loro combinazione senza portata risponde
  quasi solo a exhaust:energy, 2,8 kW per ampiezza campionata.
- Limite inferiore calcolato a mano uguale all'ottimo certificato entro 4e-9
  nelle ampiezze campionate. In estrapolazione lineare: 0,39 MW al limite
  ereditato del 2%; 3,4% di exhaust:energy per annullare la combinazione.
- Nessuna ricerca dichiarata: la famiglia attuale non la giustifica finché la
  leva non è misurata alla scala del percento. Handoff verso quella misura.
  R3 RED e M10 aperto.

### Documentazione

- Riscritto il README intorno al simulatore e alle istruzioni d'uso; riscritto
  il manuale italiano con spiegazioni dei componenti, delle interazioni e dei diagrammi.
- Aggiunte al manuale dodici figure SVG in `docs/usermanual/figure/`: panoramica
  dell'impianto, criticità, periodo, barre, titolo e vuoto, pompa e circuito,
  coppie, sincronizzazione, xeno, treno di valvole, condensatore e soglie. Ogni
  figura ha introduzione, legenda e spiegazione; le curve qualitative sono dichiarate tali.
- Aggiunte altre sedici figure SVG per lettori di ogni livello: itinerari di
  lettura, dal clic alla risposta, accumuli, saturazione, calore dopo l'arresto,
  volume di controllo, corpo cilindrico, portate della turbina, lettura di uno
  strumento, passo dei comandi, memoria e reset, protezioni temporizzate,
  percorso di avviamento, mappa della schermata, assistenza e autorità, ordine
  della diagnosi. Valori e soglie verificati sul manuale e sul codice.
- Aggiunte al capitolo 5 del manuale 22 schede tecniche dei componenti: com'è
  fatto il componente reale e nel modello, volume e litri, contenuto e masse al
  punto iniziale, campo di funzionamento, semplificazioni e conseguenze di guasti
  e blocchi. Nuova introduzione con i mattoni del modello, la tabella dei volumi
  e il catalogo dei guasti; sette figure SVG (volumi in scala, mattoni, scala
  delle pressioni, calore nel nocciolo, riserva del corpo cilindrico, inerzia del
  rotore, condensatore in sezione). Valori dai parametri del profilo Exact-V9.
- Aggiunte alla roadmap sei proposte di miglioramento emerse dalle schede, in
  ordine di priorità: protezione di bassa portata principale, canale di livello
  separato per la protezione, allarme di squilibrio vapore-alimentazione,
  reintegro dell'inventario, prova di rigetto del carico, preriscaldo
  rigenerativo. Nessun impegno né modifica fisica; R3 e M10 invariati.
- Archiviati 74 documenti conclusi o superati, preservandone byte e SHA-256;
  separati 174 input esatti dei validatori dalla documentazione leggibile.
- Consolidati risultati e ripresa in `docs/PROJECT.md`, con handoff operativo.
  La roadmap mantiene consegne e condizioni future; gli indici collegano
  manuale, validazione, decisioni, evidenze e cronologia senza un nuovo file di stato.

## 2026-11-10 — Revisione numerica offline

- Ricostruito l'arresto della ricerca H limitata, usando traccia e Jacobiano congelati.
- Implementato il confronto locale scalato con casi numerici indipendenti.
  La discesa prevista dal modello non costituiva una prova di miglioramento
  fisico o di qualificazione del seed; R3 rimaneva RED.

Il [changelog precedente](docs/history/archives/README.md#snapshot-changelog-md)
è conservato integralmente nell'archivio documentale.
