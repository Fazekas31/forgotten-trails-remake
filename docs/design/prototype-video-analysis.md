# Análise visual do protótipo enviado

## Escopo da referência

O vídeo tem cerca de 3min45s e cobre a chegada à cidade, a exploração das pegadas e o início da investigação no saloon. Ele termina antes dos acontecimentos posteriores, então serve como referência de câmera, ritmo de exploração, leitura das pistas e apresentação da interface. O PDF oficial continua determinando personagens, falas, pistas e ordem dos eventos.

## Leitura do vídeo

| Tempo aproximado | O que aparece | O que a referência ensina |
| --- | --- | --- |
| 0:00–0:32 | Menu, cartões narrativos, cavalgada em primeira pessoa e portão | Abrir com silhueta do portão, perspectiva baixa de montaria e uma pausa curta antes do controle livre. |
| 0:32–1:12 | Rua, rastros no chão e exploração do exterior | Um rastro físico deve conduzir o olhar entre marcos; o jogador descobre a rota olhando o chão e o espaço, sem depender só do marcador de objetivo. |
| 1:16–2:48 | Entrada no saloon, busca em salas e subida ao segundo piso | A exploração funciona melhor quando cada compartimento oferece uma pista focal, com tempo para olhar e ler. A escada cria mudança de ritmo e enquadramento. |
| 2:52–3:45 | Retorno ao térreo e busca pela faca; o trecho acaba durante a investigação | O retorno ao espaço já visto pode ganhar tensão com novos sons e uma pista de combate no balcão. O vídeo não mostra a aparição completa nem o restante do Ato 1. |

## Composição para Ash Creek

A cidade deve ler-se como uma rota principal com desvios, seguindo o mapa enviado:

1. **Entrada:** o portão enquadra a trilha e o letreiro gasto. Luke precisa formar uma silhueta reconhecível no primeiro plano, iluminada pelo lampião no chão; cavalo e portão dão escala sem cobrir o corpo.
2. **Rua e poço:** o poço é o marco central e a primeira âncora de navegação. Pegadas e manchas devem aparecer em trechos espaçados, apontando para o saloon, em vez de formar uma seta contínua brilhante.
3. **Saloon:** fachada e varanda devem ser reconhecíveis de longe; o interior oferece espaço ao redor das mesas viradas e do balcão. A escada parcialmente bloqueada enquadra a subida. No segundo piso, a janela e a mesa da anotação são focos separados, não uma parede cheia de texto.
4. **Igreja, beco e delegacia:** a torre da igreja distingue a silhueta da rua. O beco vira um desvio lateral mais estreito; a delegacia precisa ser legível a partir da rota e conduzir ao lenço, às celas e ao livro vermelho em níveis distintos.
5. **Celeiro e floresta:** o celeiro ganha espaço negativo e isolamento em relação à cidade. As vigas devem formar linhas que levem o olhar para o ponto de revelação; a porta dos fundos termina num recorte de árvores negras e névoa localizada.

Cada marco deve ter uma aproximação legível antes da interação: faixa livre para caminhar, silhueta em contraluz ou contraste tonal, e uma área próxima onde o detalhe da pista possa ser inspecionado. Evitar alinhar todos os elementos no centro da rua ou colocar props e árvores na mesma distância da câmera. Variar primeiro plano, plano médio e horizonte para dar profundidade sem estreitar o caminho.

## O que melhorar em relação ao vídeo

- A névoa marrom ocupa praticamente toda a imagem e reduz a leitura de paredes, trilhas e objetos. Manter a base noturna azul-acinzentada aprovada e reservar névoa mais densa para a estrada, o campo de vulnerabilidade, o celeiro e a saída para a floresta.
- A luz quente do protótipo tingiu o cenário inteiro. O lampião deve criar um foco âmbar forte e local; luz fria ambiente e silhuetas claras mantêm Luke, inimigos e arquitetura legíveis fora do feixe.
- Os cartões pretos entre deslocamentos interrompem a exploração. Usar legendas e falas fiéis ao PDF durante a cena; reservar cortes para as transições que o roteiro realmente descreve, incluindo o encerramento em preto.
- O HUD do vídeo é pequeno na própria captura. Preservar a escala maior adotada no projeto para objetivos, legendas, prompts e leitura de pistas; limitar texto persistente na tela. A anotação pode usar um painel de leitura, enquanto a mensagem do objetivo permanece discreta.
- O protótipo usa geometria simples e espaços comprimidos. Reaproveitar a economia de formas e a leitura da rota, mas aumentar a folga ao redor de Luke, do poço e dos objetos de investigação; os interiores continuam íntimos sem bloquear a câmera ou esconder a saída.
- O material exibido no protótipo tem nomes e pistas diferentes. Não reutilizar seus letreiros, cartões de história ou conteúdo narrativo no jogo oficial.

## Aplicação técnica no projeto

- Manter URP e a meta de 1080p/60 FPS no perfil médio. Assar iluminação estática; usar a luz em tempo real no lampião e em poucos acentos, sem sombras locais em todas as fontes.
- Continuar o bloqueio no Unity/ProBuilder até validar proporções, linhas de visão e distâncias na câmera de jogo. Blender está disponível para produzir depois módulos de fachada, varanda, sino, poço, feno e personagens de destaque; modelar agora, antes de estabilizar os volumes, aumentaria retrabalho.
- Para o passe de arte, usar materiais PBR simples com variação controlada de madeira, ferro, tecido, couro e lama. Usar decals ou malhas de baixo custo para sangue e pegadas; instanciar folhagem repetida e acrescentar LODs às árvores vistas à distância.
- Para a interface final, concentrar os elementos de tela numa camada de UI escalável com fontes SDF/TMP, margens seguras e estados claros de objetivo, interação, leitura e alerta. Os rótulos de navegação pertencem ao HUD ou a placas pequenas no cenário, nunca a letras gigantes em paredes.
- Uma futura passada de câmera pode migrar movimentos roteirizados do controlador para Cinemachine, separando câmera e locomoção e facilitando enquadramentos e impactos sem disputar transformações com o jogador.

## Sequência visual a validar

Validar primeiro a aproximação a Luke e o caminho portão → poço → saloon. Depois validar a leitura vertical do saloon e o retorno ao térreo na sequência exata do PDF. Só então ampliar o passe de materiais e modelos para igreja, delegacia, beco, celeiro e floresta. As falas, anotações e eventos permanecem os do roteiro oficial.
