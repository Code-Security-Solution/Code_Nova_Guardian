using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;

namespace CustomHelpExample;

internal class CustomHelpProvider : HelpProvider
{
    public CustomHelpProvider(ICommandAppSettings settings)
        : base(settings)
    {
    }


    // Header 부분에 Usage + 실행 방법 Return 되도록 변경
    public override IEnumerable<IRenderable> GetHeader(ICommandModel model, ICommandInfo? command)
    {
        return new[]
        {
            new Text("사용법:\n"),
            new Text($"    {model.ApplicationName} [옵션] <명령어>"),
            Text.NewLine,
        };
    }

    // 기본 USAGE(사용법) 섹션을 완전히 제거
    public override IEnumerable<IRenderable> GetUsage(ICommandModel model, ICommandInfo? command)
    {
        return Enumerable.Empty<IRenderable>();
    }


    public override IEnumerable<IRenderable> GetOptions(ICommandModel model, ICommandInfo? command)
    {
        var options = base.GetOptions(model, command).ToList();
        options.Insert(0, new Text("옵션:"));
        return options;
    }

    public override IEnumerable<IRenderable> GetCommands(ICommandModel model, ICommandInfo? command)
    {
        var commands = base.GetCommands(model, command).ToList();
        commands.Insert(0, new Text("명령어:"));
        return commands;
    }
}