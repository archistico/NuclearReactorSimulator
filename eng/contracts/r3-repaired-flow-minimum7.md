# Attribuzione offline al minimo delle capacità di portata dello stadio

La prova di proposte 6 ha fallito il confronto predittivo, mantenendo i
controlli registrati. Questa revisione non modifica quell'esito, i suoi
criteri o l'indicatore di disponibilità già restituito. Non esegue runtime
e non diagnostica un nuovo difetto fisico. Il contratto autentica codice,
sorgenti della legge e dati congelati prima dell'analisi.

L'ambito è esclusivamente il primo passo preparatorio. Per i record esaminati
le sei frazioni non fluide devono produrre gli stessi ingressi effettivi
nel calcolo `baseline * (1 + frazione)` e l'uscita iniziale del governor
deve coincidere con 29,281329697436618%. Le valvole stop e admission sono
inizialmente aperte; la control usa l'apertura iniziale verificata. La
resistenza di ogni valvola è 1.000 Pa·s²/kg² e quella di espansione 21.400.
La legge delle valvole è quella quadratica con caratteristica lineare.

Dalle pressioni grezze si ricostruiscono le capacità positive di stop,
control, admission ed espansione, oltre al limite di metà inventario per
passo. La loro minima, moltiplicata per la qualità iniziale dell'ingresso
turbina, deve riprodurre la portata effettiva dello stadio entro 1e-9 kg/s.
Da questa portata, lavoro ideale ed efficienza registrati e velocità di
riferimento si ricostruiscono coppia di turbina e coppia netta entro 1e-6 N·m.
Le formule riproducono la legge produttiva già presente; non la sostituiscono.

Si analizzano baseline e due proposte congelate. Per ogni componente attiva
del passo lineare, si ricostruiscono inoltre i due record centrali alla
scala selezionata della propria direzione, usando gli archivi delle prove
3–5. La derivata della coppia deve coincidere con la riga della base entro
1e-6 N·m per unità di frazione. La somma delle componenti deve riprodurre
la previsione della prova 6; la legge del minimo deve riprodurre la variazione
osservata. Si riportano vincitori e margini, senza introdurre una nuova
tolleranza per riclassificare retroattivamente i vecchi controlli.

Una fixture su min(x,y) mostra perché secanti centrali lungo gli assi
non devono necessariamente prevedere una variazione congiunta quando i
vincitori cambiano. L'esito conserva separatamente la ricostruzione della
legge e l'errore del modello affine. Non si estende l'attribuzione ai passi
successivi, nei quali le posizioni effettive degli attuatori richiedono
ulteriore osservazione. Nessun modello a tratti o famiglia è qualificato.

Il comando `python eng/review-r3-repaired-flow-minimum7.py review` autentica
i dati congelati e ricostruisce l'analisi senza nuove simulazioni. R3 e M10
restano aperti.

La prima versione della revisione richiedeva frazioni esattamente zero. Ha
fallito sulle frazioni circa 5,55e-21 e 1,11e-20 restituite dall'ottimizzatore:
in binary64 `1 + frazione` vale esattamente 1, quindi gli ingressi effettivi
sono identici. Il controllo è stato corretto dopo quel fallimento, senza
nuove chiamate fisiche. Codice, specifica, contratto e log originali sono
conservati nel pacchetto; il nuovo esito riproduce il rifiuto del vecchio
controllo e verifica l'identità degli ingressi effettivi senza tolleranza.
