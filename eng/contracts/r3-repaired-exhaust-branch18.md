# Ricostruzione locale con riserva iniziale dichiarata dell'hotwell

La prova 17 ha osservato quattro ricette sane e nell'inviluppo, ma si è
fermata al controllo di fase del punto iniziale: l'hotwell entra in miscela
durante la preparazione. Il rifiuto e tutti i dati restano congelati. La
minore condensazione riduce la massa che raggiunge l'hotwell, che parte
vicino al confine liquido. Non si rimuove quel controllo o si corregge lo
stato prodotto dall'integratore.

Questa prova distinta aggiunge una riserva iniziale **fissa** di 1e-6 della
massa grezza dell'hotwell, circa 0,00989 kg. Il verso positivo e l'ampiezza
sono già stati misurati nelle sonde unilaterali della base direzionale;
la combinazione con gli inventari di exhaust resta da verificare. La
baseline storica resta invariata per l'identità; ogni altra ricetta ha
esattamente quella riserva, senza adattamenti durante Newton o il runtime.

Si riutilizza **senza modifiche** l'algoritmo della prova 17: stesso punto
iniziale, due coordinate di exhaust, ampiezze delle differenze, obiettivo
del primo checkpoint, cinque iterazioni massime, tre dimezzamenti, guardie
e tolleranze. Non si qualifica una ricerca integrata. Massimo 84 ricette,
26 batch e 304 passi runtime. Ogni punto è ripetuto due volte.

Il nuovo esecutore diagnostico ammette solo massa di exhaust entro ±2%,
energia fra 0 e +4%, e massa di hotwell esattamente zero o +1e-6. Tutte le
altre variazioni sono rifiutate prima della fisica. Il protocollo richiede
zero soltanto per la baseline e +1e-6 per ogni altro punto. Le fixture
coprono il dominio e la riserva; i solutori restano identici. Sorgenti
produttive, test, legge fisica, guadagni e soglie non cambiano.

L'aggiunta di massa riguarda un nuovo stato iniziale diagnostico, non un
reintegro durante la simulazione o una proiezione degli inventari. Non
risolve la stazionarietà dell'hotwell e non garantisce che la sua fase
resti invariata per cento passi. Il dominio delle ricerche precedenti
resta immutato; nessun seed viene installato.

Si conservano e ricostruiscono gli stessi bilanci globali e locali della
prova 17. La baseline completa deve coincidere con quella prova e con
la prova 16, rimosso il solo blocco aggiunto CyclePower per quest'ultima.
Il replay rigenera richieste e scelte adattive, con conteggi cumulativi,
rifiuti e arresti. Una chiusura locale del primo checkpoint resta distinta
dai successivi e dall'equilibrio dei 75 residui.

Contratto, input e budget precedono le chiamate fisiche. Dopo il congelamento,
`python eng/run-r3-repaired-exhaust-branch18.py review` ricostruisce l'esito
senza runtime. R3 e M10 restano aperti.
