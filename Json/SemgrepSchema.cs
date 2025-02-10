namespace Code_Nova_Guardian.Json;

/*
  ex) Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
  Semgrep json 출력의 json 형태(Schema) 를 정의하는 클래스
  놀랍게도 C#에는 자체적으로 json을 Class로 생성해주는 기능이 존재한다.
  이 기능을 이용하여 json을 Class로 변환하였다.

  VS2022 기준 json 이 복사된 상태에서 편집 - 선택하여 붙여넣기 - Json을 Class로 붙여넣기
  주의 : 이 기능은 jsonschema 를 Class로 바꾸는 기능이 아니라 json을 class로 바꾸는 기능이다.
  semgrep 의 출력은 jsonschema 파일로 제공되서 jsonschema 틀을 통해 짜가 json을 만들었고,
  이 json을 Class로 변환한 것이다.
  여기서 type 정의시엔 기본값 설정이 필요없는데, 이미 값이 입력된 semgrep output json 파일을 참고할 것이기 때문이다.
*/

public class SemgrepJsonRootObject
{
    public Result[] results { get; set; }
    public Error[] errors { get; set; }
    public Paths paths { get; set; }
    public string version { get; set; }
    public Time time { get; set; }
    public Explanation[] explanations { get; set; }
    public object[][] rules_by_engine { get; set; }
    public object engine_requested { get; set; }
    public string[] interfile_languages_used { get; set; }
    public Skipped_Rules[] skipped_rules { get; set; }
}

public class Version
{
    public string version { get; set; }
}

public class Paths
{
    public string[] scanned { get; set; }
    public Skipped[] skipped { get; set; }
}

public class Skipped
{
    public string path { get; set; }
    public object reason { get; set; }
    public string details { get; set; }
    public string rule_id { get; set; }
}

public class Time
{
    public string[] rules { get; set; }
    public float rules_parse_time { get; set; }
    public Profiling_Times profiling_times { get; set; }
    public Target[] targets { get; set; }
    public int total_bytes { get; set; }
    public int max_memory_bytes { get; set; }
}

public class Profiling_Times
{
}

public class Target
{
    public string path { get; set; }
    public int num_bytes { get; set; }
    public float?[] match_times { get; set; }
    public float?[] parse_times { get; set; }
    public float run_time { get; set; }
}

public class Result
{
    public string check_id { get; set; }
    public string path { get; set; }
    public Start start { get; set; }
    public End end { get; set; }
    public Extra extra { get; set; }
}

public class Start
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class End
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Extra
{
    public string message { get; set; }
    public object metadata { get; set; }
    public object severity { get; set; }
    public string fingerprint { get; set; }
    public string lines { get; set; }
    public Metavars metavars { get; set; }
    public string fix { get; set; }
    public string[] fixed_lines { get; set; }
    public bool is_ignored { get; set; }
    public Sca_Info sca_info { get; set; }
    public object validation_state { get; set; }
    public Historical_Info historical_info { get; set; }
    public Dataflow_Trace dataflow_trace { get; set; }
    public object engine_kind { get; set; }
    public object extra_extra { get; set; }
}

public class Metavars
{
}

public class Sca_Info
{
    public bool reachability_rule { get; set; }
    public int sca_finding_schema { get; set; }
    public Dependency_Match dependency_match { get; set; }
    public bool reachable { get; set; }
    public object kind { get; set; }
}

public class Dependency_Match
{
    public Dependency_Pattern dependency_pattern { get; set; }
    public Found_Dependency found_dependency { get; set; }
    public string lockfile { get; set; }
}

public class Dependency_Pattern
{
    public object ecosystem { get; set; }
    public string package { get; set; }
    public string semver_range { get; set; }
}

public class Found_Dependency
{
    public string package { get; set; }
    public string version { get; set; }
    public object ecosystem { get; set; }
    public Allowed_Hashes allowed_hashes { get; set; }
    public object transitivity { get; set; }
    public string resolved_url { get; set; }
    public string manifest_path { get; set; }
    public string lockfile_path { get; set; }
    public int line_number { get; set; }
    public object[] children { get; set; }
    public string git_ref { get; set; }
}

public class Allowed_Hashes
{
}

public class Historical_Info
{
    public string git_commit { get; set; }
    public string git_commit_timestamp { get; set; }
    public string git_blob { get; set; }
}

public class Dataflow_Trace
{
    public object taint_source { get; set; }
    public Intermediate_Vars[] intermediate_vars { get; set; }
    public object taint_sink { get; set; }
}

public class Intermediate_Vars
{
    public Location location { get; set; }
    public string content { get; set; }
}

public class Location
{
    public string path { get; set; }
    public Start1 start { get; set; }
    public End1 end { get; set; }
}

public class Start1
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class End1
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Error
{
    public int code { get; set; }
    public object level { get; set; }
    public object type { get; set; }
    public string rule_id { get; set; }
    public string message { get; set; }
    public string path { get; set; }
    public string long_msg { get; set; }
    public string short_msg { get; set; }
    public Span[] spans { get; set; }
    public string help { get; set; }
}

public class Span
{
    public string file { get; set; }
    public Start2 start { get; set; }
    public End2 end { get; set; }
    public string source_hash { get; set; }
    public Config_Start config_start { get; set; }
    public Config_End config_end { get; set; }
    public string[] config_path { get; set; }
    public Context_Start context_start { get; set; }
    public Context_End context_end { get; set; }
}

public class Start2
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class End2
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Config_Start
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Config_End
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Context_Start
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Context_End
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Explanation
{
    public object op { get; set; }
    public Child[] children { get; set; }
    public Match1[] matches { get; set; }
    public Loc loc { get; set; }
    public Extra1 extra { get; set; }
}

