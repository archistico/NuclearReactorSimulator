# Trial 4: isolare il problema primario dal criterio secondario

H5 ha eseguito 2945 ricette in 64 iterazioni. Tutti i 64 sottoproblemi sono
stati rifiutati dal controllo numerico; gli aggiornamenti provengono dalle sonde.
La riproduzione offline del primo caso mostra che il LP primario passa il gap,
mentre il successivo criterio L1 ha gap circa 0.0002902 contro tolleranza 0.000001316.
Non si modifica la soglia e non si accetta quella soluzione secondaria.

La variante usa direttamente la base deterministica del LP primario certificato,
senza il secondo obiettivo. Le colonne nulle restano ferme; tutti gli altri
controlli, epsilon, budget dei pivot, box e frazione globale 0.99 restano invariati.
Non è una modifica della fisica o del criterio di radice: cambia solo come si
sceglie una soluzione del sottoproblema locale, in una nuova prova separata.

Si usa il primo Jacobiano H5, già congelato e riproducibile dalle sonde di i0,
e si riproduce il punto di partenza H5. Si provano i raggi 1e-6, 1e-4 e 4e-4,
ciascuno ripetuto due volte, con la base ripetuta per prima. Otto ricette, un
batch, massimo 26 passi fisici compresa la preparazione. Nessuna nuova ricerca.

Si verificano casi noti e la regressione numerica H5 prima delle chiamate fisiche.
La regressione deve passare il certificato primario e continuare a riprodurre
il rifiuto secondario storico. Una direzione sana, strettamente migliorativa e
con rapporto reale/previsto ≥0.1 consente la pianificazione distinta della variante
di ricerca. Si conservano ripetizioni, residui, fasi e flussi come in Trial 3.
Nessun esito installa seed o qualifica R3/M10; nessuna soglia fisica viene allentata.
