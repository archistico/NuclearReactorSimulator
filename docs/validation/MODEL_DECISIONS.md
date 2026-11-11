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

## Ricostruire una legge sullo stato letto dal solutore

I solutori di stadio e condensatore leggono lo stato committato all'inizio del
passo; il motore espone anche lo stato candidato a fine passo. Una
ricostruzione che confronta valori registrati con una funzione indipendente
deve valutarla sugli ingressi registrati nello snapshot del solutore, non sullo
stato finale: la differenza è piccola ma supera qualsiasi tolleranza di
identità. La prima ricostruzione della legge di stadio è stata invalidata per
questo motivo e resta congelata come evidenza del difetto.

## Considerare il ramo lungo tutta la ricetta

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

`docs/` spiega progetto, modello e conclusioni. Gli input M10 che richiedono
testo esatto sono in `eng/contracts/m10`, con percorsi e hash indicizzati;
i validatori storici leggono le proprie copie congelate. I risultati restituiti
conservano la propria identità e non vengono riscritti per attribuire al metodo
eseguito tentativi o regole che non aveva.

La [cronologia](../../CHANGELOG.md) registra l'evoluzione e
l'[handoff](../PROJECT.md#handoff) indica da dove riprendere. Una nuova guida
può evolvere mantenendo distinti riferimenti storici, risultati correnti e
sequenza futura.
