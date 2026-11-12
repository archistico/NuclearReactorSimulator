# Decisioni sul modello e sulla validazione

Questi principi raccolgono i vincoli durevoli emersi da M10. Il
[checkpoint](../PROJECT.md) mantiene lo stato e i contratti eseguibili fissano
le regole di ciascuna prova.

## Conservare le identità esatte

Scenari, sessioni e replay registrati mantengono la propria versione e il proprio
significato. Exact-V9 resta distinto dagli esperimenti su chiusure o inventari
iniziali. Una nuova opzione richiede un'attivazione esplicita e non reinterpreta
gli archivi precedenti.

La chiusura termodinamica C4 è un'opzione esplicita. Identità del payload,
determinismo, equivalenza e vincoli di allocazione sono verificati separatamente
da quelli dei modi storici. Sostituire una chiusura non dimostra che gli
inventari iniziali siano un punto stazionario.

## Separare implementazione, evidenza e qualificazione fisica

Un controllo d'implementazione verifica il comportamento che copre. Un replay
di evidenze ricostruisce osservazioni, conteggi e decisioni dell'esperimento.
La qualificazione fisica richiede i propri criteri di accettazione. Di conseguenza
R1/R2 e un replay H PASS non stabiliscono R3 PASS o la chiusura M10.

I confronti fisici usano riferimenti indipendenti dall'implementazione di
produzione. Equazioni, parametri, dominio e tolleranze vanno fissati prima del
confronto. Accuratezza generica del solutore, parametrizzazione dell'impianto
e partecipazione di un sottosistema al punto provato sono questioni distinte.

## Ricostruire responsabilità e bilanci prima di cambiare la fisica

Conservazione, accumuli, proprietà trasportate, accoppiamento idraulico e risposta
dei controllori devono essere attribuiti ai componenti che li governano. Un
inviluppo integrato non conforme non dimostra da solo un difetto del controllo.
Analogamente, un rifiuto numerico prima della chiamata fisica non dimostra
che l'impianto sia privo di stati ammissibili.

Gli inventari possono accumulare massa o energia mentre i bilanci di proprietà
si chiudono. La ricostruzione dei trasferimenti verifica la coerenza delle
registrazioni; l'equilibrio richiede anche stazionarietà.

## Mantenere le indagini entro il dominio dichiarato

La famiglia H varia soltanto le 24 coordinate di massa ed energia fluide,
con condizioni iniziali non fluide fisse. Residui, scale, box, conteggi e
condizioni di conferma restano quelli del protocollo congelato. Un esito
inconclusivo richiede motivazioni e prove distinte prima di cambiare box,
famiglia di trim, coefficienti, controllori o seed produttivo.

Normalizzare le colonne o le disequazioni ausiliarie non equivale a ripesare
i residui. La conversione di unità dei margini e il cap di un obiettivo numerico
secondario restano separati dalle tolleranze fisiche. La frazione verso il
limite si applica globalmente al passo, secondo il contratto; non taglia
indipendentemente le coordinate.

## Verificare le previsioni sulla risposta reale

Diminuire la somma dei quadrati può aumentare il massimo residuo usato per
l'accettazione. Il sottoproblema deve affrontare l'obiettivo effettivo.
Certificato primario, stabilità della scelta secondaria e validità del modello
locale nella risposta non lineare richiedono verifiche separate.

Le previsioni di una direzione devono essere confrontate con prove fisiche.
L'errore osservato su una direzione non costituisce un limite universale per
le altre. Ripetizioni identiche dimostrano determinismo, non accuratezza delle
derivate. Un modello può sostenere una proposta più grande verificata senza
risolvere le variazioni più piccole. Anche il confronto affine deve considerare
che coordinate arrotondate non sono perfettamente collineari.

## Leggere i residui anche in unità fisiche

Le scale dei residui sono il rumore numerico della baseline e differiscono fra
nodi di oltre due ordini di grandezza. Vicino a una radice qualunque scala
equivale; lontano, il massimo scalato può diminuire mentre gli accumuli in MW
e la coppia netta aumentano. Ogni proposta o ricerca riporta quindi anche tassi
di massa, tassi di energia, coppia e inviluppo. La metrica fisica affianca il
criterio di radice senza ripesarlo o sostituirlo.

Un modello lineare che non contiene una radice nella famiglia di trim scelta
motiva la misura di altre grandezze prima di estendere budget o limiti. Resta
una previsione locale: non dimostra l'assenza di una radice non lineare.

## Classificare i modelli lineari entro la regione delle sonde

