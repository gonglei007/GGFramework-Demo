#Requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$RepositoryRoot,
    [Parameter(Mandatory)][string]$OutputRoot,
    [string]$UnityEditor=$env:GGFRAMEWORK_UNITY_EDITOR,
    [string]$PythonExecutable=$env:GGFRAMEWORK_PYTHON,
    [ValidateRange(60,3600)][int]$ProcessTimeoutSeconds=1800
)
$ErrorActionPreference='Stop'
$repo=(Resolve-Path -LiteralPath $RepositoryRoot).Path
if (!$UnityEditor) {$UnityEditor='C:/Program Files/Unity/Hub/Editor/6000.3.13f1/Editor/Unity.exe'}
if (!(Test-Path -LiteralPath $UnityEditor)) {throw 'Unity 6000.3.13f1 editor is required'}
$UnityEditor=(Resolve-Path -LiteralPath $UnityEditor).Path
$editorVersion=(Get-Item -LiteralPath $UnityEditor).VersionInfo.ProductVersion
if (!$editorVersion.StartsWith('6000.3.13f1')) {throw 'Editor executable version is not Unity 6000.3.13f1'}
if (!$PythonExecutable) {
    $pythonCommand=Get-Command python -ErrorAction SilentlyContinue
    if ($pythonCommand) {$PythonExecutable=$pythonCommand.Source}
    else {$PythonExecutable=(& py -3 -c 'import sys; print(sys.executable)')}
}
if (!(Test-Path -LiteralPath $PythonExecutable)) {throw 'Set GGFRAMEWORK_PYTHON to a Python 3 executable'}
$configPath=Join-Path $repo 'ci/unity-regression.json'
$config=Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
if ($env:GITHUB_REPOSITORY -and $env:GITHUB_REPOSITORY -ne $config.repository) {throw 'CI contract repository differs from the workflow repository'}
& $PythonExecutable (Join-Path $PSScriptRoot 'verify_results.py') --contract $configPath
if ($LASTEXITCODE -ne 0) {throw 'Invalid regression contract'}
$output=[IO.Path]::GetFullPath($OutputRoot)
if ($config.packageMode -and ($output -eq $repo -or $output.StartsWith($repo.TrimEnd('\','/')+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase))) {throw 'Core test host output must be outside the package root'}
if (Test-Path -LiteralPath $output) {throw 'Use a fresh output directory; old success reports must not be reused'}
$private=Join-Path $output 'private'
$public=Join-Path $output 'public'
New-Item -ItemType Directory -Path $private,$public -Force | Out-Null
if ($config.packageMode) {
    $project=Join-Path $private 'package-host'
    New-Item -ItemType Directory -Path "$project/Assets","$project/Packages","$project/ProjectSettings" -Force | Out-Null
    $package=Get-Content -LiteralPath (Join-Path $repo 'package.json') -Raw|ConvertFrom-Json
    $manifest=@{dependencies=@{($package.name)="file:$($repo.Replace('\','/'))";'com.unity.test-framework'='1.6.0';'com.unity.modules.unitywebrequest'='1.0.0';'com.unity.modules.unitywebrequestwww'='1.0.0';'com.unity.modules.unitywebrequesttexture'='1.0.0'};testables=@($package.name)}
    $manifest|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "$project/Packages/manifest.json" -Encoding utf8NoBOM
    "m_EditorVersion: $($config.unityVersion)"|Set-Content -LiteralPath "$project/ProjectSettings/ProjectVersion.txt" -Encoding utf8NoBOM
} else {$project=(Resolve-Path -LiteralPath (Join-Path $repo $config.projectPath)).Path}
$version=Get-Content -LiteralPath "$project/ProjectSettings/ProjectVersion.txt" -Raw
if ($version -notmatch ('(?m)^m_EditorVersion: '+[regex]::Escape($config.unityVersion)+'\s*$')) {throw 'Project Unity version differs from the contract'}
$commit=(& git -C $repo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {throw 'Repository revision unavailable'}
if ($env:GITHUB_SHA -and $env:GITHUB_SHA -ne $commit) {throw 'Checkout differs from the exact CI commit'}
$record=[ordered]@{schemaVersion=1;repository=$config.repository;commit=$commit;unityVersion=$config.unityVersion;environment=@{os=[Runtime.InteropServices.RuntimeInformation]::OSDescription;architecture=[Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString();editorProductVersion=$editorVersion;batchmode=$true};stages=@()}
$failure=$null
function Complete-EditorChildren([int]$EditorPid,[DateTimeOffset]$Started,[DateTimeOffset]$Finished) {
    # A root process can exit before ILPP/UPM children. Reusing the project immediately
    # can make Unity's Mono KillLingeringILPPRunner race an already-exiting child.
    $editorDirectory=(Get-Item -LiteralPath $UnityEditor).Directory.FullName+[IO.Path]::DirectorySeparatorChar
    $children=@(Get-CimInstance Win32_Process -Filter "ParentProcessId=$EditorPid" | Where-Object {
        $_.Name -in @('Unity.ILPP.Runner.exe','Unity.ILPP.Trigger.exe','UnityAutoQuitter.exe','UnityPackageManager.exe','bee_backend.exe') -and
        $_.ExecutablePath -and $_.ExecutablePath.StartsWith($editorDirectory,[StringComparison]::OrdinalIgnoreCase) -and
        $_.CreationDate.ToUniversalTime() -ge $Started.UtcDateTime.AddSeconds(-1) -and $_.CreationDate.ToUniversalTime() -le $Finished.UtcDateTime.AddSeconds(1)
    })
    foreach ($child in $children) {
        $owned=Get-Process -Id $child.ProcessId -ErrorAction SilentlyContinue
        if ($owned -and !$owned.WaitForExit(10000)) {
            $owned.Kill($true)
            if (!$owned.WaitForExit(10000)) {throw 'Owned Unity helper did not finish teardown'}
        }
    }
}
Push-Location $repo
try {
    foreach ($stage in $config.stages) {
        $name=$stage.name
        $stageReports=@($stage.reports | Where-Object {$null -ne $_})
        $log=Join-Path $private "$name.log"
        $arguments=@('-batchmode','-job-worker-count','2','-logFile',$log)
        $exe=$UnityEditor
        if ($stage.kind -ne 'player') {$arguments+=@('-projectPath',$project)}
        if (!$stage.graphics) {$arguments+='-nographics'}
        if ($stage.kind -eq 'nunit') {$arguments+=@('-runTests','-testPlatform',$stage.platform,'-testResults',(Join-Path $private "$name.xml"))}
        elseif ($stage.kind -eq 'method') {
            $arguments+=@('-executeMethod',$stage.method)
            if ($stage.quit) {$arguments+='-quit'}
        } else {
            $exe=Join-Path $project 'output/showcase/GGFrameworkDemo.exe'
            if (!(Test-Path -LiteralPath (Join-Path $project 'output/showcase/MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll'))) {throw 'Expected Windows x64 Mono Player was not built'}
            $playerOutput=Join-Path $private "$name-player"
            New-Item -ItemType Directory -Path $playerOutput -Force|Out-Null
            $arguments+=@("--showcase-$($stage.mode)",'--showcase-output',$playerOutput)
        }
        # Preserve old project reports, then require newly written files for this stage.
        foreach ($report in $stageReports) {
            if ($stage.kind -eq 'player') {continue}
            $old=Join-Path $project $report.source
            if (Test-Path -LiteralPath $old) {
                $archive=Join-Path $private ("previous-"+$name+"-"+[guid]::NewGuid().ToString('N')+'.txt')
                Move-Item -LiteralPath $old -Destination $archive
            }
        }
        foreach ($file in $stage.producedFiles) {
            $old=Join-Path $project $file
            if (Test-Path -LiteralPath $old) {
                Move-Item -LiteralPath $old -Destination (Join-Path $private ("previous-output-"+$name+'-'+[guid]::NewGuid().ToString('N')))
            }
        }
        $started=[DateTimeOffset]::UtcNow
        # ArgumentList is quoted because Windows Start-Process otherwise splits paths with spaces.
        $quoted=@($arguments|ForEach-Object {'"'+$_.Replace('"','\"')+'"'})
        $process=Start-Process -FilePath $exe -WindowStyle Hidden -PassThru -ArgumentList $quoted
        $timedOut=!$process.WaitForExit($ProcessTimeoutSeconds*1000)
        if ($timedOut) {$process.Kill($true);$process.WaitForExit()}
        $finished=[DateTimeOffset]::UtcNow
        if ($stage.kind -ne 'player') {Complete-EditorChildren $process.Id $started $finished}
        $entry=[ordered]@{name=$name;startedUtc=$started.ToString('o');finishedUtc=$finished.ToString('o');exitCode=$process.ExitCode;timedOut=$timedOut;producedFiles=@()}
        foreach ($file in $stage.producedFiles) {
            $path=Join-Path $project $file
            if (!(Test-Path -LiteralPath $path)) {throw "$name did not produce $file"}
            $entry.producedFiles+=@(@{file=$file;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()})
        }
        $record.stages+=@($entry)
        $record|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $private 'run.json') -Encoding utf8NoBOM
        for ($i=0;$i -lt $stageReports.Count;$i++) {
            $report=$stageReports[$i]
            $source=if($stage.kind -eq 'player'){Join-Path $playerOutput $report.source}else{Join-Path $project $report.source}
            if (!(Test-Path -LiteralPath $source)) {throw "$name did not produce $($report.source)"}
            if ((Get-Item -LiteralPath $source).LastWriteTimeUtc -lt $started.UtcDateTime.AddSeconds(-2)) {throw "$name produced a stale report"}
            Copy-Item -LiteralPath $source -Destination (Join-Path $private "$name-$i.txt")
        }
        if ($timedOut -or $process.ExitCode -ne 0) {throw "$name failed (exit=$($process.ExitCode), timeout=$timedOut)"}
    }
    & git -C $repo diff --exit-code -- '*.cs' '*.asmdef'
    if ($LASTEXITCODE -ne 0) {throw 'Unity modified tracked runtime/test source during the run'}
    & $PythonExecutable (Join-Path $PSScriptRoot 'verify_results.py') --contract $configPath --record (Join-Path $private 'run.json') --output (Join-Path $public 'results.json')
    if ($LASTEXITCODE -ne 0) {throw 'Evidence validation failed'}
} catch {
    $failure=$_.Exception.Message
    [ordered]@{result='Failed';commit=$commit;error=$failure;stages=$record.stages}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath (Join-Path $public 'failure.json') -Encoding utf8NoBOM
    $diagnostics=@(Get-ChildItem -LiteralPath $private -Filter '*.log'|ForEach-Object {
        Select-String -LiteralPath $_.FullName -Pattern 'error CS\d+|FAIL:|^.*Exception:|Scripts have compiler errors|Aborting batchmode'|ForEach-Object {
            if ($_.Line -notmatch '(?i)licens|token|password|credential|serial|bearer') {$_.Line.Replace($repo,'<PROJECT>').Replace($env:USERPROFILE,'<USER>')}
        }
    }|Select-Object -First 80)
    $diagnostics|Set-Content -LiteralPath (Join-Path $public 'diagnostics.txt') -Encoding utf8NoBOM
} finally {Pop-Location}
if ($env:GITHUB_STEP_SUMMARY) {
    @("## Unity regression: $($config.repository)","Commit: ``$commit``",("Result: "+$(if($failure){'FAILED'}else{'PASSED'})))|Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY
}
if ($failure) {throw $failure}
Write-Output "Regression passed; public evidence: $public"
