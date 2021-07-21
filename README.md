# Customização de Moradias - Plugin para Autodesk Revit® 2021

Este plugin lê um arquivo `.json` que contém os parâmetros de elementos do Revit e os contrói no documento ativo. Além de gerar os elementos descritos no arquivo, o plugin gera um piso para cada ambiente, a laje da casa, um telhado genérico, e também nomeia os ambientes de acordo com os elementos dentro dele, como mobilía e encanamentos. 

###### Elementos atualmente suportados:

  - Paredes
  - Janelas
  - Portas
  - Mobiliário
  - Elementos hospedados (chuveiro, vaso sanitário, etc.)
  
### Instalação

Para instalar o plugin, você deve fazer o download da última versão disponível [aqui](https://github.com/GBrunelli/CustomizacaoMoradias/releases) . Navegue até a pasta de instação do Revit, ou cole o seguinte endereço na barra de endereço do Explorador de Arquivos do Windows:

> %appdata%\Autodesk\Revit\Addins\

![Explorer](https://i.ibb.co/5LK21xN/tutorial-v141.png)

Descompacte o arquivo que você fez o download na pasta 2021. Você pode deletar o arquivo `.zip`. Pela primeria vez que você abrir o Revit depois de instalar o plugin, uma mensagem irá aparecer perguntando se confia na extensão, selecione `Sempre carregar` para não receber esse alerta outras vezes.

### Usando o plugin
Na faixa de opções, clique em Customização de Moradias > Construir JSON:

![PlaceElement](https://i.ibb.co/Z1wv1mD/tutorial-V14.png)

Uma janela irá aparecer e você deve fornecer o caminho para o arquivo `.json` contendo as definições da contrução, o nível base, e o nível da laje. Caso esteja usando o template que acompanhana o plugin, o nível base deve ser `PLANTA BAIXA`, e o nível da laje deve ser `COBERTURA`. O campo Roof Type permite selecionar o lado da queda do telhado gerado.

<sub>Desenvolvido por Gustavo H. Brunelli, sob condição de bolsista PUB da Univerdade de São Paulo.</sub>

[comment]: <> (voce eh foda)