Una classificazione basata su minimi quadrati non vincolati può premiare
colonne quasi collineari: passi enormi e di segno opposto annullano i residui
sfruttando differenze minime fra derivate. Popolazione neutronica e
temperature iniziali di combustibile e struttura agiscono tutte sul calore
ceduto a `outlet`, con coseno pesato pari a 1 alla quarta cifra. Un protocollo
che classifica con un modello lineare dichiara quindi in anticipo i limiti del
passo, al più quelli delle sonde, oppure raggruppa le direzioni collineari, e
riporta il condizionamento. Un esito ottenuto fuori da quei limiti è
conservato ma non motiva una famiglia.

## Includere i corpi termici nel confronto delle direzioni

La quasi collinearità dipende anche dalle grandezze osservate. Dopo la riparazione,
le sonde su sorgente nucleare e temperature risultano quasi parallele osservando
fluidi e coppia, ma si distinguono includendo i bilanci di combustibile e struttura.
Un aumento della sorgente e un trasferimento da un corpo termico non sono la
stessa direzione fisica. Le righe diagnostiche aggiuntive mantengono separato
il criterio originale di radice; i pesi del confronto si dichiarano prima delle
misure e non si cambiano per rendere utilizzabile una colonna non conforme.

Una sonda sana può uscire dall'inviluppo operativo già nel primo passo preparatorio.
Disponibilità e linearità della colonna si verificano lungo l'intera ricetta,
non soltanto al Running. L'accordo di una piccola sonda combinata con la somma
delle risposte indipendenti non qualifica un passo maggiore o una famiglia completa.

## Usare l'obiettivo della classificazione

Il massimo energetico di una soluzione ai minimi quadrati non è il minimo
possibile di quel massimo. Per affermare un limite occorre ottimizzare la
quantità classificata e confrontare denominatori coerenti. La revisione
energetica separa quindi i minimi del massimo dai precedenti fit L2, riportando
anche massa e coppia: minimizzare soltanto l'energia non qualifica l'equilibrio.
L'esito storico resta conservato con la sua policy, senza attribuirgli una
prova generale di inesistenza di combinazioni o radici.

## Distinguere conservazione e stazionarietà nelle verifiche

Il bilancio di massa di un nodo confronta dM/dt con ingressi meno uscite.
Richiedere ingressi uguali alle uscite impone anche stazionarietà e può essere
errato per un punto grezzo dichiarato fuori equilibrio. Una correzione del
criterio successiva alla misura va dichiarata: dati ed esito originale non
si riscrivono e la nuova adjudication deve riprodurre il vecchio fallimento.
Il criterio di equilibrio dei residui resta separato dalla tolleranza del bilancio.

La portata di condensazione dipende dalla differenza fra energia del vapore
e della condensa. Correggere le proprietà attive può cambiarla anche con legge,
capacità termica e limiti invariati. I test coprono limite termico, inventario
e portata massima, senza imporre compensazioni per preservare il vecchio numero.

## Ricostruire una legge sullo stato letto dal solutore

La capacità installata del raffreddamento non coincide necessariamente con
quella effettiva: quest'ultima è il minimo fra installata, disponibile e
scambio UA. Una proposta che annulla l'accumulo energetico di exhaust deve
anche verificare la massa. Con energia specifica entrante diversa da quella
prelevata dal nodo, modificare la sola portata di condensazione a stato fisso
non può annullare entrambi. È un vincolo locale del bilancio, non una prova
di inesistenza di una radice con stato variabile o un difetto del modello.

Un flusso netto ricavato per differenza fra accumulo e altre sorgenti va
dichiarato inferito. Le portate dei componenti lette sullo stato iniziale
possono sostenere un bilancio indipendente soltanto se corrispondono a quelle
effettivamente integrate. Controllare quindi la telemetria della correzione
idraulica: l'assenza di correzione osservata in un passo non vale per gli altri.

I solutori di stadio e condensatore leggono lo stato committato all'inizio del
passo; il motore espone anche lo stato candidato a fine passo. Una
ricostruzione che confronta valori registrati con una funzione indipendente
deve valutarla sugli ingressi registrati nello snapshot del solutore, non sullo
stato finale: la differenza è piccola ma supera qualsiasi tolleranza di
identità. La prima ricostruzione della legge di stadio è stata invalidata per
questo motivo e resta congelata come evidenza del difetto.

## Considerare il ramo lungo tutta la ricetta

Il ramo non coincide soltanto con fase e limiti termodinamici o degli attuatori.
La portata dello stadio è anche il minimo fra capacità delle valvole, espansione
e inventario. Nel punto riparato le capacità iniziali sono quasi coincidenti:
le sonde possono cambiare il vincitore senza cambiare le fasi o i regimi già
registrati. Una matrice di secanti centrali che attraversano vincitori diversi
non è automaticamente un modello affine della direzione composta.

