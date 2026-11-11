# Progetto — stato corrente e punto di ripresa

## Lettura del checkpoint

Il prodotto esistente usa il desktop Exact-V9, con sala controllo, missioni,
salvataggi e replay. L'attività fisica aperta riguarda l'integrazione della nuova
chiusura termodinamica reference-consistent, disponibile come opzione esplicita.
Implementazione e verifica locale sono acquisite; l'equilibrio dell'impianto
completo con quella composizione non è ancora qualificato.

La [roadmap](ROADMAP.md) descrive le prossime consegne e il percorso fino a M15.
Questa pagina conserva invece gli esiti verificati e il punto dal quale riprendere.

## Checkpoint di pianificazione — 11 novembre 2026

Le evidenze numeriche sottostanti restano quelle congelate il 10 novembre.
L'aggiornamento odierno chiarisce il piano; non introduce nuove valutazioni fisiche.

NRS-MARKER:R3-H-LOCAL-MODEL-DESIGN1-CURRENT

NRS-MARKER:R3-H-NUMERICAL-STOP-REVIEW1-CURRENT

### Che cosa è acquisito

La ricerca H sugli inventari conservati è stata eseguita con i limiti e i criteri
congelati: 24 incognite fluide, 75 residui, 527 tentativi contabilizzati e sette
aggiornamenti accettati. L'esito è `H-INCONCLUSIVE-NO-CANDIDATE-IN-BOUNDS`.
Non è stato trovato uno stato iniziale qualificato e non è stata dimostrata
l'impossibilità fisica di trovarlo.

La **Numerical Stop Review 1** è `PASS-REVIEW-EVIDENCE-COMPLETE`. La ricostruzione
dell'ultimo modello lineare localizza i 78 rifiuti terminali prima del valutatore
fisico, sul limite invariato dell'energia di aspirazione. Le sei direzioni complete
registrate sono identiche e gli smorzamenti perdono effetto nelle matrici ricostruite.
Tre sonde di derivata cambiano etichetta di fase: la regolarità fisica locale resta
da accertare.

La **Local-Model Design 1** è `PASS-OFFLINE-DESIGN-EVIDENCE`. La scalatura delle
colonne e una sola frazione globale rispetto al limite producono sei direzioni
ammissibili, con tutte le 24 diagonali sensibili agli smorzamenti. Sei casi numerici
indipendenti verificano la trasformazione e il trattamento dei limiti.

### Che cosa significa il confronto offline

Le sei previsioni riducono il massimo residuo locale da circa 7,226 milioni a valori
fra 6,908 e 7,172 milioni: una riduzione prevista compresa fra circa 0,74% e 4,40%.
Il valore inferiore non seleziona un candidato fisico: è una previsione lineare,
ancora lontana dal criterio congelato di radice. Non sono state eseguite nuove
valutazioni non lineari per verificarla.

Gli input e gli output compatti sono conservati sotto
`eng/frozen-evidence/r3-h-numerics1` e `eng/frozen-evidence/r3-h-local-model1`.
La traccia completa H resta nel proprio archivio originale. I casi negativi
congelati documentano il rifiuto di ampliamenti di autorità, limiti o input.

### Prossima attività

Completare la revisione dei sei confronti offline e dei cambi di fase, decidendo
come usare e verificare le previsioni. Solo dopo si congela un protocollo limitato
ai test per un'eventuale nuova indagine non lineare. La roadmap esplicita consegne,
condizioni di uscita e arresto se l'esito resta inconclusivo.

Non sono attivi una nuova ricerca H, ulteriori sonde fisiche, ampliamenti del
dominio, nuove incognite, modifiche ai controllori o allo stato iniziale produttivo.
Le altre famiglie C/J rimangono non selezionate. **R3 resta RED**; la
Requalification 3 e R4 restano subordinate alle rispettive condizioni d'ingresso.

## Controlli correnti e riferimenti

```powershell
.\scripts\run-r3-h-numerical-stop-review1.cmd
.\scripts\run-r3-h-local-model-design1.cmd
```

Sono verifiche offline: autenticano gli input, controllano i 962 file di produzione
e i 406 file di test e ricostruiscono il confronto numerico senza chiamare il modello
fisico. Il secondo comando ripete anche la revisione numerica prerequisita.
Non costituiscono una nuova esecuzione della ricerca fisica o della suite ordinaria.

| Riferimento | Contenuto |
| --- | --- |
| [Ricerca H](../eng/contracts/m10/M10_FINAL_VR2_R3_H_CONSERVED_INVENTORY_FEASIBILITY1.md) | Protocollo ed evidenze della ricerca limitata già conclusa |
| [Revisione dello stop](../eng/contracts/m10/M10_FINAL_VR2_R3_H_NUMERICAL_STOP_REVIEW1.md) | Ricostruzione offline del limite numerico terminale |
| [Confronto locale scalato](../eng/contracts/m10/M10_FINAL_VR2_R3_H_LOCAL_MODEL_DESIGN1.md) | Sei smorzamenti, vincoli e casi numerici indipendenti |
| [Sintesi della validazione](validation/README.md) | Risultati, campo delle conclusioni e problema aperto |

## Lavori di documentazione conclusi

La documentazione M10 è stata separata dagli input esatti dei validatori, raccolti
in `eng/contracts/m10`. Il README descrive il prodotto e il manuale italiano è
stato riscritto con spiegazioni dei componenti, delle interazioni e dei diagrammi.
I controlli testuali storici del manuale leggono la loro copia congelata.

Questi interventi non cambiano fisica, test o identità produttive e non promuovono
R3 o M10. I checkpoint precedenti restano consultabili nel
[registro degli archivi](history/archives/README.md#snapshot-docs-project-md).
