# Revisione di sufficienza della famiglia H: scale, bilanci e box

Stato: **bozza in attesa di revisione**. Il protocollo è soltanto offline:
nessuna chiamata fisica, nessun test o sorgente produttivo aggiunto, nessun
seed installato. Non modifica scale, residui, box, criterio di radice o
inviluppo e non promuove R3 o M10.

## Domanda

Dopo H9 il massimo residuo scalato è 3.334.923,856 contro una soglia 1. Prima di
finanziare il confronto fra sonde compensate e riserva energetica, la revisione
chiede se la famiglia H (24 inventari fluidi, stati non fluidi fissi) e il box
±2% possano contenere un equilibrio, e se il massimo scalato misuri davvero
l'avvicinamento a quell'equilibrio.

## Ingressi

Sono autenticati per hash: il contratto di pianificazione con le scale, la
traccia completa di H feasibility 1, i tre CSV di Residual Diagnostic 1 e i
pacchetti restituiti delle ricerche H4–H9. Il punto di linearizzazione è il
vettore grezzo congelato del modo 2, a coordinate nulle.

## Metodo

**Scale.** Per ciascun nodo si riporta la scala rispetto all'inventario canonico
e la dispersione fra nodi. Le scale restano quelle congelate: sono il massimo
tasso di accumulo del modo canonico, o 64 ULP, cioè il rumore numerico di Exact-V9.

**Metrica fisica.** Ogni residuo scalato è riconvertito moltiplicandolo per la
propria scala. Per il punto iniziale e per il punto finale di H4–H9 si riportano
massimo tasso di massa (kg/s), massimo tasso di energia (W), coppia netta (N·m),
somma netta e somma dei valori assoluti dei tassi di energia a seed-step1, oltre
alle osservazioni di inviluppo già registrate al primo Running. È una lettura
degli stessi residui in unità fisiche; non li ripesa e non sostituisce il criterio.

**Attribuzione.** Dai CSV di Diagnostic 1 si calcola la variazione di energia
specifica fra modo 1 e modo 2 a parità di stato bersaglio. Si confrontano
portata di stadio per variazione su `control-out` con l'accumulo di
`turbine-inlet`, e portata di circolazione per variazione differenziale
`outlet`−`pressure` con l'accumulo di `outlet`. La tolleranza relativa è 2%.
L'accordo aritmetico sostiene una spiegazione; non è una prova causale.

**Modello lineare.** Lo Jacobiano 75×24 è ricostruito dalle 48 sonde a 1e-6
dell'iterazione 1. Lo stencil primario usa la stessa regola di compatibilità di
fase delle ricerche H; quello centrale attraverso la fase è riportato come
confronto di sensibilità. Si risolvono ai minimi quadrati, senza vincoli di box
e con fattorizzazione di Householder in binary64: tutti i 75 residui nelle scale
del contratto; gli stessi con pesi fisici dichiarati (1 kg/s, 2,5 MW, 100 N·m);
i soli 25 residui di seed-step1. Per ogni soluzione si riportano passo, coordinate
oltre ±2% e residui previsti. I pesi fisici servono soltanto al confronto.

Fixture numeriche coprono una soluzione esatta, un sistema incompatibile noto
e il rifiuto di un modello a rango ridotto.

## Condizioni di esito

`PASS-FAMILY-SUFFICIENCY-REVIEW` richiede che siano riprodotti tutti i seguenti
fatti; una differenza interrompe la revisione con errore tecnico.

- Da coordinate nulle a H9 il massimo scalato diminuisce, mentre somma assoluta
  dei tassi di energia e coppia netta aumentano.
- Le due attribuzioni di energia specifica rientrano nella tolleranza.
- Con entrambi gli stencil, la soluzione non vincolata su 75 residui esce dal
  box e lascia un massimo scalato previsto superiore a 1e5.

## Limiti della conclusione

Le previsioni lineari non sono osservazioni dell'impianto. La linearizzazione
vale nell'intorno del punto iniziale; hotwell, suction e drum sono a 1e-6 da un
cambio di fase e sei colonne usano uno stencil unilaterale. Passi di alcuni
punti percentuali sono fuori dalla regione descritta dalle sonde. L'esito non
dimostra l'assenza di una radice non lineare, non dimostra un difetto dei
controllori e non seleziona C o J. Non autorizza l'allargamento del box.

## Attività successiva proposta

Un PASS restituito e revisionato motiva soltanto la pianificazione di una
diagnostica distinta, solo di test, sulla sensibilità ai punti operativi non
fluidi già esposti dal seam del seed: apertura iniziale della valvola di
regolazione, percentuali iniziali delle pompe di condensato e alimento,
popolazione neutronica iniziale, temperature iniziali di combustibile e struttura.
La diagnostica misurerebbe, con sonde ripetute e budget esplicito, se una di
queste grandezze sposta i residui della linea vapore che il modello lineare
degli inventari non riesce ad annullare. Riporterebbe la metrica fisica accanto
al massimo scalato. Guadagni, resistenze, capacità, leggi fisiche, C4, Exact-V9,
soglie e inviluppo resterebbero invariati. Il confronto fra sonde compensate e
riserva energetica resta sospeso fino a quell'esito, senza essere annullato.

## Comandi

`python eng/review-r3-h-family-sufficiency1.py execute` scrive
`artifacts/r3-h-family-sufficiency-review1/01-analysis.json`. Dopo la revisione
esplicita dell'esito, `01-analysis.json` viene congelato con il meccanismo
condiviso e `python eng/review-r3-h-family-sufficiency1.py review` ne esegue
il replay indipendente.
