# Changelog

La cronologia registra le modifiche e gli esiti al momento della loro verifica.
Lo [stato corrente](docs/PROJECT.md), l'[handoff](docs/PROJECT.md#handoff) e la
[roadmap](docs/ROADMAP.md) mantengono rispettivamente checkpoint, ripresa e passi futuri.

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

### Documentazione

- Riscritto il README intorno al simulatore e alle istruzioni d'uso; riscritto
  il manuale italiano con spiegazioni dei componenti, delle interazioni e dei diagrammi.
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