public class Loc
{
    public string path { get; set; }
    public Start3 start { get; set; }
    public End3 end { get; set; }
}

public class Start3
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class End3
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Extra1
{
    public object before_negation_matches { get; set; }
    public object before_filter_matches { get; set; }
}

public class Child
{
    public float? op { get; set; }
    public Child1[] children { get; set; }
    public Match[] matches { get; set; }
    public Loc1 loc { get; set; }
    public Extra2 extra { get; set; }
}

public class Loc1
{
    public string path { get; set; }
    public Start4 start { get; set; }
    public End4 end { get; set; }
}

public class Start4
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class End4
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Extra2
{
    public object before_negation_matches { get; set; }
    public object before_filter_matches { get; set; }
}

public class Child1
{
    public object op { get; set; }
    public object[] children { get; set; }
    public object[] matches { get; set; }
    public Loc2 loc { get; set; }
    public Extra3 extra { get; set; }
}

public class Loc2
{
    public string path { get; set; }
    public Start5 start { get; set; }
    public End5 end { get; set; }
}

public class Start5
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class End5
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Extra3
{
    public object before_negation_matches { get; set; }
    public object before_filter_matches { get; set; }
}

public class Match
{
    public string check_id { get; set; }
    public string path { get; set; }
    public Start6 start { get; set; }
    public End6 end { get; set; }
    public Extra4 extra { get; set; }
}

public class Start6
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class End6
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Extra4
{
    public Metavars1 metavars { get; set; }
    public object engine_kind { get; set; }
    public bool is_ignored { get; set; }
    public string message { get; set; }
    public object metadata { get; set; }
    public object severity { get; set; }
    public string fix { get; set; }
    public Dataflow_Trace1 dataflow_trace { get; set; }
    public Sca_Match sca_match { get; set; }
    public object validation_state { get; set; }
    public Historical_Info1 historical_info { get; set; }
    public bool extra_extra { get; set; }
}

public class Metavars1
{
}

public class Dataflow_Trace1
{
    public object taint_source { get; set; }
    public object[] intermediate_vars { get; set; }
    public Taint_Sink taint_sink { get; set; }
}

public class Taint_Sink
{
}

public class Sca_Match
{
    public bool reachability_rule { get; set; }
    public int sca_finding_schema { get; set; }
    public Dependency_Match1 dependency_match { get; set; }
    public bool reachable { get; set; }
    public object kind { get; set; }
}

public class Dependency_Match1
{
    public Dependency_Pattern1 dependency_pattern { get; set; }
    public Found_Dependency1 found_dependency { get; set; }
    public string lockfile { get; set; }
}

public class Dependency_Pattern1
{
    public object ecosystem { get; set; }
    public string package { get; set; }
    public string semver_range { get; set; }
}

public class Found_Dependency1
{
    public string package { get; set; }
    public string version { get; set; }
    public object ecosystem { get; set; }
    public Allowed_Hashes1 allowed_hashes { get; set; }
    public object transitivity { get; set; }
    public string resolved_url { get; set; }
    public string manifest_path { get; set; }
    public string lockfile_path { get; set; }
    public int line_number { get; set; }
    public object[] children { get; set; }
    public string git_ref { get; set; }
}

public class Allowed_Hashes1
{
}

public class Historical_Info1
{
    public string git_commit { get; set; }
    public string git_commit_timestamp { get; set; }
    public string git_blob { get; set; }
}

public class Match1
{
    public string check_id { get; set; }
    public string path { get; set; }
    public Start7 start { get; set; }
    public End7 end { get; set; }
    public Extra5 extra { get; set; }
}

public class Start7
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class End7
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}

public class Extra5
{
    public Metavars2 metavars { get; set; }
    public object engine_kind { get; set; }
    public bool is_ignored { get; set; }
    public string message { get; set; }
    public object metadata { get; set; }
    public object severity { get; set; }
    public string fix { get; set; }
    public Dataflow_Trace2 dataflow_trace { get; set; }
    public Sca_Match1 sca_match { get; set; }
    public object validation_state { get; set; }
    public Historical_Info2 historical_info { get; set; }
    public object extra_extra { get; set; }
}

public class Metavars2
{
}

public class Dataflow_Trace2
{
    public object taint_source { get; set; }
    public object[] intermediate_vars { get; set; }
    public object taint_sink { get; set; }
}

public class Sca_Match1
{
    public bool reachability_rule { get; set; }
    public int sca_finding_schema { get; set; }
    public Dependency_Match2 dependency_match { get; set; }
    public bool reachable { get; set; }
    public object kind { get; set; }
}

public class Dependency_Match2
{
    public Dependency_Pattern2 dependency_pattern { get; set; }
    public Found_Dependency2 found_dependency { get; set; }
    public string lockfile { get; set; }
}

public class Dependency_Pattern2
{
    public object ecosystem { get; set; }
    public string package { get; set; }
    public string semver_range { get; set; }
}

public class Found_Dependency2
{
    public string package { get; set; }
    public string version { get; set; }
    public object ecosystem { get; set; }
    public Allowed_Hashes2 allowed_hashes { get; set; }
    public object transitivity { get; set; }
    public string resolved_url { get; set; }
    public string manifest_path { get; set; }
    public string lockfile_path { get; set; }
    public int line_number { get; set; }
    public object[] children { get; set; }
    public string git_ref { get; set; }
}

public class Allowed_Hashes2
{
}

public class Historical_Info2
{
    public string git_commit { get; set; }
    public string git_commit_timestamp { get; set; }
    public string git_blob { get; set; }
}

public class Skipped_Rules
{
    public string rule_id { get; set; }
    public string details { get; set; }
    public Position position { get; set; }
}

public class Position
{
    public int line { get; set; }
    public int col { get; set; }
    public int offset { get; set; }
}