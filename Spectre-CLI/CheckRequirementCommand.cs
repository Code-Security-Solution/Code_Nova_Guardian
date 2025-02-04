// --check-requirement 명령어 구현
using Spectre.Console;
using Spectre.Console.Cli;

namespace Code_Nova_Guardian
{
    public class CheckRequirementCommand : AsyncCommand
    {
        public override async Task<int> ExecuteAsync(CommandContext context)
        {
            // Check requirements 실행 (Main 함수에 있어서 Program 클래스 이름을 명시적으로 사용)
            await Program.check_requirement();

            AnsiConsole.MarkupLine("[green]System requirements verified successfully![/]");
            return 0;
        }
    }
}