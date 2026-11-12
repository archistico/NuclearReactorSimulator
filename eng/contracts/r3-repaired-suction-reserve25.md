# Riserva iniziale accoppiata in suction

La prova 24 rispetta inviluppo operativo e limiti termici, con errori numerici
di previsione sotto soglia, ma entrambe le proposte passano da liquido sottoraffreddato
a miscela satura in suction al primo Running. Rimangono escluse. Nel suo
passo ottimo L1 le due variazioni di suction sono entrambe zero; nella prova
23 erano entrambe positive e le fasi erano conservate. Questo confronto
motiva una prova distinta della riserva iniziale accoppiata, senza riscrivere
il rifiuto della prova 24 o qualificare un margine di fase generale.

## Unica aggiunta alla formulazione fisica della proposta

Si conservano obiettivo sui 75 residui originali, derivate operative,
inviluppo interno del 10%, margine termico di 10 W, vincoli delle capacità,
ampiezze e centro della prova 24. Nella soluzione del passo completo si
fissano a uno i coefficienti normalizzati delle due direzioni suction.
Massa ed energia iniziali ricevono quindi entrambe la frazione **+1e-6**,
già misurata nelle sonde originarie. La loro proporzione mantiene l'energia
interna specifica iniziale del nodo; diminuisce il volume specifico.

Questa riserva è parte dell'inventario iniziale. Non reintegra massa o energia
durante il runtime, non cambia i controllori né l'algoritmo termodinamico.
Le due proposte restano metà e intero passo: frazioni +0,5e-6 e +1e-6 in
suction, circa 3,75 g e 7,50 g di massa iniziale in più. La grafia del nodo
nei dati rimane `suction`.

I coefficienti fissati vengono eliminati algebricamente prima dei due
programmi lineari, trasferendone il contributo a residui e termini noti,
poi ripristinati nel risultato. Questa scelta evita le variabili degeneri
senza allentare certificati o vincoli. Il contributo L1 delle variabili
fisse è costante, quindi la loro eliminazione non cambia la scelta del
minimo. Una fixture analitica verifica soluzione e valore ottimo del
problema ridotto, oltre alle frazioni iniziali richieste.

## Misura e giudizio

Il budget è fisso: **otto ricette**, centro, baseline storica e due
proposte ripetute, un batch e al massimo **26 passi runtime**. Si riusa
la console e il dominio diagnostico della prova 20. Contratto, previsioni,
richieste e hash degli assembly precedono tutte le chiamate fisiche.

I criteri restano quelli della prova 24: accordo sulle variazioni dei
75 residui, delle 81 righe diagnostiche e delle capacità entro il 10%;
errore operativo entro l'1% dell'ampiezza dell'intervallo originario;
salute, inviluppo, fasi e regimi conservati, expansion limitante e almeno
metà margine delle capacità. Per l'ammissibilità alla revisione successiva
servono anche riduzione reale del massimo originale e limiti termici
misurati rispettati. Non basta che la riserva sia stata dichiarata:
la permanenza di fase deve risultare dalle nuove misure a tutti i checkpoint.

Il replay `python eng/run-r3-repaired-suction-reserve25.py review` ricostruisce
formulazione e giudizio dai dati congelati senza simulazioni. Nessun punto
è installato o accettato come aggiornamento di una ricerca integrata.
Tre checkpoint non sono conferme di 100 passi o qualificazione R3.
