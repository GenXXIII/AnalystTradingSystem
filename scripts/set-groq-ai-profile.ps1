[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$envPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot ".env"))
if (-not $envPath.StartsWith($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Resolved .env path is outside the repository."
}
if (-not (Test-Path -LiteralPath $envPath -PathType Leaf)) {
    throw "Create .env from .env.example first."
}

$content = [System.IO.File]::ReadAllText($envPath)
$lineBreak = if ($content.Contains("`r`n")) { "`r`n" } else { "`n" }

function Get-EnvValue([string]$Name) {
    $matches = [regex]::Matches($script:content, "(?m)^$([regex]::Escape($Name))=(.*)$")
    if ($matches.Count -gt 0) { return $matches[$matches.Count - 1].Groups[1].Value.Trim() }
    return ""
}

function Set-EnvValue([string]$Name, [string]$Value) {
    $pattern = "(?m)^$([regex]::Escape($Name))=.*$"
    $replacement = "$Name=$Value"
    if ([regex]::IsMatch($script:content, $pattern)) {
        $script:content = [regex]::Replace(
            $script:content,
            $pattern,
            [System.Text.RegularExpressions.MatchEvaluator]{ param($match) $replacement })
    }
    else {
        if (-not $script:content.EndsWith($script:lineBreak)) { $script:content += $script:lineBreak }
        $script:content += $replacement + $script:lineBreak
    }
}

$groqKey = Get-EnvValue "GROQ_API_KEY"
if ([string]::IsNullOrWhiteSpace($groqKey) -or $groqKey -match "^(USER_PROVIDED_LATER|YOUR_|MODEL_)") {
    throw "Set GROQ_API_KEY in .env to your Groq Console key, then run this script again."
}

Set-EnvValue "TARGET_ANALYST_ENABLED" "true"
Set-EnvValue "TARGET_ANALYST_MAXIMUM_EVIDENCE_ITEMS_PER_WORKSPACE" "6"
Set-EnvValue "TARGET_ANALYST_MAXIMUM_COMPRESSED_CHARACTERS" "2000"
Set-EnvValue "TARGET_ANALYST_MAXIMUM_TOTAL_TOKENS" "10000"
Set-EnvValue "TARGET_ANALYST_CACHE_MINUTES" "5"
Set-EnvValue "TARGET_ANALYST_CONFIGURATION_VERSION" "phase13-groq-staged-v3"
Set-EnvValue "TARGET_AI_API_KEY" ""
Set-EnvValue "FULL_ANALYST_ENABLED" "true"
Set-EnvValue "FULL_ANALYST_MAXIMUM_EVIDENCE_ITEMS_PER_WORKSPACE" "6"
Set-EnvValue "FULL_ANALYST_MAXIMUM_COMPRESSED_CHARACTERS" "2000"
Set-EnvValue "FULL_ANALYST_MAXIMUM_TOTAL_TOKENS" "9000"
Set-EnvValue "FULL_ANALYST_CACHE_MINUTES" "5"
Set-EnvValue "FULL_ANALYST_CONFIGURATION_VERSION" "phase14-groq-staged-v3"
Set-EnvValue "FULL_AI_API_KEY" ""

$targetWorkspaces = @("STRUCTURE", "LIQUIDITY", "CANDLE", "FLOW", "KTR", "NEWS", "RISK", "MASTER")
foreach ($workspace in $targetWorkspaces) {
    $prefix = "TARGET_${workspace}_AI"
    $isMaster = $workspace -eq "MASTER"
    $maxOutputTokens = if ($isMaster) { "800" } elseif ($workspace -eq "RISK") { "550" } else { "450" }
    Set-EnvValue "${prefix}_ENABLED" "true"
    Set-EnvValue "${prefix}_PROVIDER" "Groq"
    Set-EnvValue "${prefix}_ADAPTER" "OpenAiCompatible"
    Set-EnvValue "${prefix}_REQUIRES_API_KEY" "true"
    Set-EnvValue "${prefix}_API_KEY" ""
    Set-EnvValue "${prefix}_MODEL" $(if ($isMaster) { "openai/gpt-oss-120b" } else { "openai/gpt-oss-20b" })
    Set-EnvValue "${prefix}_FALLBACK_MODELS" $(if ($isMaster) { "openai/gpt-oss-20b,qwen/qwen3.8-27b" } else { "qwen/qwen3.8-27b,openai/gpt-oss-120b" })
    Set-EnvValue "${prefix}_BASE_URL" "https://api.groq.com/openai/v1/"
    Set-EnvValue "${prefix}_MAX_OUTPUT_TOKENS" $maxOutputTokens
    Set-EnvValue "${prefix}_DISABLE_REASONING" "true"
    Set-EnvValue "${prefix}_MAX_RETRIES" "0"
    Set-EnvValue "${prefix}_REQUESTS_PER_MINUTE" "30"
    Set-EnvValue "${prefix}_CONFIGURATION_VERSION" "phase13-groq-staged-v3"
}

$futureWorkspaces = @("STRUCTURE", "LIQUIDITY", "CANDLE", "FLOW", "KTR", "NEWS", "RISK", "MASTER")
foreach ($workspace in $futureWorkspaces) {
    $prefix = "FULL_${workspace}_AI"
    $maxOutputTokens = if ($workspace -eq "MASTER") { "650" } elseif ($workspace -eq "RISK") { "500" } else { "400" }
    Set-EnvValue "${prefix}_ENABLED" "true"
    Set-EnvValue "${prefix}_PROVIDER" "Groq"
    Set-EnvValue "${prefix}_ADAPTER" "OpenAiCompatible"
    Set-EnvValue "${prefix}_REQUIRES_API_KEY" "true"
    Set-EnvValue "${prefix}_API_KEY" ""
    Set-EnvValue "${prefix}_MODEL" "openai/gpt-oss-20b"
    Set-EnvValue "${prefix}_FALLBACK_MODELS" "qwen/qwen3.8-27b,openai/gpt-oss-120b"
    Set-EnvValue "${prefix}_BASE_URL" "https://api.groq.com/openai/v1/"
    Set-EnvValue "${prefix}_MAX_OUTPUT_TOKENS" $maxOutputTokens
    Set-EnvValue "${prefix}_DISABLE_REASONING" "true"
    Set-EnvValue "${prefix}_MAX_RETRIES" "0"
    Set-EnvValue "${prefix}_REQUESTS_PER_MINUTE" "30"
    Set-EnvValue "${prefix}_CONFIGURATION_VERSION" "phase14-groq-staged-v3"
}

[System.IO.File]::WriteAllText($envPath, $content, [System.Text.UTF8Encoding]::new($false))
Write-Host "Groq profile applied to .env without printing the API key."
Write-Host "Target: GPT-OSS 20B scouts/risk, 120B Master only, 10K hard run cap."
Write-Host "Future: GPT-OSS 20B throughout, 9K run cap. SQL reuse and staged gates avoid unnecessary calls."
Write-Host "Fallbacks: Qwen 3.8 27B and the alternate GPT-OSS model."