La prima prova combinata fallisce il confronto pur restando nei controlli
registrati. L'attribuzione offline del primo passo riproduce portata e coppia
con la legge del minimo. Un futuro modello deve rappresentare quel minimo
o i suoi diversi rami e osservarne i margini lungo la ricetta. Non si corregge
retroattivamente l'esito dei vecchi controlli né si deduce un difetto fisico
da un limite della linearizzazione.

Le capacità si osservano nello stato comandato del passo, lo stesso usato da
resolver e rete del vapore: nodi del passo precedente e valvole dopo attuatori,
protezioni e guasti. La posizione richiesta e quella effettiva vanno registrate
entrambe; nella prova 8 coincidono, ma il limite di corsa può separarle.
Il pareggio è strutturale: in un punto stazionario, con il drenaggio dell'umidità
e senza altri flussi sui nodi intermedi, le valvole portano la portata dello
stadio. Modello e ricerca devono quindi trattare un minimo non derivabile
anche nella radice, non soltanto lontano da essa.

Il modello locale linearizza ogni capacità nel proprio ramo, includendo le
portate dei checkpoint precedenti, e valuta il minimo un checkpoint alla volta.
Le righe rispondono alla portata comandata, quindi alla derivata della capacità
che vince. Con la base sul pareggio, le somme delle sonde opposte isolano il
salto del minimo e le differenze le risposte lisce. Un accordo ottenuto sugli
stessi record usati per scegliere la forma resta retrospettivo: per avanzare
servono proposte nuove con vincitori e margini congelati prima della misura.

Dentro una sequenza di vincitori fissata il modello è affine: una proposta si
sceglie per ramo, con un margine dichiarato sul vincitore, e si prevede con il
modello completo, non scalando il passo. La verifica prospettica vale per i
rami, le direzioni e le ampiezze misurati. Un ramo senza passo con margine,
come control dalla baseline attuale, resta non verificato: non lo si deduce
dagli altri. Un rapporto di cancellazione basso rende severo il criterio
relativo e va registrato insieme all'errore.

Il nodo che cambia fase può essere diverso da quello perturbato. La prova
estesa riparata osserva, per esempio, una sonda di drum che porta suction
in miscela soltanto al primo Running. Una colonna centrale richiede quindi
compatibilità di entrambi i segni su tutti i nodi e checkpoint. Se un solo
verso resta nel ramo, una futura sonda unilaterale va dichiarata e verificata
come tale; non diventa una coordinata libera in entrambi i versi.

Un accordo fra due ampiezze adiacenti non dimostra una derivata uniforme fra
tutte le scale. La selezione può essere locale, con regola fissata prima
della misura e tutti i confronti conservati, ma una proposta richiede ancora
la verifica della risposta reale. Un rango numerico delle sole colonne
selezionate non colma quelle mancanti e non dimostra sufficienza della famiglia.

Il controllo della fase include stato grezzo, primo passo preparatorio,
post-seed e primo Running. La fase iniziale da sola non descrive la traiettoria.
La qualità troncata a zero non è una distanza con segno dal confine; il solo
margine di volume non include il criterio energetico usato dal resolver C4.
L'accordo locale di un margine ricostruito con campioni osservati non dimostra
equivalenza globale con il resolver né qualifica da solo un vincolo di ricerca.
Una ricerca partita dal liquido che rientra nella miscela non esaurisce
l'esplorazione del ramo liquido.

Al confine del box, una sonda verso l'interno può lasciare il ramo a tutte le
ampiezze provate. La colonna resta indisponibile, senza valori fittizi. Ridurre
il raggio di fiducia mantenendo quelle sonde non ripristina la derivata.
Una base compensata o una riserva energetica interna richiedono osservazioni
esplicite e previsioni verificate. Nessuna di queste scelte introduce correzioni
automatiche agli inventari durante il normale funzionamento.

## Separare le guide dagli input dei validatori

Un controllo sulle perturbazioni deve riflettere l'aritmetica applicata agli
ingressi. Nella ricetta attuale `baseline * (1 + frazione)` può restituire lo
stesso valore quando la frazione è non nulla ma `1 + frazione` è esattamente
1 in binary64. La revisione del minimo conserva il fallimento del controllo
testuale e verifica l'identità effettiva senza tolleranza. Questa proprietà
non autorizza a ignorare perturbazioni in altre formule o versioni della ricetta.

`docs/` spiega progetto, modello e conclusioni. Gli input M10 che richiedono
testo esatto sono in `eng/contracts/m10`, con percorsi e hash indicizzati;
i validatori storici leggono le proprie copie congelate. I risultati restituiti
conservano la propria identità e non vengono riscritti per attribuire al metodo
eseguito tentativi o regole che non aveva.

La [cronologia](../../CHANGELOG.md) registra l'evoluzione e
l'[handoff](../PROJECT.md#handoff) indica da dove riprendere. Una nuova guida
può evolvere mantenendo distinti riferimenti storici, risultati correnti e
sequenza futura.
