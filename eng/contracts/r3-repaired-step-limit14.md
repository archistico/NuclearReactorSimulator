# Che cosa limita il passo del modello del minimo

Il modello 12, verificato dalla prova 13, prevede entro le ampiezze campionate
un massimo delle 81 righe pesate di circa 0,955 contro 1,157, con otto come
con 30 direzioni. Prima di dichiarare una ricerca va capito se il limite viene
dalle ampiezze campionate o da un conflitto fra righe che nessuna direzione
risolve. Questa revisione lo diagnostica offline: **zero passi runtime**,
nessuna ricetta, proposta o ricerca. Sorgenti, test, legge fisica, scale,
limite ereditato del 2% e criterio di radice restano invariati.

Nelle righe pesate le masse sono in kg/s, le energie in MW, la coppia in N·m.

## Programmi lineari

Il modello si ricostruisce dai record congelati e deve coincidere con
l'analisi 12. Per le tre sequenze a vincitore costante verificate (stop,
admission, expansion) si minimizza il massimo delle 81 righe previste:

- entro le ampiezze campionate;
- entro limiti uniformi di 0,1%, 0,5%, 1% e 2% su ogni coefficiente e su ogni
  coordinata aggregata, con i coefficienti unilaterali non negativi;
- con e senza i vincoli di ramo (margine 1e-3 kg/s).

Ogni programma deve restituire un certificato. Si riportano l'ottimo e i
moltiplicatori duali delle righe: indicano quale combinazione di righe
impedisce di scendere.

## Combinazione senza portata

La portata dello stadio sposta insieme l'energia di exhaust e la coppia del
rotore al primo checkpoint. Si costruisce la combinazione

    energia di exhaust − r · coppia,   r = risposta dell'energia di exhaust
                                           alla portata / risposta della coppia

in cui il termine di portata si cancella. Si verifica che la combinazione non
dipenda dalla sequenza di vincitori entro 1e-9. Il suo valore alla baseline,
meno quanto le direzioni possono spostarla dentro un box, diviso per 1 + |r|,
è un limite inferiore del massimo delle righe: si calcola a mano, senza
programma lineare. Deve non superare mai l'ottimo certificato e coincidere con
esso, entro 1e-6, nelle ampiezze campionate.

Si riportano, per direzione, l'effetto su energia di exhaust e coppia e la
parte che non passa dalla portata; la leva diretta principale e la coordinata
che, per estrapolazione lineare, annullerebbe la combinazione.

## Linearità della leva

Dai record congelati si riportano le secanti dell'energia di exhaust per la
direzione `exhaust:energy` a tutte le ampiezze e ai due segni disponibili.
Ogni sonda deve restare nelle fasi e nei regimi della baseline.

## Lettura dell'esito

Dentro le ampiezze campionate il limite è una proprietà del modello
verificato. **Oltre è un'estrapolazione lineare**: un'indicazione per
scegliere la prossima misura, non una previsione né una prova di
impossibilità. La diagnosi riguarda le 30 direzioni della famiglia H;
grandezze esterne, come il lato di raffreddamento del condensatore, non sono
esaminate. Un PASS significa evidenza completa e riproducibile. I risultati
precedenti non vengono riscritti.

Dopo il congelamento,
`python eng/review-r3-repaired-step-limit14.py review` ricostruisce l'analisi
senza simulazioni. R3 e M10 restano aperti.
