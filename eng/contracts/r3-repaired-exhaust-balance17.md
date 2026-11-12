# Ricostruzione diagnostica dei due inventari di exhaust e bilancio globale

Le prove 15–16 hanno misurato il segno di una combinazione di energia e coppia,
senza annullare l'accumulo di massa. La presente prova ricostruisce insieme
massa ed energia interna di exhaust e osserva le potenze dell'intero ciclo.
Non modifica fisica, altri stati iniziali, test, soglie o dominio dei vecchi
esperimenti. Contratto, algoritmo, input e budget sono autenticati prima
della prima chiamata fisica. Nessun punto è installato nel desktop.

## Dominio e obiettivo locale

Solo le coordinate 4 e 5 variano: massa di exhaust entro ±2%, energia fra
0 e +4%, con tutte le altre coordinate e grandezze non fluide alla baseline.
È il dominio diagnostico esteso già motivato, ora con massa variabile nel
limite precedente. Non è un nuovo box di ricerca integrata.

Il bersaglio è il **solo primo checkpoint**: |dM/dt| ≤1e-6 kg/s e
|dU/dt| ≤0,01 W in exhaust. Questi limiti diagnostici non sostituiscono
le scale o il criterio originale dei 75 residui. Si riportano tutti i tre
checkpoint, i corpi termici, la coppia, le capacità e gli altri inventari.

## Procedura congelata prima dell'acquisizione

La prima chiamata misura baseline e punto iniziale (massa −0,8%, energia
+3,5%), ciascuno ripetuto due volte. Il punto iniziale è una proposta di
partenza, non un equilibrio presunto. Si usa Newton su dM/dt in kg/s e
dU/dt in MW, con un massimo di cinque aggiornamenti. Ogni aggiornamento:

1. misura sonde centrali a ±1e-5 sulle due coordinate correnti, ripetute;
2. verifica salute, inviluppo, fasi, rami già controllati e ricostruzione
   delle capacità lungo l'intera ricetta, senza imporre un vincitore fisso;
3. risolve il sistema 2×2, rifiutandolo se il determinante relativo è ≤1e-12;
4. prova il passo intero, poi al massimo tre dimezzamenti, conservando il
   dominio. Ogni proposta fisica è ripetuta e accettata soltanto con gli
   stessi controlli e una riduzione stretta della norma delle due righe.

Sonde fuori dominio, guardie non conformi, modello singolare, mancata discesa
o fine del budget fermano la ricostruzione con il proprio esito. Non si
proiettano inventari o stati; un passo fuori dominio è registrato senza
chiamata fisica. La norma diagnostica non è il criterio di radice R3.

## Budget e tracciabilità

Al massimo **84 ricette**, **26 batch** e **304 passi runtime**: quattro
ricette iniziali, fino a cinque batch di otto ricette per le derivate e
venti batch di due per le proposte. Ogni ricetta esegue tre passi da 10 ms;
ogni batch aggiunge due passi iniziali dell'harness. La console controlla
dominio, richieste, contratto e assembly prima della fisica. La procedura
controlla e ricostruisce i conteggi cumulativi.

Richieste, risposte, scelte e rifiuti sono tracciati; nessun file parziale
è sovrascritto. Il replay rigenera le stesse richieste adattive e le stesse
decisioni. Ogni punto ha due record identici, tolto l'identificativo. La
baseline coincide con la prova 16 dopo aver rimosso il solo blocco aggiunto
`CyclePower`. Un problema di analisi non autorizza nuove acquisizioni.

## Bilanci osservati indipendentemente dall'obiettivo locale

La nuova console legge gli audit del passo senza cambiare i solutori.
Registra sorgente nucleare, confini primari, sorgenti generiche, lavoro
idraulico delle pompe, condizionamento dell'alimentazione, condensatore,
turbina, generatore, esportazione elettrica, perdite e inventari termici
e cinetici. Si ricostruisce:

- la somma di energia fluida, corpi termici e rotore dagli inventari;
- la potenza netta esterna dalle singole sorgenti e dai singoli pozzi;
- lo scambio turbina-rotore-generatore e la conversione elettrica;
- la conservazione globale di massa ed energia.

Tolleranze: 0,01 W e 1e-6 kg/s, oltre ai controlli meccanici precedenti
entro 1e-6 N·m. Conservazione non significa stazionarietà: l'audit può
chiudere con un accumulo globale molto diverso da zero. Si riporta anche
quanto dovrebbe variare la sorgente se le altre potenze restassero fisse;
è un confronto algebrico, non una nuova ricetta o un comando ai controllori.

Compilazione, fixture di dominio, Newton, guardie, singolarità e primo
principio precedono l'acquisizione. Dopo il congelamento,
`python eng/run-r3-repaired-exhaust-balance17.py review` ricostruisce
l'esperimento senza runtime. Una ricostruzione locale conforme non
qualifica l'equilibrio integrato, R3 o la chiusura M10.
