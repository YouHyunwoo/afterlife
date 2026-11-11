# 어셈블리 경계를 각각 컴파일한다. Unity 실행/테스트/패키지 복원은 하지 않는다.
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$moduleSpecs = @(
    @{ Name='Afterlife.Flow'; Pattern='Assets\Afterlife\Modules\Flow\*.cs'; Dependencies=@(); Pure=$true },
    @{ Name='Afterlife.Upgrades'; Pattern='Assets\Afterlife\Modules\Upgrades\*.cs'; Dependencies=@(); Pure=$true },
    @{ Name='Afterlife.Skills'; Pattern='Assets\Afterlife\Modules\Skills\*.cs'; Dependencies=@(); Pure=$true },
    @{ Name='Afterlife.Progression.Unity'; Pattern='Assets\Afterlife\Modules\Progression.Unity\*.cs'; Dependencies=@('Afterlife.Upgrades','Afterlife.Skills') },
    @{ Name='Afterlife.Prototype'; Pattern='Assets\_AfterlifePrototype\Runtime\*.cs'; Dependencies=@('Afterlife.Upgrades','Afterlife.Skills','Afterlife.Progression.Unity','Afterlife.Flow') },
    @{ Name='Afterlife.Prototype.Editor'; Pattern='Assets\_AfterlifePrototype\Editor\*.cs'; Dependencies=@('Afterlife.Upgrades','Afterlife.Skills','Afterlife.Progression.Unity','Afterlife.Prototype') }
)
Push-Location $projectRoot
try {
    foreach ($spec in $moduleSpecs) {
        [xml]$moduleXml = Get-Content -LiteralPath 'Assembly-CSharp.csproj' -Raw
        foreach ($node in @($moduleXml.SelectNodes('//Compile | //ProjectReference | //Analyzer'))) { $node.ParentNode.RemoveChild($node) | Out-Null }
        foreach ($reference in @($moduleXml.SelectNodes('//Reference'))) {
            if ($reference.Include -like 'Afterlife.*' -or ($spec.Pure -and $reference.Include -notmatch '^(System|mscorlib|netstandard|Microsoft.CSharp)')) {
                $reference.ParentNode.RemoveChild($reference) | Out-Null
            }
        }
        foreach ($node in $moduleXml.SelectNodes('//AssemblyName')) { $node.InnerText = $spec.Name }
        foreach ($node in $moduleXml.SelectNodes('//BaseIntermediateOutputPath | //IntermediateOutputPath')) { $node.InnerText = "Temp\obj\Modules\$($spec.Name)\" }
        foreach ($node in $moduleXml.SelectNodes('//OutputPath')) { $node.InnerText = 'Temp\bin\Modules\' }
        $properties = $moduleXml.CreateElement('PropertyGroup')
        foreach ($pair in @(@('SkipResolvePackageAssets','true'), @('GenerateDependencyFile','false'))) {
            $node = $moduleXml.CreateElement($pair[0]); $node.InnerText = $pair[1]; $properties.AppendChild($node) | Out-Null
        }
        $moduleXml.Project.AppendChild($properties) | Out-Null
        $items = $moduleXml.CreateElement('ItemGroup')
        $source = $moduleXml.CreateElement('Compile'); $source.SetAttribute('Include',$spec.Pattern); $items.AppendChild($source) | Out-Null
        $references = @($spec.Dependencies | ForEach-Object { @{ Name=$_; Path="Temp\bin\Modules\$_.dll" } })
        if (-not $spec.Pure) {
            $references += @('Unity.TextMeshPro','Moonstone.Core','Moonstone.Arc','Unity.InputSystem','Unity.RenderPipelines.Core.Runtime','Unity.RenderPipelines.Universal.Runtime') | ForEach-Object { @{ Name=$_; Path="Library\ScriptAssemblies\$_.dll" } }
        }
        foreach ($assembly in $references) {
            if ($moduleXml.SelectNodes('//Reference') | Where-Object { $_.Include -eq $assembly.Name }) { continue }
            $reference = $moduleXml.CreateElement('Reference'); $reference.SetAttribute('Include',$assembly.Name)
            $hint = $moduleXml.CreateElement('HintPath'); $hint.InnerText = $assembly.Path; $reference.AppendChild($hint) | Out-Null; $items.AppendChild($reference) | Out-Null
        }
        $moduleXml.Project.AppendChild($items) | Out-Null
        $project = Join-Path $projectRoot "$($spec.Name).ModuleValidation.csproj"
        $moduleXml.Save($project)
        dotnet build $project --no-restore --nologo -v minimal
        if ($LASTEXITCODE -ne 0) { throw "$($spec.Name) 모듈 컴파일 실패" }
    }
} finally { Pop-Location }
