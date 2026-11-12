# Sonda positiva dell'energia di exhaust oltre il precedente box

## Decisione dopo la misura 15

Le 22 ricette della prova 15 sono sane, nell'inviluppo e ripetibili fino al
limite precedente del 2%. Le secanti della combinazione energia-coppia
restano entro il 2,22% del riferimento locale, con superficie UA e limite
termico di condensazione attivi. Al punto +2% la combinazione vale ancora
0,4116 MW e non attraversa lo zero; la secante indica circa +3,50%, non misurato.
La revisione 14 individuava la stessa leva mediante un limite del modello.

Questi risultati non provano impossibilità della famiglia nel box, ma non
motivano una ricerca per tentativi in quel box. Si sceglie prima una sonda
di dominio dell'inventario di exhaust: è l'energia dello stato iniziale che
determina la differenza fra energia specifica entrante e prelevata dal nodo.
Non si modifica il lato di raffreddamento per compensare quell'accumulo e
non si inizializzano diversamente i controllori senza evidenza dedicata.

La presente eccezione riguarda **solo questa diagnostica**, non amplia il
vecchio box H né autorizza un box di ricerca. Gli altri inventari e tutte
le grandezze non fluide rimangono fissi. Nessuna geometria, conduttanza,
capacità termica, resistenza, legge fisica, soglia o criterio di radice cambia.
La scelta della famiglia di ricerca richiederà ancora revisione del dominio,
bilanci di massa ed energia congiunti e nuove previsioni verificate.

## Acquisizione

Si misurano solo frazioni positive di energia interna di exhaust: +2%,
+2,5%, +3%, +3,5% e +4%. Ogni punto e la baseline sono ripetuti due volte:
**12 ricette**, al massimo **38 passi runtime**. Il punto +2% e la baseline
devono coincidere integralmente con la prova 15. Non si sovrascrivono dati.

Una nuova console separata deriva da R3CapacityObservation e cambia solo
la verifica del dominio delle richieste: accetta energia di exhaust fra
0 e +4%, rifiuta qualunque altra variazione, valori non finiti e vettori
incompleti. Le fixture del dominio, senza runtime, verificano questi limiti.
Sorgenti produttive e test non cambiano; restano i medesimi solutori e la
medesima ricetta con due passi preparatori e un Running da 10 ms.

Contratto, implementazione, input, richieste e assembly sono autenticati
prima della prima chiamata fisica. Si applicano osservazioni, ricostruzioni
e tolleranze della prova 15, conservando anche ogni cambio di fase, regime,
salute o inviluppo. Il confronto con la secante locale, al 10%, classifica
la risposta senza estendere automaticamente la validità del modello 12.

Si riporta l'eventuale intervallo fra campioni ammessi in cui la combinazione
del primo checkpoint cambia segno. È solo un intervallo per una combinazione:
non annulla la massa del nodo, gli altri 74 residui o i corpi termici e non
qualifica un equilibrio. Gli altri checkpoint conservano i propri bilanci;
il rapporto di cancellazione della revisione 14 resta riferito al primo.

Non si eseguono proposte, ricerca o installazione di seed. Il replay
`python eng/run-r3-repaired-exhaust-domain16.py review` ricostruisce l'esito
congelato senza nuovi passi runtime. R3 resta RED e M10 aperto.
