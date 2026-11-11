# H7: verificare il ramo liquido già osservato, nello stesso box H

H6 è conclusa con 3521 ricette e massimo 3.537.690,124. Tutti i 64 certificati
numerici passano, ma nessun passo di modello viene accettato: avanzano soltanto
le sonde. Nell'iterazione iniziale, persino il dimezzamento 8 conserva la fase
grezza e quella al primo seed-step, poi passa a liquido al post-seed e Running 1.
Il Jacobiano del ramo miscela non descrive quindi il passo accoppiato completo.

H6 contiene già punti sani che sono liquidi nell'hotwell sia grezzo sia in tutti
e tre i checkpoint. H7 sceglie fra questi il minore massimo assoluto, con spareggio
sull'ordine del trace. È una nuova ipotesi iniziale dichiarata: può avere un
residuo maggiore del finale H6. Non riscrive le accettazioni H6 e non installa
un seed produttivo. Il punto deve riprodurre esattamente residui e fasi.

Il metodo rimane H6, con solo partenza e raggio iniziale diversi: iniziale 1e-6,
massimo 1e-4, minimo 1e-8. Il piccolo raggio permette sonde h=1e-7 all'inizio
del ramo compresso, anziché trasferire direttamente il Jacobiano dalla miscela.
La compatibilità di fase vale come prima; l'ottimizzazione non obbliga a mantenere
la fase liquida per sempre, e i cambi di fase sani restano valutabili.

LP primario, cap secondario normalizzato 1e-6, certificati 1e-7, frazione globale
0.99, otto dimezzamenti, adattamento del raggio e contabilità sono quelli H6.
Limiti: 4096 ricette, 64 iterazioni, 129 batch e 12546 passi conservativi. Restano
gli arresti a radice osservata, minimo raggio senza discesa, tre progressi relativi
consecutivi ≤1e-9 e ogni errore tecnico inatteso. Rifiuti sono registrati.

La superficie non cambia: 24 inventari, box ±2%, 75 residui e scale, criterio
di radice ≤1, fisica e controllori invariati. Non è l'avvio di C/J o un ampliamento
dei limiti. Il confronto fra rami non dimostra impossibilità fisica; una radice
richiede ancora le conferme indipendenti di 100 passi prima di R3/M10.
