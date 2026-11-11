# Verifica di due proposte piccole dalla base direzionale riparata

La prova confronta previsione e risposta reale usando la base direzionale 5,
che contiene 30 direzioni e rango numerico diagnostico 30. Non qualifica una
famiglia e non avvia una ricerca. Contratto, implementazione e input sono
autenticati prima del calcolo delle proposte e delle chiamate fisiche.
Sorgenti, test, ricetta e legge fisica restano invariati.

Si costruisce un modello lineare delle 81 righe diagnostiche, con gli stessi
pesi fisici precedenti. Ogni coefficiente è limitato all'ampiezza campionata
della propria direzione. I quattro coefficienti unilaterali restano non
negativi; gli altri ammettono entrambi i segni. La mappa conservata nella
prova 5 ricostruisce esplicitamente le 24 frazioni fluide e le sei non fluide.
La somma deve rispettare il limite ereditato del 2% prima dell'esecuzione.
La compatibilità di fase del passo composto non è presunta.

L'obiettivo primario è minimizzare il massimo valore assoluto delle 81 righe
pesate. Un secondo programma lineare minimizza la somma assoluta dei
coefficienti normalizzati, consentendo un peggioramento primario di 1e-9
nella scala interna normalizzata. Entrambi i programmi devono restituire
un certificato numerico. Eventuali correzioni dei soli coefficienti oltre
un bordo per arrotondamento, fino a 1e-12, sono registrate; non si correggono
inventari o stati dell'impianto. Pesi diagnostici e obiettivo non modificano
i 75 residui o il criterio originale di radice.

Il passo lineare intero non viene eseguito. Si misurano solo le sue frazioni
0,25 e 0,50, ciascuna ripetuta due volte, più due baseline: **sei ricette**,
al massimo **20 passi runtime**, inclusi i due passi iniziali dell'harness.
Modello, coefficienti, richieste e hash degli assembly sono registrati prima
della prima chiamata fisica. Non si sovrascrivono acquisizioni esistenti.

Ogni proposta deve mantenere salute, inviluppo, fasi di tutti i nodi e regimi
idraulici, termici, meccanici e del governor lungo tutta la ricetta. La
previsione è verificata sulla variazione di tutte le 81 righe rispetto alla
baseline: norma dell'errore divisa per norma della variazione prevista, al
massimo 10%. Non si divide per il residuo iniziale, che potrebbe mascherare
una previsione piccola errata. I rifiuti dei controlli restano rifiuti anche
con errore numerico basso. Si conservano tutte le variazioni e il massimo
dei 75 residui originali osservati.

Ripetizioni identiche; baseline con identici inventari, residui, fasi e hash
completi rispetto alla prova 5. Si ricostruiscono tutti i bilanci precedenti
entro 0,01 W, 1e-6 kg/s e 1e-6 N·m. Un PASS del pacchetto significa evidenza
completa e riproducibile; l'esito delle due previsioni resta separato. Una
proposta rifiutata non autorizza correzioni, nuove scale o altre ricette.

Compilazione, fixture del vincolo unilaterale e del minimo numerico e
preflight senza runtime precedono l'acquisizione. Dopo il congelamento,
`python eng/run-r3-repaired-proposal-trial6.py review` ricostruisce modello,
richieste e risultati senza nuove simulazioni. R3 e M10 restano aperti.
