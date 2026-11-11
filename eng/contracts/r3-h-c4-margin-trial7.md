# Trial 7: il margine C4 non troncato al bordo q=0

H8 ha migliorato il residuo fino a 3.337.270,068, ma nel checkpoint Running 1
la qualità riportata è zero. Otto sottoproblemi successivi sono rifiutati dal
guardiano di qualità interna strettamente positiva. Il punto non è per questo
un errore fisico: C4 restituisce miscela anche sul bordo.

La lettura del payload C4 originale e del resolver ricostruisce la qualità
prima del clamp, (v-v_l)/(v_v-v_l), e il margine di raggiungibilità esistente:
M=(v-v_l+max(1e-14,|v|*1e-10))/(v_v-v_l). Il volume hotwell resta 10 m³,
come nella definizione congelata. Il payload deve avere il suo SHA originale,
schema e conteggi corretti. La qualità ricostruita dopo clamp deve coincidere
con quella registrata. Non viene cambiato il payload né la tolleranza C4.

Nel punto finale H8, q=0 è ricostruito esattamente e M è circa 7.4e-15 positivo.
Si usa M e il suo Jacobiano nelle medesime quattro disequazioni locali di H8,
M_k+JM_k*delta ≥0.1*M_k. Non si inserisce una qualità fittizia positiva e non si
estende il dominio termodinamico: si rappresenta il margine che C4 usa già.
La raggiungibilità di volume è necessaria, non sufficiente per tutto il resolver;
la verifica fisica di fase e salute rimane quindi obbligatoria.

La prova riproduce il punto finale H8 e verifica tre raggi 1e-6, 1e-5 e 1e-4,
ciascuno ripetuto due volte: otto ricette in un batch, massimo 26 passi fisici.
Si usano le sonde già congelate dell'ultima iterazione H8 per ricostruire
residui, pressione e margine non troncato, senza nuove derivate fisiche.

Si verificano interpolazione del prefix, identità del payload, bordo esatto,
volume appena interno/esterno alla tolleranza originale e i casi LP di fase.
Le ripetizioni e la base devono coincidere esattamente. Direzioni promettenti:
sane, in discesa stretta con rapporto reale/previsto ≥0.1 e hotwell miscela nei
quattro checkpoint. Qualità zero resta una misura valida, non un nuovo gas.

LP, cap secondario, certificati, box ±2%, 75 residui, criterio ≤1, controllori
e produzione sono invariati. Il nuovo margine è una rappresentazione numerica,
non una tolleranza di equilibrio. Nessun esito qualifica R3/M10 o installa seed.
