# Direção visual — Ash Creek

## Objetivo

Fazer Ash Creek parecer um lugar real e gasto, com estranheza crescente, mantendo pistas, portas e silhuetas reconhecíveis. O acabamento é realismo estilizado: formas e materiais críveis, composição controlada e detalhe concentrado nos pontos narrativos.

## Paleta e iluminação

- Base noturna azul-acinzentada; âmbar quente no lampião, janelas e luzes narrativas.
- Uma Directional Light fria fornece a leitura geral e as sombras principais.
- Luzes estáticas de interiores e rua ficam assadas em lightmaps. Reservar luz em tempo real para o lampião e momentos importantes; luzes locais sem mapas de sombra.
- Usar cookies de luz em poucos pontos para projetar padrões de lanternas e janelas.
- Volume global com correção de cor sutil; preservar os âmbar sem esmagar os detalhes nas sombras. Grão, vinheta e aberração cromática, se usados, devem ser discretos.

## Névoa e leitura

- Fog exponencial leve para unir o horizonte.
- Criar camadas localizadas no campo vulnerável e na Floresta dos Suspiros com cartões/meshes translúcidos de baixa densidade e pequenas variações de movimento.
- Não cobrir pistas, trilhas de sangue, portas ou o contorno dos personagens. Reservar maior opacidade para transições roteirizadas.
- Não depender de fog volumétrico 3D, que não faz parte do conjunto URP desta versão do projeto.

## Materiais, geometria e composição

- Kit modular de madeira envelhecida, metal oxidado, couro, vidro sujo e lama. Variação de roughness e tons dá riqueza sem multiplicar materiais.
- Silhuetas claras para casas do oeste, torre da igreja, poço, árvores e celeiro. Usar LODs/instancing para vegetação repetida.
- Reservar maior densidade de detalhe a evidências, sino da igreja, livro vermelho, lampião e criatura do celeiro.
- Decals seletivos para sangue, pegadas e marcas no chão; se um decal depender de renderer feature, verificar leitura em superfícies transparentes e preservar alternativa por malha.

## Perfil gráfico inicial

- Médio: resolução 1920×1080, escala 1.0, alvo 60 FPS, sombras direcionais até cerca de 35 m, duas cascatas e uma luz principal sombreada.
- Luzes locais em tempo real sem sombras; luz ambiente e interiores estáticos assados.
- Usar antialiasing leve e pós-processamento moderado; sem ray tracing, reflexos em espaço de tela pesados ou volumetria contínua.
- Criar níveis baixo/médio/alto após medir a primeira cena; o perfil integrado Intel pode usar menor distância de sombra, menos partículas e escala de renderização menor.
- Validar as builds macOS (Metal) e Windows (Direct3D) no hardware disponível antes de fechar orçamento de arte.

## Referências técnicas

- [Comparação oficial dos recursos das render pipelines do Unity 6](https://docs.unity3d.com/6000.0/Documentation/Manual/render-pipelines-feature-comparison.html)
- [Requisitos oficiais do Unity 6](https://docs.unity3d.com/6000.0/Documentation/Manual/system-requirements.html)
