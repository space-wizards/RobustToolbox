# Strings Loc para vários comandos relacionados ao estado da entidade e ao PVS do lado do cliente

cmd-reset-ent-help = Uso: resetent <Entity UID>
cmd-reset-ent-desc = Redefina uma entidade para o estado do servidor recebido mais recentemente. Isso também redefinirá as entidades que foram desanexadas para espaço nulo.

cmd-reset-all-ents-help = Uso: resetallents
cmd-reset-all-ents-desc = Redefine todas as entidades para o estado do servidor recebido mais recentemente. Isso afeta apenas as entidades que não foram desanexadas ao espaço nulo.

cmd-detach-ent-help = Uso: detachent <Entity UID>
cmd-detach-ent-desc = Desanexar uma entidade para espaço nulo, como se tivesse saído do intervalo PVS.

cmd-local-delete-help = Uso: localdelete <Entity UID>
cmd-local-delete-desc = Exclui uma entidade. Ao contrário do comando delete normal, este é CLIENT-SIDE. A menos que a entidade seja uma entidade do lado do cliente, isso provavelmente causará erros.

cmd-full-state-reset-help = Uso: fullstatereset
cmd-full-state-reset-desc = Descarta qualquer informação de estado da entidade e solicita um estado completo do servidor.
