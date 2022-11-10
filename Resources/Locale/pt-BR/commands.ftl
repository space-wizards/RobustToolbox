### Localization for engine console commands

## generic

cmd-invalid-arg-number-error = Número inválido de argumentos.

cmd-parse-failure-integer = {$arg} não é um inteiro válido.
cmd-parse-failure-float = {$arg} não é um float válido.
cmd-parse-failure-bool = {$arg} não é um booleano válido.
cmd-parse-failure-uid = {$arg} não é um UID de entidade válido.
cmd-parse-failure-entity-exist = UID {$arg} não corresponde a uma entidade existente.


## 'help' command
cmd-help-desc = Exibir ajuda geral ou texto de ajuda para um comando específico
cmd-help-help = Uso: help [command name]
    Quando nenhum nome de comando é fornecido, exibe o texto de ajuda geral. Se um nome de comando for fornecido, exibe o texto de ajuda para esse comando.

cmd-help-no-args = Para exibir a ajuda de um comando específico, escreva 'help <command>'. Para listar todos os comandos disponíveis, escreva 'list'. Para procurar comandos, use 'list <filter>'.
cmd-help-unknown = Comando desconhecido: { $command }
cmd-help-top = { $command } - { $description }
cmd-help-invalid-args = Quantidade de argumentos inválida.
cmd-help-arg-cmdname = [command name]

## 'cvar' command
cmd-cvar-desc = Obtém ou define um CVar.
cmd-cvar-help = Uso: cvar <name | ?> [value]
    Se um valor for passado, o valor será analisado e armazenado como o novo valor do CVar.
    Caso contrário, o valor atual do CVar é exibido.
    Use 'cvar ?' para obter uma lista de todos os CVars registrados.

cmd-cvar-invalid-args = Deve fornecer exatamente um ou dois argumentos.
cmd-cvar-not-registered = CVar '{ $cvar }' não está registrado. Use 'cvar ?' para obter uma lista de todos os CVars registrados.
cmd-cvar-parse-error = O valor de entrada está no formato incorreto para o tipo { $type }
cmd-cvar-compl-list = Listar CVars disponíveis
cmd-cvar-arg-name = <name | ?>
cmd-cvar-value-hidden = <value hidden>

## 'list' command
cmd-list-desc = Lista os comandos disponíveis, com filtro de pesquisa opcional
cmd-list-help = Uso: list [filter]
    Lista todos os comandos disponíveis. Se um argumento for fornecido, ele será usado para filtrar comandos por nome.

cmd-list-heading = NOME                 DESC{"\u000A"}-------------------------{"\u000A"}

cmd-list-arg-filter = [filter]

## '>' command, aka remote exec
cmd-remoteexec-desc = Executa comandos do lado do servidor
cmd-remoteexec-help = Uso: > <command> [arg] [arg] [arg...]
    Executa um comando no servidor. Isso é necessário se um comando com o mesmo nome existir no cliente, pois a simples execução do comando executaria o comando do cliente primeiro.

## 'gc' command
cmd-gc-desc = Execute o GC (coletor de lixo)
cmd-gc-help = Uso: gc [generation]
    Usa GC.Collect() para executar o Garbage Collector.
    Se um argumento for fornecido, ele será analisado como um número de geração do GC e GC.Collect(int) será usado.
    Use o comando 'gfc' para fazer um GC completo compactando LOH.
cmd-gc-failed-parse = Falha ao analisar o argumento.
cmd-gc-arg-generation = [generation]

## 'gcf' command
cmd-gcf-desc = Execute o GC, totalmente, compactando LOH e tudo.
cmd-gcf-help = Uso: gcf
    Faz um GC.Collect(2, GCCollectionMode.Forced, true, true) completo enquanto também compacta LOH.
    Isso provavelmente será bloqueado por centenas de milissegundos, esteja avisado.

## 'gc_mode' command
cmd-gc_mode-desc = Alterar/ler o modo de latência do GC
cmd-gc_mode-help = Uso: gc_mode [type]
    Se nenhum argumento for fornecido, retornará o modo de latência do GC atual.
    Se um argumento for passado, ele será analisado como GCLatencyMode e definido como o modo de latência do GC.

