# Documentazione del progetto

## Usare il simulatore

Il [manuale utente italiano](usermanual/MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md)
spiega impianto, componenti, interazioni e operazioni di sala controllo.
I [limiti del modello](KNOWN_MODEL_LIMITATIONS.md) ne definiscono il campo educativo.
Per approfondire l'interfaccia sono disponibili la [sala controllo](CONTROL_ROOM_APPLICATION_SHELL.md),
il [computer operatore](OPERATOR_COMPUTER_INTEGRATED_UI.md) e la guida a
[sessioni, checkpoint, replay e salvataggi](OPERATOR_COMPUTER_SESSION_CHECKPOINT_REPLAY_SAVE.md).

## Riprendere il lavoro sul progetto

| Esigenza | Riferimento |
| --- | --- |
| Conoscere baseline, risultato verificato e blocco | [Stato corrente](PROJECT.md#stato-corrente) |
| Sapere che cosa manca alla qualifica integrata | [Passaggi per chiudere R3](PROJECT.md#che-cosa-manca-per-chiudere-r3) |
| Riprendere il lavoro da dati e comandi esatti | [Handoff](PROJECT.md#handoff) |
| Capire le prossime consegne e le condizioni per avanzare | [Roadmap](ROADMAP.md) |
| Sapere che cosa è cambiato | [Changelog](../CHANGELOG.md) |
| Capire che cosa dimostrano i controlli | [Sintesi della validazione](validation/README.md) |
| Consultare i vincoli ingegneristici durevoli | [Decisioni sul modello](validation/MODEL_DECISIONS.md) |

Il checkpoint e l'handoff appartengono alla stessa pagina, `PROJECT.md`.
Le diciture di stato nei documenti storici mantengono il loro significato originario.

## Comprendere il modello

L'[architettura](ARCHITECTURE.md) descrive le responsabilità dei livelli; la
[composizione dell'impianto](PLANT_COMPOSITION.md) collega i sottosistemi.
Le [grandezze fisiche](PHYSICAL_QUANTITIES.md) chiariscono le unità, mentre
l'[orchestrazione della rete](PLANT_NETWORK_ORCHESTRATION.md) spiega come avanza il modello.
Per la regolazione, partire dai [circuiti reattore e primario](REACTOR_PRIMARY_CONTROL_LOOPS.md),
da [turbina, vapore e alimento](TURBINE_STEAM_FEEDWATER_CONTROL_LOOPS.md) e da
[strumentazione, controllo e guasti](INSTRUMENTATION_CONTROL_FAULTS.md).

## Trovare un riferimento

L'[indice completo](TOP_LEVEL_DOCUMENT_INDEX.md) elenca i documenti tecnici attivi.
Le [decisioni architetturali](adr/README.md) conservano le motivazioni del progetto;
i [piani di milestone](ROADMAP.md#4-la-direzione-successiva-m11m15) dettagliano il percorso futuro.
Gli [input dei validatori](../eng/contracts/m10/README.md), le
[evidenze congelate](../eng/frozen-evidence/README.md) e gli
[archivi storici](history/README.md) hanno indici dedicati.
Le [regole documentali](DOCUMENTATION_ARCHITECTURE_AND_INDEXING.md) definiscono
dove mantenere ogni tipo di informazione.
