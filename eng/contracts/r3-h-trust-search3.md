# H3: ricerca locale con raggio controllato e derivate attente alla fase

La revisione completa H2 è stata riprodotta prima di questa aggiunta. I suoi
24 tentativi terminali prevedevano discesa ma attraversavano il confine di fase
dell'hotwell e peggioravano l'obiettivo. La migliore osservazione già acquisita
è invece la sonda `i1/probe/6/1`, con massimo 4.480.780,636 e nessun cambio di fase
rispetto al punto H2 accettato. H3 seleziona questo punto come partenza numerica,
da riprodurre esattamente prima delle nuove sonde. Non è un seed qualificato.

Restano fissi 24 coordinate, 75 residui, scale, box frazionario ±0.02, criterio
di radice massimo assoluto ≤1 e tutte le definizioni fisiche e di controllo.
Il sottoproblema scalato H2 viene risolto intersecando il box originale con un
box locale sul passo. Una frazione globale 0.99 mantiene il passo all'interno;
nessuna coordinata fisica viene tagliata indipendentemente.

Raggio iniziale 1e-4, minimo 1e-10, massimo 1e-3. Sonde di ampiezza
min(1e-6, raggio/10), centrali dove entrambe ammesse dal box, altrimenti rivolte
all'interno. Si usano le differenze centrali solo se entrambe le sonde sono sane
e hanno le stesse 48 etichette di fase del punto corrente. Con una sola sonda
compatibile si usa quella differenza unilaterale. Senza sonde compatibili la
colonna è dichiarata indisponibile e non si costruisce un Jacobiano fittizio:
resta utilizzabile il confronto diretto delle sonde sane.

Per ciascuno dei sei damping originali si provano tutte le frazioni 2^-k,
k=0..20, nell'ordine damping poi frazione. Si sceglie il minimo massimo residuo
reale sano fra sonde e tentativi, spareggiando sull'ordine (sonde prima).
Si accetta soltanto discesa stretta. Le sonde possono diventare aggiornamenti
solo per questa nuova regola esplicita; le decisioni H2 non vengono riscritte.
I cambi di fase sono osservazioni, non rifiuti fisici automatici dei tentativi.

Senza discesa il raggio si riduce a un quarto. Se si accetta una sonda si dimezza.
Per un tentativo di modello, il rapporto tra riduzione reale e prevista governa
il raggio: sotto 0.1 (o previsione non migliorativa) un quarto; sopra 0.75 e passo
di norma massima ≥0.8 volte il raggio, raddoppio entro il massimo; altrimenti fisso.
Nessun aggiornamento del raggio cambia i limiti fisici originali.

Budget massimo: 8192 ricette, compresi rifiuti; 64 iterazioni; 257 batch fisici
da massimo 48 ricette. Ogni batch aggiunge due passi preparatori e ogni ricetta
al massimo tre passi, per un massimo conservativo di 25090 passi fisici.
Il prefisso finale di tentativi può essere troncato dal budget, mai riordinato.
Ci si arresta anche al raggio minimo senza discesa, a tre miglioramenti relativi
consecutivi ≤1e-10, a radice osservata, oppure per errore tecnico inatteso.
Rifiuti di dominio noti consumano budget e sono registrati; nessun tentativo
aggiuntivo nasce da un rifiuto. Il punto iniziale deve riprodurre residui e fasi.

Prima dell'esecuzione si verificano i casi numerici H2, limiti del nuovo passo,
scelta unilaterale al cambio di pendenza, colonne non disponibili, convergenza
su un problema noto con cambio di pendenza e contabilità del budget. Contratto,
assembly Release e richiesta sono legati tramite SHA-256. Ogni osservazione
mantiene inventari, fasi, salute, inviluppo e hash dello stato esplicito. Il replay
offline deve ricostruire residui, richieste, selezioni, raggi e arresto esatti.

Una radice osservata richiede conferme indipendenti di 100 passi prima di R3.
Un esito inconclusivo non dimostra impossibilità fisica e richiede revisione dei
bilanci; non attiva C/J, nuove incognite, tarature o un nuovo seed produttivo.
Non si modificano contratti o evidenze precedenti. Il nuovo gate ammette un solo
file aggiuntivo rispetto ai 408 file di test in ingresso.
