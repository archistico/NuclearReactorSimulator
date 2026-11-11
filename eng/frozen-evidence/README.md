# Evidenze congelate

Questa directory conserva gli input e i risultati immutabili necessari ai
validatori storici, ai test ordinari e ai replay delle indagini revisionate.
Lo [stato corrente e l'handoff](../../docs/PROJECT.md) indicano quali controlli
usare oggi; questo indice descrive come trovare e autenticare le evidenze.

## Raccolte

| Percorso | Contenuto e uso |
| --- | --- |
| `ordinary/` | File compatti congelati consumati direttamente dai test. |
| [large-payload-manifest.csv](large-payload-manifest.csv) | Identità SHA-256 canoniche delle tracce storiche di grandi dimensioni. |
| [archive/](archive/README.md) | Contenitori compressi e istruzioni per ripristinare i payload storici quando un validatore li richiede. |
| `r3-h-*/`, `r3-op-*/`, `r3-stage-*/` | Pacchetti delle indagini su inventari, punti operativi e trasporto: dati, esiti e manifest; le tracce dei batch sono compresse. |
| `r3-repaired-*/` | Diagnosi del punto riparato, basi centrali e unilaterali, direzioni accoppiate, proposte, attribuzione al minimo delle portate, osservazione delle capacità a tutti i checkpoint e modello offline del minimo. Ogni pacchetto mantiene il proprio risultato; le previsioni affini della prova 6 restano fallite. |

Le grandi tracce storiche descritte dal manifest non sono incluse espanse
nei pacchetti sorgente del candidato. La vecchia directory di test
`tests/NuclearReactorSimulator.Application.Tests/Scenarios/Gameplay/Evidence`
non è una dipendenza del pacchetto sorgente.

## Identità dei protocolli di indagine

Per un protocollo `r3-…`, il contratto macchina è `eng/<protocollo>-contract.json`
e la ricevuta è `eng/<protocollo>-returned.json`. La ricevuta autentica il
contratto e il manifest del pacchetto. Il manifest registra nome logico,
dimensione e hash dei byte originali, oltre al nome e all'hash del file conservato.
Il replay verifica anche i dati decompressi prima di ricostruire l'esito.

Le specifiche leggibili sono in `eng/contracts/`. Il
[checkpoint](../../docs/PROJECT.md#evidenze-e-verifiche) collega i protocolli
applicabili; i nomi e le baseline dei protocolli precedenti restano storici.
Un pacchetto integro o un replay PASS non qualificano da soli la fisica.

Gli output in lavorazione rimangono in `artifacts/`. Una nuova evidenza viene
conservata qui dopo una revisione esplicita del contratto e dell'esito;
manifest, ricevute e payload restituiti non si sovrascrivono.

## Riferimento storico RP1B

`M10FinalVR2EngineeringRepairPlanning1_RP1B_PerformanceReplanning1_UserReturnedAudit.txt`
è l'audit `PASS-AS-AUTHORED` del protocollo di misura fra processi Refinement 5;
il corrispondente `UserReturnedSummary.txt` ne conserva la sintesi di navigazione.
Quel risultato riguarda il proprio ambito originario e non è il checkpoint
corrente né un'attivazione produttiva della termodinamica C4.
