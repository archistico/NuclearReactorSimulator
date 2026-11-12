# Verifica prospettica delle prime proposte nel nuovo punto

Le prove 20–21 hanno ricostruito trenta direzioni nel punto diagnostico 19:
28 centrali e due unilaterali. Il rango numerico 30 e l'accordo fra ampiezze
non garantiscono che la somma delle risposte preveda una proposta composta.
Questo protocollo verifica due passi piccoli prima di qualsiasi ricerca.

## Modello e scelta congelati prima dell'acquisizione

Il modello è affine nelle direzioni rimisurate, sulle stesse 81 righe
pesate: 75 residui originali più sei righe diagnostiche dei corpi termici.
Le quindici capacità di portata vengono previste separatamente con le
derivate nuove. Non si riusa il modello del minimo stimato nel vecchio punto.

Il medesimo programma lineare certificato della prova 6 minimizza il
massimo assoluto previsto, con passo normalizzato in [-1,1] per le 28
direzioni centrali e [0,1] per le due unilaterali. Le ampiezze sono quelle
selezionate nelle prove 20–21. Il criterio secondario minimizza la norma
L1 del passo normalizzato con l'identica tolleranza numerica della prova 6.
Questa metrica diagnostica non sostituisce il criterio originale di radice.

Prima di misurare, si limita la frazione del passo affinché ogni differenza
prevista tra una capacità concorrente e quella expansion conservi almeno
metà del margine del centro, in tutti e tre i checkpoint. Il limite della
frazione intera non supera uno. Si eseguono il 25% e il 50% di questa
frazione ammessa. La regola usa soltanto le misure precedenti; non può
essere adattata agli esiti della nuova prova. Se il modello non ha una
variazione non nulla o il centro non ha margine stretto, la prova non parte.

## Misura, giudizio e limiti

Si ripetono baseline storica, centro e due proposte: **otto ricette** in
un batch, al massimo **26 passi runtime**. Si riusa senza modifiche la
console della prova 20; le variazioni aggregate devono restare entro il
suo dominio diagnostico di ±0,0002 rispetto al centro. Nessun parametro
fisico, guadagno, sorgente produttiva o file di test viene modificato.

Una previsione è conforme soltanto se la variazione misurata delle 81
righe concorda entro il 10% con quella prevista, la variazione delle 15
capacità concorda entro il 10% e il vincitore osservato resta expansion
in tutti i checkpoint. Per il confronto delle capacità si usa un
pavimento di 1e-9 kg/s sulla norma prevista. Restano necessarie salute,
inviluppo, fasi e regimi originari. Un fallimento viene conservato, senza
proiezioni, correzioni o ripetizioni adattive della proposta.

Le previsioni confrontano variazioni, non il rapporto con un residuo finale
quasi nullo. Si riportano anche massimo dei 75 residui originali, potenze
globali e tassi fisici di exhaust. I bilanci indipendenti e le ripetizioni
mantengono i controlli delle prove precedenti. Centro e baseline devono
coincidere integralmente con i record congelati, tolti gli identificativi.

Contratto, modello, richieste e hash degli assembly precedono le chiamate
fisiche. Il replay `python eng/run-r3-repaired-local-proposals22.py review`
ricostruisce il giudizio senza simulazioni. Anche un risultato conforme
vale per questi due passi frazionari: nessuna ricerca integrata, nuovo
dominio di ricerca, conferma di 100 passi, installazione di seed o R3 PASS.
