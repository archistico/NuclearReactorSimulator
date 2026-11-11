# Verifica della riparazione — revisione del criterio di massa

La prima misura ha eseguito due ricette identiche dopo compilazione, suite
ordinaria e baseline correnti PASS. Il protocollo 1 ha però richiesto che massa
entrante e uscente da turbine-inlet coincidessero entro 1e-6 kg/s. Nel punto
grezzo riparato la loro differenza è 0,00017251666 kg/s e coincide con il tasso
di accumulo dell'inventario: conservazione soddisfatta, stazionarietà non acquisita.
La legge corretta cambia le pressioni durante i passi preparatori e non promette
stazionarietà del punto iniziale. L'esito 1 resta **non conforme** e congelato.

Questa revisione è un'adjudication successiva delle misure già osservate,
senza nuove chiamate fisiche né cambiamenti ai dati. Corregge il criterio:
`abs(dM/dt - (portata entrante - portata uscente)) <= 1e-6 kg/s`.
La tolleranza di conservazione è invariata. Fixture distinguono bilancio chiuso
stazionario, bilancio chiuso con accumulo e massa non contabilizzata.

Tutti gli altri controlli della riparazione restano quelli dichiarati prima
della misura: due ripetizioni identiche, 75 residui ricostruiti, energia C4 entro
1e-9 relativo, accumulo turbine-inlet ≤10% del valore storico in tutti i tre
checkpoint, bilanci di stadio, ingresso e condensatore entro 0,01 W. Si
ricostruiscono limite termico e portata di condensazione dalle energie attive.

Il replay deve riprodurre prima il fallimento del criterio originale e verificare
che il suo esito sia rimasto non conforme, poi ricostruire la verifica corretta.
Si riportano tutti gli accumuli, il massimo scalato e l'inviluppo. Il risultato
non è una qualificazione di equilibrio o R3; la soglia dei 75 residui resta ≤1.
Sorgenti e test restano quelli compilati e provati dalla misura 1.

Comandi: `python eng/review-r3-stage-transport-repair-adjudication2.py execute`,
congelamento di `01-analysis.json`, poi `review` per il replay indipendente.
