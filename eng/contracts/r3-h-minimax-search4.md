# H4: ricerca locale guidata dal massimo residuo

Trial 2 ha ripetuto sei punti due volte senza divergenze: tutte le quattro
direzioni minimax producono discesa reale senza cambi di fase. Il migliore
massimo sano è 3.763.367,921. H4 parte da quel record, con spareggio sull'ordine
registrato, e ne riproduce residui e fasi prima delle nuove sonde.

La superficie fisica resta quella H: 24 coordinate, box ±0.02, 75 residui,
scale originali e radice massimo assoluto ≤1. Leggi, guadagni, stati non fluidi
e identità produttive restano invariati. H4 modifica soltanto il metodo numerico.

Raggio iniziale 1e-4, massimo 1e-3, minimo 1e-8. La soglia minima evita di
ripetere il regime di derivate 1e-11 che Trial 2 ha mostrato meno predittivo.
Sonde di ampiezza min(1e-6,raggio/10); centrali solo con due sonde sane di fase
compatibile, altrimenti unilaterali compatibili. Una colonna non disponibile
non viene inventata: si usano le altre direzioni coordinate disponibili e le
sonde sane. Le direzioni accoppiate H3 richiedono tutte le colonne disponibili.

Si confrontano le direzioni coordinate in ordine indice, verso positivo poi
negativo, e le sei direzioni H3 nell'ordine dei damping originari. Ognuna resta
nel raggio e nel box grazie a una sola frazione 0.99. Lungo ciascuna si minimizza
direttamente il massimo lineare con la bisezione verificata della revisione
minimax. Si conservano le prime otto proposte uniche che prevedono discesa,
ordinate per massimo previsto e ordine originario. Per ciascuna si provano
il punto proposto e otto dimezzamenti. Il budget può troncare soltanto il
prefisso finale, mai riordinare o scegliere dopo aver visto la risposta fisica.

Il punto sano con massimo reale minore fra sonde e tentativi è accettato solo
con discesa stretta; spareggio sull'ordine, sonde prima. Se vince una sonda,
il raggio resta fisso: non si ripete il dimezzamento sistematico di H3. Per un
tentativo, rapporto riduzione reale/prevista <0.1 o previsione non migliorativa
riduce il raggio a un quarto; rapporto >0.75 e passo ≥0.8 volte il raggio lo
raddoppia entro il massimo. Un cambiamento di fase accettato limita comunque
il nuovo raggio a metà di quello precedente. Senza discesa si usa un quarto.

Budget: 4096 ricette, inclusi rifiuti; 64 iterazioni; massimo 193 batch fisici,
ciascuno con massimo 48 ricette e due passi preparatori. Ogni ricetta esegue
al massimo tre passi: limite conservativo 12674 passi. Arresti ulteriori:
radice osservata, raggio minimo senza discesa, tre aggiornamenti consecutivi
con miglioramento relativo ≤1e-9, errore tecnico o mancata riproduzione iniziale.
Un rifiuto non provoca tentativi aggiuntivi. Il replay deve ricostruire tutte
le scelte, i raggi, la contabilità e i residui dalle osservazioni conservate.

I casi numerici verificano il conflitto fra quadrati e massimo, la convergenza
su un sistema lineare accoppiato noto, il budget e la permanenza del raggio
quando vince una sonda. La ricerca lungo un insieme finito di direzioni non
è un algoritmo minimax globale e un arresto non dimostra impossibilità fisica.
Si registrano anche i trasferimenti energetici del passo Running 1, per distinguere
un accumulo da un difetto di conservazione. Una radice richiede ancora conferme
indipendenti di 100 passi; nessun esito installa un seed o promuove R3/M10.
