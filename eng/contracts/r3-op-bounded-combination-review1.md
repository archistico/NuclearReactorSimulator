# Revisione vincolata della combinazione non fluida

La [diagnostica delle altre grandezze](r3-op-remaining-quantities-sensitivity1.md)
ha congelato l'esito `NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL`. La
previsione richiedeva però passi fino a −6.077% sulla pompa di condensato,
perché popolazione neutronica e temperature di combustibile e struttura hanno
colonne pesate collineari. Il suo contratto non vincolava il passo. Questa
revisione ripete il confronto entro limiti dichiarati prima del calcolo, come
richiesto dalle [decisioni ingegneristiche](../../docs/validation/MODEL_DECISIONS.md#classificare-i-modelli-lineari-entro-la-regione-delle-sonde).

È una revisione offline: non chiama l'impianto, non cambia codice di test o
di produzione, non seleziona una famiglia e non cerca una radice.

## Ingressi

- Residui e Jacobiano degli inventari compatibile di fase del record iniziale
  di H feasibility 1, come nelle diagnostiche precedenti.
- Le colonne centrali a 1e-4 delle sei grandezze non fluide, ricostruite dalle
  tracce congelate della [valvola](r3-op-control-valve-sensitivity1.md) e
  delle [altre grandezze](r3-op-remaining-quantities-sensitivity1.md), ciascuna
  autenticata dalla propria ricevuta. Le baseline devono coincidere.
- I pesi fisici della revisione di sufficienza: 1 kg/s, 2,5 MW, 100 N·m.

## Direzione del nocciolo

Le tre colonne del nocciolo spostano tutte l'energia di `outlet` con lo stesso
segno. Il raggruppamento le muove con una frazione comune: la colonna è la
somma delle tre. La revisione riporta i coseni pesati fra le tre colonne.

## Solutore

Minimi quadrati pesati con limiti sulle variabili, a insieme attivo, a partire
dall'origine ammissibile. I sottoproblemi liberi usano lo stesso QR di
Householder in binary64 della revisione di sufficienza; le colonne sono
equilibrate solo per il condizionamento. Ogni soluzione riporta un certificato
KKT: gradiente nullo sulle variabili libere e segno corretto su quelle al
limite, entro 1e-8 relativo, al più 500 iterazioni. Sei casi analitici, uno
con rilascio di un limite, precedono il calcolo.

## Modelli

| Modello | Inventari | Grandezze non fluide | Uso |
| --- | --- | --- | --- |
| `inventories/bounded` | ±2% | nessuna | denominatore |
| `grouped/probe-bounds` | ±2% | valvola, due pompe, nocciolo raggruppato: ±1e-2 | classificazione |
| `ungrouped/probe-bounds` | ±2% | sei grandezze separate: ±1e-2 | controllo del raggruppamento |
| `grouped/operating-bounds` | ±2% | come il raggruppato: ±2% | solo riportato |

±2% è il box della famiglia H; ±1e-2 è l'ampiezza massima effettivamente
sondata sulle grandezze non fluide. Il modello a ±2% esce dalla regione delle
sonde e non partecipa alla classificazione.

## Esiti

Il rapporto di un modello è il suo massimo tasso di energia previsto diviso
quello di `inventories/bounded`. Una riduzione è materiale se il rapporto è al
più del 50%.

- `INCONCLUSIVE-SOLVER-CERTIFICATE`: un modello di classificazione non ha
  certificato KKT.
- `INCONCLUSIVE-GROUPING-SENSITIVE`: il modello raggruppato e quello separato
  danno esiti di materialità diversi.
- `BOUNDED-NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL`: il modello
  raggruppato è materiale; motiva soltanto la pianificazione di una famiglia
  che includa quelle grandezze, con protocollo distinto.
- `NO-BOUNDED-NONFLUID-COMBINATION-MATERIAL-IN-LINEAR-MODEL`: nessuna
  combinazione entro i limiti è materiale; si conserva l'esito e si rivede la
  legge di energia dello stadio prima di altre ricerche.

Ogni modello riporta passo, limiti attivi, metrica fisica e tassi di energia
previsti lungo `header`, `stop-out`, `control-out` e `turbine-inlet`. Le
previsioni lineari non sono osservazioni: il Jacobiano degli inventari è
misurato a 1e-6 e passi di qualche punto percentuale sono fuori dalla regione
descritta da quelle sonde. Nessun esito cambia scale, residui, box, inviluppo,
guadagni, C4 o Exact-V9, né promuove R3 o M10.

## Comandi

`python eng/review-r3-op-bounded-combination1.py static` autentica il contratto
ed esegue i casi analitici. `execute` scrive
`artifacts/r3-op-bounded-combination-review1/01-analysis.json`. Dopo la
revisione esplicita dell'esito il file viene congelato e `review` ne esegue il
replay indipendente.
