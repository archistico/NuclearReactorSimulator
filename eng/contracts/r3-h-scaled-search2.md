# H2: ricerca scalata con soluzione vincolata del sottoproblema

La Local Trial 1 ha riprodotto 14 ricette: tutte le sei direzioni migliorano
il residuo reale, con rapporti riduzione reale/prevista fra 0.9978 e 1.0002.
Il punto migliore è lambda-0.01 (6,909,148.735315985), ancora fuori inviluppo.
È il punto di partenza deterministico di questa nuova ricerca, non un seed
qualificato. I cambi di fase restano registrati e non vengono interpretati
come prova di differenziabilità.

La ricerca conserva le 24 coordinate e i 75 residui originali, il box ±0.02
e la soglia di radice 1. Non modifica produzione, guadagni o controllori.
Risolve per ciascuno dei sei damping originari il problema ai minimi quadrati
scalato con vincoli sulle variazioni, anziché lasciare che una direzione
esterna al box determini da sola la lunghezza del passo globale. La soluzione
usa un active set deterministico, con rilascio dei vincoli secondo le condizioni
KKT e arresto tecnico del sottoproblema dopo 256 iterazioni. I casi numerici
verificano soluzione interna, optimum sul bordo, accoppiamento, colonna nulla
e scale molto diverse prima di ogni esecuzione fisica.

Le colonne sono normalizzate con norma euclidea; i residui non sono ripesati.
La regolarizzazione è sul passo scalato. Ogni direzione è ricondotta all'interno
con una sola frazione globale 0.99 del limite ammesso, mai con clipping delle
coordinate del punto fisico. Si valutano quattro frazioni della direzione:
1, 1/2, 1/4 e 1/8, per tutti e sei i damping nell'ordine congelato. Si sceglie
il minimo massimo residuo sano, con spareggio sull'ordine di valutazione,
soltanto se diminuisce strettamente rispetto al punto corrente.

Il Jacobiano usa differenze centrali di ampiezza 1e-6 dove entrambe le sonde
sono nel box. Al bordo usa una differenza unilaterale rivolta all'interno con
la stessa ampiezza e il residuo corrente. Non inventa derivate da sonde rifiutate:
un rifiuto arresta la ricerca con esito inconclusivo. Tutte le fasi sono conservate.

Budget: al massimo 4096 ricette, inclusi i rifiuti, 64 aggiornamenti e 129
batch. Ogni batch costruisce un harness (due passi preparatori) e ogni ricetta
esegue al massimo tre passi. Il limite totale è quindi 12546 passi fisici.
Si arresta anche per nessuna discesa, sottoproblemi tutti rifiutati, budget,
residuo massimo ≤1 o miglioramento relativo ≤1e-10 per tre aggiornamenti
consecutivi. Errori inattesi e divergenze della riproduzione sono errori tecnici.
L'esecuzione del punto iniziale deve riprodurre esattamente i residui e le fasi
della Local Trial 1 prima di iniziare le nuove sonde.

Una radice osservata richiede un successivo protocollo di conferma indipendente
di 100 passi; questa ricerca non la qualifica automaticamente, non installa
inventari e non avvia R3. Un esito inconclusivo non prova impossibilità fisica
e non autorizza ulteriori ricette o un ampliamento dei limiti. In quel caso
si analizzano i residui dominanti e i vincoli attivi prima di scegliere una
diversa famiglia di intervento.

Il validatore verifica sorgenti, i 407 test preesistenti e l'unica aggiunta
consentita, autentica l'evidenza della prova precedente e lega ogni batch al
contratto, all'assembly e al contenuto della richiesta. Al termine ripercorre
l'intera ricerca offline, ricalcolando sonde, sottoproblemi, selezioni e arresto,
e ricostruisce ogni residuo dalle osservazioni fisiche. Le evidenze precedenti
rimangono immutate; il comando corrente è `scripts/run-r3-h-scaled-search2.cmd`.
