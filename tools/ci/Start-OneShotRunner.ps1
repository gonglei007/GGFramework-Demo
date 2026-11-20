#Requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidatePattern('^gonglei007/GGFramework-(Demo|Core|GGResource|GGNetwork|GGUI|GGUser)$')][string]$Repository,
    [Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{40}$')][string]$Commit,
    [Parameter(Mandatory)][string]$RunnerRoot,
    [string]$UnityEditor='C:/Program Files/Unity/Hub/Editor/6000.3.13f1/Editor/Unity.exe',
    [string]$PythonExecutable=$env:GGFRAMEWORK_PYTHON,
    [string]$SeedRepository,
    [long]$RunId=0,
    [ValidateRange(5,120)][int]$JobTimeoutMinutes=75,
    [ValidateRange(60,900)][int]$QueueTimeoutSeconds=600
)
$ErrorActionPreference='Stop'
function Api([string]$Endpoint,[string]$Method='GET') {
    $json=& gh api --method $Method $Endpoint
    if ($LASTEXITCODE -ne 0) {throw "GitHub API failed: $Endpoint"}
    return ($json|ConvertFrom-Json)
}
if (!(Test-Path -LiteralPath $UnityEditor)) {throw 'Install and activate Unity 6000.3.13f1 for the current Windows user first'}
if (!$PythonExecutable) {
    $python=Get-Command python -ErrorAction SilentlyContinue
    if ($python) {$PythonExecutable=$python.Source}
    else {$PythonExecutable=& py -3 -c 'import sys; print(sys.executable)'}
}
if (!(Test-Path -LiteralPath $PythonExecutable)) {throw 'Supply a Python 3 executable'}
& $PythonExecutable -c 'import sys; assert sys.version_info >= (3, 10)'
if ($LASTEXITCODE -ne 0) {throw 'Python 3.10+ is required'}
$login=(& gh api user --jq .login).Trim()
if ($login -ne $Repository.Split('/')[0]) {throw 'Only the repository owner may launch this workstation runner'}
$deadline=[DateTimeOffset]::UtcNow.AddSeconds($QueueTimeoutSeconds)
$target=$null
$job=$null
do {
    if ($RunId) {$candidates=@(Api "repos/$Repository/actions/runs/$RunId")}
    else {$candidates=@((Api "repos/$Repository/actions/runs?head_sha=$Commit&per_page=30").workflow_runs|Where-Object {$_.path -eq '.github/workflows/unity-regression.yml' -and $_.status -ne 'completed'})}
    foreach ($run in $candidates) {
        if ($run.head_sha -ne $Commit -or $run.actor.login -ne $login -or $run.triggering_actor.login -ne $login -or $run.event -notin @('push','workflow_dispatch') -or $run.path -ne '.github/workflows/unity-regression.yml') {throw 'Run is not the exact owner-triggered trusted commit'}
        $jobs=(Api "repos/$Repository/actions/runs/$($run.id)/jobs").jobs
        $candidate=@($jobs|Where-Object {$_.name -eq 'Unity Regression' -and $_.status -eq 'queued'})
        if ($candidate.Count -eq 1) {$target=$run;$job=$candidate[0];break}
    }
    if (!$target) {Start-Sleep -Seconds 5}
} while (!$target -and [DateTimeOffset]::UtcNow -lt $deadline)
if (!$target) {throw 'No queued Unity job for this exact commit; inspect workflow/contract checks first'}
$label="ggframework-unity-$($target.id)-$($target.run_attempt)"
if ($label -notin $job.labels) {throw 'Run-specific runner label does not match the workflow'}
$root=[IO.Path]::GetFullPath($RunnerRoot)
New-Item -ItemType Directory -Path $root -Force|Out-Null
$release=Api 'repos/actions/runner/releases/latest'
$asset=@($release.assets|Where-Object {$_.name -match '^actions-runner-win-x64-[0-9.]+\.zip$'})
if ($asset.Count -ne 1 -or $asset[0].digest -notmatch '^sha256:([0-9a-f]{64})$') {throw 'Official runner asset SHA-256 unavailable'}
$expectedDigest=$Matches[1]
if ($asset[0].browser_download_url -notlike 'https://github.com/actions/runner/releases/download/*') {throw 'Unexpected runner download host'}
$archive=Join-Path $root $asset[0].name
if (!(Test-Path -LiteralPath $archive)) {Invoke-WebRequest -Uri $asset[0].browser_download_url -OutFile $archive}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedDigest) {throw 'Runner download checksum mismatch'}
$instance=Join-Path $root ("runner-"+$target.id+'-'+$target.run_attempt+'-'+[guid]::NewGuid().ToString('N'))
Expand-Archive -LiteralPath $archive -DestinationPath $instance
$work=Join-Path $instance 'work'
if ($SeedRepository) {
    $seed=(Resolve-Path -LiteralPath $SeedRepository).Path
    $origin=(& git -C $seed remote get-url origin).Trim().TrimEnd('/')
    if ($origin -notin @("https://github.com/$Repository","https://github.com/$Repository.git")) {throw 'Seed Git repository has a different origin'}
    & git -C $seed cat-file -e ($Commit+'^{commit}')
    if ($LASTEXITCODE -ne 0) {throw 'Exact CI commit is absent from the seed'}
    $repoName=$Repository.Split('/')[1]
    $workspace=Join-Path $work "$repoName/$repoName"
    New-Item -ItemType Directory -Path (Split-Path -Parent $workspace) -Force|Out-Null
    & git clone --local --no-hardlinks --no-checkout -- $seed $workspace
    if ($LASTEXITCODE -ne 0) {throw 'Local committed-object seed failed'}
    & git -C $workspace remote set-url origin "https://github.com/$Repository"
    if ($LASTEXITCODE -ne 0) {throw 'Cannot restore the canonical GitHub origin'}
}
$name="ggf-once-$($target.id)-$($target.run_attempt)-$([guid]::NewGuid().ToString('N').Substring(0,8))"
$registrationAttempted=$false
$oldUnity=$env:GGFRAMEWORK_UNITY_EDITOR
$oldPython=$env:GGFRAMEWORK_PYTHON
$oldTemp=$env:TEMP
$oldTmp=$env:TMP
$oldUpm=$env:UPM_CACHE_ROOT
Push-Location $instance
try {
    $token=(Api "repos/$Repository/actions/runners/registration-token" 'POST').token
    # Tokens stay in memory/configuration command arguments; never print them or export licenses.
    $registrationAttempted=$true
    & ./config.cmd --url "https://github.com/$Repository" --token $token --name $name --labels $label --work $work --ephemeral --unattended --disableupdate
    if ($LASTEXITCODE -ne 0) {throw 'Ephemeral runner registration failed'}
    $token=$null
    $env:GGFRAMEWORK_UNITY_EDITOR=$UnityEditor
    $env:GGFRAMEWORK_PYTHON=$PythonExecutable
    $hostTemp=Join-Path $instance 'host-temp'
    $upmCache=Join-Path $root 'upm-cache'
    New-Item -ItemType Directory -Path $hostTemp,$upmCache -Force|Out-Null
    $env:TEMP=$hostTemp
    $env:TMP=$hostTemp
    $env:UPM_CACHE_ROOT=$upmCache
    Write-Output "Running one trusted CI job: https://github.com/$Repository/actions/runs/$($target.id)"
    $listener=Start-Process -FilePath $env:ComSpec -ArgumentList @('/d','/c','run.cmd --once') -WorkingDirectory $instance -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $instance 'runner.log') -RedirectStandardError (Join-Path $instance 'runner.stderr.log')
    if (!$listener.WaitForExit($JobTimeoutMinutes*60000)) {$listener.Kill($true);$listener.WaitForExit();throw 'One-shot runner timeout'}
    if ($listener.ExitCode -ne 0) {throw 'Runner process failed; inspect the workflow'}
} finally {
    $env:GGFRAMEWORK_UNITY_EDITOR=$oldUnity
    $env:GGFRAMEWORK_PYTHON=$oldPython
    $env:TEMP=$oldTemp
    $env:TMP=$oldTmp
    $env:UPM_CACHE_ROOT=$oldUpm
    if ($registrationAttempted) {
        $remaining=@((Api "repos/$Repository/actions/runners?per_page=100").runners|Where-Object {$_.name -eq $name})
        foreach ($runner in $remaining) {Api "repos/$Repository/actions/runners/$($runner.id)" 'DELETE'|Out-Null}
        Write-Output 'One-shot runner registration removed; no service was installed.'
    }
    Pop-Location
}
$finishDeadline=[DateTimeOffset]::UtcNow.AddSeconds(120)
do {
    $final=Api "repos/$Repository/actions/runs/$($target.id)"
    if ($final.status -ne 'completed') {Start-Sleep -Seconds 5}
} while ($final.status -ne 'completed' -and [DateTimeOffset]::UtcNow -lt $finishDeadline)
if ($final.status -ne 'completed' -or $final.conclusion -ne 'success') {throw "Unity CI not successful: $($final.status) / $($final.conclusion)"}
Write-Output "CI job finished for commit $Commit; verify all checks before merging."
