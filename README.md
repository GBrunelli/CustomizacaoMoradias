# Customização de Moradias - Plugin para Autodesk Revit® 2021

Este plugin lê uma tabela `.csv` que contém os parâmetros de elementos do Revit e os contrói no documento ativo.

### Elementos atualmente suportados

  - Paredes
  - Janelas
  - Portas

### Instalação

Para instalar o plugin, navegue até o local onde o Revit está instalado. Baixe a última versão disponivel [aqui](https://github.com/GBrunelli/CustomizacaoMoradias/releases) e coloque os arquivos  em $(PastaInstalação)\Revit\Addins\2021\ . Quando você abrir o Revit pela primeira vez depois de ter instalado o plugin, uma mensagem perguntando se confia na extensão deve aparecer, se essa mensagem não aparecer, verifique se colocou os arquivos na pasta correta, e certifique-se que os arquivos estejam descompactados.

### Usando o plugin
Para usar o plugin, clique em Add-Ins > External Tools > PlaceLineElement:
![PlaceElement](https://i.ibb.co/WxNv1Y2/tutorial.png)
Uma janela do explorer irá aparecer para selecionar o arquvio `.csv` contendo as definições da contrução, selecione o arquivo que deseja integrar no projeto e clique ok.

<sub>Desenvolvido por Gustavo H. Brunelli, sob condição de bolsista PUB da Univerdade de São Paulo.</sub>

