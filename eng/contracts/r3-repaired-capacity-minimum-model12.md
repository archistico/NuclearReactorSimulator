# Modello del minimo delle capacità sulle 30 direzioni

Il modello della revisione 9 copre le otto direzioni attive della prova 6.
L'osservazione 11 ha misurato le capacità nelle altre 22. Questa revisione
ristima il modello, con la stessa forma, su tutte le **30 direzioni di
lavoro**, e lo confronta con record già congelati. Lavora offline: **zero
passi runtime**. Non esegue proposte e non avvia ricerche. Sorgenti, test,
ricetta, legge fisica, scale e criterio di radice restano invariati.

## Stima

La forma è quella della revisione 9: a ogni checkpoint ogni capacità è lineare
nelle direzioni e nelle portate comandate dei checkpoint precedenti; la
portata dello stadio è il minimo; ognuna delle 81 righe è lineare nelle
direzioni e nelle portate fino al proprio checkpoint. Le direzioni entrano in
tre modi, decisi dall'osservazione 11 e non dall'accordo con le proposte:

| Gruppo | Direzioni | Stima |
| --- | --- | --- |
| Cambiano la portata comandata | 13: le otto della prova 8 più control-valve, header:mass, stop-out:mass, control-out:raw-isobaric, steam:energy | I termini di portata si stimano ai minimi quadrati sulle somme delle due sonde opposte; la risposta diretta dalla differenza, tolti i termini di portata. |
| Non la cambiano, due segni | 13 | Differenza centrale semplice. |
| Non la cambiano, unilaterali | 4: massa ed energia di hotwell e suction | Differenza in avanti alla scala selezionata; il coefficiente resta non negativo. |

Nessuna proposta entra nella stima. Per le unilaterali non entra nemmeno la
sonda a scala doppia. La forma è stata esplorata su questi record prima del
contratto: il confronto è retrospettivo.

## Confronti

- **Sonde che stimano i termini di portata** (26): vincitori previsti e
  osservati a ogni checkpoint. Sono dati usati nella stima: verificano il
  minimo, non le pendenze.
- **Direzioni centrali senza termine di portata**: rapporto fra parte pari e
  parte dispari delle due sonde, cioè quanto resta fuori da una pendenza
  lineare.
- **Sonda doppia delle unilaterali** (4, non usate nella stima): errore
  relativo della previsione a scala doppia.
- **Proposte congelate delle prove 6 e 10** (2 + 6, non usate nella stima):
  errore relativo sulle 81 righe, vincitori ed errore di portata, a confronto
  con il modello 9.

Una previsione è conforme con vincitori uguali a tutti i checkpoint ed errore
relativo non superiore a 0,10. Si ripetono i controlli strutturali del modello
9: coppia per unità di portata entro 1e-3 relativo e risposta dell'inventario
−0,5 entro 1e-2. I termini di portata devono restare identificabili: valore
singolare relativo minimo almeno 1e-3. Tutti i record devono restare nelle
stesse fasi e regimi della baseline.

## Esito e limiti

Un PASS significa evidenza completa e riproducibile; l'esito dei confronti è
riportato a parte. Le proposte congelate muovono soltanto le otto direzioni
della prova 6: **nessun passo combinato lungo le 22 direzioni aggiunte è stato
misurato**. Diciassette direzioni non hanno termine di portata perché le loro
sonde non cambiano la portata alle ampiezze campionate; un passo combinato può
comunque spostare il minimo attraverso le capacità, che il modello segue solo
linearmente. Serve quindi una verifica prospettica con proposte nuove prima di
qualsiasi ricerca. I risultati precedenti non vengono riscritti.

Dopo il congelamento,
`python eng/review-r3-repaired-capacity-minimum-model12.py review`
ricostruisce l'analisi senza simulazioni. R3 e M10 restano aperti.
