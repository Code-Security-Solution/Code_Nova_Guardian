// --check-requirement 명령어 구현

using Code_Nova_Guardian.Class;
using Spectre.Console.Cli;

namespace Code_Nova_Guardian.Spectre_CLI;

public class GetSemgrepTokenCommand : AsyncCommand
{
    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        await Program.check_requirement(); // 항상 하는 Docker 실행 여부 검사
        var docker_runner = new DockerRunner();
        await docker_runner.get_semgrep_token();

        return 0;
    }
}