cmd-gc_mode-current = modo de latência atual do gc: { $prevMode }
cmd-gc_mode-possible = modos possíveis:
cmd-gc_mode-option = - { $mode }
cmd-gc_mode-unknown = modo de latência gc desconhecido: { $arg }
cmd-gc_mode-attempt = tentando alterar o modo de latência do gc: { $prevMode } -> { $mode }
cmd-gc_mode-result = modo de latência gc resultante: { $mode }
cmd-gc_mode-arg-type = [type]

## 'mem' command
cmd-mem-desc = Imprime informações de memória gerenciada
cmd-mem-help = Uso: mem

cmd-mem-report = Tamanho da pilha: { TOSTRING($heapSize, "N0") }
    Total alocado: { TOSTRING($totalAllocated, "N0") }

## 'physics' command
cmd-physics-overlay = {$overlay} não é uma sobreposição reconhecida

## 'lsasm' command
cmd-lsasm-desc = Lista assemblies carregados por contexto de carregamento
cmd-lsasm-help = Uso: lsasm

## 'exec' command
cmd-exec-desc = Executa um arquivo de script dos dados de usuário graváveis ​​do jogo
cmd-exec-help = Uso: exec <fileName>
    Cada linha no arquivo é executada como um único comando, a menos que comece com um #

cmd-exec-arg-filename = <fileName>

## 'dump_net_comps' command
cmd-dump_net_comps-desc = Imprime a tabela de componentes em rede.
cmd-dump_net_comps-help = Uso: dump_net-comps

cmd-dump_net_comps-error-writeable = Registro ainda gravável, IDs de rede não foram gerados.
cmd-dump_net_comps-header = Registros de componentes em rede:

## 'dump_event_tables' command
cmd-dump_event_tables-desc = Imprime tabelas de eventos direcionados para uma entidade.
cmd-dump_event_tables-help = Uso: dump_event_tables <entityUid>

cmd-dump_event_tables-missing-arg-entity = Argumento de entidade ausente
cmd-dump_event_tables-error-entity = Entidade inválida
cmd-dump_event_tables-arg-entity = <entityUid>

## 'monitor' command
cmd-monitor-desc = Alterna um monitor de depuração no menu F3.
cmd-monitor-help = Uso: monitor <name>
    Os monitores possíveis são: { $monitors }
    Você também pode usar os valores especiais "-all" e "+all" para ocultar ou mostrar todos os monitores, respectivamente.

cmd-monitor-arg-monitor = <monitor>
cmd-monitor-invalid-name = Nome do monitor inválido
cmd-monitor-arg-count = Argumento do monitor ausente
cmd-monitor-minus-all-hint = Esconde todos os monitores
cmd-monitor-plus-all-hint = Mostra todos os monitores


## Mapping commands

cmd-savemap-desc = Serializa um mapa para o disco. Não salvará um mapa pós-inicialização a menos que seja forçado.
cmd-savemap-help = savemap <MapID> <Path> [force]
cmd-savemap-not-exist = O mapa de destino não existe.
cmd-savemap-init-warning = Tentativa de salvar um mapa pós-inicialização sem forçar o salvamento.
cmd-savemap-attempt = Tentando salvar o mapa {$mapId} em {$path}.
cmd-savemap-success = Mapa salvo com sucesso.
cmd-hint-savemap-id = <MapID>
cmd-hint-savemap-path = <Path>
cmd-hint-savemap-force = [bool]

cmd-loadmap-desc = Carrega um mapa do disco para o jogo.
cmd-loadmap-help = loadmap <MapID> <Path> [x] [y] [rotation] [consistentUids]
cmd-loadmap-nullspace = Você não pode carregar no mapa 0.
cmd-loadmap-exists = Mapa {$mapId} já existe.
cmd-loadmap-success = Mapa {$mapId} foi carregado em {$path}.
cmd-loadmap-error = Ocorreu um erro ao carregar o mapa de {$path}.
cmd-hint-loadmap-x-position = [x-position]
cmd-hint-loadmap-y-position = [y-position]
cmd-hint-loadmap-rotation = [rotation]
cmd-hint-loadmap-uids = [float]

cmd-hint-savebp-id = <Grid EntityID>

## 'flushcookies' command
# Note: the flushcookies command is from Robust.Client.WebView, it's not in the main engine code.

cmd-flushcookies-desc = Liberar o armazenamento de cookies CEF para o disco
cmd-flushcookies-help = Isso garante que os cookies sejam salvos corretamente no disco no caso de desligamentos impróprios.
    Observe que a operação real é assíncrona.
