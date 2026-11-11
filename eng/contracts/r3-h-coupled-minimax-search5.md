# H5: ricerca con sottoproblema minimax accoppiato

Trial 3 ha ripetuto otto ricette senza divergenze. Tutti i tre raggi producono
discesa sana senza cambi di fase, con rapporti riduzione reale/prevista fra
0.9990 e 1.0000. H5 sceglie il minimo reale sano (spareggio sull'ordine), pari
a 3.564.754,512, e riproduce residui e fasi prima delle nuove sonde.

Restano fissi 24 coordinate, box ±0.02, 75 residui, scale, criterio di radice
massimo ≤1, fisica e controllori. Raggio iniziale 4e-4, verificato da Trial 3;
massimo 1e-3 e minimo 1e-8. Le derivate e le regole di compatibilità di fase
sono quelle H4: h=min(1e-6,raggio/10), centrali compatibili o unilaterali.

Il sottoproblema LP di Trial 3 ottimizza simultaneamente il massimo di tutti
i residui lineari, con ammissibilità primale/duale e gap numerico controllati.
Il secondo obiettivo minimizza le variazioni entro lo slack normalizzato 1e-9.
Le colonne nulle restano ferme. La soluzione usa una sola frazione globale 0.99,
senza clipping; si minimizza ancora il massimo lungo quella direzione verificata.
Si provano il punto proposto e otto dimezzamenti, in ordine. Sonde sane e tentativi
sono confrontati sul massimo reale, con discesa stretta e spareggio sull'ordine.

Un certificato numerico fallito, il budget dei pivot, un punto fuori dal box o
una colonna non disponibile sono registrati come rifiuti del sottoproblema:
nessuna direzione viene inventata. Restano utilizzabili le sonde sane già previste.
Eccezioni inattese arrestano tecnicamente l'esecuzione.

Le regole di adattamento restano H4: sonda accettata mantiene il raggio; tentativo
con rapporto reale/previsto sotto 0.1 riduce a un quarto; sopra 0.75 e passo almeno
0.8 volte il raggio raddoppia entro il massimo. Un cambiamento di fase accettato
limita il raggio a metà. Senza discesa si usa un quarto, fino al minimo.

Budget conservativo: 4096 ricette inclusi rifiuti, 64 iterazioni, 129 batch fisici
da massimo 48 ricette. Due passi preparatori per batch e tre per ricetta danno
il limite 12546 passi. Si arresta anche a radice osservata, al minimo raggio senza
discesa o dopo tre miglioramenti relativi consecutivi ≤1e-9. La struttura della
ricerca può raggiungere il limite di iterazioni prima di usare tutte le ricette.

Prima delle chiamate fisiche si verificano i casi LP di Trial 3, i casi minimax,
la convergenza accoppiata nota e il budget. Il replay ricostruisce richieste,
certificati, raggi, scelte, arresto e residui dalle osservazioni. I flussi Running 1
permettono di analizzare i bilanci dei nodi senza nuove sonde.

Il certificato LP riguarda soltanto il modello lineare locale. Un arresto H5
non dimostra impossibilità fisica e non autorizza ampliamenti dei limiti o C/J.
Una radice osservata richiede il distinto protocollo di conferme indipendenti
di 100 passi prima di R3. Nessun esito installa seed o chiude R3/M10.
