# Organizzazione e indicizzazione della documentazione

## Scopo e riferimenti autorevoli

Ogni pagina ha una responsabilità precisa. Il lettore deve poter distinguere
descrizione del prodotto, risultato verificato, istruzioni di ripresa, piani
futuri e cronologia.

| Informazione | Riferimento autorevole |
| --- | --- |
| Progetto, funzionalità e avvio | [README del progetto](../README.md) |
| Baseline, risultato corrente e controlli applicabili | [PROJECT.md](PROJECT.md#stato-corrente) |
| Ripresa fra sessioni e passaggio di consegne | [Handoff in PROJECT.md](PROJECT.md#handoff) |
| Consegne e dipendenze future | [ROADMAP.md](ROADMAP.md) e piani di milestone |
| Cronologia delle modifiche | [CHANGELOG.md](../CHANGELOG.md) |
| Responsabilità stabili dei livelli e sottosistemi | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Campo e limiti del modello | [KNOWN_MODEL_LIMITATIONS.md](KNOWN_MODEL_LIMITATIONS.md) |
| Motivazioni delle decisioni architetturali | [ADR](adr/README.md) |
| Criteri e limiti delle conclusioni di validazione | [Sintesi](validation/README.md) e [decisioni](validation/MODEL_DECISIONS.md) |
| Ricerca dei documenti tecnici attivi | [Indice completo](TOP_LEVEL_DOCUMENT_INDEX.md) |
| Provenienza e documentazione superata | [history/](history/README.md) |
| Guida operativa | [Manuale utente](usermanual/MANUALE_UTENTE_NUCLEAR_REACTOR_SIMULATOR.md) |

Il checkpoint resta in `PROJECT.md`: le altre guide lo collegano quando serve
lo stato corrente. L'handoff appartiene alla stessa pagina; evitare documenti
paralleli `CURRENT_STATUS`, `CHAT_HANDOFF` o `RESTART` che possano divergere.
Il changelog conserva gli esiti con la loro data, mentre la roadmap descrive
le condizioni per il lavoro futuro.

## Guide, contratti e artefatti

`docs/README.md` è l'ingresso ragionato; `TOP_LEVEL_DOCUMENT_INDEX.md` elenca
tutti i riferimenti attivi al primo livello. Ogni sottosistema mantiene una
descrizione coerente con le proprie responsabilità. L'architettura è organizzata
per competenze; la cronologia per milestone è conservata nel
[registro storico](history/ARCHITECTURE_MILESTONE_LEDGER.md).

Le checklist di accettazione possono rimanere attive finché servono alla fase
corrente, distinguendole dai riferimenti architetturali durevoli. Gli input
testuali esatti dei vecchi validatori sono indicizzati in
[eng/contracts/m10](../eng/contracts/m10/README.md). Le nuove indagini mantengono
specifiche e identità sotto `eng/`; i risultati revisionati sono descritti
nell'[indice delle evidenze congelate](../eng/frozen-evidence/README.md).
Gli output locali di lavoro restano in `artifacts/`.

Il README delle evidenze è navigazione modificabile. Manifest, payload,
ricevute e input autenticati conservano invece i byte del protocollo.
Aggiornare una guida non richiede di riscrivere quei risultati.

## Decisioni e storia

L'indice ADR mantiene stato, area, numerazione e riferimenti di orientamento.
I singoli ADR conservano le motivazioni originali: la loro cronologia non è
il risultato corrente della compilazione o della validazione.

I documenti storici possono intenzionalmente citare percorsi precedenti.
Questi riferimenti descrivono la provenienza; le guide attive usano collegamenti
validi o percorsi espliciti dalla radice del repository. Prima di compattare
una nota occorre verificarne i consumatori e conservarne contenuto e identità
secondo le [regole dell'archivio](history/README.md).

## Verifiche per il consolidamento della release

M11.5 mantiene l'automazione prevista per verificare:

- risoluzione di collegamenti e ancore Markdown attivi;
- presenza di ogni `docs/*.md` nell'indice completo;
- numerazione ADR univoca e contigua, stato dichiarato e presenza nell'indice;
- checkpoint concentrato in `PROJECT.md`, con roadmap rivolta al futuro e
  architettura priva di stati transitori del candidato;
- coerenza dei commenti sorgente sulle responsabilità con l'architettura stabile.

Una revisione documentale riporta i controlli realmente eseguiti, distinguendoli
da compilazioni, prove fisiche o accettazioni effettuate in precedenza.
