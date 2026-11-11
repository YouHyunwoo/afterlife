# Compile only: no Unity Play Mode, Test Runner or batch process.
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$specs = @(
    @{Name='Afterlife.Flow'; Pattern='Assets\Afterlife\Modules\Flow\*.cs'; Dependencies=@(); Pure=$true},
    @{Name='Afterlife.Threshold.Core'; Pattern='Assets\Afterlife\Modules\Threshold\Core\*.cs'; Dependencies=@('Afterlife.Flow'); Pure=$true},
    @{Name='Afterlife.Threshold.Unity'; Pattern='Assets\Afterlife\Modules\Threshold\Runtime\*.cs'; Dependencies=@('Afterlife.Threshold.Core')},
    @{Name='Afterlife.Threshold.Editor'; Pattern='Assets\Afterlife\Modules\Threshold\Editor\*.cs'; Dependencies=@('Afterlife.Threshold.Core','Afterlife.Threshold.Unity')}
)
Push-Location $projectRoot
try {
    foreach ($spec in $specs) {
        [xml]$xml = Get-Content -LiteralPath 'Assembly-CSharp.csproj' -Raw
        foreach ($node in @($xml.SelectNodes('//Compile | //ProjectReference | //Analyzer'))) { $node.ParentNode.RemoveChild($node) | Out-Null }
        foreach ($node in @($xml.SelectNodes('//Reference'))) {
            if ($node.Include -like 'Afterlife.*' -or ($spec.Pure -and $node.Include -notmatch '^(System|mscorlib|netstandard|Microsoft.CSharp)')) { $node.ParentNode.RemoveChild($node) | Out-Null }
        }
        foreach ($node in $xml.SelectNodes('//AssemblyName')) { $node.InnerText=$spec.Name }
        foreach ($node in $xml.SelectNodes('//BaseIntermediateOutputPath | //IntermediateOutputPath')) { $node.InnerText="Temp\obj\Threshold\$($spec.Name)\" }
        foreach ($node in $xml.SelectNodes('//OutputPath')) { $node.InnerText='Temp\bin\Threshold\' }
        $properties=$xml.CreateElement('PropertyGroup')
        foreach ($pair in @(@('SkipResolvePackageAssets','true'),@('GenerateDependencyFile','false'))) {
            $node=$xml.CreateElement($pair[0]); $node.InnerText=$pair[1]; $properties.AppendChild($node) | Out-Null
        }
        $xml.Project.AppendChild($properties) | Out-Null
        $items=$xml.CreateElement('ItemGroup')
        $compile=$xml.CreateElement('Compile'); $compile.SetAttribute('Include',$spec.Pattern); $items.AppendChild($compile) | Out-Null
        $refs=@($spec.Dependencies | ForEach-Object { @{Name=$_;Path="Temp\bin\Threshold\$_.dll"} })
        if (-not $spec.Pure) {
            $refs+=@('UnityEngine.UI','Unity.TextMeshPro','Unity.InputSystem','Moonstone.Core','Moonstone.Arc') | ForEach-Object { @{Name=$_;Path="Library\ScriptAssemblies\$_.dll"} }
        }
        foreach ($ref in $refs) {
            if ($xml.SelectNodes('//Reference') | Where-Object { $_.Include -eq $ref.Name }) { continue }
            $node=$xml.CreateElement('Reference'); $node.SetAttribute('Include',$ref.Name)
            $hint=$xml.CreateElement('HintPath'); $hint.InnerText=$ref.Path; $node.AppendChild($hint) | Out-Null; $items.AppendChild($node) | Out-Null
        }
        $xml.Project.AppendChild($items) | Out-Null
        $project=Join-Path $projectRoot "$($spec.Name).ThresholdValidation.csproj"
        $xml.Save($project)
        dotnet build $project --no-restore --nologo -v minimal
        if ($LASTEXITCODE -ne 0) { throw "$($spec.Name) compile failed" }
    }
} finally { Pop-Location }
