# H6: ricerca con scelta secondaria stabilizzata e raggio contenuto

Trial 5 riproduce il rifiuto storico H5, usa un cap secondario normalizzato 1e-6
senza allentare i certificati, e verifica fisicamente tre raggi. I raggi 1e-6
e 1e-4 migliorano senza cambio di fase; quello 4e-4 attraversa l'hotwell e viene
respinto per peggioramento. H6 parte dal miglior punto sano (3.538.409,484),
verificandone residui e fasi prima delle nuove sonde.

H6 mantiene il sottoproblema e le regole H5, con due modifiche congelate:
cap numerico secondario 1e-6 e raggio iniziale/massimo 1e-4. Il raggio massimo
è limitato al maggiore verificato favorevolmente da Trial 5. Restano minimo
1e-8, h=min(1e-6,raggio/10), compatibilità di fase, frazione unica 0.99 e otto
dimezzamenti. Certificati falliti restano rifiuti numerici registrati; una
colonna mancante non viene inventata. Si possono ancora confrontare le sonde sane.

Il LP primario, le scale e il criterio fisico massimo ≤1 sono invariati. Il
secondo obiettivo sceglie variazioni più piccole entro un cap matematico esplicito;
non è una tolleranza aggiunta alla radice o all'inviluppo. L'ammissibilità primale,
duale e il gap mantengono il controllo relativo 1e-7 del solver. La revisione su
64 modelli H5 ha restituito 63 certificati validi e un rifiuto, non un successo
universale. Le eccezioni inattese restano errori tecnici.

Si conservano contabilità e arresti H5: massimo 4096 ricette, 64 iterazioni,
129 batch, 12546 passi fisici conservativi; radice osservata, raggio minimo
senza discesa o tre miglioramenti relativi consecutivi ≤1e-9. Ogni richiesta,
certificato e scelta deve essere ricostruibile offline. Sono registrati anche
i flussi energetici Running 1, per riesaminare i bilanci del punto finale.

Restano fissi i 24 inventari, il box ±2%, i 75 residui, fisica, controllori e
produzione. Un rifiuto numerico o un arresto non prova impossibilità fisica.
Un'eventuale radice richiede conferme indipendenti di 100 passi prima di R3;
nessun esito installa seed o chiude M10.
