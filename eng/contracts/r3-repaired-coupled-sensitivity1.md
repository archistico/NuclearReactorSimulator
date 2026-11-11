# Sensibilità accoppiate sul punto C4 riparato

La prova misura direzioni locali nuove dopo la riparazione del trasporto di fase.
Parte dallo stesso vettore grezzo, mantiene la ricetta di due passi preparatori
e un Running da 10 ms e non installa seed. Il contratto JSON, scritto prima
dell'acquisizione, autentica implementazione, input congelati e alberi `src`/`tests`.
La console separata usa mediante riflessione le stesse funzioni interne dell'harness
esistente; sorgenti e test non cambiano. Non viene eseguito un altro test runner.

Si osservano nove direzioni indipendenti: massa ed energia di `pressure`, `outlet`
ed `exhaust`, popolazione neutronica e temperature iniziali di combustibile e
struttura. Le coordinate fluide sono frazioni degli inventari grezzi; quelle
non fluide seguono la convenzione già esistente, compresa la frazione della
temperatura in gradi Celsius. Nessuna variazione di geometria, UA o legge fisica.

Una decima direzione verifica la sovrapposizione fra le tre grandezze termiche.
Alla frazione ε della popolazione associa incrementi di temperatura
ε·Q_fuel/G_fuel e ε·Q_structure/G_structure, convertiti nelle frazioni Celsius.
Le potenze e le conduttanze provengono dalla diagnosi congelata precedente.
È una sonda combinata dichiarata prima delle misure, non una proposta di equilibrio.

Ogni direzione ha sonde centrali ±1e-5 e ±1e-4. Baseline e ciascuna sonda sono
ripetute due volte: **82 ricette**, al massimo **248 passi runtime**, inclusi i
due passi di inizializzazione dell'harness eseguiti una sola volta. Richieste
e limiti vengono verificati prima di istanziare l'harness. Un file di misura
esistente o parziale non è sovrascritto; gli errori di infrastruttura interrompono
l'acquisizione. I rifiuti fisici previsti vengono registrati e non ricevono derivate.

Per tutti i tre checkpoint si conservano inventari, 75 residui originali, hash
dello stato completo, salute, inviluppo, regimi idraulici e limiti del condensatore.
Si aggiungono inventari termici, sorgenti nucleari e scambi, oltre ai trasferimenti
dello scarico turbina e del condensatore. Il replay ricostruisce tutti i residui
originali, i sei tassi dei corpi termici e i bilanci termici e di exhaust entro
0,01 W e 1e-6 kg/s. I dati di baseline devono coincidere esattamente con la misura
congelata della riparazione, inclusi gli hash ai tre checkpoint.

Una colonna centrale è disponibile soltanto con entrambi i segni sani,
nell'inviluppo originale, con le 48 fasi della baseline e gli stessi regimi
idraulici e limiti del condensatore lungo la ricetta. Non si sostituisce una
colonna mancante con uno stencil diverso. La colonna primaria a 1e-4 è validata
solo se differisce da quella a 1e-5 di non oltre il 10% nella norma euclidea
pesata. Per questo confronto diagnostico, 1 kg/s, 1 MW, 1 N·m e 1 MW termico
hanno peso unitario. I sei tassi termici integrano i 75 residui, senza modificare
le scale o il criterio di radice originali.

Il risultato riporta separatamente disponibilità, linearità, risposte fisiche
e coseni fra le tre colonne non fluide, prima e dopo l'inclusione dei corpi
termici. Se le colonne sono validate, la sonda combinata è confrontata con la
somma delle tre risposte indipendenti, alla stessa tolleranza del 10%.
Un PASS del pacchetto indica completezza e ricostruibilità dell'evidenza:
non implica che le colonne siano utilizzabili o che una famiglia sia sufficiente.
Non si calcolano proposte, minimi o ricerche; nessuna estrapolazione oltre le
ampiezze misurate. Nove direzioni non costituiscono una base completa del ciclo.

Compilazione, fixture algebriche e verifica della console in modalità
`--validate-only` precedono l'acquisizione. Le identità degli assembly caricati
sono registrate nella ricevuta preliminare. L'analisi è separata dall'acquisizione:
un problema di analisi non autorizza nuove chiamate fisiche. Dopo il congelamento,
`python eng/run-r3-repaired-coupled-sensitivity1.py review` ricostruisce l'esito
senza eseguire il runtime. R3 e M10 restano aperti.
