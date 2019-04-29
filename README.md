#Espia Perdido

A Web Page (half PWA), with 2 similar games:

1.El Espia Perdido (Spy Fall)
2.El Imbecil

Both games have a wait room and handle different rooms identified by codes. Both games are based in 1 player not knowing some information, but all the rest yes.
To do that WebSockets was needed. To abstract from this level, SignalR is used.

The list of words and situations for the games are fecthed from a json file. To update or customized the game, redeploy with the changes added to the [espia.json] (./EspiaPerdido/espia.json) and [imbecil.json] (./EspiaPerdido/imbecil.json) files.