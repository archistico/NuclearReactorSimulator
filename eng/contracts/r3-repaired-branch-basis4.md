# Direzioni compatibili con i rami della base riparata

La prova tratta le undici colonne non selezionate nella misura estesa 3,
conservandone dati e classificazioni. Non modifica fisica, sorgenti o test.
Il contratto autentica prima dell'acquisizione implementazione, input,
richieste e budget. Si riutilizzano runner e ricetta già verificati.

Per massa ed energia di drum, hotwell e suction e massa di alimento, le
ampiezze base sono 1e-8, 3e-8, 1e-7, 3e-7 e 1e-6. Si misurano entrambi i segni
anche al doppio di ogni ampiezza, eliminando i duplicati. Per massa ed energia
di control-out e turbine-inlet si usano sonde centrali a 1e-7, 3e-7, 1e-6,
3e-6, 1e-5, 3e-5 e 1e-4. Ogni punto e la baseline sono ripetuti due volte:
**394 ricette**, al massimo **1.184 passi runtime** inclusi due passi iniziali
dell'harness. Nessuna acquisizione esistente o parziale è sovrascritta.

Salute, inviluppo, fasi di tutti i nodi, regimi idraulici, del condensatore,
meccanici e del governor devono coincidere con la baseline per ammettere una
sonda. Il controllo vale ai tre checkpoint, oltre alla fase del punto grezzo.
Bilanci e 75 residui sono ricostruiti con le tolleranze precedenti: 0,01 W,
1e-6 kg/s e 1e-6 N·m. I punti sovrapposti alla prova 3 devono avere identici
inventari, residui, fasi e hash dello stato completo. Ripetizioni identiche.

Si confrontano anzitutto gli stencil centrali alle ampiezze base. Per le sette
coordinate con problemi di ramo sono previsti anche stencil unilaterali di
secondo ordine, uno per ogni verso: (-3 f(0) + 4 f(s·h) - f(s·2h))/(2h).
Questa è la derivata orientata lungo s, non la derivata lungo la coordinata
positiva quando s è negativo. Entrambi i punti dello stencil devono essere
ammessi; non si usa il punto di un altro ramo per completare la formula.

Ogni tipo di stencil e verso confronta tutte le coppie di ampiezze base
adiacenti. Pesi, 81 righe diagnostiche e limite relativo del 10% restano quelli
della prova 1. Si preferisce una coppia centrale validata, scegliendo la
maggiore ampiezza; solo in assenza di coppie centrali validate si sceglie la
maggiore coppia unilaterale validata, preferendo il verso positivo in caso
di parità. Tutti i confronti sono conservati. Le quattro coordinate degli
inventari di linea vapore qui trattate usano solo stencil centrali.

Le nuove colonne integrano le 19 già selezionate nella prova 3. Per quelle
unilaterali si registrano verso e vincolo di coefficiente non negativo:
non diventano coordinate libere nei due versi. L'ampiezza massima campionata
non prova la compatibilità dell'intero intervallo o della somma di direzioni.
Si riporta il medesimo rango numerico diagnostico normalizzato, con soglia
1e-8, senza attribuirgli un certificato di rango, fattibilità del cono o
sufficienza della famiglia. I 75 residui e il criterio di radice non cambiano.

Compilazione, fixture su stencil, orientamento e selezione e preflight senza
chiamate fisiche precedono l'acquisizione. Nessuna proposta, ricerca o nuovo
seed è previsto. L'analisi è separata dall'acquisizione; dopo il congelamento,
`python eng/run-r3-repaired-branch-basis4.py review` ricostruisce l'esito
senza nuovi passi runtime. R3 e M10 restano aperti.
