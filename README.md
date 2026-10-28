# AudioMaxing

Amplificador de áudio para Windows 10/11, com ganho adicional e limitador de picos para o fone ou dispositivo de saída escolhido.

Windows 10/11 · C# / WPF · WASAPI · Licença MIT

## Recursos

- Interface em português e seleção do fone de saída.
- Ganho adicional de 0 a +24 dB com transição gradual.
- Limitador de picos, medidor de nível e alerta para ganho elevado.
- Botão de emergência e tentativa de restauração da saída anterior.

**Requer VB-CABLE e .NET 7 Desktop Runtime x64.** O driver não acompanha o projeto.

## Como funciona

```text
YouTube e outros aplicativos
          ↓
CABLE Input (saída padrão do Windows)
          ↓ (internamente no VB-CABLE)
CABLE Output (capturado por WASAPI)
          ↓
AudioMaxing: ganho → limitador de pico de amostra
          ↓
fone selecionado (WASAPI compartilhado)
```

O destino físico é aberto explicitamente pelo programa. Portanto, o áudio não volta ao cabo e não forma um ciclo. O programa troca a saída padrão para `CABLE Input` ao ativar e restaura a anterior ao desativar, fechar, usar Emergência ou sofrer uma exceção tratável. Também grava o ID anterior em `%LOCALAPPDATA%\AudioMaxing\previous-output.txt`; se a execução terminar abruptamente, a próxima inicialização tenta restaurá-lo.

## Dependências e licenças

- [.NET 7](https://dotnet.microsoft.com/download/dotnet/7.0): Desktop Runtime x64 para executar ou SDK para compilar.
- [NAudio.Wasapi 2.2.1](https://www.nuget.org/packages/NAudio.Wasapi/2.2.1) e sua dependência NAudio.Core: bibliotecas WASAPI, licença MIT; restauradas pelo NuGet durante a compilação.
- [VB-CABLE](https://vb-audio.com/Cable/): driver de cabo virtual da VB-Audio. É donationware; uso e redistribuição devem obedecer aos termos do fornecedor. Não é incluído nem instalado por este repositório.

## Instalar o VB-CABLE

1. Baixe `VBCABLE_Driver_Pack*.zip` no site oficial da VB-Audio.
2. Extraia o ZIP.
3. Clique com o botão direito em `VBCABLE_Setup_x64.exe` e escolha **Executar como administrador**.
4. Clique em **Install Driver** e reinicie o Windows.
5. Não altere manualmente a saída padrão; o AudioMaxing faz isso somente quando for ativado.

## Compilar e executar

Pré-requisitos: Windows 10/11 x64, PowerShell e SDK .NET 7.

```powershell
git clone https://github.com/Ambrosios13/AudioMaxing.git
Set-Location AudioMaxing
.\build.ps1 Build
.\src\AudioMaxing\bin\Release\net7.0-windows\AudioMaxing.exe
```

A restauração baixa as dependências pelo NuGet oficial. Para gerar a pasta de distribuição:

```powershell
.\build.ps1 Publish
```

Saída: `dist\AudioMaxing\AudioMaxing.exe`. Preserve a pasta inteira, pois as DLLs acompanham o executável.

## Uso seguro

1. Abra o programa; ele sempre inicia desligado e em 0 dB.
2. Selecione o fone físico. O VB-CABLE é ocultado da lista para evitar realimentação.
3. Clique **ATIVAR**. Somente então a saída padrão muda para o cabo.
4. Aumente o ganho aos poucos. Acima de +12 dB é exigida confirmação em cada execução.
5. Se houver ruído, volume inesperado ou falha, clique **EMERGÊNCIA: DESATIVAR**.

O limitador tem teto de −1 dBFS, ataque imediato e soltura de 120 ms, preservando os canais juntos. Ele limita picos das amostras digitais; não é um true-peak limiter com oversampling, não detecta picos entre amostras, não repara clipping da fonte e pode produzir distorção audível com redução intensa. Ele não é proteção auditiva certificada.

## Roteiro de testes

Use inicialmente um conteúdo conhecido em volume baixo e mantenha o fone fora dos ouvidos durante o primeiro teste.

1. **Ganho real:** deixe Windows/YouTube em 100%, ative em 0 dB, anote o nível; passe para +3 dB e +6 dB. O medidor e o nível percebido devem subir, sem mudança no slider mestre.
2. **Ausência de eco:** reproduza fala ou cliques secos. Deve existir uma só ocorrência. Confirme em Configurações de Som que `CABLE Input` é a saída padrão enquanto ativo e que não há “Escutar este dispositivo” habilitado para `CABLE Output`.
3. **Limitador/distorção:** use um tom ou música forte, eleve o ganho e confirme `LIMITADOR ATIVO`. A saída não deve ultrapassar −1 dBFS no medidor. Compare +3/+6/+12 dB; redução intensa pode soar comprimida ou distorcida.
4. **Latência:** reproduza vídeo com fala e observe sincronismo labial; depois execute um teste de clique audiovisual. A implementação usa buffers de captura de 20 ms, reprodução de 30 ms e buffer máximo de 250 ms. A latência real depende do driver.
5. **Troca de dispositivo:** enquanto ativo, selecione outra saída; o programa deve parar e restaurar a saída anterior. Reative e confirme o novo fone.
6. **Emergência:** ative, aplique ganho e pressione Emergência. O áudio processado deve parar, o ganho voltar a 0 dB e o endpoint anterior ser restaurado.
7. **Recuperação:** com o programa ativo, encerre-o normalmente e confirme a restauração. Para simular falha abrupta, termine o processo e reabra o programa; ele deve usar o arquivo de recuperação.
8. **Aplicativos diversos:** teste navegador, player local e chamada. Aplicativos em WASAPI exclusivo podem ignorar o dispositivo padrão/cabo e não são suportados.

## Desinstalar e desfazer alterações

1. No AudioMaxing, clique **EMERGÊNCIA: DESATIVAR** e feche-o.
2. Em **Configurações → Sistema → Som**, escolha manualmente seu fone como saída, caso o cabo ainda esteja selecionado.
3. Apague a pasta do AudioMaxing.
4. Para remover o VB-CABLE, execute `VBCABLE_Setup_x64.exe` como administrador, clique **Remove Driver** e reinicie. Consulte os termos/instruções da versão baixada.
5. Opcionalmente apague `%LOCALAPPDATA%\AudioMaxing`; ali existe apenas o arquivo temporário de recuperação.

## Limitações

Fontes em modo exclusivo, DRM ou apps presos a outro endpoint podem não atravessar o cabo. Nenhum software pode exceder a potência física do DAC/amplificador/fone; ganho digital excessivo apenas aciona o limitador ou aumenta distorção/ruído.

## Contribuições

Relate problemas nas Issues com a versão do Windows, dispositivo de áudio e passos para reproduzir. Sugestões e pull requests são bem-vindos.

## Licença

Código do AudioMaxing sob [licença MIT](LICENSE). NAudio e VB-CABLE têm licenças próprias, indicadas acima.
