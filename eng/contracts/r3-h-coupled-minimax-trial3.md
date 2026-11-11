# Trial 3: verifica fisica del sottoproblema minimax accoppiato

H4 è conclusa senza radice dopo 4071 ricette: il massimo è 3.672.671,871.
Nel suo ultimo modello competono soprattutto energia hotwell e ingresso turbina.
Le direzioni finite già provate non risolvono il problema minimax simultaneo.

Questa prova ricostruisce il modello dell'ultima iterazione H4 (37), riproduce
il suo punto iniziale e prova tre direzioni accoppiate con raggi 1e-6, 1e-4 e
4e-4. Ogni punto viene ripetuto due volte in ordine base poi raggi crescenti:
otto ricette in un solo batch, due passi preparatori e massimo 26 passi fisici.
Nessun aggiornamento iterativo, nuove sonde o ampliamento del box sono consentiti.

Il sottoproblema minimizza t imponendo -t ≤ r+J*delta ≤ t e i limiti originali
intersecati col raggio. Il solver LP a due fasi usa soltanto la libreria standard,
spareggi deterministici, epsilon 1e-10 e budget di 20000 pivot. Una normalizzazione
comune per il massimo iniziale migliora i numeri senza ripesare i residui.
La soluzione primaria riceve controlli di ammissibilità primale e duale e gap
entro 1e-7 relativo all'obiettivo numerico. Sono certificati del modello lineare
entro precisione dichiarata, non certificati di equilibrio fisico.

Un secondo LP minimizza la somma dei moduli delle variazioni normalizzate,
mantenendo t entro 1e-9 normalizzato dall'optimum primario. Le colonne nulle
restano ferme. Una sola frazione globale 0.99 mantiene il punto all'interno;
un punto fuori dal box è rifiutato, senza clipping. Si verificano casi noti
di fase artificiale, programma impossibile o illimitato, conflitto quadrati/massimo,
radice accoppiata, colonna nulla e discesa simultanea invisibile ai singoli assi.

Base, 75 residui e 48 fasi devono riprodurre esattamente H4; le ripetizioni
devono coincidere in tutte le osservazioni. Si registrano anche i flussi energetici
Running 1 come in Trial 2. Una direzione è promettente se sana, in discesa stretta
e con rapporto riduzione reale/prevista ≥0.1. L'inviluppo resta riportato separatamente.
Un esito favorevole permette una distinta pianificazione di ricerca accoppiata;
uno sfavorevole impone revisione dei bilanci e della validità del modello locale.

Restano immutati incognite, box ±2%, scale, radice ≤1, fisica, controllori e stato
produttivo. Nessun esito installa un seed, avvia conferme lunghe o qualifica R3/M10.
